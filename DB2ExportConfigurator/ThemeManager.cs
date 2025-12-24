using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace DB2ExportConfigurator
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public class ThemeColors
    {
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
        public Color ControlBackColor { get; set; }
        public Color ControlForeColor { get; set; }
        public Color HeaderBackColor { get; set; }
        public Color HeaderForeColor { get; set; }
        public Color ButtonBackColor { get; set; }
        public Color ButtonForeColor { get; set; }
        public Color BorderColor { get; set; }
        public Color AccentColor { get; set; }
        public Color SuccessColor { get; set; }
        public Color ErrorColor { get; set; }
        public Color WarningColor { get; set; }
        public Color GridBackColor { get; set; }
        public Color GridForeColor { get; set; }
        public Color GridAlternateBackColor { get; set; }
        public Color GridHeaderBackColor { get; set; }
        public Color TabBackColor { get; set; }
        public Color PanelBackColor { get; set; }
    }

    public static class ThemeManager
    {
        private static AppTheme _currentTheme = AppTheme.Light;

        public static AppTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                _currentTheme = value;
                OnThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static event EventHandler? OnThemeChanged;

        public static ThemeColors LightTheme => new ThemeColors
        {
            BackColor = Color.FromArgb(240, 240, 240),
            ForeColor = Color.FromArgb(51, 51, 51),
            ControlBackColor = Color.White,
            ControlForeColor = Color.FromArgb(51, 51, 51),
            HeaderBackColor = Color.FromArgb(52, 73, 94),
            HeaderForeColor = Color.White,
            ButtonBackColor = Color.FromArgb(52, 152, 219),
            ButtonForeColor = Color.White,
            BorderColor = Color.FromArgb(189, 195, 199),
            AccentColor = Color.FromArgb(52, 152, 219),
            SuccessColor = Color.FromArgb(39, 174, 96),
            ErrorColor = Color.FromArgb(231, 76, 60),
            WarningColor = Color.FromArgb(243, 156, 18),
            GridBackColor = Color.White,
            GridForeColor = Color.FromArgb(51, 51, 51),
            GridAlternateBackColor = Color.FromArgb(245, 245, 245),
            GridHeaderBackColor = Color.FromArgb(52, 73, 94),
            TabBackColor = Color.FromArgb(236, 240, 241),
            PanelBackColor = Color.White
        };

        public static ThemeColors DarkTheme => new ThemeColors
        {
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(220, 220, 220),
            ControlBackColor = Color.FromArgb(45, 45, 48),
            ControlForeColor = Color.FromArgb(220, 220, 220),
            HeaderBackColor = Color.FromArgb(20, 20, 20),
            HeaderForeColor = Color.FromArgb(220, 220, 220),
            ButtonBackColor = Color.FromArgb(0, 122, 204),
            ButtonForeColor = Color.White,
            BorderColor = Color.FromArgb(60, 60, 60),
            AccentColor = Color.FromArgb(0, 122, 204),
            SuccessColor = Color.FromArgb(76, 175, 80),
            ErrorColor = Color.FromArgb(244, 67, 54),
            WarningColor = Color.FromArgb(255, 152, 0),
            GridBackColor = Color.FromArgb(45, 45, 48),
            GridForeColor = Color.FromArgb(220, 220, 220),
            GridAlternateBackColor = Color.FromArgb(38, 38, 40),
            GridHeaderBackColor = Color.FromArgb(20, 20, 20),
            TabBackColor = Color.FromArgb(37, 37, 38),
            PanelBackColor = Color.FromArgb(45, 45, 48)
        };

        public static ThemeColors GetCurrentTheme()
        {
            return _currentTheme == AppTheme.Light ? LightTheme : DarkTheme;
        }

        public static void ApplyTheme(Control control, ThemeColors? colors = null)
        {
            if (colors == null)
                colors = GetCurrentTheme();

            try
            {
                // Apply to the control itself
                if (control is Form form)
                {
                    form.BackColor = colors.BackColor;
                    form.ForeColor = colors.ForeColor;
                }
                else if (control is TabControl tabControl)
                {
                    tabControl.BackColor = colors.TabBackColor;
                    tabControl.ForeColor = colors.ForeColor;
                }
                else if (control is TabPage tabPage)
                {
                    tabPage.BackColor = colors.BackColor;
                    tabPage.ForeColor = colors.ForeColor;
                }
                else if (control is Panel panel)
                {
                    // Skip panels with custom colors (gradients, etc.)
                    if (panel.Tag?.ToString() != "skip-theme")
                    {
                        panel.BackColor = colors.PanelBackColor;
                        panel.ForeColor = colors.ForeColor;
                    }
                }
                else if (control is GroupBox groupBox)
                {
                    groupBox.BackColor = colors.BackColor;
                    groupBox.ForeColor = colors.ForeColor;
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = colors.ControlBackColor;
                    textBox.ForeColor = colors.ControlForeColor;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = colors.ControlBackColor;
                    comboBox.ForeColor = colors.ControlForeColor;
                    comboBox.FlatStyle = FlatStyle.Flat;
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.BackColor = colors.BackColor;
                    checkBox.ForeColor = colors.ForeColor;
                }
                else if (control is RadioButton radioButton)
                {
                    radioButton.BackColor = colors.BackColor;
                    radioButton.ForeColor = colors.ForeColor;
                }
                else if (control is Label label)
                {
                    // Skip labels with custom colors
                    if (label.Tag?.ToString() != "skip-theme")
                    {
                        if (label.BackColor != Color.Transparent)
                            label.BackColor = colors.BackColor;
                        label.ForeColor = colors.ForeColor;
                    }
                }
                else if (control is Button button)
                {
                    // Skip buttons with custom colors
                    if (button.Tag?.ToString() != "skip-theme")
                    {
                        button.BackColor = colors.ButtonBackColor;
                        button.ForeColor = colors.ButtonForeColor;
                        button.FlatStyle = FlatStyle.Flat;
                        button.FlatAppearance.BorderSize = 0;
                    }
                }
                else if (control is DataGridView grid)
                {
                    grid.BackgroundColor = colors.GridBackColor;
                    grid.ForeColor = colors.GridForeColor;
                    grid.DefaultCellStyle.BackColor = colors.GridBackColor;
                    grid.DefaultCellStyle.ForeColor = colors.GridForeColor;
                    grid.AlternatingRowsDefaultCellStyle.BackColor = colors.GridAlternateBackColor;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = colors.GridHeaderBackColor;
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = colors.HeaderForeColor;
                    grid.EnableHeadersVisualStyles = false;
                    grid.GridColor = colors.BorderColor;
                }
                else if (control is ListView listView)
                {
                    listView.BackColor = colors.GridBackColor;
                    listView.ForeColor = colors.GridForeColor;
                }
                else if (control is DateTimePicker dtp)
                {
                    dtp.BackColor = colors.ControlBackColor;
                    dtp.ForeColor = colors.ControlForeColor;
                }
                else if (control is NumericUpDown nud)
                {
                    nud.BackColor = colors.ControlBackColor;
                    nud.ForeColor = colors.ControlForeColor;
                }
                else if (control is RichTextBox richTextBox)
                {
                    richTextBox.BackColor = colors.ControlBackColor;
                    richTextBox.ForeColor = colors.ControlForeColor;
                }

                // Recursively apply to child controls
                foreach (Control child in control.Controls)
                {
                    ApplyTheme(child, colors);
                }
            }
            catch
            {
                // Ignore errors for controls that don't support certain properties
            }
        }

        public static void ToggleTheme()
        {
            CurrentTheme = CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
        }

        public static void SaveThemePreference()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Db2BackupConfigurator",
                    "theme.txt"
                );

                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
                File.WriteAllText(settingsPath, CurrentTheme.ToString());
            }
            catch
            {
                // Ignore save errors
            }
        }

        public static void LoadThemePreference()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Db2BackupConfigurator",
                    "theme.txt"
                );

                if (File.Exists(settingsPath))
                {
                    var theme = File.ReadAllText(settingsPath).Trim();
                    if (Enum.TryParse<AppTheme>(theme, out var parsedTheme))
                    {
                        CurrentTheme = parsedTheme;
                    }
                }
            }
            catch
            {
                // Ignore load errors, use default
            }
        }
    }
}
