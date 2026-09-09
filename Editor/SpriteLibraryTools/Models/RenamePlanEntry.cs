namespace SpriteLibraryTools
{
    public sealed class RenamePlanEntry
    {
        public string OldName;
        public string NewName;

        public bool WillChange => !string.IsNullOrWhiteSpace(NewName) && NewName != OldName;
    }
}
