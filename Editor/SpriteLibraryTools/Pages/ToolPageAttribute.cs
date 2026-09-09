using System;

namespace SpriteLibraryTools
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ToolPageAttribute : Attribute
    {
        public int Order { get; }

        public ToolPageAttribute(int order)
        {
            Order = order;
        }
    }
}
