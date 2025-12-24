using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DB2ExportConfigurator
{
    /// <summary>
    /// Helper class for Windows 11 theme integration including title bar dark mode
    /// </summary>
    public static class Windows11ThemeHelper
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        /// <summary>
        /// Applies Windows 11 dark or light mode to the window title bar
        /// </summary>
        /// <param name="form">The form to apply the theme to</param>
        /// <param name="isDarkMode">True for dark mode, false for light mode</param>
        public static void UseImmersiveDarkMode(Form form, bool isDarkMode)
        {
            if (form.Handle == IntPtr.Zero)
                return;

            try
            {
                if (IsWindows10OrGreater(17763))
                {
                    var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;

                    if (IsWindows10OrGreater(18985))
                    {
                        attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                    }

                    int useImmersiveDarkMode = isDarkMode ? 1 : 0;
                    DwmSetWindowAttribute(form.Handle, attribute, ref useImmersiveDarkMode, sizeof(int));

                    // For Windows 11 build 22000+, set explicit caption and text colors
                    if (IsWindows11OrGreater())
                    {
                        // Set caption background color
                        int captionColor = isDarkMode ? 0x00202020 : 0x00FFFFFF; // Dark gray or white
                        DwmSetWindowAttribute(form.Handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

                        // Set text color
                        int textColor = isDarkMode ? 0x00FFFFFF : 0x00000000; // White or black
                        DwmSetWindowAttribute(form.Handle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
                    }
                }
            }
            catch
            {
                // Ignore errors on older Windows versions or if API call fails
            }
        }

        /// <summary>
        /// Checks if the current Windows version is greater than or equal to the specified build number
        /// </summary>
        private static bool IsWindows10OrGreater(int build = -1)
        {
            try
            {
                var version = Environment.OSVersion.Version;

                // Windows 10 is version 10.0
                if (version.Major < 10)
                    return false;

                if (version.Major > 10)
                    return true;

                // If build number is specified, check it
                if (build > 0)
                    return version.Build >= build;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if running on Windows 11 or greater (build 22000+)
        /// </summary>
        private static bool IsWindows11OrGreater()
        {
            try
            {
                var version = Environment.OSVersion.Version;
                return version.Major >= 10 && version.Build >= 22000;
            }
            catch
            {
                return false;
            }
        }
    }
}
