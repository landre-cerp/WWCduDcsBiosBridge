using System.Windows;
using System.Windows.Controls;

namespace WWCduDcsBiosBridge.UI;

public partial class OptionsPanel : UserControl
{
    public event EventHandler? SettingsChanged;

    public OptionsPanel()
    {
        InitializeComponent();
    }

    private void CheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Notify parent that settings have changed
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
