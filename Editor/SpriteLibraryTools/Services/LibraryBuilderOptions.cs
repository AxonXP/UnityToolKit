namespace SpriteLibraryTools
{
    public enum CategoryCreationMode
    {
        FolderNames,
        SpriteNamePrefix,
        ExistingCategories
    }

    // Also reused (minus IndexNumber, which the UI never offers there) for Create From
    // Folder's Category Naming section, so category names support the same transforms.
    public enum LabelNamingMode
    {
        KeepOriginal,
        RemovePrefix,
        RemoveSuffix,
        SearchReplace,
        IndexNumber
    }

    public enum DuplicateHandling
    {
        Skip,
        Replace,
        Rename
    }

    public sealed class LibraryBuilderOptions
    {
        public CategoryCreationMode CategoryCreation = CategoryCreationMode.FolderNames;
        public string PrefixDelimiter = "_";

        public LabelNamingMode LabelNaming = LabelNamingMode.KeepOriginal;
        public string RemovePrefixText = string.Empty;
        public string RemoveSuffixText = string.Empty;
        public string SearchText = string.Empty;
        public string ReplaceText = string.Empty;
        public int IndexStart;

        // Create From Folder only: transforms applied to each subfolder's name after it's
        // resolved as a category. Left at KeepOriginal, this is a no-op for other tools.
        public LabelNamingMode CategoryNaming = LabelNamingMode.KeepOriginal;
        public string CategoryRemovePrefixText = string.Empty;
        public string CategoryRemoveSuffixText = string.Empty;
        public string CategorySearchText = string.Empty;
        public string CategoryReplaceText = string.Empty;

        public DuplicateHandling Duplicates = DuplicateHandling.Skip;

        public bool SortAlphabetical;
        public bool NaturalSort;
    }
}
