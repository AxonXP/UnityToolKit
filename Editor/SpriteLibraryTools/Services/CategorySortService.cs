using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    public enum CategorySortMode
    {
        Alphabetical,
        Reverse,
        Natural
    }

    public static class CategorySortService
    {
        public static List<string> BuildOrder(SpriteLibraryModel model, CategorySortMode mode)
        {
            List<string> names = model.Categories.Select(c => c.Name).ToList();
            switch (mode)
            {
                case CategorySortMode.Alphabetical:
                    names.Sort(StringComparer.OrdinalIgnoreCase);
                    break;
                case CategorySortMode.Reverse:
                    names.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(b, a));
                    break;
                case CategorySortMode.Natural:
                    names.Sort(NaturalStringComparer.Instance.Compare);
                    break;
            }
            return names;
        }

        // Only .spriteLib assets support reordering; the plain SpriteLibraryAsset public API
        // has no way to move an existing category to a different index.
        public static SpriteLibraryAsset Apply(SpriteLibraryAsset library, List<string> orderedNames)
        {
            string path = AssetDatabase.GetAssetPath(library);
            if (!SpriteLibrarySourceIO.IsSourceAsset(path))
            {
                Debug.LogWarning("Sprite Library Tools: reordering categories is only supported for .spriteLib assets.");
                return library;
            }

            SpriteLibrarySourceIO.ReorderCategories(path, orderedNames);
            return AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(path);
        }
    }
}
