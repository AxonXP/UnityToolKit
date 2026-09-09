using System.Collections.Generic;
using UnityEngine;

namespace SpriteLibraryTools
{
    public static class DependencyViewerService
    {
        public static List<(string category, string label)> FindReferences(SpriteLibraryModel model, Sprite sprite)
        {
            var results = new List<(string category, string label)>();
            if (sprite == null || model?.Categories == null)
                return results;

            foreach (CategoryModel category in model.Categories)
                foreach (LabelModel label in category.Labels)
                    if (label.Sprite == sprite)
                        results.Add((category.Name, label.Name));

            return results;
        }
    }
}
