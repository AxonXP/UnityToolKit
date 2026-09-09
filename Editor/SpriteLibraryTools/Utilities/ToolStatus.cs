namespace SpriteLibraryTools
{
    // Lets any page report a message to the window's status bar without holding a window reference.
    public static class ToolStatus
    {
        public static string Message { get; private set; } = string.Empty;

        public static void Set(string message)
        {
            Message = message;
        }

        public static void Clear()
        {
            Message = string.Empty;
        }
    }
}
