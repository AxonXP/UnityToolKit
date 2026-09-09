using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    // Core logic for Library Builder Tool 1 (Bulk Auto Fill) and Tool 3 (Create From Folder).
    // Pure planning (BuildPlan) is kept separate from writing (Apply) so pages can preview
    // changes cheaply and only rescan/replan when something actually changes.
    public static class LibraryBuilderService
    {
        public static List<BuildEntry> BuildPlan(IReadOnlyList<ScannedSprite> sprites, SpriteLibraryAsset target, LibraryBuilderOptions options)
        {
            var existing = new Dictionary<string, HashSet<string>>();
            if (target != null)
            {
                foreach (string category in target.GetCategoryNames())
                    existing[category] = new HashSet<string>(target.GetCategoryLabelNames(category));
            }

            List<string> existingCategoryNames = options.CategoryCreation == CategoryCreationMode.ExistingCategories
                ? existing.Keys.ToList()
                : null;

            // Categories are resolved up front because Index Number labeling needs each
            // sprite's rank within its category, which requires knowing every sprite's
            // category before any labels are assigned.
            var categories = new string[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
                categories[i] = ResolveCategory(sprites[i], options, existingCategoryNames);

            string[] labels = new string[sprites.Count];
            if (options.LabelNaming == LabelNamingMode.IndexNumber)
                AssignIndexLabels(sprites, categories, options, labels);
            else
                for (int i = 0; i < sprites.Count; i++)
                    labels[i] = ResolveLabel(sprites[i].Sprite.name, options);

            var entries = new List<BuildEntry>();

            for (int i = 0; i < sprites.Count; i++)
            {
                ScannedSprite scanned = sprites[i];
                string category = categories[i];
                if (category == null)
                {
                    entries.Add(new BuildEntry
                    {
                        Category = "(unmatched)",
                        Label = scanned.Sprite.name,
                        Sprite = scanned.Sprite,
                        Status = BuildEntryStatus.UnmatchedCategory
                    });
                    continue;
                }

                string label = labels[i];

                if (!existing.TryGetValue(category, out HashSet<string> labelsInCategory))
                {
                    labelsInCategory = new HashSet<string>();
                    existing[category] = labelsInCategory;
                }

                BuildEntryStatus status;
                if (!labelsInCategory.Contains(label))
                {
                    status = BuildEntryStatus.New;
                    labelsInCategory.Add(label);
                }
                else
                {
                    switch (options.Duplicates)
                    {
                        case DuplicateHandling.Replace:
                            status = BuildEntryStatus.WillReplaceDuplicate;
                            break;
                        case DuplicateHandling.Rename:
                            label = UniqueNameGenerator.MakeUnique(label, labelsInCategory);
                            labelsInCategory.Add(label);
                            status = BuildEntryStatus.RenamedDuplicate;
                            break;
                        default:
                            status = BuildEntryStatus.WillSkipDuplicate;
                            break;
                    }
                }

                entries.Add(new BuildEntry
                {
                    Category = category,
                    Label = label,
                    Sprite = scanned.Sprite,
                    Status = status
                });
            }

            if (options.SortAlphabetical)
            {
                IComparer<string> comparer = options.NaturalSort
                    ? (IComparer<string>)NaturalStringComparer.Instance
                    : System.StringComparer.OrdinalIgnoreCase;

                entries = entries
                    .OrderBy(e => e.Category, comparer)
                    .ThenBy(e => e.Label, comparer)
                    .ToList();
            }

            return entries;
        }

        private static string ResolveCategory(ScannedSprite scanned, LibraryBuilderOptions options, List<string> existingCategoryNames)
        {
            string rawCategory;
            switch (options.CategoryCreation)
            {
                case CategoryCreationMode.SpriteNamePrefix:
                {
                    string name = scanned.Sprite.name;
                    string delimiter = string.IsNullOrEmpty(options.PrefixDelimiter) ? "_" : options.PrefixDelimiter;
                    int index = name.IndexOf(delimiter, System.StringComparison.Ordinal);
                    rawCategory = index > 0 ? name.Substring(0, index) : name;
                    break;
                }

                case CategoryCreationMode.ExistingCategories:
                {
                    string name = scanned.Sprite.name;
                    rawCategory = existingCategoryNames
                        .Where(c => name.StartsWith(c, System.StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(c => c.Length)
                        .FirstOrDefault();
                    break;
                }

                default:
                    rawCategory = scanned.FolderName;
                    break;
            }

            if (rawCategory == null)
                return null;

            return ApplyNameTransform(rawCategory, options.CategoryNaming,
                options.CategoryRemovePrefixText, options.CategoryRemoveSuffixText,
                options.CategorySearchText, options.CategoryReplaceText);
        }

        private static string ResolveLabel(string spriteName, LibraryBuilderOptions options)
        {
            return ApplyNameTransform(spriteName, options.LabelNaming,
                options.RemovePrefixText, options.RemoveSuffixText,
                options.SearchText, options.ReplaceText);
        }

        // Shared by label naming and (Create From Folder's) category naming. IndexNumber is
        // handled separately by AssignIndexLabels since it needs cross-sprite context; here
        // it just falls through as a no-op, same as KeepOriginal.
        private static string ApplyNameTransform(string input, LabelNamingMode mode, string prefixText, string suffixText, string searchText, string replaceText)
        {
            switch (mode)
            {
                case LabelNamingMode.RemovePrefix:
                    return !string.IsNullOrEmpty(prefixText) && input.StartsWith(prefixText, System.StringComparison.Ordinal)
                        ? input.Substring(prefixText.Length)
                        : input;

                case LabelNamingMode.RemoveSuffix:
                    return !string.IsNullOrEmpty(suffixText) && input.EndsWith(suffixText, System.StringComparison.Ordinal)
                        ? input.Substring(0, input.Length - suffixText.Length)
                        : input;

                case LabelNamingMode.SearchReplace:
                    return !string.IsNullOrEmpty(searchText)
                        ? input.Replace(searchText, replaceText ?? string.Empty)
                        : input;

                default:
                    return input;
            }
        }

        // Numbers sprites per category, ordered by natural sort of their original name, so
        // "Idle_2" ranks before "Idle_10" instead of after.
        private static void AssignIndexLabels(IReadOnlyList<ScannedSprite> sprites, string[] categories, LibraryBuilderOptions options, string[] labels)
        {
            var indicesByCategory = new Dictionary<string, List<int>>();
            for (int i = 0; i < sprites.Count; i++)
            {
                if (categories[i] == null)
                    continue;

                if (!indicesByCategory.TryGetValue(categories[i], out List<int> indices))
                {
                    indices = new List<int>();
                    indicesByCategory[categories[i]] = indices;
                }
                indices.Add(i);
            }

            foreach (List<int> indices in indicesByCategory.Values)
            {
                List<int> ordered = indices
                    .OrderBy(i => sprites[i].Sprite.name, NaturalStringComparer.Instance)
                    .ToList();

                for (int rank = 0; rank < ordered.Count; rank++)
                    labels[ordered[rank]] = (options.IndexStart + rank).ToString();
            }
        }

        // Returns the (possibly reloaded) target: for .spriteLib assets, forcing a reimport
        // after writing can invalidate the previous runtime object reference.
        public static SpriteLibraryAsset Apply(SpriteLibraryAsset target, IEnumerable<BuildEntry> entries, string undoName)
        {
            IEnumerable<SpriteLibrarySourceIO.SourceLabel> writes = entries
                .Where(e => e.WillApply)
                .Select(e => new SpriteLibrarySourceIO.SourceLabel(e.Category, e.Label, e.Sprite));

            return SpriteLibrarySourceIO.ApplyWrites(target, writes, undoName);
        }

        public static SpriteLibraryAsset CreateLibraryFromFolder(string rootFolderPath, string assetSavePath, LibraryBuilderOptions namingOptions)
        {
            SpriteLibraryAsset asset;

            if (SpriteLibrarySourceAssetFactory.TryCreate(assetSavePath, out string failureReason))
            {
                asset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(assetSavePath);
            }
            else
            {
                // A .spriteLib-named file must be parseable by the SpriteLibrarySourceAssetImporter;
                // a plain ScriptableObject asset has to use the conventional .asset extension instead.
                string fallbackPath = System.IO.Path.ChangeExtension(assetSavePath, "asset");
                Debug.LogWarning($"Sprite Library Tools: could not create a native .spriteLib asset ({failureReason}). " +
                    $"Creating a plain Sprite Library Asset at '{fallbackPath}' instead.");
                asset = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
                AssetDatabase.CreateAsset(asset, fallbackPath);
                Undo.RegisterCreatedObjectUndo(asset, "Create Sprite Library");
            }

            List<ScannedSprite> sprites = SpriteAssetScanner.ScanPaths(new[] { rootFolderPath });

            // Category creation strategy and duplicate handling are fixed for this tool;
            // only the naming transforms are user-configurable.
            var options = new LibraryBuilderOptions
            {
                CategoryCreation = CategoryCreationMode.FolderNames,
                Duplicates = DuplicateHandling.Rename,
                LabelNaming = namingOptions.LabelNaming,
                RemovePrefixText = namingOptions.RemovePrefixText,
                RemoveSuffixText = namingOptions.RemoveSuffixText,
                SearchText = namingOptions.SearchText,
                ReplaceText = namingOptions.ReplaceText,
                IndexStart = namingOptions.IndexStart,
                CategoryNaming = namingOptions.CategoryNaming,
                CategoryRemovePrefixText = namingOptions.CategoryRemovePrefixText,
                CategoryRemoveSuffixText = namingOptions.CategoryRemoveSuffixText,
                CategorySearchText = namingOptions.CategorySearchText,
                CategoryReplaceText = namingOptions.CategoryReplaceText
            };

            List<BuildEntry> plan = BuildPlan(sprites, asset, options);
            return Apply(asset, plan, "Create Sprite Library From Folder");
        }
    }
}
