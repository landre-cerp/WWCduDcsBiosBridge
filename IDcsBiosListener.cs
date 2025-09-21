using DCS_BIOS.Interfaces;

namespace WWCduDcsBiosBridge;

internal interface IDcsBiosListener : IDcsBiosConnectionListener , IDcsBiosDataListener, IDCSBIOSStringListener
{
    public void Start();

    public void Stop();
}
