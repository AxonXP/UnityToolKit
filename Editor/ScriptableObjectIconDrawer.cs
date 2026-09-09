using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace CustomIcons.Editor
{
    [InitializeOnLoad]
    public static class ScriptableObjectIconDrawer
    {
        static ScriptableObjectIconDrawer()
        {
            EditorApplication.projectWindowItemOnGUI += DrawCustomIcon;
        }

        private static void DrawCustomIcon(string guid, Rect selectionRect)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

            if (asset == null || !(asset is ScriptableObject scriptableObject))
                return;

            Sprite icon = GetIconFromScriptableObject(scriptableObject);
            if (icon == null)
                return;

            Rect iconRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.width);

            if (selectionRect.height > selectionRect.width)
            {
                iconRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.width);
            }
            else
            {
                float size = selectionRect.height;
                iconRect = new Rect(selectionRect.x, selectionRect.y, size, size);
            }

            if (icon.texture != null)
            {
                EditorGUI.DrawRect(iconRect, new Color(0.22f, 0.22f, 0.22f));

                Rect textureRect = icon.textureRect;
                Rect uv = new Rect(
                    textureRect.x / icon.texture.width,
                    textureRect.y / icon.texture.height,
                    textureRect.width / icon.texture.width,
                    textureRect.height / icon.texture.height);

                GUI.DrawTextureWithTexCoords(iconRect, icon.texture, uv, true);
            }
        }

        private static Sprite GetIconFromScriptableObject(ScriptableObject scriptableObject)
        {
            System.Type type = scriptableObject.GetType();

            //look for a field with the ScriptableObjectIconAttribute
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(Sprite) &&
                    System.Attribute.IsDefined(field, typeof(ScriptableObjectIconAttribute)))
                {
                    return field.GetValue(scriptableObject) as Sprite;
                }
            }
            return null;
        }
    }
}
