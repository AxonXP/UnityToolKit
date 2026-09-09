using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpriteLibraryTools
{
    // Pages register themselves with [ToolPage(order)]; nothing here needs to change
    // when a new page is added, satisfying the PRD's future-proofing requirement.
    public static class ToolPageRegistry
    {
        public static IReadOnlyList<IToolPage> CreatePages()
        {
            var pages = new List<(int order, IToolPage page)>();

            foreach (Type type in typeof(ToolPageRegistry).Assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(IToolPage).IsAssignableFrom(type))
                    continue;

                var attribute = type.GetCustomAttribute<ToolPageAttribute>();
                if (attribute == null)
                    continue;

                if (Activator.CreateInstance(type) is IToolPage page)
                    pages.Add((attribute.Order, page));
            }

            return pages.OrderBy(p => p.order).Select(p => p.page).ToList();
        }
    }
}
