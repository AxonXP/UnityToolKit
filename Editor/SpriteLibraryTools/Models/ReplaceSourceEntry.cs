namespace SpriteLibraryTools
{
    public sealed class ReplaceSourceEntry
    {
        public string Path;
        // Null/empty = no override: this source's sprites join the global name/index
        // matching pool. Set = its sprites replace only this category's labels, in order,
        // regardless of the selected Matching mode.
        public string CategoryOverride;
    }
}
