using System.Collections.Generic;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    // Merges Library B's categories/labels into Library A. Same-named categories combine
    // automatically; duplicate label handling reuses the Skip/Replace/Rename semantics from
    // Library Builder for consistency, and the resulting plan is applied the same way too
    // (LibraryBuilderService.Apply already handles both .spriteLib and legacy assets).
    public static class LibraryMergeService
    {
        public static List<BuildEntry> BuildPlan(SpriteLibraryAsset target, SpriteLibraryAsset source, DuplicateHandling duplicates)
        {
            var entries = new List<BuildEntry>();
            if (source == null)
                return entries;

            var existing = new Dictionary<string, HashSet<string>>();
            if (target != null)
            {
                foreach (string category in target.GetCategoryNames())
                    existing[category] = new HashSet<string>(target.GetCategoryLabelNames(category));
            }

            foreach (string category in source.GetCategoryNames())
            {
                if (!existing.TryGetValue(category, out HashSet<string> labelsInCategory))
                {
                    labelsInCategory = new HashSet<string>();
                    existing[category] = labelsInCategory;
                }

                foreach (string label in source.GetCategoryLabelNames(category))
                {
                    UnityEngine.Sprite sprite = source.GetSprite(category, label);
                    string finalLabel = label;
                    BuildEntryStatus status;

                    if (!labelsInCategory.Contains(label))
                    {
                        status = BuildEntryStatus.New;
                        labelsInCategory.Add(label);
                    }
                    else
                    {
                        switch (duplicates)
                        {
                            case DuplicateHandling.Replace:
                                status = BuildEntryStatus.WillReplaceDuplicate;
                                break;
                            case DuplicateHandling.Rename:
                                finalLabel = UniqueNameGenerator.MakeUnique(label, labelsInCategory);
                                labelsInCategory.Add(finalLabel);
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
                        Label = finalLabel,
                        Sprite = sprite,
                        Status = status
                    });
                }
            }

            return entries;
        }
    }
}
