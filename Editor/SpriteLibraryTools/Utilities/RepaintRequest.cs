namespace SpriteLibraryTools
{
    // Lets a page ask the hosting window to repaint (e.g. while an AssetPreview thumbnail is
    // still loading asynchronously) without holding a direct window reference.
    public static class RepaintRequest
    {
        public static event System.Action Requested;

        public static void Request()
        {
            Requested?.Invoke();
        }
    }
}
