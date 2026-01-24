/// <summary>
/// User-configurable options for the application and aircraft-specific settings.
/// These options are persisted to useroptions.json.
/// </summary>
public class UserOptions
{
    /// <summary>
    /// Gets or sets whether the A10C display should be aligned to the bottom of the screen.
    /// </summary>
    public bool DisplayBottomAligned { get; set; }
    
    /// <summary>
    /// Gets or sets whether the A10C CMS should be displayed.
    /// </summary>
    public bool DisplayCMS { get; set; } 
    
    /// <summary>
    /// Gets or sets whether lighting management should be disabled (for SimApp Pro users).
    /// </summary>
    public bool DisableLightingManagement { get; set; }
    
    /// <summary>
    /// Gets or sets whether the CH-47F CDU display should switch with seat position (useful for single CDU setups).
    /// </summary>
    public bool Ch47CduSwitchWithSeat { get; set; }
    
    /// <summary>
    /// Gets or sets whether the bridge should automatically start when conditions are met.
    /// </summary>
    public bool AutoStart { get; set; }
    
    /// <summary>
    /// Gets or sets whether the application window should be minimized when the bridge starts.
    /// When enabled, the main window is automatically minimized after successful bridge startup.
    /// </summary>
    public bool MinimizeOnStart { get; set; }
}
