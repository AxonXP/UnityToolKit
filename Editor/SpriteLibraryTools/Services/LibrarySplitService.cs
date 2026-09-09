using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    public enum SplitMode
    {
        SelectedCategories,
        CategoryPrefix,
        MaximumCategories
    }

    public static class LibrarySplitService
    {
        public static List<SplitGroup> BuildPlan(SpriteLibraryModel model, SplitMode mode, IEnumerable<string> selectedCategories, string prefixDelimiter, int maxCategoriesPerGroup)
        {
            var groups = new List<SplitGroup>();
            if (model == null)
                return groups;

            switch (mode)
            {
                case SplitMode.SelectedCategories:
                {
                    var selected = new HashSet<string>(selectedCategories);
                    List<string> categories = model.Categories.Select(c => c.Name).Where(selected.Contains).ToList();
                    if (categories.Count > 0)
                        groups.Add(new SplitGroup { OutputName = "Split", Categories = categories });
                    break;
                }

                case SplitMode.CategoryPrefix:
                {
                    string delimiter = string.IsNullOrEmpty(prefixDelimiter) ? "_" : prefixDelimiter;
                    var byPrefix = new Dictionary<string, List<string>>();
                    foreach (CategoryModel category in model.Categories)
                    {
                        int index = category.Name.IndexOf(delimiter, StringComparison.Ordinal);
                        string prefix = index > 0 ? category.Name.Substring(0, index) : category.Name;
                        if (!byPrefix.TryGetValue(prefix, out List<string> list))
                        {
                            list = new List<string>();
                            byPrefix[prefix] = list;
                        }
                        list.Add(category.Name);
                    }

                    foreach (KeyValuePair<string, List<string>> group in byPrefix)
                        groups.Add(new SplitGroup { OutputName = group.Key, Categories = group.Value });
                    break;
                }

                case SplitMode.MaximumCategories:
                {
                    List<string> allCategories = model.Categories.Select(c => c.Name).ToList();
                    int max = Mathf.Max(1, maxCategoriesPerGroup);
                    int groupIndex = 1;
                    for (int i = 0; i < allCategories.Count; i += max)
                    {
                        int count = Mathf.Min(max, allCategories.Count - i);
                        groups.Add(new SplitGroup
                        {
                            OutputName = $"Part{groupIndex}",
                            Categories = allCategories.GetRange(i, count)
                        });
                        groupIndex++;
                    }
                    break;
                }
            }

            return groups;
        }

        public static List<SpriteLibraryAsset> Apply(SpriteLibraryAsset source, List<SplitGroup> groups, string outputFolder, bool removeFromSource)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            var created = new List<SpriteLibraryAsset>();
            var allMovedCategories = new List<string>();

            foreach (SplitGroup group in groups)
            {
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{outputFolder}/{group.OutputName}.spriteLib");
                SpriteLibraryAsset newAsset;

                if (SpriteLibrarySourceAssetFactory.TryCreate(assetPath, out string failureReason))
                {
                    newAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(assetPath);
                }
                else
                {
                    string fallbackPath = System.IO.Path.ChangeExtension(assetPath, "asset");
                    Debug.LogWarning($"Sprite Library Tools: could not create a native .spriteLib asset for split group '{group.OutputName}' ({failureReason}). " +
                        $"Creating a plain Sprite Library Asset at '{fallbackPath}' instead.");
                    newAsset = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
                    AssetDatabase.CreateAsset(newAsset, fallbackPath);
                    Undo.RegisterCreatedObjectUndo(newAsset, "Split Sprite Library");
                }

                var writes = new List<SpriteLibrarySourceIO.SourceLabel>();
                foreach (string categoryName in group.Categories)
                {
                    foreach (string label in source.GetCategoryLabelNames(categoryName))
                        writes.Add(new SpriteLibrarySourceIO.SourceLabel(categoryName, label, source.GetSprite(categoryName, label)));
                }

                newAsset = SpriteLibrarySourceIO.ApplyWrites(newAsset, writes, "Split Sprite Library");
                created.Add(newAsset);
                allMovedCategories.AddRange(group.Categories);
            }

            if (removeFromSource && allMovedCategories.Count > 0)
            {
                if (SpriteLibrarySourceIO.IsSourceAsset(sourcePath))
                {
                    SpriteLibrarySourceIO.RemoveCategories(sourcePath, allMovedCategories);
                }
                else
                {
                    Debug.LogWarning("Sprite Library Tools: removing categories from a plain Sprite Library Asset after splitting isn't supported; the source library was left unchanged.");
                }
            }

            return created;
        }
    }
}
