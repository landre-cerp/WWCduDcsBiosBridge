﻿
namespace WWCduDcsBiosBridge.Aircrafts;

internal static class SupportedAircrafts
{
    public const int A10C = 5;
    public const string A10C_Name = "A-10C";

    public const int AH64D = 46;
    public const string AH64D_Name = "AH-64D";

    public const int FA18C = 20;
    public const string FA18C_Name = "F/A-18C";

    public const int CH47 = 50;
    public const string CH47_Name = "CH-47F";

    public const int F15E = 44;
    public const string F15E_Name = "F-15E";

    public static readonly string[] expected_json = { "A-10C.json", "AH-64D.json", "FA-18C_hornet.json", "CH-47F.json", "F-15E.json" };

}
