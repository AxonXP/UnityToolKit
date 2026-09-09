using UnityEditor;
using UnityEngine;

namespace SpriteLibraryTools
{
    // Base for windows that host exactly one IToolPage as its own standalone window, as an
    // alternative to opening it as a tab inside the combined SpriteLibraryToolsWindow. No tab
    // UI here by design - a single-tool window is dedicated to just that one tool.
    public abstract class SingleToolPageWindow : EditorWindow
    {
        private IToolPage page;

        protected abstract IToolPage CreatePage();

        protected static T OpenWindow<T>() where T : SingleToolPageWindow
        {
            var window = GetWindow<T>();
            window.minSize = new Vector2(600, 400);
            return window;
        }

        private void OnEnable()
        {
            page = CreatePage();
            titleContent = new GUIContent(page.Title);
            page.OnEnable();

            RepaintRequest.Requested += Repaint;
        }

        private void OnDisable()
        {
            RepaintRequest.Requested -= Repaint;
            page?.OnDisable();
        }

        private void OnGUI()
        {
            page.OnGUI();
            DrawStatusBar();
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string message = string.IsNullOrEmpty(ToolStatus.Message) ? "Ready" : ToolStatus.Message;
            GUILayout.Label(message, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
