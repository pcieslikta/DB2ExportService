using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace DB2ExportConfigurator
{
    /// <summary>
    /// Fluent UI style collapsible sidebar navigation (Microsoft 365 style)
    /// </summary>
    public class FluentSidebar : Panel
    {
        // Constants
        private const int EXPANDED_WIDTH = 200;
        private const int COLLAPSED_WIDTH = 50;
        private const int ITEM_HEIGHT = 45;
        private const int CHILD_ITEM_HEIGHT = 38;
        private const int CHILD_INDENT = 15;
        private const int ANIMATION_INTERVAL = 10;
        private const int ANIMATION_STEPS = 20;
        private const int ACTIVE_INDICATOR_WIDTH = 3;
        private const int SEARCH_BOX_HEIGHT = 35;
        private const int SEARCH_BOX_MARGIN = 8;

        // State
        private bool _isExpanded = true;
        private int _animationStep = 0;
        private System.Windows.Forms.Timer? _animationTimer;
        private List<SidebarItem> _items = new List<SidebarItem>();
        private List<SidebarItem> _filteredItems = new List<SidebarItem>();
        private SidebarItem? _selectedItem;
        private SidebarItem? _hoveredItem;
        private Rectangle _activeIndicatorRect = Rectangle.Empty;
        private bool _isDarkMode = false;
        private string _searchQuery = "";
        private TextBox? _searchBox;

        // Events
        public event EventHandler<string>? NavigationChanged;
        public event EventHandler? SidebarToggled;

        // Properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    AnimateToggle();
                    SidebarToggled?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public string? SelectedTag => _selectedItem?.Tag;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                UpdateColors();
                Invalidate();
            }
        }

        // Colors
        private Color BackgroundColor => _isDarkMode ? Color.FromArgb(37, 36, 35) : Color.FromArgb(243, 242, 241);
        private Color TextColor => _isDarkMode ? Color.White : Color.FromArgb(50, 49, 48);
        private Color HoverColor => _isDarkMode ? Color.FromArgb(47, 46, 45) : Color.FromArgb(237, 235, 233);
        private Color ActiveColor => _isDarkMode ? Color.FromArgb(57, 56, 55) : Color.FromArgb(225, 223, 221);
        private Color AccentColor => Color.FromArgb(0, 120, 212); // Microsoft blue

        public FluentSidebar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Width = EXPANDED_WIDTH;
            this.Dock = DockStyle.Left;
            this.DoubleBuffered = true;
            this.BackColor = BackgroundColor;

            // Create search box
            InitializeSearchBox();

            // Mouse events
            this.MouseMove += FluentSidebar_MouseMove;
            this.MouseLeave += FluentSidebar_MouseLeave;
            this.MouseClick += FluentSidebar_MouseClick;

            // Paint
            this.Paint += FluentSidebar_Paint;
        }

        private void InitializeSearchBox()
        {
            _searchBox = new TextBox
            {
                Location = new Point(SEARCH_BOX_MARGIN, SEARCH_BOX_MARGIN),
                Size = new Size(EXPANDED_WIDTH - (SEARCH_BOX_MARGIN * 2), SEARCH_BOX_HEIGHT),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = _isDarkMode ? Color.FromArgb(47, 46, 45) : Color.White,
                ForeColor = _isDarkMode ? Color.White : Color.Black,
                Text = ""
            };

            // Placeholder text
            SetPlaceholder(_searchBox, "Szukaj...");

            // Text changed event
            _searchBox.TextChanged += (s, e) =>
            {
                if (_searchBox.Text == "Szukaj...")
                {
                    SetSearchQuery("");
                    return;
                }
                SetSearchQuery(_searchBox.Text);
            };

            this.Controls.Add(_searchBox);
        }

        private void SetPlaceholder(TextBox textBox, string placeholder)
        {
            textBox.Text = placeholder;
            textBox.ForeColor = Color.Gray;

            textBox.GotFocus += (s, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.ForeColor = _isDarkMode ? Color.White : Color.Black;
                }
            };

            textBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = Color.Gray;
                }
            };
        }

        public void SetSearchQuery(string query)
        {
            _searchQuery = query?.ToLower() ?? "";
            FilterItems();
            Invalidate();
        }

        public void SetItems(List<SidebarItem> items)
        {
            _items = items;
            FilterItems();

            // Select first item by default
            if (_items.Count > 0)
            {
                var firstSelectableItem = _items.First();
                if (firstSelectableItem.IsExpandable && firstSelectableItem.Children.Count > 0)
                {
                    firstSelectableItem.IsExpanded = true;
                    _selectedItem = firstSelectableItem.Children.First();
                }
                else
                {
                    _selectedItem = firstSelectableItem;
                }
            }

            Invalidate();
        }

        private void FilterItems()
        {
            if (string.IsNullOrEmpty(_searchQuery))
            {
                _filteredItems = new List<SidebarItem>(_items);
                return;
            }

            _filteredItems = new List<SidebarItem>();

            foreach (var item in _items)
            {
                // Check if parent or any child matches search
                bool parentMatches = item.Text.ToLower().Contains(_searchQuery);
                var matchingChildren = item.Children.Where(c => c.Text.ToLower().Contains(_searchQuery)).ToList();

                if (parentMatches || matchingChildren.Any())
                {
                    var filteredItem = new SidebarItem(item.Icon, item.Text, item.Tag)
                    {
                        IsExpanded = matchingChildren.Any() // Auto-expand if children match
                    };

                    // Add matching children
                    if (matchingChildren.Any())
                    {
                        foreach (var child in matchingChildren)
                        {
                            filteredItem.AddChild(new SidebarItem("", child.Text, child.Tag));
                        }
                    }
                    else if (parentMatches && !item.IsExpandable)
                    {
                        // Leaf item that matches
                        _filteredItems.Add(item);
                        continue;
                    }
                    else if (parentMatches)
                    {
                        // Parent matches, show all children
                        foreach (var child in item.Children)
                        {
                            filteredItem.AddChild(new SidebarItem("", child.Text, child.Tag));
                        }
                    }

                    if (filteredItem.Children.Any() || !item.IsExpandable)
                    {
                        _filteredItems.Add(filteredItem);
                    }
                }
            }
        }

        public void NavigateToTag(string tag)
        {
            foreach (var item in _items)
            {
                var found = item.FindByTag(tag);
                if (found != null)
                {
                    SelectItem(found);

                    // Expand parent if needed
                    if (found.Parent != null)
                    {
                        found.Parent.IsExpanded = true;
                    }

                    Invalidate();
                    return;
                }
            }
        }

        public void Toggle()
        {
            IsExpanded = !IsExpanded;
        }

        private void AnimateToggle()
        {
            if (_animationTimer != null && _animationTimer.Enabled)
            {
                _animationTimer.Stop();
            }

            _animationTimer = new System.Windows.Forms.Timer();
            _animationTimer.Interval = ANIMATION_INTERVAL;
            _animationStep = 0;

            _animationTimer.Tick += (s, e) =>
            {
                _animationStep++;

                int targetWidth = _isExpanded ? EXPANDED_WIDTH : COLLAPSED_WIDTH;
                int startWidth = _isExpanded ? COLLAPSED_WIDTH : EXPANDED_WIDTH;
                int diff = targetWidth - startWidth;

                // EaseInOut animation
                double progress = (double)_animationStep / ANIMATION_STEPS;
                progress = progress < 0.5
                    ? 2 * progress * progress
                    : 1 - Math.Pow(-2 * progress + 2, 2) / 2;

                this.Width = startWidth + (int)(diff * progress);

                if (_animationStep >= ANIMATION_STEPS)
                {
                    _animationTimer?.Stop();
                    this.Width = targetWidth;

                    // Show/hide search box
                    if (_searchBox != null)
                    {
                        _searchBox.Visible = _isExpanded;
                    }

                    Invalidate();
                }
            };

            // Hide search box immediately when collapsing
            if (!_isExpanded && _searchBox != null)
            {
                _searchBox.Visible = false;
            }

            _animationTimer.Start();
        }

        private void FluentSidebar_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Start below search box
            int currentY = _isExpanded ? SEARCH_BOX_HEIGHT + (SEARCH_BOX_MARGIN * 2) + 5 : 5;

            // Use filtered items for display
            var itemsToDisplay = string.IsNullOrEmpty(_searchQuery) ? _items : _filteredItems;

            foreach (var item in itemsToDisplay)
            {
                currentY = DrawItem(e.Graphics, item, currentY, 0);
            }

            // Draw active indicator
            if (!_activeIndicatorRect.IsEmpty)
            {
                using (var brush = new SolidBrush(AccentColor))
                {
                    e.Graphics.FillRectangle(brush, _activeIndicatorRect);
                }
            }

            // Draw "No results" message if filtered and empty
            if (!string.IsNullOrEmpty(_searchQuery) && _filteredItems.Count == 0 && _isExpanded)
            {
                using (var brush = new SolidBrush(Color.FromArgb(150, TextColor)))
                {
                    Font font = new Font("Segoe UI", 9F, FontStyle.Italic);
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Near
                    };
                    e.Graphics.DrawString("Brak wyników", font, brush, new RectangleF(0, 50, this.Width, 50), sf);
                }
            }
        }

        private int DrawItem(Graphics g, SidebarItem item, int y, int level)
        {
            bool isHovered = item == _hoveredItem;
            bool isActive = item == _selectedItem;
            int itemHeight = level == 0 ? ITEM_HEIGHT : CHILD_ITEM_HEIGHT;

            Rectangle itemRect = new Rectangle(0, y, this.Width, itemHeight);

            // Background
            if (isActive)
            {
                using (var brush = new SolidBrush(ActiveColor))
                {
                    g.FillRectangle(brush, itemRect);
                }

                // Active indicator
                _activeIndicatorRect = new Rectangle(0, y, ACTIVE_INDICATOR_WIDTH, itemHeight);
            }
            else if (isHovered)
            {
                using (var brush = new SolidBrush(HoverColor))
                {
                    g.FillRectangle(brush, itemRect);
                }
            }

            // Icon
            if (!string.IsNullOrEmpty(item.Icon))
            {
                Font iconFont = new Font("Segoe UI Emoji", 14F, FontStyle.Regular);
                using (var brush = new SolidBrush(isActive ? AccentColor : TextColor))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    // Wyśrodkuj ikonę w lewej części sidebar
                    Rectangle iconRect = new Rectangle(5, y, COLLAPSED_WIDTH - 10, itemHeight);
                    g.DrawString(item.Icon, iconFont, brush, iconRect, sf);
                }
            }

            // Text (only when expanded)
            if (_isExpanded)
            {
                Font textFont = new Font("Segoe UI", level == 0 ? 10F : 9.5F,
                    level == 0 ? FontStyle.Bold : FontStyle.Regular);

                using (var brush = new SolidBrush(isActive ? AccentColor : TextColor))
                {
                    int textX = COLLAPSED_WIDTH + (level * CHILD_INDENT);
                    Rectangle textRect = new Rectangle(textX, y, this.Width - textX - 55, itemHeight);
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    g.DrawString(item.Text, textFont, brush, textRect, sf);
                }

                // Keyboard hint (Ctrl+N) for leaf items
                if (!item.IsExpandable && level > 0)
                {
                    string shortcut = GetKeyboardShortcut(item.Tag);
                    if (!string.IsNullOrEmpty(shortcut))
                    {
                        Font hintFont = new Font("Segoe UI", 8F, FontStyle.Regular);
                        using (var brush = new SolidBrush(Color.FromArgb(150, TextColor)))
                        {
                            Rectangle hintRect = new Rectangle(this.Width - 50, y, 45, itemHeight);
                            StringFormat sf = new StringFormat
                            {
                                Alignment = StringAlignment.Far,
                                LineAlignment = StringAlignment.Center
                            };
                            g.DrawString(shortcut, hintFont, brush, hintRect, sf);
                        }
                    }
                }

                // Expand/collapse arrow for parent items
                if (item.IsExpandable)
                {
                    Font arrowFont = new Font("Segoe UI Symbol", 9F, FontStyle.Regular);
                    string arrow = item.IsExpanded ? "▼" : "▶";
                    using (var brush = new SolidBrush(TextColor))
                    {
                        Rectangle arrowRect = new Rectangle(this.Width - 25, y, 20, itemHeight);
                        StringFormat sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString(arrow, arrowFont, brush, arrowRect, sf);
                    }
                }
            }

            y += itemHeight;

            // Draw children if expanded
            if (item.IsExpanded && item.Children.Count > 0)
            {
                foreach (var child in item.Children)
                {
                    y = DrawItem(g, child, y, level + 1);
                }
            }

            return y;
        }

        private string GetKeyboardShortcut(string tag)
        {
            return tag switch
            {
                "dashboard" => "Ctrl+1",
                "trends" => "Ctrl+2",
                "basic" => "Ctrl+3",
                "paths" => "Ctrl+4",
                "timing" => "Ctrl+5",
                "email" => "Ctrl+6",
                "backup" => "Ctrl+7",
                "history" => "Ctrl+8",
                "logs" => "Ctrl+9",
                _ => ""
            };
        }

        private void FluentSidebar_MouseMove(object? sender, MouseEventArgs e)
        {
            var item = GetItemAtPoint(e.Location);

            if (item != _hoveredItem)
            {
                _hoveredItem = item;
                Invalidate();

                // Show tooltip when collapsed
                if (!_isExpanded && item != null)
                {
                    ToolTip tooltip = new ToolTip
                    {
                        InitialDelay = 500,
                        ReshowDelay = 100,
                        AutoPopDelay = 5000
                    };
                    tooltip.Show(item.Text, this, e.X + 10, e.Y, 3000);
                }
            }

            this.Cursor = item != null ? Cursors.Hand : Cursors.Default;
        }

        private void FluentSidebar_MouseLeave(object? sender, EventArgs e)
        {
            _hoveredItem = null;
            Invalidate();
            this.Cursor = Cursors.Default;
        }

        private void FluentSidebar_MouseClick(object? sender, MouseEventArgs e)
        {
            var item = GetItemAtPoint(e.Location);

            if (item != null)
            {
                if (item.IsExpandable)
                {
                    // Toggle expand/collapse
                    item.IsExpanded = !item.IsExpanded;
                    Invalidate();
                }
                else
                {
                    // Navigate to item
                    SelectItem(item);
                }
            }
        }

        private void SelectItem(SidebarItem item)
        {
            if (_selectedItem != item && !item.IsExpandable)
            {
                _selectedItem = item;
                Invalidate();
                NavigationChanged?.Invoke(this, item.Tag);
            }
        }

        private SidebarItem? GetItemAtPoint(Point location)
        {
            // Start below search box
            int currentY = _isExpanded ? SEARCH_BOX_HEIGHT + (SEARCH_BOX_MARGIN * 2) + 5 : 5;

            // Use filtered items (consistent with Paint)
            var itemsToDisplay = string.IsNullOrEmpty(_searchQuery) ? _items : _filteredItems;

            foreach (var item in itemsToDisplay)
            {
                var result = GetItemAtPointRecursive(item, location, ref currentY, 0);
                if (result != null)
                    return result;
            }

            return null;
        }

        private SidebarItem? GetItemAtPointRecursive(SidebarItem item, Point location, ref int y, int level)
        {
            int itemHeight = level == 0 ? ITEM_HEIGHT : CHILD_ITEM_HEIGHT;
            Rectangle itemRect = new Rectangle(0, y, this.Width, itemHeight);

            if (itemRect.Contains(location))
            {
                return item;
            }

            y += itemHeight;

            // Check children if expanded
            if (item.IsExpanded && item.Children.Count > 0)
            {
                foreach (var child in item.Children)
                {
                    var result = GetItemAtPointRecursive(child, location, ref y, level + 1);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        private void UpdateColors()
        {
            this.BackColor = BackgroundColor;

            // Update search box colors
            if (_searchBox != null)
            {
                _searchBox.BackColor = _isDarkMode ? Color.FromArgb(47, 46, 45) : Color.White;
                if (_searchBox.Text != "Szukaj...")
                {
                    _searchBox.ForeColor = _isDarkMode ? Color.White : Color.Black;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Stop();
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
