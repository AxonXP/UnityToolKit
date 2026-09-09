using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    [ToolPage(1)]
    public sealed class AnimationPage : IToolPage
    {
        private enum SubTab
        {
            Generate,
            Preview
        }

        public string Title => "Animation Generator";

        private SubTab activeSubTab;
        private SpriteLibraryAsset target;
        private Rect targetDropRect;
        private SpriteLibraryModel cachedModel;

        // Generate state
        private readonly AnimationGeneratorOptions options = new AnimationGeneratorOptions();
        private DefaultAsset outputFolderAsset;
        private AnimationBatchScope batchScope;
        private string categoryFilter = string.Empty;
        private readonly HashSet<string> selectedCategories = new HashSet<string>();
        private Vector2 categoryListScroll;
        private List<AnimationGenerationResult> lastResults;
        private Vector2 resultsScroll;

        // Preview state
        private int previewCategoryIndex;
        private List<LabelModel> previewLabels;
        private int previewFrameIndex;
        private bool previewPlaying;
        private float previewFps = 12f;
        private bool previewLoop = true;
        private double previewLastFrameTime;

        public void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        public void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
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
                case SubTab.Generate:
                    DrawGenerate();
                    break;
                case SubTab.Preview:
                    DrawPreview();
                    break;
            }
        }

        private void RefreshModel()
        {
            cachedModel = target != null ? SpriteLibraryModel.FromAsset(target) : null;
            lastResults = null;
            selectedCategories.Clear();
            previewFrameIndex = 0;
            previewLabels = null;
        }

        private void DrawSubTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            activeSubTab = (SubTab)EditorGUILayout.Popup(
                (int)activeSubTab,
                new[] { "Generate", "Preview" },
                EditorStyles.toolbarPopup,
                GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        // ----- Generate -----

        private void DrawGenerate()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to generate animations from it.", MessageType.Info);
                return;
            }

            if (cachedModel.Categories.Count == 0)
            {
                EditorGUILayout.HelpBox("This library has no categories yet.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            outputFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder (optional)", outputFolderAsset, typeof(DefaultAsset), false);
            options.NameFormat = EditorGUILayout.TextField("Name Format", options.NameFormat);
            options.ResolverPath = EditorGUILayout.TextField("Sprite Resolver Path", options.ResolverPath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Clip Settings", EditorStyles.boldLabel);
            options.FrameRate = Mathf.Max(0.01f, EditorGUILayout.FloatField("Frame Rate", options.FrameRate));
            options.Loop = EditorGUILayout.ToggleLeft("Loop", options.Loop);
            options.Reverse = EditorGUILayout.ToggleLeft("Reverse", options.Reverse);
            options.PingPong = EditorGUILayout.ToggleLeft("Ping Pong", options.PingPong);
            options.OverwriteExisting = EditorGUILayout.ToggleLeft("Overwrite Existing", options.OverwriteExisting);
            options.GenerateAnimatorController = EditorGUILayout.ToggleLeft("Generate Animator Controller", options.GenerateAnimatorController);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);
            batchScope = (AnimationBatchScope)EditorGUILayout.Popup(
                (int)batchScope,
                new[] { "All Categories", "Selected Categories", "Filtered Categories" });

            if (batchScope == AnimationBatchScope.SelectedCategories)
                DrawCategoryChecklist();
            else if (batchScope == AnimationBatchScope.FilteredCategories)
                categoryFilter = EditorGUILayout.TextField("Filter", categoryFilter);

            List<CategoryModel> categoriesToProcess = ResolveCategoriesToProcess();
            EditorGUILayout.LabelField($"{categoriesToProcess.Count} categor{(categoriesToProcess.Count == 1 ? "y" : "ies")} will be processed.");

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(categoriesToProcess.Count == 0))
            {
                if (GUILayout.Button("Generate", GUILayout.Height(30)))
                    RunGenerate(categoriesToProcess);
            }

            DrawResults();
        }

        private List<CategoryModel> ResolveCategoriesToProcess()
        {
            if (cachedModel == null)
                return new List<CategoryModel>();

            switch (batchScope)
            {
                case AnimationBatchScope.SelectedCategories:
                    return cachedModel.Categories.Where(c => selectedCategories.Contains(c.Name)).ToList();

                case AnimationBatchScope.FilteredCategories:
                    return string.IsNullOrEmpty(categoryFilter)
                        ? cachedModel.Categories.ToList()
                        : cachedModel.Categories.Where(c => c.Name.IndexOf(categoryFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                default:
                    return cachedModel.Categories.ToList();
            }
        }

        private void DrawCategoryChecklist()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Height(100));
            categoryListScroll = EditorGUILayout.BeginScrollView(categoryListScroll);

            foreach (CategoryModel category in cachedModel.Categories)
            {
                bool wasSelected = selectedCategories.Contains(category.Name);
                bool isSelected = EditorGUILayout.ToggleLeft($"{category.Name} ({category.Labels.Count})", wasSelected);
                if (isSelected != wasSelected)
                {
                    if (isSelected)
                        selectedCategories.Add(category.Name);
                    else
                        selectedCategories.Remove(category.Name);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void RunGenerate(List<CategoryModel> categories)
        {
            options.OutputFolder = outputFolderAsset != null ? AssetDatabase.GetAssetPath(outputFolderAsset) : string.Empty;

            lastResults = AnimationClipGeneratorService.GenerateBatch(target, categories, options);

            int created = lastResults.Count(r => r.Status == AnimationGenerationStatus.Created);
            int updated = lastResults.Count(r => r.Status == AnimationGenerationStatus.Updated);
            int skipped = lastResults.Count(r => r.Status == AnimationGenerationStatus.Skipped);
            int errors = lastResults.Count(r => r.Status == AnimationGenerationStatus.Error);

            foreach (AnimationGenerationResult result in lastResults)
            {
                if (result.Status == AnimationGenerationStatus.Error)
                    Debug.LogWarning($"Sprite Library Tools: failed to generate animation for '{result.Category}': {result.Message}");
            }

            if (options.GenerateAnimatorController)
            {
                string controllerFolder = string.IsNullOrEmpty(options.OutputFolder)
                    ? System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(target))?.Replace('\\', '/') ?? "Assets"
                    : options.OutputFolder;
                string controllerPath = $"{controllerFolder}/{target.name}.controller";
                AnimatorControllerGeneratorService.CreateOrUpdate(controllerPath, lastResults);
            }

            ToolStatus.Set($"Animation Generator: {created} created, {updated} updated, {skipped} skipped, {errors} error(s).");
            Debug.Log($"Sprite Library Tools — Animation Generator: {created} created, {updated} updated, {skipped} skipped, {errors} error(s).");
        }

        private void DrawResults()
        {
            if (lastResults == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
            resultsScroll = EditorGUILayout.BeginScrollView(resultsScroll, GUILayout.Height(140));

            foreach (AnimationGenerationResult result in lastResults)
            {
                string icon = result.Status switch
                {
                    AnimationGenerationStatus.Created => "+",
                    AnimationGenerationStatus.Updated => "~",
                    AnimationGenerationStatus.Skipped => "-",
                    AnimationGenerationStatus.Error => "!",
                    _ => " "
                };
                string suffix = string.IsNullOrEmpty(result.Message) ? string.Empty : $" - {result.Message}";
                EditorGUILayout.LabelField($"[{icon}] {result.Category}: {result.Status}{suffix}");
            }

            EditorGUILayout.EndScrollView();
        }

        // ----- Preview -----
        // Scrubs through a category's sprites at the given FPS without needing a baked
        // AnimationClip, driven by EditorApplication.update and RepaintRequest.

        private void DrawPreview()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to preview its animations.", MessageType.Info);
                return;
            }

            if (cachedModel.Categories.Count == 0)
            {
                EditorGUILayout.HelpBox("This library has no categories yet.", MessageType.Info);
                return;
            }

            string[] categoryNames = cachedModel.Categories.Select(c => c.Name).ToArray();
            previewCategoryIndex = Mathf.Clamp(previewCategoryIndex, 0, categoryNames.Length - 1);

            EditorGUI.BeginChangeCheck();
            previewCategoryIndex = EditorGUILayout.Popup("Category", previewCategoryIndex, categoryNames);
            if (EditorGUI.EndChangeCheck())
            {
                previewFrameIndex = 0;
                previewLabels = null;
            }

            if (previewLabels == null)
            {
                previewLabels = cachedModel.Categories[previewCategoryIndex].Labels
                    .OrderBy(l => l.Name, NaturalStringComparer.Instance)
                    .ToList();
            }

            if (previewLabels.Count == 0)
            {
                EditorGUILayout.HelpBox("This category has no labels to preview.", MessageType.Info);
                return;
            }

            previewFrameIndex = Mathf.Clamp(previewFrameIndex, 0, previewLabels.Count - 1);
            LabelModel currentLabel = previewLabels[previewFrameIndex];

            EditorGUILayout.Space();

            Rect previewRect = GUILayoutUtility.GetRect(160, 160, GUILayout.ExpandWidth(false));
            previewRect.x = (EditorGUIUtility.currentViewWidth - previewRect.width) * 0.5f;
            Texture2D thumb = SpriteThumbnailCache.GetThumbnail(currentLabel.Sprite);
            if (thumb != null)
                GUI.DrawTexture(previewRect, thumb, ScaleMode.ScaleToFit);
            else
                GUI.Box(previewRect, GUIContent.none);
            if (SpriteThumbnailCache.IsLoading(currentLabel.Sprite))
                RepaintRequest.Request();

            EditorGUILayout.LabelField($"Frame {previewFrameIndex + 1}/{previewLabels.Count}: {currentLabel.Name}", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(previewPlaying ? "Pause" : "Play"))
            {
                previewPlaying = !previewPlaying;
                previewLastFrameTime = EditorApplication.timeSinceStartup;
            }
            if (GUILayout.Button("Stop"))
            {
                previewPlaying = false;
                previewFrameIndex = 0;
            }
            EditorGUILayout.EndHorizontal();

            previewFps = Mathf.Max(0.01f, EditorGUILayout.FloatField("FPS", previewFps));
            previewLoop = EditorGUILayout.ToggleLeft("Loop", previewLoop);
            previewFrameIndex = EditorGUILayout.IntSlider("Frame", previewFrameIndex, 0, previewLabels.Count - 1);
        }

        private void OnEditorUpdate()
        {
            if (!previewPlaying || previewLabels == null || previewLabels.Count == 0)
                return;

            double now = EditorApplication.timeSinceStartup;
            double frameDuration = 1.0 / Mathf.Max(previewFps, 0.01f);
            if (now - previewLastFrameTime < frameDuration)
                return;

            previewLastFrameTime = now;
            previewFrameIndex++;
            if (previewFrameIndex >= previewLabels.Count)
            {
                if (previewLoop)
                {
                    previewFrameIndex = 0;
                }
                else
                {
                    previewFrameIndex = previewLabels.Count - 1;
                    previewPlaying = false;
                }
            }

            RepaintRequest.Request();
        }
    }
}
