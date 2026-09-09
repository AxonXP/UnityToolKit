using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpriteLibraryTools
{
    public class SpriteLibraryToolsWindow : EditorWindow
    {
        private const string PrefKeySelectedPage = "SelectedPage";

        private List<IToolPage> pages;
        private string[] pageTitles;
        private int selectedIndex;

        [MenuItem("Tools/Sprite Library Tools/All Tools", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<SpriteLibraryToolsWindow>();
            window.titleContent = new GUIContent("Sprite Library Tools");
            window.minSize = new Vector2(760, 460);
        }

        private void OnEnable()
        {
            pages = new List<IToolPage>(ToolPageRegistry.CreatePages());
            pageTitles = pages.ConvertAll(page => page.Title).ToArray();
            foreach (IToolPage page in pages)
                page.OnEnable();

            int savedIndex = EditorPrefsState.GetInt(PrefKeySelectedPage, 0);
            selectedIndex = pages.Count == 0 ? 0 : Mathf.Clamp(savedIndex, 0, pages.Count - 1);

            RepaintRequest.Requested += Repaint;
        }

        private void OnDisable()
        {
            RepaintRequest.Requested -= Repaint;

            if (pages == null)
                return;

            foreach (IToolPage page in pages)
                page.OnDisable();

            EditorPrefsState.SetInt(PrefKeySelectedPage, selectedIndex);
        }

        private void OnGUI()
        {
            DrawTabs();
            DrawActivePage();
            DrawStatusBar();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (pages.Count == 0)
            {
                GUILayout.Label("Sprite Library Tools", EditorStyles.boldLabel);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup(selectedIndex, pageTitles, EditorStyles.toolbarPopup, GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    selectedIndex = newIndex;
                    ToolStatus.Clear();
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawActivePage()
        {
            EditorGUILayout.BeginVertical();

            if (pages.Count == 0)
                EditorGUILayout.HelpBox("No tool pages registered.", MessageType.Warning);
            else
                pages[selectedIndex].OnGUI();

            EditorGUILayout.EndVertical();
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
