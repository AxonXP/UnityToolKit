using UnityEngine;

namespace SpriteLibraryTools
{
    public enum AnimationGenerationStatus
    {
        Created,
        Updated,
        Skipped,
        Error
    }

    public sealed class AnimationGenerationResult
    {
        public string Category;
        public AnimationGenerationStatus Status;
        public string Message;
        public string ClipPath;
        public AnimationClip Clip;
    }
}
