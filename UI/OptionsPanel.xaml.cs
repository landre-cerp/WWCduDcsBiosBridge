using System.Windows;
using System.Windows.Controls;

namespace WWCduDcsBiosBridge.UI;

public partial class OptionsPanel : UserControl
{
    private bool _isInitializing = true;
    public event EventHandler? SettingsChanged;

    public OptionsPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // When DataContext is set, prevent CheckBox_Changed from firing during binding initialization
        _isInitializing = true;
        
        // Use Dispatcher to ensure all bindings are applied before re-enabling event handling
        Dispatcher.BeginInvoke(new Action(() => _isInitializing = false), System.Windows.Threading.DispatcherPriority.DataBind);
    }

    private void CheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Only notify parent if this is a user-initiated change (not during initialization)
        if (!_isInitializing)
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
