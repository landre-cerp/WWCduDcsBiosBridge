using McduDotNet;
using System;

namespace WWCduDcsBiosBridge.Aircrafts;

internal interface IAircraftListenerFactory
{
    public AircraftListener CreateListener(int aircraftNumber, ICdu mcdu, UserOptions options, bool pilot);
}


internal class AircraftListenerFactory : IAircraftListenerFactory
{
    public AircraftListener CreateListener(
        int aircraftNumber, 
        ICdu mcdu, 
        UserOptions options, 
        bool pilot=false) =>
    
        aircraftNumber switch
        {
            SupportedAircrafts.A10C => new A10C_Listener(mcdu, options),
            SupportedAircrafts.AH64D => new AH64D_Listener(mcdu, options),
            SupportedAircrafts.FA18C => new FA18C_Listener(mcdu, options),
            SupportedAircrafts.CH47=> new CH47F_Listener(mcdu, options, pilot),
            SupportedAircrafts.F15E => new F15E_Listener(mcdu, options),
            _ => throw new NotSupportedException($"Aircraft {aircraftNumber} not supported")

        };

}