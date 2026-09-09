using UnityEngine;

namespace SpriteLibraryTools
{
    public enum DiffChangeType
    {
        Added,
        Removed,
        Modified,
        Unchanged
    }

    public sealed class DiffEntry
    {
        public string Category;
        public string Label;
        public DiffChangeType Type;
        public Sprite SpriteA;
        public Sprite SpriteB;
    }
}
