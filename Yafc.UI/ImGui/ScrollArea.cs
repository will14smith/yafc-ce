﻿using System;
using System.Collections.Generic;
using System.Numerics;
using SDL2;

namespace Yafc.UI {
    public abstract class Scrollable(bool vertical, bool horizontal, bool collapsible) : IKeyboardFocus {
        private Vector2 contentSize;
        private Vector2 maxScroll;
        private Vector2 _scroll;
        private float height;
        private ImGui? gui;
        public const float ScrollbarSize = 1f;

        // Padding to add at the bottom of the scroll area to be able to scroll past the
        // last item; needs useBottomPadding to be set to true in method Build()
        private const float BottomPaddingInPixels = 100f;

        protected abstract void PositionContent(ImGui gui, Rect viewport);

        public void Build(ImGui gui, float height, bool useBottomPadding = false) {
            this.gui = gui;
            this.height = height;
            var rect = gui.statePosition;
            float width = rect.Width;
            if (vertical) {
                width -= ScrollbarSize;
            }

            if (gui.isBuilding) {
                var innerRect = rect;
                innerRect.Width = width;
                contentSize = MeasureContent(innerRect, gui);
                if (contentSize.Y > height && useBottomPadding) {
                    contentSize.Y += BottomPaddingInPixels / gui.pixelsPerUnit;
                }
                maxScroll = Vector2.Max(contentSize - new Vector2(innerRect.Width, height), Vector2.Zero);
                float realHeight = collapsible ? MathF.Min(contentSize.Y, height) : height;
                innerRect.Height = rect.Height = realHeight;
                if (horizontal && maxScroll.X > 0) {
                    realHeight -= ScrollbarSize;
                    innerRect.Height = realHeight;
                }
                _ = gui.EncapsulateRect(rect);
                scroll2d = Vector2.Clamp(scroll2d, Vector2.Zero, maxScroll);
                PositionContent(gui, innerRect);
            }
            else {
                float realHeight = collapsible ? MathF.Min(contentSize.Y, height) : height;
                if (horizontal && maxScroll.X > 0) {
                    realHeight -= ScrollbarSize;
                }

                rect.Height = realHeight;
                _ = gui.EncapsulateRect(rect);
            }
            Vector2 size = new Vector2(width, height);
            var scrollSize = size * size / (size + maxScroll);
            scrollSize = Vector2.Max(scrollSize, Vector2.One);
            var scrollStart = _scroll / maxScroll * (size - scrollSize);
            if ((gui.action == ImGuiAction.MouseDown || gui.action == ImGuiAction.MouseScroll) && rect.Contains(gui.mousePosition)) {
                InputSystem.Instance.SetKeyboardFocus(this);
            }

            if (gui.action == ImGuiAction.MouseScroll) {
                if (gui.ConsumeEvent(rect)) {
                    if (vertical && (!horizontal || !InputSystem.Instance.control)) {
                        scroll += gui.actionParameter * 3f;
                    }
                    else {
                        scrollX += gui.actionParameter * 3f;
                    }
                }
            }
            else {
                if (horizontal && maxScroll.X > 0f) {
                    Rect fullScrollRect = new Rect(rect.X, rect.Bottom - ScrollbarSize, rect.Width, ScrollbarSize);
                    Rect scrollRect = new Rect(rect.X + scrollStart.X, fullScrollRect.Y, scrollSize.X, ScrollbarSize);
                    BuildScrollBar(gui, 0, in fullScrollRect, in scrollRect);
                }

                if (vertical && maxScroll.Y > 0f) {
                    Rect fullScrollRect = new Rect(rect.Right - ScrollbarSize, rect.Y, ScrollbarSize, rect.Height);
                    Rect scrollRect = new Rect(fullScrollRect.X, rect.Y + scrollStart.Y, ScrollbarSize, scrollSize.Y);
                    BuildScrollBar(gui, 1, in fullScrollRect, in scrollRect);
                }
            }
        }

