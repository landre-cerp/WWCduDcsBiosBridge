using System.CommandLine;

namespace McduDcsBiosBridge
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
    }
}
