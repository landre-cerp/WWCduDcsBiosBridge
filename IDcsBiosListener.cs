using DCS_BIOS.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McduDcsBiosBridge
{
    internal interface IDcsBiosListener : IDcsBiosConnectionListener , IDcsBiosDataListener, IDCSBIOSStringListener
    {
        public void Start();

        public void Stop();
    }
}