        private void BuildScrollBar(ImGui gui, int axis, in Rect fullScrollRect, in Rect scrollRect) {
            switch (gui.action) {
                case ImGuiAction.MouseDown:
                    if (scrollRect.Contains(gui.mousePosition)) {
                        _ = gui.ConsumeMouseDown(fullScrollRect);
                    }

                    break;
                case ImGuiAction.MouseMove:
                    if (gui.IsMouseDown(fullScrollRect, SDL.SDL_BUTTON_LEFT)) {
                        if (axis == 0) {
                            scrollX += InputSystem.Instance.mouseDelta.X * contentSize.X / fullScrollRect.Width;
                        }
                        else {
                            scroll += InputSystem.Instance.mouseDelta.Y * contentSize.Y / fullScrollRect.Height;
                        }
                    }
                    break;
                case ImGuiAction.Build:
                    gui.DrawRectangle(scrollRect, gui.IsMouseDown(fullScrollRect, SDL.SDL_BUTTON_LEFT) ? SchemeColor.GreyAlt : SchemeColor.Grey);
                    break;
            }
        }

        public virtual Vector2 scroll2d {
            get => _scroll;
            set {
                value = Vector2.Clamp(value, Vector2.Zero, maxScroll);
                if (value != _scroll) {
                    _scroll = value;
                    gui?.Rebuild();
                }
            }
        }

        public float scroll {
            get => _scroll.Y;
            set => scroll2d = new Vector2(_scroll.X, value);
        }

        public float scrollX {
            get => _scroll.X;
            set => scroll2d = new Vector2(value, _scroll.Y);
        }

        protected abstract Vector2 MeasureContent(Rect rect, ImGui gui);
        public bool KeyDown(SDL.SDL_Keysym key) {
            switch (key.scancode) {
                case SDL.SDL_Scancode.SDL_SCANCODE_UP:
                    scroll -= 3;
                    return true;
                case SDL.SDL_Scancode.SDL_SCANCODE_DOWN:
                    scroll += 3;
                    return true;
                case SDL.SDL_Scancode.SDL_SCANCODE_LEFT:
                    scrollX -= 3;
                    return true;
                case SDL.SDL_Scancode.SDL_SCANCODE_RIGHT:
                    scrollX += 3;
                    return true;
                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN:
                    scroll += height;
                    return true;
                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP:
                    scroll -= height;
                    return true;
                case SDL.SDL_Scancode.SDL_SCANCODE_HOME:
                    scroll = 0;
                    return true;
                case SDL.SDL_Scancode.SDL_SCANCODE_END:
                    scroll = maxScroll.Y;
                    return true;
                default:
                    return false;
            }
        }

        public bool TextInput(string input) => false;

        public bool KeyUp(SDL.SDL_Keysym key) => false;

        public void FocusChanged(bool focused) { }
    }

    public abstract class ScrollAreaBase : Scrollable {
        protected ImGui contents;
        protected readonly float height;

        public ScrollAreaBase(float height, Padding padding, bool collapsible = false, bool vertical = true, bool horizontal = false) : base(vertical, horizontal, collapsible) {
            contents = new ImGui(BuildContents, padding, clip: true);
            this.height = height;
        }

        protected override void PositionContent(ImGui gui, Rect viewport) {
            gui.DrawPanel(viewport, contents);
            contents.offset = -scroll2d;
        }

        public void Build(ImGui gui) => Build(gui, height);

        protected abstract void BuildContents(ImGui gui);

        public void RebuildContents() => contents.Rebuild();

        protected override Vector2 MeasureContent(Rect rect, ImGui gui) => contents.CalculateState(rect.Width, gui.pixelsPerUnit);
    }

    public class ScrollArea(float height, GuiBuilder builder, Padding padding = default, bool collapsible = false, bool vertical = true, bool horizontal = false) : ScrollAreaBase(height, padding, collapsible, vertical, horizontal) {
        protected override void BuildContents(ImGui gui) => builder(gui);

