using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    public enum NameMatchMode
    {
        ExactName,
        IgnorePrefix,
        IgnoreSuffix,
        Index
    }

    // Core logic for Library Builder Tool 2 (Batch Replace Sprites): swaps the Sprite
    // assigned to each existing label without touching category/label names.
    public static class BatchSpriteReplacerService
    {
        // globalSprites feed the selected NameMatchMode across every category (as before).
        // categoryScopedSprites are sprites the user pinned to a specific category (e.g. a
        // sprite sheet dragged in with a category chosen for it) - those always match
        // positionally within just that category, regardless of NameMatchMode, since the
        // whole point of pinning a sheet is that its sprite names won't line up with the
        // existing labels. Categories with a pin are excluded from the global pass so a
        // label isn't matched twice.
        public static List<ReplaceEntry> BuildPlan(
            SpriteLibraryAsset target,
            IReadOnlyList<ScannedSprite> globalSprites,
            IReadOnlyDictionary<string, List<ScannedSprite>> categoryScopedSprites,
            NameMatchMode mode,
            string delimiter)
        {
            var entries = new List<ReplaceEntry>();
            if (target == null)
                return entries;

            var scopedCategories = new HashSet<string>(categoryScopedSprites?.Keys ?? Enumerable.Empty<string>());

            if (categoryScopedSprites != null)
            {
                foreach (KeyValuePair<string, List<ScannedSprite>> group in categoryScopedSprites)
                    entries.AddRange(BuildCategoryScopedPlan(target, group.Key, group.Value));
            }

            entries.AddRange(mode == NameMatchMode.Index
                ? BuildIndexPlan(target, globalSprites, scopedCategories)
                : BuildNameMatchPlan(target, globalSprites, mode, delimiter, scopedCategories));

            return entries;
        }

        private static List<ReplaceEntry> BuildNameMatchPlan(SpriteLibraryAsset target, IReadOnlyList<ScannedSprite> sourceSprites, NameMatchMode mode, string delimiter, HashSet<string> excludedCategories)
        {
            var entries = new List<ReplaceEntry>();
            string effectiveDelimiter = string.IsNullOrEmpty(delimiter) ? "_" : delimiter;

            var candidatesByKey = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            foreach (ScannedSprite scanned in sourceSprites)
            {
                string key = MatchKey(scanned.Sprite.name, mode, effectiveDelimiter);
                if (!candidatesByKey.ContainsKey(key))
                    candidatesByKey[key] = scanned.Sprite;
            }

            foreach (string category in target.GetCategoryNames())
            {
                if (excludedCategories.Contains(category))
                    continue;

                foreach (string label in target.GetCategoryLabelNames(category))
                {
                    Sprite current = target.GetSprite(category, label);
                    if (current == null)
                        continue;

                    string key = MatchKey(current.name, mode, effectiveDelimiter);
                    bool matched = candidatesByKey.TryGetValue(key, out Sprite replacement) && replacement != current;

                    entries.Add(new ReplaceEntry
                    {
                        Category = category,
                        Label = label,
                        OldSprite = current,
                        NewSprite = matched ? replacement : null,
                        Matched = matched
                    });
                }
            }

            return entries;
        }

        // Ignores names entirely: the Nth existing label across the whole library (in
        // library order, skipping empty ones and any category handled by a category pin) is
        // paired with the Nth source sprite, ordered by natural sort of its name.
        private static List<ReplaceEntry> BuildIndexPlan(SpriteLibraryAsset target, IReadOnlyList<ScannedSprite> sourceSprites, HashSet<string> excludedCategories)
        {
            var entries = new List<ReplaceEntry>();

            List<Sprite> orderedSource = sourceSprites
                .Select(s => s.Sprite)
                .OrderBy(s => s.name, NaturalStringComparer.Instance)
                .ToList();

            int index = 0;
            foreach (string category in target.GetCategoryNames())
            {
                if (excludedCategories.Contains(category))
                    continue;

                foreach (string label in target.GetCategoryLabelNames(category))
                {
                    Sprite current = target.GetSprite(category, label);
                    if (current == null)
                        continue;

                    bool matched = index < orderedSource.Count;
                    Sprite replacement = matched ? orderedSource[index] : null;
                    matched = matched && replacement != current;

                    entries.Add(new ReplaceEntry
                    {
                        Category = category,
                        Label = label,
                        OldSprite = current,
                        NewSprite = matched ? replacement : null,
                        Matched = matched
                    });
                    index++;
                }
            }

            return entries;
        }

        // Same positional idea as BuildIndexPlan, but scoped to one category with its own
        // independent counter instead of sharing one running index across the whole library.
        private static List<ReplaceEntry> BuildCategoryScopedPlan(SpriteLibraryAsset target, string category, List<ScannedSprite> sprites)
        {
            var entries = new List<ReplaceEntry>();

            IEnumerable<string> labelNames = target.GetCategoryLabelNames(category);
            if (labelNames == null)
                return entries;

            List<Sprite> orderedSource = sprites
                .Select(s => s.Sprite)
                .OrderBy(s => s.name, NaturalStringComparer.Instance)
                .ToList();

            int index = 0;
            foreach (string label in labelNames)
            {
                Sprite current = target.GetSprite(category, label);
                if (current == null)
                    continue;

                bool matched = index < orderedSource.Count;
                Sprite replacement = matched ? orderedSource[index] : null;
                matched = matched && replacement != current;

                entries.Add(new ReplaceEntry
                {
                    Category = category,
                    Label = label,
                    OldSprite = current,
                    NewSprite = matched ? replacement : null,
                    Matched = matched
                });
                index++;
            }

            return entries;
        }

        private static string MatchKey(string spriteName, NameMatchMode mode, string delimiter)
        {
            switch (mode)
            {
                case NameMatchMode.IgnorePrefix:
                {
                    int index = spriteName.IndexOf(delimiter, StringComparison.Ordinal);
                    return index >= 0 ? spriteName.Substring(index + delimiter.Length) : spriteName;
                }
                case NameMatchMode.IgnoreSuffix:
                {
                    int index = spriteName.LastIndexOf(delimiter, StringComparison.Ordinal);
                    return index >= 0 ? spriteName.Substring(0, index) : spriteName;
                }
                default:
                    return spriteName;
            }
        }

        // Returns the (possibly reloaded) target: for .spriteLib assets, forcing a reimport
        // after writing can invalidate the previous runtime object reference.
        public static SpriteLibraryAsset Apply(SpriteLibraryAsset target, IEnumerable<ReplaceEntry> entries, string undoName)
        {
            IEnumerable<SpriteLibrarySourceIO.SourceLabel> writes = entries
                .Where(e => e.Matched)
                .Select(e => new SpriteLibrarySourceIO.SourceLabel(e.Category, e.Label, e.NewSprite));

            return SpriteLibrarySourceIO.ApplyWrites(target, writes, undoName);
        }
    }
}
