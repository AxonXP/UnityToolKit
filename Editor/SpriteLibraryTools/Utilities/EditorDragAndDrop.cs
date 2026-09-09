using UnityEditor;
using UnityEngine;

namespace SpriteLibraryTools
{
    // Shared drop-zone handling so every page implements drag-and-drop the same way.
    public static class EditorDragAndDrop
    {
        public static void HandlePathDrop(Rect dropArea, System.Action<string[]> onDrop)
        {
            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                onDrop(DragAndDrop.paths);
                evt.Use();
            }
        }

        public static void HandleObjectDrop(Rect dropArea, System.Action<Object[]> onDrop)
        {
            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                onDrop(DragAndDrop.objectReferences);
                evt.Use();
            }
        }
    }
}
