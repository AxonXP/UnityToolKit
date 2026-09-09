namespace SpriteLibraryTools
{
    public interface IToolPage
    {
        string Title { get; }
        void OnEnable();
        void OnDisable();
        void OnGUI();
    }
}
