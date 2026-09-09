using System.Collections.Generic;

namespace SpriteLibraryTools
{
    public sealed class CategoryModel
    {
        public string Name { get; }
        public IReadOnlyList<LabelModel> Labels { get; }

        public CategoryModel(string name, IReadOnlyList<LabelModel> labels)
        {
            Name = name;
            Labels = labels;
        }
    }
}
