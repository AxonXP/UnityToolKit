using UnityEngine;

namespace SpriteLibraryTools
{
    public sealed class LabelModel
    {
        public string Name { get; }
        public Sprite Sprite { get; }

        public LabelModel(string name, Sprite sprite)
        {
            Name = name;
            Sprite = sprite;
        }
    }
}
