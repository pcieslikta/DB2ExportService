using System.Drawing;

namespace DB2ExportConfigurator
{
    /// <summary>
    /// Renderer for vehicle icons (Y/N → ✓/✗ Unicode characters)
    /// Supports Dark/Light mode with appropriate colors
    /// </summary>
    public static class VehicleIconRenderer
    {
        // Unicode icons
        public const string ICON_CHECK = "✓";      // U+2713 Check mark
        public const string ICON_CROSS = "✗";      // U+2717 Ballot X
        public const string ICON_UNKNOWN = "—";    // U+2014 Em dash

        /// <summary>
        /// Converts MA_BRAMKI value (Y/N/1/0) to icon
        /// </summary>
        public static string GetGatesIcon(string? maBramki)
        {
            return (maBramki?.ToUpper()) switch
            {
                "Y" => ICON_CHECK,
                "1" => ICON_CHECK,
                "N" => ICON_CROSS,
                "0" => ICON_CROSS,
                _ => ICON_UNKNOWN
            };
        }

        /// <summary>
        /// Converts WGOTOWOSCI value (Y/N/1/0) to icon
        /// </summary>
        public static string GetActiveIcon(string? wGotowosci)
        {
            return (wGotowosci?.ToUpper()) switch
            {
                "Y" => ICON_CHECK,
                "1" => ICON_CHECK,
                "N" => ICON_CROSS,
                "0" => ICON_CROSS,
                _ => ICON_UNKNOWN
            };
        }

        /// <summary>
        /// Gets color for gates icon based on value and theme
        /// </summary>
        public static Color GetGatesColor(string? maBramki, bool isDarkMode)
        {
            bool hasGates = maBramki == "Y" || maBramki == "1";

            if (hasGates)
            {
                return isDarkMode
                    ? Color.FromArgb(76, 175, 80)   // Light Green (Dark mode)
                    : Color.FromArgb(39, 174, 96);  // Green (Light mode)
            }
            else
            {
                return isDarkMode
                    ? Color.FromArgb(180, 180, 180) // Light Gray (Dark mode)
                    : Color.FromArgb(149, 165, 166); // Gray (Light mode)
            }
        }

        /// <summary>
        /// Gets color for active icon based on value and theme
        /// </summary>
        public static Color GetActiveColor(string? wGotowosci, bool isDarkMode)
        {
            bool isActive = wGotowosci == "Y" || wGotowosci == "1";

            if (isActive)
            {
                return isDarkMode
                    ? Color.FromArgb(76, 175, 80)   // Light Green (Dark mode)
                    : Color.FromArgb(39, 174, 96);  // Green (Light mode)
            }
            else
            {
                return isDarkMode
                    ? Color.FromArgb(244, 67, 54)    // Red (Dark mode)
                    : Color.FromArgb(231, 76, 60);   // Red (Light mode)
            }
        }
    }
}
