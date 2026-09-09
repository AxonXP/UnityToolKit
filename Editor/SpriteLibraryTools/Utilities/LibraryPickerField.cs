using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    // Shared "drag a Sprite Library Asset, or a GameObject with a Sprite Library component"
    // field used by both the Builder and Inspector pages.
    public static class LibraryPickerField
    {
        public static SpriteLibraryAsset Draw(string label, SpriteLibraryAsset target, ref Rect dropRect)
        {
            var picked = (SpriteLibraryAsset)EditorGUILayout.ObjectField(label, target, typeof(SpriteLibraryAsset), false);

            GUILayout.Box(
                "Drop a Sprite Library Asset, or a GameObject with a Sprite Library component",
                EditorStyles.helpBox,
                GUILayout.Height(30),
                GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                dropRect = GUILayoutUtility.GetLastRect();

            SpriteLibraryAsset dropped = null;
            EditorDragAndDrop.HandleObjectDrop(dropRect, objects =>
            {
                foreach (Object obj in objects)
                {
                    SpriteLibraryAsset resolved = Resolve(obj);
                    if (resolved != null)
                    {
                        dropped = resolved;
                        break;
                    }
                }
            });

            return dropped != null ? dropped : picked;
        }

        private static SpriteLibraryAsset Resolve(Object obj)
        {
            if (obj is SpriteLibraryAsset asset)
                return asset;

            if (obj is GameObject go)
            {
                SpriteLibrary component = go.GetComponent<SpriteLibrary>();
                if (component != null)
                    return component.spriteLibraryAsset;
            }

            return null;
        }
    }
}
