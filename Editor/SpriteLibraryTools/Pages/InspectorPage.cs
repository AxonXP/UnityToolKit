using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    [ToolPage(3)]
    public sealed class InspectorPage : IToolPage
    {
        private enum SubTab
        {
            VisualBrowser,
            Statistics,
            Validation
        }

        private abstract class BrowserRow
        {
            public float Height;
        }

        private sealed class HeaderRow : BrowserRow
        {
            public string Category;
            public int LabelCount;
        }

        private sealed class GridRow : BrowserRow
        {
            public List<LabelModel> Labels;
        }

        private const float CellPadding = 6f;
        private const float HeaderHeight = 20f;

        public string Title => "Library Inspector";

        private SubTab activeSubTab;
        private SpriteLibraryAsset target;
        private Rect targetDropRect;

        private string searchFilter = string.Empty;
        private float thumbnailSize = 64f;
        private Vector2 browserScroll;
        private readonly HashSet<string> collapsedCategories = new HashSet<string>();

        private SpriteLibraryModel cachedModel;
        private LibraryStatistics? cachedStatistics;
        private List<ValidationIssue> cachedIssues;

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
        }

        public void OnGUI()
        {
            DrawSubTabs();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Target Library", EditorStyles.boldLabel);
            SpriteLibraryAsset newTarget = LibraryPickerField.Draw("Sprite Library Asset", target, ref targetDropRect);
            if (newTarget != target)
            {
                target = newTarget;
                RefreshModel();
            }
            else if (cachedModel == null && target != null)
            {
                RefreshModel();
            }

            EditorGUILayout.Space();

            switch (activeSubTab)
            {
                case SubTab.VisualBrowser:
                    DrawVisualBrowser();
                    break;
                case SubTab.Statistics:
                    DrawStatistics();
                    break;
                case SubTab.Validation:
                    DrawValidation();
                    break;
            }
        }

        private void DrawSubTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            activeSubTab = (SubTab)EditorGUILayout.Popup(
                (int)activeSubTab,
                new[] { "Visual Browser", "Statistics", "Validation" },
                EditorStyles.toolbarPopup,
                GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        // ----- Visual Browser -----
        // Rows (category headers and grid rows of thumbnails) are only actually drawn when
        // they fall within the current scroll viewport, so browsing stays responsive even for
        // very large libraries.

        private void DrawVisualBrowser()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
            GUILayout.Label("Size", GUILayout.Width(30));
            thumbnailSize = GUILayout.HorizontalSlider(thumbnailSize, 32f, 128f, GUILayout.Width(100));
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                RefreshModel();
            EditorGUILayout.EndHorizontal();

            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to browse it.", MessageType.Info);
                return;
            }

            int columns = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 40f) / (thumbnailSize + CellPadding)));
            List<BrowserRow> rows = BuildRows(cachedModel, columns);

            if (rows.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(searchFilter) ? "This library has no categories yet." : "No categories or labels match your search.",
                    MessageType.Info);
                return;
            }

            float totalHeight = rows.Sum(r => r.Height);
            Rect viewRect = GUILayoutUtility.GetRect(10, 300, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var contentRect = new Rect(0, 0, viewRect.width - 16f, totalHeight);

            browserScroll = GUI.BeginScrollView(viewRect, browserScroll, contentRect);

            float y = 0f;
            float visibleTop = browserScroll.y;
            float visibleBottom = browserScroll.y + viewRect.height;

            foreach (BrowserRow row in rows)
            {
                if (y + row.Height >= visibleTop && y <= visibleBottom)
                    DrawRow(row, new Rect(4f, y, contentRect.width - 8f, row.Height));

                y += row.Height;
            }

            GUI.EndScrollView();
        }

        private List<BrowserRow> BuildRows(SpriteLibraryModel model, int columns)
        {
            var rows = new List<BrowserRow>();
            string filter = searchFilter.Trim();

            foreach (CategoryModel category in model.Categories)
            {
                bool categoryNameMatches = string.IsNullOrEmpty(filter) ||
                    category.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                IReadOnlyList<LabelModel> labelsToShow = categoryNameMatches
                    ? category.Labels
                    : category.Labels.Where(l => l.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                if (!categoryNameMatches && labelsToShow.Count == 0)
                    continue;

                rows.Add(new HeaderRow { Category = category.Name, LabelCount = labelsToShow.Count, Height = HeaderHeight });

                if (collapsedCategories.Contains(category.Name))
                    continue;

                for (int i = 0; i < labelsToShow.Count; i += columns)
                {
                    int count = Mathf.Min(columns, labelsToShow.Count - i);
                    var cellLabels = new List<LabelModel>(count);
                    for (int j = 0; j < count; j++)
                        cellLabels.Add(labelsToShow[i + j]);

                    rows.Add(new GridRow { Labels = cellLabels, Height = thumbnailSize + EditorGUIUtility.singleLineHeight + CellPadding });
                }
            }

            return rows;
        }

        private void DrawRow(BrowserRow row, Rect rect)
        {
            if (row is HeaderRow header)
            {
                bool wasExpanded = !collapsedCategories.Contains(header.Category);
                var foldRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                bool isExpanded = EditorGUI.Foldout(foldRect, wasExpanded, $"{header.Category} ({header.LabelCount})", true);
                if (isExpanded != wasExpanded)
                {
                    if (isExpanded)
                        collapsedCategories.Remove(header.Category);
                    else
                        collapsedCategories.Add(header.Category);
                }
            }
            else if (row is GridRow grid)
            {
                float x = rect.x;
                foreach (LabelModel label in grid.Labels)
                {
                    DrawThumbnailCell(new Rect(x, rect.y, thumbnailSize, rect.height), label);
                    x += thumbnailSize + CellPadding;
                }
            }
        }

        private void DrawThumbnailCell(Rect rect, LabelModel label)
        {
            var imageRect = new Rect(rect.x, rect.y, thumbnailSize, thumbnailSize);
            Texture2D thumb = SpriteThumbnailCache.GetThumbnail(label.Sprite);
            if (thumb != null)
                GUI.DrawTexture(imageRect, thumb, ScaleMode.ScaleToFit);
            else
                GUI.Box(imageRect, GUIContent.none);

            if (SpriteThumbnailCache.IsLoading(label.Sprite))
                RepaintRequest.Request();

            var labelRect = new Rect(rect.x, rect.y + thumbnailSize, thumbnailSize, EditorGUIUtility.singleLineHeight);
            GUI.Label(labelRect, label.Name, EditorStyles.miniLabel);

            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && label.Sprite != null && imageRect.Contains(evt.mousePosition))
            {
                Selection.activeObject = label.Sprite;
                EditorGUIUtility.PingObject(label.Sprite);
                evt.Use();
            }
        }

        // ----- Statistics -----

        private void DrawStatistics()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to see its statistics.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Refresh") || cachedStatistics == null)
            {
                RefreshModel();
                cachedStatistics = LibraryStatisticsService.Compute(cachedModel);
            }

            LibraryStatistics stats = cachedStatistics.Value;
            EditorGUILayout.LabelField("Categories", stats.CategoryCount.ToString());
            EditorGUILayout.LabelField("Labels", stats.LabelCount.ToString());
            EditorGUILayout.LabelField("Unique Sprites", stats.UniqueSpriteCount.ToString());
            EditorGUILayout.LabelField("Duplicate Sprites", stats.DuplicateSpriteCount.ToString());
            EditorGUILayout.LabelField("Empty Categories", stats.EmptyCategoryCount.ToString());
            EditorGUILayout.LabelField("Missing Sprites", stats.MissingSpriteCount.ToString());
        }

        // ----- Validation -----

        private void DrawValidation()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to validate it.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Run Validation") || cachedIssues == null)
            {
                RefreshModel();
                cachedIssues = LibraryValidator.Validate(cachedModel, target);
            }

            if (cachedIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues found.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"{cachedIssues.Count} issue(s) found.", EditorStyles.boldLabel);

            foreach (ValidationIssue issue in cachedIssues)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField($"[{issue.Type}] {issue.Message}", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Locate", GUILayout.Width(60)))
                    LocateIssue(issue);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void LocateIssue(ValidationIssue issue)
        {
            activeSubTab = SubTab.VisualBrowser;
            searchFilter = issue.Label ?? issue.Category ?? string.Empty;
            if (!string.IsNullOrEmpty(issue.Category))
                collapsedCategories.Remove(issue.Category);
        }

        private void RefreshModel()
        {
            cachedModel = target != null ? SpriteLibraryModel.FromAsset(target) : null;
            cachedStatistics = null;
            cachedIssues = null;
        }
    }
}
