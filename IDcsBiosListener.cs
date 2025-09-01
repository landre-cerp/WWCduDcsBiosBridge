using DCS_BIOS.Interfaces;

namespace McduDcsBiosBridge
{
    internal interface IDcsBiosListener : IDcsBiosConnectionListener , IDcsBiosDataListener, IDCSBIOSStringListener
    {
        public void Start();

        public void Stop();
    }
}