        public void Rebuild() => RebuildContents();
    }

    public class VirtualScrollList<TData> : ScrollAreaBase {
        private readonly Vector2 elementSize;
        // When rendering the scrollable content, render 'blocks' of 4 rows at a time. (As far as I can tell, any positive value works. Shadow picked 4, so I kept that.)
        private readonly int bufferRows = 4;
        // The first block of bufferRows that was rendered last time BuildContents was called. If it changes while scrolling, we need to re-render the scrollable content.
        private int firstVisibleBlock;
        private int elementsPerRow;
        private IReadOnlyList<TData> _data = [];
        private readonly int maxRowsVisible;
        private readonly Drawer drawer;
        private float _spacing;
        private readonly Action<int, int>? reorder;

        public float spacing {
            get => _spacing;
            set {
                _spacing = value;
                RebuildContents();
            }
        }

        public delegate void Drawer(ImGui gui, TData element, int index);

        public IReadOnlyList<TData> data {
            get => _data;
            set {
                _data = value ?? [];
                RebuildContents();
            }
        }

        public VirtualScrollList(float height, Vector2 elementSize, Drawer drawer, Padding padding = default, Action<int, int>? reorder = null, bool collapsible = false) : base(height, padding, collapsible) {
            this.elementSize = elementSize;
            maxRowsVisible = MathUtils.Ceil(height / this.elementSize.Y) + bufferRows + 1;
            this.drawer = drawer;
            this.reorder = reorder;
        }

        private int CalcFirstBlock() => Math.Max(0, MathUtils.Floor((scroll - contents.initialPadding.top) / (elementSize.Y * bufferRows)));

        public override Vector2 scroll2d {
            get => base.scroll2d;
            set {
                base.scroll2d = value;
                int row = CalcFirstBlock();
                if (row != firstVisibleBlock) {
                    RebuildContents();
                }
            }
        }

        protected override void BuildContents(ImGui gui) {
            elementsPerRow = MathUtils.Floor((gui.width + _spacing) / (elementSize.X + _spacing));
            if (elementsPerRow < 1) {
                elementsPerRow = 1;
            }

            int rowCount = ((_data.Count - 1) / elementsPerRow) + 1;
            firstVisibleBlock = CalcFirstBlock();
            // Scroll up until there are maxRowsVisible, or to the top.
            int firstRow = Math.Max(0, Math.Min(firstVisibleBlock * bufferRows, rowCount - maxRowsVisible));
            int index = firstRow * elementsPerRow;
            if (index >= _data.Count) {
                // If _data is empty, there's nothing to draw. Make sure MeasureContent reports that, instead of the size of the most recent non-empty content.
                // This will remove the scroll bar when the search doesn't match anything.
                gui.lastContentRect = new Rect(gui.lastContentRect.X, gui.lastContentRect.Y, 0, 0);
                return;
            }

            int lastRow = firstRow + maxRowsVisible;
            using var manualPlacing = gui.EnterFixedPositioning(gui.width, rowCount * elementSize.Y, default);
            var offset = gui.statePosition.Position;
            float elementWidth = gui.width / elementsPerRow;
            Rect cell = new Rect(offset.X, offset.Y, elementWidth - _spacing, elementSize.Y);
            for (int row = firstRow; row < lastRow; row++) {
                cell.Y = row * (elementSize.Y + _spacing);
                for (int elem = 0; elem < elementsPerRow; elem++) {
                    cell.X = elem * elementWidth;
                    manualPlacing.SetManualRectRaw(cell);
                    BuildElement(gui, _data[index], index);
                    if (reorder != null) {
                        if (gui.DoListReordering(cell, cell, index, out int fromIndex)) {
                            reorder(fromIndex, index);
                        }
                    }
                    if (++index >= _data.Count) {
                        return;
                    }
                }
            }
        }

        protected virtual void BuildElement(ImGui gui, TData element, int index) => drawer(gui, element, index);
    }
}
