using UnityEditor;

namespace SpriteLibraryTools
{
    public sealed class BuilderWindow : SingleToolPageWindow
    {
        [MenuItem("Tools/Sprite Library Tools/Library Builder", priority = 101)]
        public static void Open()
        {
            OpenWindow<BuilderWindow>();
        }

        protected override IToolPage CreatePage() => new BuilderPage();
    }
}
