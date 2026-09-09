namespace SpriteLibraryTools
{
    public enum ValidationIssueType
    {
        DuplicateLabel,
        DuplicateSprite,
        MissingReference,
        EmptyCategory,
        InvalidName,
        UnusedCategory
    }

    public sealed class ValidationIssue
    {
        public ValidationIssueType Type;
        public string Category;
        public string Label;
        public string Message;
    }
}
