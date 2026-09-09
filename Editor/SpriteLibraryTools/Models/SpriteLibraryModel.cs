using System.Collections.Generic;
using System.Linq;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    // Read-only snapshot of a SpriteLibraryAsset's categories/labels for UI binding.
    // Built only from the asset's public API so it stays valid across 2D Animation package versions.
    public sealed class SpriteLibraryModel
    {
        public SpriteLibraryAsset Asset { get; }
        public IReadOnlyList<CategoryModel> Categories { get; }

        private SpriteLibraryModel(SpriteLibraryAsset asset, IReadOnlyList<CategoryModel> categories)
        {
            Asset = asset;
            Categories = categories;
        }

        public static SpriteLibraryModel FromAsset(SpriteLibraryAsset asset)
        {
            if (asset == null)
                return new SpriteLibraryModel(null, new List<CategoryModel>());

            List<CategoryModel> categories = asset.GetCategoryNames()
                .Select(categoryName =>
                {
                    List<LabelModel> labels = asset.GetCategoryLabelNames(categoryName)
                        .Select(labelName => new LabelModel(labelName, asset.GetSprite(categoryName, labelName)))
                        .ToList();
                    return new CategoryModel(categoryName, labels);
                })
                .ToList();

            return new SpriteLibraryModel(asset, categories);
        }
    }
}
