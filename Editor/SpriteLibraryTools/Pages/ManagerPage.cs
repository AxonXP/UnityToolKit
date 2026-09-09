using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    [ToolPage(2)]
    public sealed class ManagerPage : IToolPage
    {
        private enum SubTab
        {
            Merge,
            Split,
            DuplicateCategories,
            MoveLabels,
            Rename,
            Sort
        }

        public string Title => "Library Manager";

        private SubTab activeSubTab;
        private SpriteLibraryAsset target;
        private Rect targetDropRect;
        private SpriteLibraryModel cachedModel;

        // Merge state
        private SpriteLibraryAsset mergeSource;
        private Rect mergeSourceDropRect;
        private DuplicateHandling mergeDuplicates = DuplicateHandling.Skip;
        private List<BuildEntry> mergePlan;
        private Vector2 mergePreviewScroll;

        // Split state
        private SplitMode splitMode;
        private readonly HashSet<string> splitSelectedCategories = new HashSet<string>();
        private Vector2 splitCategoryScroll;
        private string splitPrefixDelimiter = "_";
        private int splitMaxCategories = 5;
        private DefaultAsset splitOutputFolderAsset;
        private bool splitRemoveFromSource;
        private List<SplitGroup> splitPlan;
        private Vector2 splitPreviewScroll;

        // Duplicate Categories state
        private int duplicateCategoryIndex;
        private string duplicateNewNames = string.Empty;

        // Move Labels state
        private int moveFromCategoryIndex;
        private int moveToCategoryIndex;
        private string moveNewCategoryName = string.Empty;
        private readonly HashSet<string> moveSelectedLabels = new HashSet<string>();
        private Vector2 moveLabelScroll;

        // Rename state
        private bool renameLabelsInsteadOfCategories;
        private int renameCategoryIndex;
        private RenameMode renameMode;
        private string renameSearchText = string.Empty;
        private string renameReplaceText = string.Empty;
        private string renamePrefixText = string.Empty;
        private string renameSuffixText = string.Empty;
        private string renameRegexPattern = string.Empty;
        private string renameRegexReplacement = string.Empty;
        private List<RenamePlanEntry> renamePlan;
        private string renameError;
        private Vector2 renamePreviewScroll;

        // Sort state
        private CategorySortMode sortMode;
        private List<string> sortPlan;
        private Vector2 sortPreviewScroll;

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
                case SubTab.Merge:
                    DrawMerge();
                    break;
                case SubTab.Split:
                    DrawSplit();
                    break;
                case SubTab.DuplicateCategories:
                    DrawDuplicateCategories();
                    break;
                case SubTab.MoveLabels:
                    DrawMoveLabels();
                    break;
                case SubTab.Rename:
                    DrawRename();
                    break;
                case SubTab.Sort:
                    DrawSort();
                    break;
            }
        }

        private void RefreshModel()
        {
            cachedModel = target != null ? SpriteLibraryModel.FromAsset(target) : null;
            mergePlan = null;
            splitPlan = null;
            splitSelectedCategories.Clear();
            moveSelectedLabels.Clear();
            renamePlan = null;
            renameError = null;
            sortPlan = null;
        }

        private void DrawSubTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            activeSubTab = (SubTab)EditorGUILayout.Popup(
                (int)activeSubTab,
                new[] { "Merge", "Split", "Duplicate Categories", "Move Labels", "Rename", "Sort" },
                EditorStyles.toolbarPopup,
                GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        // ----- Merge -----

        private void DrawMerge()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to merge into.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Merge From", EditorStyles.boldLabel);
            mergeSource = LibraryPickerField.Draw("Source Library (B)", mergeSource, ref mergeSourceDropRect);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Duplicates", EditorStyles.boldLabel);
            mergeDuplicates = (DuplicateHandling)EditorGUILayout.Popup((int)mergeDuplicates, new[] { "Skip", "Replace", "Rename" });

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(mergeSource == null))
            {
                if (GUILayout.Button("Refresh Preview"))
                    mergePlan = LibraryMergeService.BuildPlan(target, mergeSource, mergeDuplicates);
            }

            DrawMergePreview();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(mergeSource == null))
            {
                if (GUILayout.Button("Merge", GUILayout.Height(30)))
                    ApplyMerge();
            }
        }

        private void DrawMergePreview()
        {
            if (mergePlan == null)
            {
                EditorGUILayout.HelpBox("Click Refresh Preview to see planned changes.", MessageType.Info);
                return;
            }

            if (mergePlan.Count == 0)
            {
                EditorGUILayout.HelpBox("Source library has no labels to merge.", MessageType.Info);
                return;
            }

            mergePreviewScroll = EditorGUILayout.BeginScrollView(mergePreviewScroll, GUILayout.Height(140));
            foreach (BuildEntry entry in mergePlan)
            {
                string suffix = entry.Status switch
                {
                    BuildEntryStatus.WillReplaceDuplicate => " (replace)",
                    BuildEntryStatus.WillSkipDuplicate => " (skip - duplicate)",
                    BuildEntryStatus.RenamedDuplicate => " (renamed)",
                    _ => string.Empty
                };
                EditorGUILayout.LabelField($"{entry.Category}/{entry.Label}{suffix}");
            }
            EditorGUILayout.EndScrollView();

            int applyCount = mergePlan.Count(e => e.WillApply);
            EditorGUILayout.LabelField($"{applyCount} of {mergePlan.Count} label(s) will be merged in.");
        }

        private void ApplyMerge()
        {
            if (mergeSource == null)
                return;

            if (mergePlan == null)
                mergePlan = LibraryMergeService.BuildPlan(target, mergeSource, mergeDuplicates);

            int applyCount = mergePlan.Count(e => e.WillApply);
            if (applyCount == 0)
            {
                ToolStatus.Set("Merge: nothing to merge.");
                return;
            }

            target = LibraryBuilderService.Apply(target, mergePlan, "Merge Libraries");
            InspectorRefresher.RefreshOpenInspectors();
            string mergedName = target.name;
            RefreshModel();
            ToolStatus.Set($"Merge: added {applyCount} label(s) into '{mergedName}'.");
        }

        // ----- Split -----

        private void DrawSplit()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to split.", MessageType.Info);
                return;
            }

            if (cachedModel.Categories.Count == 0)
            {
                EditorGUILayout.HelpBox("This library has no categories yet.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Split By", EditorStyles.boldLabel);
            splitMode = (SplitMode)EditorGUILayout.Popup((int)splitMode, new[] { "Selected Categories", "Category Prefix", "Maximum Categories" });

            if (splitMode == SplitMode.SelectedCategories)
                DrawSplitCategoryChecklist();
            else if (splitMode == SplitMode.CategoryPrefix)
                splitPrefixDelimiter = EditorGUILayout.TextField("Prefix Delimiter", splitPrefixDelimiter);
            else if (splitMode == SplitMode.MaximumCategories)
                splitMaxCategories = Mathf.Max(1, EditorGUILayout.IntField("Maximum Categories", splitMaxCategories));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            splitOutputFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder (optional)", splitOutputFolderAsset, typeof(DefaultAsset), false);
            splitRemoveFromSource = EditorGUILayout.ToggleLeft("Remove categories from source after splitting", splitRemoveFromSource);

            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh Preview"))
                splitPlan = LibrarySplitService.BuildPlan(cachedModel, splitMode, splitSelectedCategories, splitPrefixDelimiter, splitMaxCategories);

            DrawSplitPreview();

            EditorGUILayout.Space();
            bool canApply = splitPlan != null && splitPlan.Count > 0;
            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("Split", GUILayout.Height(30)))
                    ApplySplit();
            }
        }

        private void DrawSplitCategoryChecklist()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Height(100));
            splitCategoryScroll = EditorGUILayout.BeginScrollView(splitCategoryScroll);

            foreach (CategoryModel category in cachedModel.Categories)
            {
                bool wasSelected = splitSelectedCategories.Contains(category.Name);
                bool isSelected = EditorGUILayout.ToggleLeft(category.Name, wasSelected);
                if (isSelected != wasSelected)
                {
                    if (isSelected)
                        splitSelectedCategories.Add(category.Name);
                    else
                        splitSelectedCategories.Remove(category.Name);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSplitPreview()
        {
            if (splitPlan == null)
            {
                EditorGUILayout.HelpBox("Click Refresh Preview to see the planned split.", MessageType.Info);
                return;
            }

            if (splitPlan.Count == 0)
            {
                EditorGUILayout.HelpBox("No categories match this split configuration.", MessageType.Info);
                return;
            }

            splitPreviewScroll = EditorGUILayout.BeginScrollView(splitPreviewScroll, GUILayout.Height(140));
            foreach (SplitGroup group in splitPlan)
                EditorGUILayout.LabelField($"{group.OutputName}.spriteLib: {string.Join(", ", group.Categories)}");
            EditorGUILayout.EndScrollView();
        }

        private void ApplySplit()
        {
            string targetPath = AssetDatabase.GetAssetPath(target);
            string folder = splitOutputFolderAsset != null
                ? AssetDatabase.GetAssetPath(splitOutputFolderAsset)
                : System.IO.Path.GetDirectoryName(targetPath)?.Replace('\\', '/') ?? "Assets";

            List<SpriteLibraryAsset> created = LibrarySplitService.Apply(target, splitPlan, folder, splitRemoveFromSource);

            if (splitRemoveFromSource)
                target = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(targetPath);

            InspectorRefresher.RefreshOpenInspectors();
            RefreshModel();
            ToolStatus.Set($"Split: created {created.Count} librar{(created.Count == 1 ? "y" : "ies")}.");
        }

        // ----- Duplicate Categories -----

        private void DrawDuplicateCategories()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to duplicate a category in.", MessageType.Info);
                return;
            }

            if (cachedModel.Categories.Count == 0)
            {
                EditorGUILayout.HelpBox("This library has no categories yet.", MessageType.Info);
                return;
            }

            string[] categoryNames = cachedModel.Categories.Select(c => c.Name).ToArray();
            duplicateCategoryIndex = Mathf.Clamp(duplicateCategoryIndex, 0, categoryNames.Length - 1);
            duplicateCategoryIndex = EditorGUILayout.Popup("Category", duplicateCategoryIndex, categoryNames);

            EditorGUILayout.LabelField("New Names (one per line)", EditorStyles.boldLabel);
            duplicateNewNames = EditorGUILayout.TextArea(duplicateNewNames, GUILayout.Height(80));

            List<string> newNames = duplicateNewNames
                .Split('\n')
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
            EditorGUILayout.LabelField($"{newNames.Count} new categor{(newNames.Count == 1 ? "y" : "ies")} will be created.");

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(newNames.Count == 0))
            {
                if (GUILayout.Button("Duplicate", GUILayout.Height(30)))
                {
                    CategoryModel source = cachedModel.Categories[duplicateCategoryIndex];
                    target = CategoryDuplicatorService.Duplicate(target, source, newNames);
                    InspectorRefresher.RefreshOpenInspectors();
                    string sourceName = source.Name;
                    RefreshModel();
                    ToolStatus.Set($"Duplicated '{sourceName}' into {newNames.Count} new categor{(newNames.Count == 1 ? "y" : "ies")}.");
                    duplicateNewNames = string.Empty;
                }
            }
        }

        // ----- Move Labels -----

        private void DrawMoveLabels()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to move labels in.", MessageType.Info);
                return;
            }

            if (cachedModel.Categories.Count == 0)
            {
                EditorGUILayout.HelpBox("This library has no categories yet.", MessageType.Info);
                return;
            }

            if (!SpriteLibrarySourceIO.IsSourceAsset(AssetDatabase.GetAssetPath(target)))
                EditorGUILayout.HelpBox("Moving labels is only supported for .spriteLib assets.", MessageType.Warning);

            string[] categoryNames = cachedModel.Categories.Select(c => c.Name).ToArray();
            moveFromCategoryIndex = Mathf.Clamp(moveFromCategoryIndex, 0, categoryNames.Length - 1);

            EditorGUI.BeginChangeCheck();
            moveFromCategoryIndex = EditorGUILayout.Popup("From Category", moveFromCategoryIndex, categoryNames);
            if (EditorGUI.EndChangeCheck())
                moveSelectedLabels.Clear();

            CategoryModel fromCategory = cachedModel.Categories[moveFromCategoryIndex];

            EditorGUILayout.LabelField("Labels", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box", GUILayout.Height(100));
            moveLabelScroll = EditorGUILayout.BeginScrollView(moveLabelScroll);

            foreach (LabelModel label in fromCategory.Labels)
            {
                bool wasSelected = moveSelectedLabels.Contains(label.Name);
                bool isSelected = EditorGUILayout.ToggleLeft(label.Name, wasSelected);
                if (isSelected != wasSelected)
                {
                    if (isSelected)
                        moveSelectedLabels.Add(label.Name);
                    else
                        moveSelectedLabels.Remove(label.Name);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            string[] toOptions = categoryNames.Append("(New Category)").ToArray();
            moveToCategoryIndex = Mathf.Clamp(moveToCategoryIndex, 0, toOptions.Length - 1);
            moveToCategoryIndex = EditorGUILayout.Popup("To Category", moveToCategoryIndex, toOptions);
            bool isNewCategory = moveToCategoryIndex == toOptions.Length - 1;
            if (isNewCategory)
                moveNewCategoryName = EditorGUILayout.TextField("New Category Name", moveNewCategoryName);

            string toCategoryName = isNewCategory ? moveNewCategoryName.Trim() : categoryNames[moveToCategoryIndex];

            EditorGUILayout.Space();
            bool canMove = moveSelectedLabels.Count > 0 && !string.IsNullOrEmpty(toCategoryName) && toCategoryName != fromCategory.Name;
            using (new EditorGUI.DisabledScope(!canMove))
            {
                if (GUILayout.Button("Move", GUILayout.Height(30)))
                {
                    var moves = moveSelectedLabels
                        .Select(label => (fromCategory: fromCategory.Name, toCategory: toCategoryName, label))
                        .ToList();

                    target = LabelMoverService.Move(target, moves);
                    InspectorRefresher.RefreshOpenInspectors();
                    int count = moveSelectedLabels.Count;
                    RefreshModel();
                    ToolStatus.Set($"Moved {count} label(s) to '{toCategoryName}'.");
                }
            }
        }

        // ----- Rename -----

        private void DrawRename()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to rename categories or labels in.", MessageType.Info);
                return;
            }

            if (cachedModel.Categories.Count == 0)
            {
                EditorGUILayout.HelpBox("This library has no categories yet.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Rename", EditorStyles.boldLabel);
            int targetTypeIndex = renameLabelsInsteadOfCategories ? 1 : 0;
            EditorGUI.BeginChangeCheck();
            targetTypeIndex = EditorGUILayout.Popup(targetTypeIndex, new[] { "Categories", "Labels" });
            if (EditorGUI.EndChangeCheck())
            {
                renameLabelsInsteadOfCategories = targetTypeIndex == 1;
                renamePlan = null;
            }

            string[] categoryNames = cachedModel.Categories.Select(c => c.Name).ToArray();
            List<string> namesToRename;

            if (renameLabelsInsteadOfCategories)
            {
                renameCategoryIndex = Mathf.Clamp(renameCategoryIndex, 0, categoryNames.Length - 1);
                EditorGUI.BeginChangeCheck();
                renameCategoryIndex = EditorGUILayout.Popup("Category", renameCategoryIndex, categoryNames);
                if (EditorGUI.EndChangeCheck())
                    renamePlan = null;
                namesToRename = cachedModel.Categories[renameCategoryIndex].Labels.Select(l => l.Name).ToList();
            }
            else
            {
                namesToRename = categoryNames.ToList();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel);
            renameMode = (RenameMode)EditorGUILayout.Popup((int)renameMode, new[] { "Search Replace", "Add Prefix", "Add Suffix", "Regex" });

            switch (renameMode)
            {
                case RenameMode.SearchReplace:
                    renameSearchText = EditorGUILayout.TextField("Search", renameSearchText);
                    renameReplaceText = EditorGUILayout.TextField("Replace", renameReplaceText);
                    break;
                case RenameMode.AddPrefix:
                    renamePrefixText = EditorGUILayout.TextField("Prefix", renamePrefixText);
                    break;
                case RenameMode.AddSuffix:
                    renameSuffixText = EditorGUILayout.TextField("Suffix", renameSuffixText);
                    break;
                case RenameMode.Regex:
                    renameRegexPattern = EditorGUILayout.TextField("Pattern", renameRegexPattern);
                    renameRegexReplacement = EditorGUILayout.TextField("Replacement", renameRegexReplacement);
                    break;
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh Preview"))
            {
                renamePlan = RenameService.BuildPlan(namesToRename, renameMode, renameSearchText, renameReplaceText,
                    renamePrefixText, renameSuffixText, renameRegexPattern, renameRegexReplacement, out renameError);
            }

            DrawRenamePreview();

            EditorGUILayout.Space();
            bool canApply = renamePlan != null && string.IsNullOrEmpty(renameError) && renamePlan.Any(p => p.WillChange);
            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("Rename", GUILayout.Height(30)))
                    ApplyRename();
            }
        }

        private void DrawRenamePreview()
        {
            if (!string.IsNullOrEmpty(renameError))
            {
                EditorGUILayout.HelpBox($"Invalid regex pattern: {renameError}", MessageType.Error);
                return;
            }

            if (renamePlan == null)
            {
                EditorGUILayout.HelpBox("Click Refresh Preview to see planned renames.", MessageType.Info);
                return;
            }

            renamePreviewScroll = EditorGUILayout.BeginScrollView(renamePreviewScroll, GUILayout.Height(140));
            foreach (RenamePlanEntry entry in renamePlan)
            {
                string line = entry.WillChange ? $"{entry.OldName} -> {entry.NewName}" : $"{entry.OldName} (unchanged)";
                EditorGUILayout.LabelField(line);
            }
            EditorGUILayout.EndScrollView();

            int changeCount = renamePlan.Count(p => p.WillChange);
            EditorGUILayout.LabelField($"{changeCount} of {renamePlan.Count} will be renamed.");
        }

        private void ApplyRename()
        {
            int changeCount = renamePlan.Count(p => p.WillChange);

            if (renameLabelsInsteadOfCategories)
            {
                string category = cachedModel.Categories[renameCategoryIndex].Name;
                target = RenameService.ApplyLabelRenames(target, category, renamePlan);
            }
            else
            {
                target = RenameService.ApplyCategoryRenames(target, renamePlan);
            }

            InspectorRefresher.RefreshOpenInspectors();
            RefreshModel();
            ToolStatus.Set($"Rename: {changeCount} renamed.");
        }

        // ----- Sort -----

        private void DrawSort()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to sort its categories.", MessageType.Info);
                return;
            }

            if (cachedModel.Categories.Count == 0)
            {
                EditorGUILayout.HelpBox("This library has no categories yet.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Sort Categories", EditorStyles.boldLabel);
            sortMode = (CategorySortMode)EditorGUILayout.Popup((int)sortMode, new[] { "Alphabetical", "Reverse", "Natural" });

            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh Preview"))
                sortPlan = CategorySortService.BuildOrder(cachedModel, sortMode);

            if (sortPlan == null)
            {
                EditorGUILayout.HelpBox("Click Refresh Preview to see the planned order.", MessageType.Info);
            }
            else
            {
                sortPreviewScroll = EditorGUILayout.BeginScrollView(sortPreviewScroll, GUILayout.Height(140));
                for (int i = 0; i < sortPlan.Count; i++)
                    EditorGUILayout.LabelField($"{i + 1}. {sortPlan[i]}");
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(sortPlan == null))
            {
                if (GUILayout.Button("Apply Order", GUILayout.Height(30)))
                {
                    target = CategorySortService.Apply(target, sortPlan);
                    InspectorRefresher.RefreshOpenInspectors();
                    RefreshModel();
                    ToolStatus.Set("Sort: category order updated.");
                }
            }
        }
    }
}
