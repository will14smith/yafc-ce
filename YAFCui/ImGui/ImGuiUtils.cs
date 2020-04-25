using System.Numerics;
using SDL2;

namespace YAFC.UI
{
    public static class ImGuiUtils
    {
        public static readonly Padding DefaultButtonPadding = new Padding(1f, 0.5f);
        public static readonly Padding DefaultScreenPadding = new Padding(5f, 2f);

        public static bool BuildButton(this ImGui gui, Rect rect, SchemeColor normal, SchemeColor over, SchemeColor down = SchemeColor.None)
        {
            switch (gui.action)
            {
                case ImGuiAction.MouseMove:
                    gui.ConsumeMouseOver(rect, RenderingUtils.cursorHand);
                    return false;
                case ImGuiAction.MouseDown:
                    if (gui.actionParameter == SDL.SDL_BUTTON_LEFT)
                        gui.ConsumeMouseDown(rect);
                    return false;
                case ImGuiAction.MouseUp:
                    return gui.ConsumeMouseUp(rect);
                case ImGuiAction.Build:
                    var color = gui.IsMouseOver(rect) ? (down != SchemeColor.None && gui.IsMouseDown(rect, SDL.SDL_BUTTON_LEFT)) ? down : over : normal;
                    gui.DrawRectangle(rect, color);
                    return false;
                default:
                    return false;
            }
        }

        public static bool OnClick(this ImGui gui, Rect rect)
        {
            if (gui.action == ImGuiAction.MouseUp)
                return gui.ConsumeMouseUp(rect);
            if (gui.action == ImGuiAction.MouseDown && gui.actionParameter == SDL.SDL_BUTTON_LEFT)
                gui.ConsumeMouseDown(rect);
            return false;
        }
        
        public static bool BuildButton(this ImGui gui, string text, SchemeColor color = SchemeColor.Primary, Padding? padding = null, bool active = true)
        {
            if (!active)
                color = SchemeColor.Grey;
            using (gui.EnterGroup(padding ?? DefaultButtonPadding, color+2))
            {
                gui.BuildText(text, Font.text, align:RectAlignment.Middle);
            }

            return gui.BuildButton(gui.lastRect, color, color + 1) && active;
        }

        public static bool BuildCheckBox(this ImGui gui, string text, bool value, out bool newValue, SchemeColor color = SchemeColor.None)
        {
            using (gui.EnterRow())
            {
                gui.BuildIcon(value ? Icon.CheckBoxCheck : Icon.CheckBoxEmpty, 1.5f, color);
                gui.BuildText(text, Font.text, color:color);
            }

            if (gui.OnClick(gui.lastRect))
            {
                newValue = !value;
                return true;
            }

            newValue = value;
            return false;
        }
    }
}