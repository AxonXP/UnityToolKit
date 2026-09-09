using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    [ToolPage(0)]
    public sealed class BuilderPage : IToolPage
    {
        private enum SubTool
        {
            BulkAutoFill,
            BatchReplace,
            CreateFromFolder
        }

        public string Title => "Library Builder";

        private SubTool activeSubTool;

        // Bulk Auto Fill state
        private SpriteLibraryAsset autoFillTarget;
        private readonly List<string> autoFillSourcePaths = new List<string>();
        private readonly LibraryBuilderOptions autoFillOptions = new LibraryBuilderOptions();
        private bool showAutoFillPreview = true;
        private List<BuildEntry> autoFillPlan;
        private Vector2 autoFillSourceScroll;
        private Vector2 autoFillPreviewScroll;
        private Rect autoFillTargetDropRect;
        private Rect autoFillSourceDropRect;

        // Batch Replace state
        private SpriteLibraryAsset replaceTarget;
        private readonly List<ReplaceSourceEntry> replaceSourceEntries = new List<ReplaceSourceEntry>();
        private NameMatchMode replaceMode = NameMatchMode.IgnorePrefix;
        private string replaceDelimiter = "_";
        private List<ReplaceEntry> replacePlan;
        private Vector2 replaceSourceScroll;
        private Vector2 replacePreviewScroll;
        private Rect replaceTargetDropRect;
        private Rect replaceSourceDropRect;

        // Create From Folder state
        private DefaultAsset createFromFolderRoot;
        private Rect createFromFolderDropRect;
        private readonly LibraryBuilderOptions createFromFolderOptions = new LibraryBuilderOptions();

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
        }

        public void OnGUI()
        {
            DrawSubToolTabs();
            EditorGUILayout.Space();

            switch (activeSubTool)
            {
                case SubTool.BulkAutoFill:
                    DrawBulkAutoFill();
                    break;
                case SubTool.BatchReplace:
                    DrawBatchReplace();
                    break;
                case SubTool.CreateFromFolder:
                    DrawCreateFromFolder();
                    break;
            }
        }

        private void DrawSubToolTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            activeSubTool = (SubTool)EditorGUILayout.Popup(
                (int)activeSubTool,
                new[] { "Bulk Auto Fill", "Batch Replace Sprites", "Create From Folder" },
                EditorStyles.toolbarPopup,
                GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        // ----- Tool 1: Bulk Auto Fill -----

        private void DrawBulkAutoFill()
        {
            EditorGUILayout.LabelField("Target Library", EditorStyles.boldLabel);
            autoFillTarget = LibraryPickerField.Draw("Sprite Library Asset", autoFillTarget, ref autoFillTargetDropRect);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Source Folders", EditorStyles.boldLabel);
            DrawSourceFolderList(autoFillSourcePaths, ref autoFillSourceScroll, ref autoFillSourceDropRect);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Category Creation", EditorStyles.boldLabel);
            autoFillOptions.CategoryCreation = (CategoryCreationMode)EditorGUILayout.Popup(
                (int)autoFillOptions.CategoryCreation,
                new[] { "Folder Names", "Sprite Name Prefix", "Existing Categories" });
            if (autoFillOptions.CategoryCreation == CategoryCreationMode.SpriteNamePrefix)
                autoFillOptions.PrefixDelimiter = EditorGUILayout.TextField("Prefix Delimiter", autoFillOptions.PrefixDelimiter);

            EditorGUILayout.Space();
            DrawLabelNamingSection(autoFillOptions);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Duplicates", EditorStyles.boldLabel);
            autoFillOptions.Duplicates = (DuplicateHandling)EditorGUILayout.Popup(
                (int)autoFillOptions.Duplicates,
                new[] { "Skip", "Replace", "Rename" });

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sorting", EditorStyles.boldLabel);
            autoFillOptions.SortAlphabetical = EditorGUILayout.ToggleLeft("Alphabetical", autoFillOptions.SortAlphabetical);
            using (new EditorGUI.DisabledScope(!autoFillOptions.SortAlphabetical))
                autoFillOptions.NaturalSort = EditorGUILayout.ToggleLeft("Natural Sort", autoFillOptions.NaturalSort);

            bool optionsChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            showAutoFillPreview = EditorGUILayout.ToggleLeft("Show Changes", showAutoFillPreview);

            if (GUILayout.Button("Refresh Preview") || (optionsChanged && showAutoFillPreview))
                RefreshAutoFillPlan();

            if (showAutoFillPreview)
                DrawAutoFillPreview();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(autoFillTarget == null || autoFillSourcePaths.Count == 0))
            {
                if (GUILayout.Button("Apply", GUILayout.Height(30)))
                    ApplyAutoFill();
            }
        }

        private void RefreshAutoFillPlan()
        {
            List<ScannedSprite> sprites = SpriteAssetScanner.ScanPaths(autoFillSourcePaths);
            autoFillPlan = LibraryBuilderService.BuildPlan(sprites, autoFillTarget, autoFillOptions);

            if (sprites.Count == 0)
            {
                Debug.LogWarning("Sprite Library Tools: no Sprite assets were found in the selected source folder(s). " +
                    "Check that the textures' Texture Type is set to 'Sprite (2D and UI)'.");
            }
        }

        private void DrawAutoFillPreview()
        {
            if (autoFillPlan == null)
            {
                EditorGUILayout.HelpBox("Click Refresh Preview to see planned changes.", MessageType.Info);
                return;
            }

            if (autoFillPlan.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No Sprite assets were found in the selected source folder(s). " +
                    "Check that the textures' Texture Type is set to 'Sprite (2D and UI)' in their Import Settings.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            autoFillPreviewScroll = EditorGUILayout.BeginScrollView(autoFillPreviewScroll, GUILayout.Height(220));

            foreach (IGrouping<string, BuildEntry> group in autoFillPlan.GroupBy(e => e.Category))
            {
                EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
                foreach (BuildEntry entry in group)
                {
                    string suffix = entry.Status switch
                    {
                        BuildEntryStatus.WillReplaceDuplicate => " (replace)",
                        BuildEntryStatus.WillSkipDuplicate => " (skip - duplicate)",
                        BuildEntryStatus.RenamedDuplicate => " (renamed)",
                        BuildEntryStatus.UnmatchedCategory => " (no matching category)",
                        _ => string.Empty
                    };
                    EditorGUILayout.LabelField($"    {entry.Label}{suffix}");
                }
            }

            EditorGUILayout.EndScrollView();

            int newCount = autoFillPlan.Count(e => e.Status == BuildEntryStatus.New || e.Status == BuildEntryStatus.RenamedDuplicate);
            int replaceCount = autoFillPlan.Count(e => e.Status == BuildEntryStatus.WillReplaceDuplicate);
            int skipCount = autoFillPlan.Count(e => e.Status == BuildEntryStatus.WillSkipDuplicate || e.Status == BuildEntryStatus.UnmatchedCategory);
            EditorGUILayout.LabelField($"{newCount} to add, {replaceCount} to replace, {skipCount} to skip.");
        }

        private void ApplyAutoFill()
        {
            if (autoFillTarget == null || autoFillSourcePaths.Count == 0)
                return;

            if (autoFillPlan == null)
                RefreshAutoFillPlan();

            int applyCount = autoFillPlan.Count(e => e.WillApply);
            if (applyCount == 0)
            {
                ToolStatus.Set("Bulk Auto Fill: nothing to add. Check the source folders contain Sprite-type textures, or that Duplicates isn't set to Skip.");
                autoFillPlan = null;
                return;
            }

            autoFillTarget = LibraryBuilderService.Apply(autoFillTarget, autoFillPlan, "Bulk Auto Fill Library");
            InspectorRefresher.RefreshOpenInspectors();
            ToolStatus.Set($"Bulk Auto Fill: added {applyCount} label(s) to '{autoFillTarget.name}' (now {autoFillTarget.GetCategoryNames().Count()} categories). " +
                "If a character's Sprite Resolver was already selected, its dropdown will refresh automatically.");
            autoFillPlan = null;
        }

        // ----- Tool 2: Batch Replace Sprites -----

        private void DrawBatchReplace()
        {
            EditorGUILayout.LabelField("Target Library", EditorStyles.boldLabel);
            replaceTarget = LibraryPickerField.Draw("Sprite Library Asset", replaceTarget, ref replaceTargetDropRect);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Replacement Sprites", EditorStyles.boldLabel);
            DrawReplaceSourceList();
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Matching", EditorStyles.boldLabel);
            replaceMode = (NameMatchMode)EditorGUILayout.Popup(
                (int)replaceMode,
                new[] { "Exact Name", "Ignore Prefix", "Ignore Suffix", "Index" });
            if (replaceMode == NameMatchMode.IgnorePrefix || replaceMode == NameMatchMode.IgnoreSuffix)
                replaceDelimiter = EditorGUILayout.TextField("Delimiter", replaceDelimiter);
            else if (replaceMode == NameMatchMode.Index)
                EditorGUILayout.HelpBox("Ignores names: the Nth label in the library is paired with the Nth sprite in the folder (sorted naturally).", MessageType.None);
            bool optionsChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh Preview") || optionsChanged)
                RefreshReplacePlan();

            DrawReplacePreview();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(replaceTarget == null || replaceSourceEntries.Count == 0))
            {
                if (GUILayout.Button("Apply", GUILayout.Height(30)))
                    ApplyReplace();
            }
        }

        private void RefreshReplacePlan()
        {
            var globalSprites = new List<ScannedSprite>();
            var categoryScopedSprites = new Dictionary<string, List<ScannedSprite>>();

            foreach (ReplaceSourceEntry entry in replaceSourceEntries)
            {
                List<ScannedSprite> scanned = SpriteAssetScanner.ScanPaths(new[] { entry.Path });
                if (string.IsNullOrEmpty(entry.CategoryOverride))
                {
                    globalSprites.AddRange(scanned);
                }
                else
                {
                    if (!categoryScopedSprites.TryGetValue(entry.CategoryOverride, out List<ScannedSprite> list))
                    {
                        list = new List<ScannedSprite>();
                        categoryScopedSprites[entry.CategoryOverride] = list;
                    }
                    list.AddRange(scanned);
                }
            }

            replacePlan = BatchSpriteReplacerService.BuildPlan(replaceTarget, globalSprites, categoryScopedSprites, replaceMode, replaceDelimiter);

            if (globalSprites.Count == 0 && categoryScopedSprites.Count == 0)
            {
                Debug.LogWarning("Sprite Library Tools: no Sprite assets were found in the selected replacement source(s). " +
                    "Check that the textures' Texture Type is set to 'Sprite (2D and UI)'.");
            }
        }

        private void DrawReplacePreview()
        {
            if (replacePlan == null)
            {
                EditorGUILayout.HelpBox("Click Refresh Preview to see planned replacements.", MessageType.Info);
                return;
            }

            if (replacePlan.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "The target library has no labels yet, so there is nothing to replace. Populate it first with Bulk Auto Fill.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            replacePreviewScroll = EditorGUILayout.BeginScrollView(replacePreviewScroll, GUILayout.Height(220));

            foreach (ReplaceEntry entry in replacePlan)
            {
                string line = entry.Matched
                    ? $"{entry.Category}/{entry.Label}: {entry.OldSprite.name} -> {entry.NewSprite.name}"
                    : $"{entry.Category}/{entry.Label}: {entry.OldSprite.name} (no match)";
                EditorGUILayout.LabelField(line);
            }

            EditorGUILayout.EndScrollView();

            int matched = replacePlan.Count(e => e.Matched);
            EditorGUILayout.LabelField($"{matched} of {replacePlan.Count} label(s) will be replaced.");
        }

        private void ApplyReplace()
        {
            if (replaceTarget == null || replaceSourceEntries.Count == 0)
                return;

            if (replacePlan == null)
                RefreshReplacePlan();

            int matched = replacePlan.Count(e => e.Matched);
            if (matched == 0)
            {
                ToolStatus.Set("Batch Replace: no matches found. Check the Matching mode/delimiter, and that replacement sprites are imported as Sprite (2D and UI).");
                replacePlan = null;
                return;
            }

            replaceTarget = BatchSpriteReplacerService.Apply(replaceTarget, replacePlan, "Batch Replace Sprites");
            InspectorRefresher.RefreshOpenInspectors();
            ToolStatus.Set($"Batch Replace: replaced {matched} sprite(s) in '{replaceTarget.name}'.");
            replacePlan = null;
        }

        // Accepts folders (as before) or individual sprite sheet files. Each entry can
        // optionally be pinned to a category: useful when a whole re-drawn sheet's sprite
        // names won't line up with the existing labels, so name/index matching wouldn't find
        // them - pinning tells the tool exactly which category's labels to replace with it.
        private void DrawReplaceSourceList()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Height(140));
            replaceSourceScroll = EditorGUILayout.BeginScrollView(replaceSourceScroll);

            if (replaceSourceEntries.Count == 0)
                EditorGUILayout.HelpBox("Drag sprite folders or individual sprite sheets here, or use Add Selected.", MessageType.Info);

            string[] categoryOptions = null;
            if (replaceTarget != null)
                categoryOptions = new[] { "(Auto - match by name/index)" }.Concat(replaceTarget.GetCategoryNames()).ToArray();

            ReplaceSourceEntry toRemove = null;
            foreach (ReplaceSourceEntry entry in replaceSourceEntries)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(entry.Path);

                if (categoryOptions != null)
                {
                    int currentIndex = string.IsNullOrEmpty(entry.CategoryOverride) ? 0 : System.Array.IndexOf(categoryOptions, entry.CategoryOverride);
                    if (currentIndex < 0)
                        currentIndex = 0;

                    int newIndex = EditorGUILayout.Popup(currentIndex, categoryOptions, GUILayout.Width(160));
                    entry.CategoryOverride = newIndex == 0 ? null : categoryOptions[newIndex];
                }

                if (GUILayout.Button("x", GUILayout.Width(20)))
                    toRemove = entry;
                EditorGUILayout.EndHorizontal();
            }
            if (toRemove != null)
                replaceSourceEntries.Remove(toRemove);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
                replaceSourceDropRect = GUILayoutUtility.GetLastRect();

            EditorDragAndDrop.HandlePathDrop(replaceSourceDropRect, dropped =>
            {
                foreach (string path in dropped)
                    AddReplaceSource(path);
            });

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Selected"))
            {
                foreach (Object obj in Selection.objects)
                    AddReplaceSource(AssetDatabase.GetAssetPath(obj));
            }
            if (GUILayout.Button("Clear All"))
                replaceSourceEntries.Clear();
            EditorGUILayout.EndHorizontal();
        }

        private void AddReplaceSource(string path)
        {
            if (string.IsNullOrEmpty(path) || replaceSourceEntries.Any(e => e.Path == path))
                return;

            bool isFolder = AssetDatabase.IsValidFolder(path);
            bool hasSprite = !isFolder && AssetDatabase.LoadAllAssetsAtPath(path).Any(a => a is Sprite);
            if (!isFolder && !hasSprite)
                return;

            replaceSourceEntries.Add(new ReplaceSourceEntry { Path = path });
        }

        // ----- Tool 3: Create Library From Folder -----

        private void DrawCreateFromFolder()
        {
            EditorGUILayout.HelpBox(
                "Pick a root folder. Each immediate subfolder becomes a category, and every sprite inside becomes a label.",
                MessageType.Info);

            createFromFolderRoot = (DefaultAsset)EditorGUILayout.ObjectField("Root Folder", createFromFolderRoot, typeof(DefaultAsset), false);

            GUILayout.Box("Drop a folder here", EditorStyles.helpBox, GUILayout.Height(30), GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                createFromFolderDropRect = GUILayoutUtility.GetLastRect();

            EditorDragAndDrop.HandlePathDrop(createFromFolderDropRect, paths =>
            {
                foreach (string path in paths)
                {
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        createFromFolderRoot = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                        break;
                    }
                }
            });

            EditorGUILayout.Space();
            DrawCategoryNamingSection(createFromFolderOptions);

            EditorGUILayout.Space();
            DrawLabelNamingSection(createFromFolderOptions);

            EditorGUILayout.Space();

            string rootPath = createFromFolderRoot != null ? AssetDatabase.GetAssetPath(createFromFolderRoot) : null;
            bool validFolder = !string.IsNullOrEmpty(rootPath) && AssetDatabase.IsValidFolder(rootPath);

            using (new EditorGUI.DisabledScope(!validFolder))
            {
                if (GUILayout.Button("Create Library From Folder", GUILayout.Height(30)))
                    CreateFromFolder(rootPath);
            }
        }

        private void CreateFromFolder(string rootPath)
        {
            string folderName = System.IO.Path.GetFileName(rootPath);
            string defaultAssetName = folderName + "SpriteLibrary";
            string savePath = EditorUtility.SaveFilePanelInProject(
                "Save Sprite Library",
                defaultAssetName,
                "spriteLib",
                "Choose a location for the new Sprite Library asset.",
                rootPath);

            if (string.IsNullOrEmpty(savePath))
                return;

            SpriteLibraryAsset asset = LibraryBuilderService.CreateLibraryFromFolder(rootPath, savePath, createFromFolderOptions);
            int categoryCount = asset.GetCategoryNames().Count();
            ToolStatus.Set(categoryCount == 0
                ? $"Created '{asset.name}', but no sprites were found under '{rootPath}'. Check the textures' Texture Type."
                : $"Created '{asset.name}' with {categoryCount} categor{(categoryCount == 1 ? "y" : "ies")} from '{rootPath}'.");
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        // ----- Shared widgets -----

        private static void DrawLabelNamingSection(LibraryBuilderOptions options)
        {
            EditorGUILayout.LabelField("Label Naming", EditorStyles.boldLabel);
            options.LabelNaming = (LabelNamingMode)EditorGUILayout.Popup(
                (int)options.LabelNaming,
                new[] { "Keep Original", "Remove Prefix", "Remove Suffix", "Search Replace", "Index Number" });
            switch (options.LabelNaming)
            {
                case LabelNamingMode.RemovePrefix:
                    options.RemovePrefixText = EditorGUILayout.TextField("Prefix", options.RemovePrefixText);
                    break;
                case LabelNamingMode.RemoveSuffix:
                    options.RemoveSuffixText = EditorGUILayout.TextField("Suffix", options.RemoveSuffixText);
                    break;
                case LabelNamingMode.SearchReplace:
                    options.SearchText = EditorGUILayout.TextField("Search", options.SearchText);
                    options.ReplaceText = EditorGUILayout.TextField("Replace", options.ReplaceText);
                    break;
                case LabelNamingMode.IndexNumber:
                    options.IndexStart = EditorGUILayout.IntField("Index Start", options.IndexStart);
                    break;
            }
        }

        // Create From Folder only: no Index Number here since numbering a folder name is
        // meaningless (categories aren't a numbered sequence the way animation frames are).
        private static void DrawCategoryNamingSection(LibraryBuilderOptions options)
        {
            EditorGUILayout.LabelField("Category Naming", EditorStyles.boldLabel);
            options.CategoryNaming = (LabelNamingMode)EditorGUILayout.Popup(
                (int)options.CategoryNaming,
                new[] { "Keep Original", "Remove Prefix", "Remove Suffix", "Search Replace" });
            switch (options.CategoryNaming)
            {
                case LabelNamingMode.RemovePrefix:
                    options.CategoryRemovePrefixText = EditorGUILayout.TextField("Prefix", options.CategoryRemovePrefixText);
                    break;
                case LabelNamingMode.RemoveSuffix:
                    options.CategoryRemoveSuffixText = EditorGUILayout.TextField("Suffix", options.CategoryRemoveSuffixText);
                    break;
                case LabelNamingMode.SearchReplace:
                    options.CategorySearchText = EditorGUILayout.TextField("Search", options.CategorySearchText);
                    options.CategoryReplaceText = EditorGUILayout.TextField("Replace", options.CategoryReplaceText);
                    break;
            }
        }

        private static void DrawSourceFolderList(List<string> paths, ref Vector2 scroll, ref Rect dropRect)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Height(100));
            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (paths.Count == 0)
                EditorGUILayout.HelpBox("Drag sprite folders here, or use Add Selected.", MessageType.Info);

            string toRemove = null;
            foreach (string path in paths)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(path);
                if (GUILayout.Button("x", GUILayout.Width(20)))
                    toRemove = path;
                EditorGUILayout.EndHorizontal();
            }
            if (toRemove != null)
                paths.Remove(toRemove);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
                dropRect = GUILayoutUtility.GetLastRect();

            EditorDragAndDrop.HandlePathDrop(dropRect, dropped =>
            {
                foreach (string path in dropped)
                    AddFolderPath(paths, path);
            });

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Selected"))
            {
                foreach (Object obj in Selection.objects)
                    AddFolderPath(paths, AssetDatabase.GetAssetPath(obj));
            }
            if (GUILayout.Button("Clear All"))
                paths.Clear();
            EditorGUILayout.EndHorizontal();
        }

        private static void AddFolderPath(List<string> paths, string path)
        {
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
                return;
            if (!paths.Contains(path))
                paths.Add(path);
        }
    }
}
