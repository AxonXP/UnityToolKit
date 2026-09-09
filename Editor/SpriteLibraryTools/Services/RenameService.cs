using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    public enum RenameMode
    {
        SearchReplace,
        AddPrefix,
        AddSuffix,
        Regex
    }

    // Batch-renames either every category, or every label within one category. Category and
    // label renames both only work on .spriteLib assets - the plain SpriteLibraryAsset public
    // API has no rename, only add/remove.
    public static class RenameService
    {
        public static List<RenamePlanEntry> BuildPlan(
            IEnumerable<string> names,
            RenameMode mode,
            string searchText,
            string replaceText,
            string prefixText,
            string suffixText,
            string regexPattern,
            string regexReplacement,
            out string error)
        {
            error = null;
            var plan = new List<RenamePlanEntry>();

            Regex regex = null;
            if (mode == RenameMode.Regex && !string.IsNullOrEmpty(regexPattern))
            {
                try
                {
                    regex = new Regex(regexPattern);
                }
                catch (System.ArgumentException ex)
                {
                    error = ex.Message;
                    return plan;
                }
            }

            foreach (string name in names)
            {
                string newName = mode switch
                {
                    RenameMode.SearchReplace => string.IsNullOrEmpty(searchText) ? name : name.Replace(searchText, replaceText ?? string.Empty),
                    RenameMode.AddPrefix => $"{prefixText}{name}",
                    RenameMode.AddSuffix => $"{name}{suffixText}",
                    RenameMode.Regex => regex != null ? regex.Replace(name, regexReplacement ?? string.Empty) : name,
                    _ => name
                };
                plan.Add(new RenamePlanEntry { OldName = name, NewName = newName });
            }

            return plan;
        }

        public static SpriteLibraryAsset ApplyCategoryRenames(SpriteLibraryAsset library, IEnumerable<RenamePlanEntry> plan)
        {
            string path = AssetDatabase.GetAssetPath(library);
            Dictionary<string, string> renames = plan
                .Where(p => p.WillChange)
                .ToDictionary(p => p.OldName, p => p.NewName);

            if (renames.Count == 0)
                return library;

            if (!SpriteLibrarySourceIO.IsSourceAsset(path))
            {
                Debug.LogWarning("Sprite Library Tools: renaming categories is only supported for .spriteLib assets.");
                return library;
            }

            SpriteLibrarySourceIO.RenameCategories(path, renames);
            return AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(path);
        }

        public static SpriteLibraryAsset ApplyLabelRenames(SpriteLibraryAsset library, string category, IEnumerable<RenamePlanEntry> plan)
        {
            string path = AssetDatabase.GetAssetPath(library);
            List<(string category, string oldLabel, string newLabel)> renames = plan
                .Where(p => p.WillChange)
                .Select(p => (category, p.OldName, p.NewName))
                .ToList();

            if (renames.Count == 0)
                return library;

            if (!SpriteLibrarySourceIO.IsSourceAsset(path))
            {
                Debug.LogWarning("Sprite Library Tools: renaming labels is only supported for .spriteLib assets.");
                return library;
            }

            SpriteLibrarySourceIO.RenameLabels(path, renames);
            return AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(path);
        }
    }
}
