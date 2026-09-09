namespace SpriteLibraryTools
{
    public enum AnimationBatchScope
    {
        AllCategories,
        SelectedCategories,
        FilteredCategories
    }

    public sealed class AnimationGeneratorOptions
    {
        // Empty = same folder as the Sprite Library asset.
        public string OutputFolder = string.Empty;
        public string NameFormat = "{category}";
        // Relative path from the Animator root to the GameObject with the SpriteResolver
        // component; must match how the character's Animator is actually set up.
        public string ResolverPath = "Sprite";

        public float FrameRate = 12f;
        public bool Loop = true;
        public bool Reverse;
        public bool PingPong;
        public bool OverwriteExisting = true;
        public bool GenerateAnimatorController;
    }
}
