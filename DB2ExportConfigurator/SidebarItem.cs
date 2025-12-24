using System.Collections.Generic;
using System.Linq;

namespace DB2ExportConfigurator
{
    /// <summary>
    /// Represents a navigation item in the Fluent UI Sidebar
    /// Supports 2-level hierarchy (parent items with children)
    /// </summary>
    public class SidebarItem
    {
        /// <summary>
        /// Icon emoji or character (e.g., "🏠", "⚙️")
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Display text for the item
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Unique identifier for navigation routing
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Child items for expandable navigation groups
        /// </summary>
        public List<SidebarItem> Children { get; set; }

        /// <summary>
        /// Indicates if this item has child items
        /// </summary>
        public bool IsExpandable => Children != null && Children.Count > 0;

        /// <summary>
        /// Indicates if this item's children are currently expanded
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Reference to parent item (null for root items)
        /// </summary>
        public SidebarItem? Parent { get; set; }

        /// <summary>
        /// Creates a simple navigation item (leaf node)
        /// </summary>
        public SidebarItem(string icon, string text, string tag)
        {
            Icon = icon;
            Text = text;
            Tag = tag;
            Children = new List<SidebarItem>();
            IsExpanded = false;
        }

        /// <summary>
        /// Creates an expandable navigation group with child items
        /// </summary>
        public SidebarItem(string icon, string text, params (string text, string tag)[] children)
        {
            Icon = icon;
            Text = text;
            Tag = text.ToLower().Replace(" ", "-");
            Children = new List<SidebarItem>();
            IsExpanded = false;

            foreach (var (childText, childTag) in children)
            {
                var child = new SidebarItem("", childText, childTag)
                {
                    Parent = this
                };
                Children.Add(child);
            }
        }

        /// <summary>
        /// Adds a child item to this navigation group
        /// </summary>
        public void AddChild(SidebarItem child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Gets all child items recursively
        /// </summary>
        public IEnumerable<SidebarItem> GetAllChildren()
        {
            var result = new List<SidebarItem>();
            foreach (var child in Children)
            {
                result.Add(child);
                result.AddRange(child.GetAllChildren());
            }
            return result;
        }

        /// <summary>
        /// Finds an item by tag in this item and all children
        /// </summary>
        public SidebarItem? FindByTag(string tag)
        {
            if (Tag == tag) return this;

            foreach (var child in Children)
            {
                var found = child.FindByTag(tag);
                if (found != null) return found;
            }

            return null;
        }

        public override string ToString()
        {
            return $"{Icon} {Text} ({Tag})";
        }
    }
}
