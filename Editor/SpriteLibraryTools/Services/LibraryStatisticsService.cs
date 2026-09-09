using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteLibraryTools
{
    public static class LibraryStatisticsService
    {
        public static LibraryStatistics Compute(SpriteLibraryModel model)
        {
            var stats = new LibraryStatistics();
            if (model?.Categories == null)
                return stats;

            var spriteCounts = new Dictionary<Sprite, int>();

            foreach (CategoryModel category in model.Categories)
            {
                stats.CategoryCount++;
                if (category.Labels.Count == 0)
                    stats.EmptyCategoryCount++;

                foreach (LabelModel label in category.Labels)
                {
                    stats.LabelCount++;
                    if (label.Sprite == null)
                    {
                        stats.MissingSpriteCount++;
                        continue;
                    }

                    spriteCounts.TryGetValue(label.Sprite, out int count);
                    spriteCounts[label.Sprite] = count + 1;
                }
            }

            stats.UniqueSpriteCount = spriteCounts.Count;
            stats.DuplicateSpriteCount = spriteCounts.Count(kvp => kvp.Value > 1);
            return stats;
        }
    }
}
