using System.Collections.Generic;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    public static class CategoryDuplicatorService
    {
        public static SpriteLibraryAsset Duplicate(SpriteLibraryAsset library, CategoryModel sourceCategory, IEnumerable<string> newNames)
        {
            var writes = new List<SpriteLibrarySourceIO.SourceLabel>();
            foreach (string newName in newNames)
            {
                if (string.IsNullOrWhiteSpace(newName))
                    continue;

                foreach (LabelModel label in sourceCategory.Labels)
                    writes.Add(new SpriteLibrarySourceIO.SourceLabel(newName, label.Name, label.Sprite));
            }

            return SpriteLibrarySourceIO.ApplyWrites(library, writes, "Duplicate Category");
        }
    }
}
