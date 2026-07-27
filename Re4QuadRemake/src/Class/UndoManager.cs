using System;
using System.Collections.Generic;
using OpenTK;
using Re4QuadExtremeEditor.src.Class.TreeNodeObj;

namespace Re4QuadExtremeEditor.src.Class
{
    /// <summary>
    /// Generic position/rotation/scale undo (Ctrl+Z) that works across every object type (ESL,
    /// ITA, AEV, EAR, SAR, EMI, ESE, DSE, FSE, LIT, EtcModel, EFFBLOB...) without needing per-type
    /// logic, because it operates purely through the same NodeMoveMethods contract that
    /// MoveObj.cs already uses to drag objects around (GetObjPostion_ToMove_General /
    /// GetObjRotarionAngles_ToMove / GetObjScale_ToMove and their Set counterparts).
    ///
    /// Usage: BeginTransaction() right before a drag/property-edit starts (mouse down, or before
    /// applying a PropertyGrid change), then CommitTransaction() right after it ends (mouse up,
    /// or after the PropertyGrid change is applied). If nothing actually changed, no undo entry
    /// is pushed. Undo() pops the most recent entry and restores every object's saved values.
    /// </summary>
    public static class UndoManager
    {
        private const int MaxUndoDepth = 100;

        private class ObjectSnapshot
        {
            public MoveObj.ObjKey Key;
            public Object3D Node;
            public Vector3[] Position;
            public Vector3[] Rotation;
            public Vector3[] Scale;
        }

        private class UndoEntry
        {
            public List<ObjectSnapshot> Before;
        }

        private static readonly Stack<UndoEntry> undoStack = new Stack<UndoEntry>();

        // snapshot currently being built by BeginTransaction, waiting for CommitTransaction/CancelTransaction
        private static List<ObjectSnapshot> pendingBefore;

        /// <summary>
        /// Call right before a change to the selected objects' position/rotation/scale begins
        /// (e.g. on mouse-down while dragging a gizmo, or right before applying a manual
        /// PropertyGrid edit that touches coordinates).
        /// </summary>
        public static void BeginTransaction()
        {
            pendingBefore = CaptureSnapshot();
        }

        /// <summary>
        /// Call right after the change finishes (e.g. on mouse-up). Compares the "before" state
        /// captured by BeginTransaction() against the current ("after") state; if anything
        /// actually moved/rotated/scaled, pushes an undo entry. Safe to call even if
        /// BeginTransaction() was never called (does nothing in that case).
        /// </summary>
        public static void CommitTransaction()
        {
            if (pendingBefore == null || pendingBefore.Count == 0)
            {
                pendingBefore = null;
                return;
            }

            bool changed = false;
            foreach (ObjectSnapshot snap in pendingBefore)
            {
                if (snap.Node == null) continue;

                Vector3[] curPos = SafeGetPosition(snap.Node);
                Vector3[] curRot = SafeGetRotation(snap.Node);
                Vector3[] curScale = SafeGetScale(snap.Node);

                if (!VectorArraysEqual(snap.Position, curPos) ||
                    !VectorArraysEqual(snap.Rotation, curRot) ||
                    !VectorArraysEqual(snap.Scale, curScale))
                {
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                undoStack.Push(new UndoEntry { Before = pendingBefore });
                while (undoStack.Count > MaxUndoDepth)
                {
                    // trim oldest entries (Stack doesn't support removing from the bottom
                    // directly, so rebuild once we exceed the cap)
                    UndoEntry[] arr = undoStack.ToArray();
                    undoStack.Clear();
                    for (int i = arr.Length - 2; i >= 0; i--)
                    {
                        undoStack.Push(arr[i]);
                    }
                }
            }

            pendingBefore = null;
        }

        /// <summary>
        /// Discards the pending transaction without pushing an undo entry (e.g. if the drag was
        /// cancelled, or nothing was actually selected).
        /// </summary>
        public static void CancelTransaction()
        {
            pendingBefore = null;
        }

        /// <summary>
        /// Restores the most recent undo entry, moving/rotating/scaling every affected object
        /// back to its previous values. Returns true if something was undone.
        /// </summary>
        public static bool Undo()
        {
            if (undoStack.Count == 0) return false;

            UndoEntry entry = undoStack.Pop();
            foreach (ObjectSnapshot snap in entry.Before)
            {
                if (snap.Node == null) continue;

                if (snap.Position != null)
                {
                    try { snap.Node.SetObjPostion_ToMove_General(snap.Position); } catch (Exception) { }
                }
                if (snap.Rotation != null)
                {
                    try { snap.Node.SetObjRotarionAngles_ToMove(snap.Rotation); } catch (Exception) { }
                }
                if (snap.Scale != null)
                {
                    try { snap.Node.SetObjScale_ToMove(snap.Scale); } catch (Exception) { }
                }
            }

            return true;
        }

        public static void Clear()
        {
            undoStack.Clear();
            pendingBefore = null;
        }

        private static List<ObjectSnapshot> CaptureSnapshot()
        {
            List<ObjectSnapshot> list = new List<ObjectSnapshot>();

            if (DataBase.SelectedNodes == null) return list;

            foreach (var item in DataBase.SelectedNodes.Values)
            {
                if (item is Object3D obj && obj.Parent is TreeNodeGroup)
                {
                    list.Add(new ObjectSnapshot
                    {
                        Key = new MoveObj.ObjKey(obj.ObjLineRef, obj.Group),
                        Node = obj,
                        Position = SafeGetPosition(obj),
                        Rotation = SafeGetRotation(obj),
                        Scale = SafeGetScale(obj)
                    });
                }
            }

            return list;
        }

        private static Vector3[] SafeGetPosition(Object3D obj)
        {
            try { return CloneArray(obj.GetObjPostion_ToMove_General()); } catch (Exception) { return null; }
        }

        private static Vector3[] SafeGetRotation(Object3D obj)
        {
            try { return CloneArray(obj.GetObjRotarionAngles_ToMove()); } catch (Exception) { return null; }
        }

        private static Vector3[] SafeGetScale(Object3D obj)
        {
            try { return CloneArray(obj.GetObjScale_ToMove()); } catch (Exception) { return null; }
        }

        private static Vector3[] CloneArray(Vector3[] arr)
        {
            return arr == null ? null : (Vector3[])arr.Clone();
        }

        private static bool VectorArraysEqual(Vector3[] a, Vector3[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }
    }
}
