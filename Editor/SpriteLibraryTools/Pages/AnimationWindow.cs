using UnityEditor;

namespace SpriteLibraryTools
{
    public sealed class AnimationWindow : SingleToolPageWindow
    {
        [MenuItem("Tools/Sprite Library Tools/Animation Generator", priority = 102)]
        public static void Open()
        {
            OpenWindow<AnimationWindow>();
        }

        protected override IToolPage CreatePage() => new AnimationPage();
    }
}
