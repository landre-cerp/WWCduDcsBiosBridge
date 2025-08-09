using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McduDcsBiosBridge
{
    static class Options
    {
        public static Option<bool> DisplayBottomAligned = new("--bottom-aligned", "-ba")
        {
            Description = "Display is aligned to bottom of the screen ",
        };

        public static Option<int> AircraftNumber = new("--aircraft", "-a")
        {
            Description = "Set Aircraft number",
        };

        public static Option<bool> DisplayCMS = new("--display-cms", "-cms")
        {
            Description = "Display CMS on the",
        };
    }
}
