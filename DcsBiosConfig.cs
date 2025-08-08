using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McduDcsBiosBridge
{
    public class DcsBiosConfig
    {
            public string ReceiveFromIpUdp { get; set; } = "239.255.50.10";
            public string SendToIpUdp { get; set; } = "127.0.0.1";
            public int ReceivePortUdp { get; set; } = 5010;
            public int SendPortUdp { get; set; } = 7778;

            public string dcsBiosJsonLocation { get; set; } = "D:\\Saved Games\\DCS\\Scripts\\DCS-BIOS\\doc\\json";

    }
}
