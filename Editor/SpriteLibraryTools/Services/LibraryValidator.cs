using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    public static class LibraryValidator
    {
        public static List<ValidationIssue> Validate(SpriteLibraryModel model, SpriteLibraryAsset target)
        {
            var issues = new List<ValidationIssue>();
            if (model?.Categories == null)
                return issues;

            var spriteUsage = new Dictionary<Sprite, List<(string category, string label)>>();

            foreach (CategoryModel category in model.Categories)
            {
                if (string.IsNullOrWhiteSpace(category.Name))
                {
                    issues.Add(new ValidationIssue
                    {
                        Type = ValidationIssueType.InvalidName,
                        Category = category.Name,
                        Message = "A category name is empty or whitespace."
                    });
                }

                if (category.Labels.Count == 0)
                {
                    issues.Add(new ValidationIssue
                    {
                        Type = ValidationIssueType.EmptyCategory,
                        Category = category.Name,
                        Message = $"Category '{category.Name}' has no labels."
                    });
                }

                var seenLabelNames = new HashSet<string>();
                foreach (LabelModel label in category.Labels)
                {
                    if (string.IsNullOrWhiteSpace(label.Name))
                    {
                        issues.Add(new ValidationIssue
                        {
                            Type = ValidationIssueType.InvalidName,
                            Category = category.Name,
                            Label = label.Name,
                            Message = $"A label in category '{category.Name}' has an empty or whitespace name."
                        });
                    }
                    else if (!seenLabelNames.Add(label.Name))
                    {
                        issues.Add(new ValidationIssue
                        {
                            Type = ValidationIssueType.DuplicateLabel,
                            Category = category.Name,
                            Label = label.Name,
                            Message = $"Label '{label.Name}' appears more than once in category '{category.Name}'."
                        });
                    }

                    if (label.Sprite == null)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Type = ValidationIssueType.MissingReference,
                            Category = category.Name,
                            Label = label.Name,
                            Message = $"'{category.Name}/{label.Name}' has no Sprite assigned."
                        });
                        continue;
                    }

                    if (!spriteUsage.TryGetValue(label.Sprite, out List<(string, string)> usages))
                    {
                        usages = new List<(string, string)>();
                        spriteUsage[label.Sprite] = usages;
                    }
                    usages.Add((category.Name, label.Name));
                }
            }

            foreach (KeyValuePair<Sprite, List<(string category, string label)>> kvp in spriteUsage)
            {
                if (kvp.Value.Count <= 1)
                    continue;

                string locations = string.Join(", ", kvp.Value.Select(u => $"{u.category}/{u.label}"));
                issues.Add(new ValidationIssue
                {
                    Type = ValidationIssueType.DuplicateSprite,
                    Category = kvp.Value[0].category,
                    Label = kvp.Value[0].label,
                    Message = $"Sprite '{kvp.Key.name}' is used by {kvp.Value.Count} labels: {locations}."
                });
            }

            HashSet<string> usedCategories = FindUsedCategories(target);
            foreach (CategoryModel category in model.Categories)
            {
                if (!usedCategories.Contains(category.Name))
                {
                    issues.Add(new ValidationIssue
                    {
                        Type = ValidationIssueType.UnusedCategory,
                        Category = category.Name,
                        Message = $"No Sprite Resolver in open scenes or project prefabs references category '{category.Name}'."
                    });
                }
            }

            return issues;
        }

        // Scoped to currently loaded scenes and project prefabs; scenes that were never
        // opened this session aren't scanned (opening every scene on disk to check would be
        // slow and disruptive). Revisit with a fuller scan in the Phase 6 hardening pass.
        private static HashSet<string> FindUsedCategories(SpriteLibraryAsset target)
        {
            var used = new HashSet<string>();
            if (target == null)
                return used;

            foreach (SpriteResolver resolver in Resources.FindObjectsOfTypeAll<SpriteResolver>())
                CollectUsedCategory(resolver, target, used);

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                foreach (SpriteResolver resolver in prefab.GetComponentsInChildren<SpriteResolver>(true))
                    CollectUsedCategory(resolver, target, used);
            }

            return used;
        }

        private static void CollectUsedCategory(SpriteResolver resolver, SpriteLibraryAsset target, HashSet<string> used)
        {
            if (resolver.spriteLibrary == null || resolver.spriteLibrary.spriteLibraryAsset != target)
                return;

            string category = resolver.GetCategory();
            if (!string.IsNullOrEmpty(category))
                used.Add(category);
        }
    }
}
