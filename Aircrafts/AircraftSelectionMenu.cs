using McduDotNet;

namespace WWCduDcsBiosBridge.Aircrafts;

internal class AircraftSelectionMenu : IDisposable
{
    private readonly ICdu mcdu;
    private bool isActive;

    public event EventHandler<AircraftSelectedEventArgs>? AircraftSelected;

    public AircraftSelectionMenu(ICdu mcdu)
    {
        this.mcdu = mcdu;
    }

    public void Show()
    {
        if (isActive) return;
        
        DisplayMenu();
        AttachEventHandlers();
        isActive = true;
    }

    public void Hide()
    {
        if (!isActive) return;

        DetachEventHandlers();
        isActive = false;
    }

    private void DisplayMenu()
    {
        mcdu.Output.Clear().Green()
            .Line(0).Centered("DCSbios/WWCDU Bridge")
            .NewLine().Large().Yellow().Centered("by Cerppo")
            .White()
            .LeftLabel(2, SupportedAircrafts.A10C_Name)
            .RightLabel(2, SupportedAircrafts.AH64D_Name)
            .LeftLabel(3, SupportedAircrafts.FA18C_Name)
            .RightLabel(3, $"{SupportedAircrafts.CH47_Name} (PLT)")
            .LeftLabel(4, SupportedAircrafts.F15E_Name)
            .RightLabel(4, $"{SupportedAircrafts.CH47_Name} (CPLT)")
            .BottomLine().WriteLine("Close app to exit");
        mcdu.RefreshDisplay();
    }

    private void AttachEventHandlers() => mcdu.KeyDown += HandleKeyDown;
    private void DetachEventHandlers() => mcdu.KeyDown -= HandleKeyDown;

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        var selection = e.Key switch
        {
            Key.LineSelectLeft2 => new AircraftSelection(SupportedAircrafts.A10C, true),
            Key.LineSelectRight2 => new AircraftSelection(SupportedAircrafts.AH64D, true),
            Key.LineSelectLeft3 => new AircraftSelection(SupportedAircrafts.FA18C, true),
            Key.LineSelectRight3 => new AircraftSelection(SupportedAircrafts.CH47, true),
            Key.LineSelectLeft4 => new AircraftSelection(SupportedAircrafts.F15E, true),
            Key.LineSelectRight4 => new AircraftSelection(SupportedAircrafts.CH47, false),
            _ => null
        };

        if (selection != null)
        {
            Hide();
            AircraftSelected?.Invoke(this, new AircraftSelectedEventArgs(selection));
        }
    }

    public void Dispose() => Hide();
}

internal sealed record AircraftSelection(int AircraftId, bool IsPilot);

internal class AircraftSelectedEventArgs : EventArgs
{
    public AircraftSelection Selection { get; }

    public AircraftSelectedEventArgs(AircraftSelection selection)
    {
        Selection = selection;
    }
}