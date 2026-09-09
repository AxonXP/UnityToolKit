using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    public static class LibraryDiffService
    {
        public static List<DiffEntry> Compare(SpriteLibraryAsset libraryA, SpriteLibraryAsset libraryB)
        {
            var keysA = new HashSet<(string category, string label)>();
            var keysB = new HashSet<(string category, string label)>();

            if (libraryA != null)
            {
                foreach (string category in libraryA.GetCategoryNames())
                    foreach (string label in libraryA.GetCategoryLabelNames(category))
                        keysA.Add((category, label));
            }

            if (libraryB != null)
            {
                foreach (string category in libraryB.GetCategoryNames())
                    foreach (string label in libraryB.GetCategoryLabelNames(category))
                        keysB.Add((category, label));
            }

            var allKeys = new HashSet<(string category, string label)>(keysA);
            allKeys.UnionWith(keysB);

            var entries = new List<DiffEntry>();
            foreach ((string category, string label) key in allKeys)
            {
                bool inA = keysA.Contains(key);
                bool inB = keysB.Contains(key);
                Sprite spriteA = inA ? libraryA.GetSprite(key.category, key.label) : null;
                Sprite spriteB = inB ? libraryB.GetSprite(key.category, key.label) : null;

                DiffChangeType type;
                if (!inA)
                    type = DiffChangeType.Added;
                else if (!inB)
                    type = DiffChangeType.Removed;
                else if (spriteA != spriteB)
                    type = DiffChangeType.Modified;
                else
                    type = DiffChangeType.Unchanged;

                entries.Add(new DiffEntry
                {
                    Category = key.category,
                    Label = key.label,
                    Type = type,
                    SpriteA = spriteA,
                    SpriteB = spriteB
                });
            }

            return entries.OrderBy(e => e.Category).ThenBy(e => e.Label).ToList();
        }
    }
}
