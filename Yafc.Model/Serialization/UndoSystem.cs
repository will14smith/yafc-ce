﻿using System;
using System.Collections.Generic;
using System.IO;
using Yafc.UI;

namespace Yafc.Model;

public class UndoSystem {
    public uint version { get; private set; } = 2;
    private bool undoBatchVisualOnly = true;
    private readonly List<UndoSnapshot> currentUndoBatch = [];
    private readonly List<ModelObject> changedList = [];
    private readonly Stack<UndoBatch> undo = new Stack<UndoBatch>();
    private readonly Stack<UndoBatch> redo = new Stack<UndoBatch>();
    private bool suspended;
    private bool scheduled;
    internal void CreateUndoSnapshot(ModelObject target, bool visualOnly) {
        if (SerializationMap.IsDeserializing) {
            throw new InvalidOperationException("Do not record an undo event while deserializing.");
        }

        if (changedList.Count == 0) {
            version++;

            if (!suspended && !scheduled) {
                Schedule();
            }
        }

        undoBatchVisualOnly &= visualOnly;

        if (target.objectVersion == version) {
            return;
        }

        changedList.Add(target);
        target.objectVersion = version;

        if (visualOnly && undo.Count > 0 && undo.Peek().Contains(target)) {
            return;
        }

        var builder = target.GetUndoBuilder();
        currentUndoBatch.Add(builder.MakeUndoSnapshot(target));
    }

    private static void MakeUndoBatch(object? state) {
        UndoSystem system = (UndoSystem)state!; // null-forgiving: Only called by the instance method Schedule, which passes its this.
        system.scheduled = false;
        bool visualOnly = system.undoBatchVisualOnly;

        for (int i = 0; i < system.changedList.Count; i++) {
            system.changedList[i].ThisChanged(visualOnly);
        }

        system.changedList.Clear();

        if (system.currentUndoBatch.Count == 0) {
            return;
        }

        UndoBatch batch = new UndoBatch(system.currentUndoBatch.ToArray(), visualOnly);
        system.undo.Push(batch);
        system.undoBatchVisualOnly = true;
        system.redo.Clear();
        system.currentUndoBatch.Clear();
    }

    private void Schedule() {
        InputSystem.Instance.DispatchOnGestureFinish(MakeUndoBatch, this);
        scheduled = true;
    }

    public void Suspend() => suspended = true;

    public void Resume() {
        suspended = false;

        if (!scheduled && changedList.Count > 0) {
            Schedule();
        }
    }

    public void PerformUndo() {
        if (undo.Count == 0 || changedList.Count > 0) {
            return;
        }

        redo.Push(undo.Pop().Restore(++version));
    }

    public void PerformRedo() {
        if (redo.Count == 0 || changedList.Count > 0) {
            return;
        }

        undo.Push(redo.Pop().Restore(++version));
    }

    public void RecordChange() => version++;

    public bool HasChangesPending(ModelObject obj) => changedList.Contains(obj);
}
internal readonly struct UndoSnapshot(ModelObject target, object?[]? managed, byte[]? unmanaged) {
    internal readonly ModelObject target = target;
    internal readonly object?[]? managedReferences = managed;
    internal readonly byte[]? unmanagedData = unmanaged;

    public UndoSnapshot Restore() {
        var builder = target.GetUndoBuilder();
        var redo = builder.MakeUndoSnapshot(target);
        builder.RevertToUndoSnapshot(target, this);
        return redo;
    }
}

internal readonly struct UndoBatch(UndoSnapshot[] snapshots, bool visualOnly) {
    public readonly UndoSnapshot[] snapshots = snapshots;
    public readonly bool visualOnly = visualOnly;

    public UndoBatch Restore(uint undoState) {
        for (int i = 0; i < snapshots.Length; i++) {
            snapshots[i] = snapshots[i].Restore();
            snapshots[i].target.objectVersion = undoState;
        }

        foreach (var snapshot in snapshots) {
            snapshot.target.AfterDeserialize();
        }

        foreach (var snapshot in snapshots) {
            snapshot.target.ThisChanged(visualOnly);
        }

        return this;
    }

    public bool Contains(ModelObject target) {
        foreach (var snapshot in snapshots) {
            if (snapshot.target == target) {
                return true;
            }
        }

        return false;
    }
}

internal class UndoSnapshotBuilder {
    private readonly MemoryStream stream = new MemoryStream();
    private readonly List<object?> managedRefs = [];
    public readonly BinaryWriter writer;
    private readonly ModelObject currentTarget;

    internal UndoSnapshotBuilder(ModelObject target) {
        writer = new BinaryWriter(stream);
        currentTarget = target;
    }

    internal UndoSnapshot Build() {
        byte[]? buffer = null;

        if (stream.Position > 0) {
            buffer = new byte[stream.Position];
            Array.Copy(stream.GetBuffer(), buffer, stream.Position);
        }

        UndoSnapshot result = new UndoSnapshot(currentTarget, managedRefs.Count > 0 ? managedRefs.ToArray() : null, buffer);
        stream.Position = 0;
        managedRefs.Clear();

        return result;
    }

    public void WriteManagedReference(object? reference) => managedRefs.Add(reference);

    public void WriteManagedReferences(IEnumerable<object> references) => managedRefs.AddRange(references);
}

internal class UndoSnapshotReader {
    private static readonly BinaryReader NullReader = new BinaryReader(Stream.Null);
    public BinaryReader reader { get; }
    private int refId;
    private readonly object?[]? managed;

    internal UndoSnapshotReader(UndoSnapshot snapshot) {
        if (snapshot.unmanagedData != null) {
            MemoryStream stream = new MemoryStream(snapshot.unmanagedData, false);
            reader = new BinaryReader(stream);
        }
        else {
            reader = NullReader;
        }

        managed = snapshot.managedReferences;
        refId = 0;
    }

    public object? ReadManagedReference() {
        if (managed == null) {
            throw new InvalidOperationException("No managed objects are available to read in this undo snapshot.");
        }

        return managed[refId++];
    }

    public T? ReadOwnedReference<T>(ModelObject owner) where T : ModelObject {
        T? obj = ReadManagedReference() as T;
        if (obj != null && obj.ownerObject != owner) {
            obj.ownerObject = owner;
        }

        return obj;
    }
}
