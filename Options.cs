using System.CommandLine;

namespace WWCduDcsBiosBridge
{
    /// <summary>
    /// Command line options for the MCDU DCS-BIOS Bridge application.
    /// </summary>
    static class Options
    {
        /// <summary>
        /// Option to align display to bottom of the screen.
        /// </summary>
        public static Option<bool> DisplayBottomAligned = new("--bottom-aligned", "-ba")
        {
            Description = "Display is aligned to bottom of the screen",
        };

        /// <summary>
        /// Option to set the aircraft number.
        /// </summary>
        public static Option<int> AircraftNumber = new("--aircraft", "-a")
        {
            Description = "Set Aircraft number",
        };

        /// <summary>
        /// Option to display CMS information.
        /// </summary>
        public static Option<bool> DisplayCMS = new("--display-cms", "-cms")
        {
            Description = "Display CMS on the screen",
        };

        /// <summary>
        /// Option to link ch47 screen brightness
        /// </summary>
        public static Option<bool> CH47_LinkedBGBrightness = new("--ch47-linked-brightness", "-ch47lbg")
        {
            Description = "Link BG Brightness to other",
        };

        /// <summary>
        /// Disable Lighting management for people using SimApp pro.
        /// </summary>
        public static Option<bool> DisableLightingManagement = new("--disable-lighting-management", "-dlm")
        {
            Description = "Disable lighting management (for SimApp Pro users)",
        };

    }
}
