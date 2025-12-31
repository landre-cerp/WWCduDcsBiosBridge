using WwDevicesDotNet;
using WwDevicesDotNet.WinWing.FcuAndEfis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWCduDcsBiosBridge.UI;

/// <summary>
/// Factory class for creating device tabs in the UI
/// </summary>
public class DeviceTabFactory
{

    /// <summary>
    /// Creates a new device tab for the specified device
    /// </summary>
    /// <param name="deviceInfo">Device information</param>
    /// <param name="bridgeStarted">Whether the bridge is currently running</param>
    /// <returns>A configured TabItem</returns>
    public static TabItem CreateDeviceTab(
        DeviceInfo deviceInfo, 
        bool bridgeStarted)
    {
        var tabItem = new TabItem
        {
            Header = deviceInfo.DisplayName,
            Tag = deviceInfo
        };

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(10, 10, 10, 10)
        };

        var stackPanel = new StackPanel();

        stackPanel.Children.Add(CreateDeviceInfoSection(deviceInfo));
        
        // Add device-specific controls
        if (deviceInfo.Frontpanel != null)
        {
            stackPanel.Children.Add(CreateFcuTestSection(deviceInfo.Frontpanel));
        }
        else if (deviceInfo.Cdu != null)
        {
            // TODO: Enable LED mapping UI when feature is ready
            //stackPanel.Children.Add(CreateLedCheckBoxes(deviceInfo.Cdu));
        }

        scrollViewer.Content = stackPanel;
        tabItem.Content = scrollViewer;

        return tabItem;
    }

    private static UIElement CreateFcuTestSection(IFrontpanel frontpanel)
    {
        var fcuGroup = new GroupBox
        {
            Header = "FCU/EFIS Test Features",
            Padding = new Thickness(10, 10, 10, 10),
            Margin = new Thickness(0, 10, 0, 10)
        };

        var mainStack = new StackPanel();

        // Connection status
        var statusPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        statusPanel.Children.Add(new TextBlock { Text = "Connection Status: ", FontWeight = FontWeights.Bold });
        var statusText = new TextBlock 
        { 
            Text = frontpanel.IsConnected ? "Connected" : "Disconnected",
            Foreground = frontpanel.IsConnected ? Brushes.Green : Brushes.Red
        };
        statusPanel.Children.Add(statusText);
        mainStack.Children.Add(statusPanel);

        // Event monitor
        var eventGroup = new GroupBox
        {
            Header = "Event Monitor",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 5, 0, 0)
        };

        var eventStack = new StackPanel();
        var eventLog = new TextBox
        {
            IsReadOnly = true,
            Height = 200,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10
        };

        var clearButton = new Button
        {
            Content = "Clear Log",
            Margin = new Thickness(0, 5, 0, 0),
            Padding = new Thickness(10, 2, 10, 2)
        };
        clearButton.Click += (s, e) => eventLog.Clear();

        // Subscribe to events
        frontpanel.ControlActivated += (s, e) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                eventLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] PRESSED: {e.ControlId}\n");
                eventLog.ScrollToEnd();
            });
        };

        frontpanel.ControlDeactivated += (s, e) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                eventLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] RELEASED: {e.ControlId}\n");
                eventLog.ScrollToEnd();
            });
        };

        frontpanel.Disconnected += (s, e) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                eventLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] DISCONNECTED\n");
                eventLog.ScrollToEnd();
                statusText.Text = "Disconnected";
                statusText.Foreground = Brushes.Red;
            });
        };

        eventStack.Children.Add(new TextBlock 
        { 
            Text = "Monitor button presses, releases, and rotary encoder movements:",
            Margin = new Thickness(0, 0, 0, 5)
        });
        eventStack.Children.Add(eventLog);
        eventStack.Children.Add(clearButton);

        eventGroup.Content = eventStack;
        mainStack.Children.Add(eventGroup);

        fcuGroup.Content = mainStack;
        return fcuGroup;
    }

    private static CheckBox CreateLedCheckBox(string label, Action<bool> onChange)
    {
        var cb = new CheckBox
        {
            Content = label,
            Margin = new Thickness(5, 2, 5, 2)
        };
        cb.Checked += (s, e) => onChange(true);
        cb.Unchecked += (s, e) => onChange(false);
        return cb;
    }

    private static StackPanel CreateNumericInput(string label, int min, int max, int defaultValue, Action<int> onChange)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };
        
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
        headerPanel.Children.Add(new TextBlock { Text = label, Width = 100 });
        
        var textBox = new TextBox 
        { 
            Text = defaultValue.ToString(), 
            Width = 80,
            Margin = new Thickness(5, 0, 10, 0)
        };
        
        var slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            Value = defaultValue,
            Width = 200,
            Margin = new Thickness(5, 0, 0, 0)
        };

        var updateButton = new Button
        {
            Content = "Update",
            Padding = new Thickness(10, 2, 10, 2),
            Margin = new Thickness(10, 0, 0, 0)
        };

        headerPanel.Children.Add(textBox);
        headerPanel.Children.Add(slider);
        headerPanel.Children.Add(updateButton);

        slider.ValueChanged += (s, e) =>
        {
            var value = (int)e.NewValue;
            textBox.Text = value.ToString();
        };

        textBox.TextChanged += (s, e) =>
        {
            // Use the slider's CURRENT min/max, not the captured min/max from method parameters
            // This allows the range to be updated dynamically (e.g., when switching units)
            if (int.TryParse(textBox.Text, out int value) && value >= (int)slider.Minimum && value <= (int)slider.Maximum)
            {
                slider.Value = value;
            }
        };

        updateButton.Click += (s, e) =>
        {
            // Use the slider's CURRENT min/max for validation
            if (int.TryParse(textBox.Text, out int value) && value >= (int)slider.Minimum && value <= (int)slider.Maximum)
            {
                onChange(value);
            }
            else
            {
                MessageBox.Show($"Please enter a value between {(int)slider.Minimum} and {(int)slider.Maximum}", "Invalid Value", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        panel.Children.Add(headerPanel);
        
        return panel;
    }

    // Overload for double values (for inHg barometric pressure with decimal point)
    // User enters decimal values like 29.92, which get multiplied by 100 and sent as 2992 to hardware
    // The hardware displays it as "29.92" with the decimal point
    private static StackPanel CreateNumericInput(string label, double min, double max, double defaultValue, Action<int> onChange, int multiplier)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };
        
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
        headerPanel.Children.Add(new TextBlock { Text = label, Width = 100 });
        
        var textBox = new TextBox 
        { 
            Text = defaultValue.ToString("F2"), 
            Width = 80,
            Margin = new Thickness(5, 0, 10, 0)
        };
        
        var slider = new Slider
        {
            Minimum = min * multiplier,
            Maximum = max * multiplier,
            Value = defaultValue * multiplier,
            Width = 200,
            Margin = new Thickness(5, 0, 0, 0)
        };

        var updateButton = new Button
        {
            Content = "Update",
            Padding = new Thickness(10, 2, 10, 2),
            Margin = new Thickness(10, 0, 0, 0)
        };

        headerPanel.Children.Add(textBox);
        headerPanel.Children.Add(slider);
        headerPanel.Children.Add(updateButton);

        slider.ValueChanged += (s, e) =>
        {
            var value = e.NewValue / multiplier;
            textBox.Text = value.ToString("F2");
        };

        textBox.TextChanged += (s, e) =>
        {
            if (double.TryParse(textBox.Text, out double value))
            {
                var intValue = (int)(value * multiplier);
                if (intValue >= slider.Minimum && intValue <= slider.Maximum)
                {
                    slider.Value = intValue;
                }
            }
        };

        updateButton.Click += (s, e) =>
        {
            if (double.TryParse(textBox.Text, out double value))
            {
                var intValue = (int)(value * multiplier);
                if (intValue >= slider.Minimum && intValue <= slider.Maximum)
                {
                    onChange(intValue);
                }
                else
                {
                    MessageBox.Show($"Please enter a value between {min:F2} and {max:F2}", "Invalid Value", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show($"Please enter a valid number between {min:F2} and {max:F2}", "Invalid Value", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        panel.Children.Add(headerPanel);
        
        return panel;
    }

    private static StackPanel CreateBrightnessSlider(string label, int defaultValue, Action<int> onValueChanged)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };
        
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
        headerPanel.Children.Add(new TextBlock { Text = label, Width = 120 });
        var valueLabel = new TextBlock { Text = defaultValue.ToString(), Width = 40, TextAlignment = TextAlignment.Right };
        headerPanel.Children.Add(valueLabel);
        
        panel.Children.Add(headerPanel);
        
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 255,
            Value = defaultValue,
            TickFrequency = 25,
            IsSnapToTickEnabled = false,
            Margin = new Thickness(0, 2, 0, 0)
        };

        slider.ValueChanged += (s, e) =>
        {
            var value = (int)e.NewValue;
            valueLabel.Text = value.ToString();
            onValueChanged(value);
        };

        panel.Children.Add(slider);
        
        return panel;
    }

    private static StackPanel CreateDeviceInfoSection(DeviceInfo deviceInfo)
    {
        var deviceInfoStack = new StackPanel();

        var deviceType = deviceInfo.Cdu != null ? "CDU Device" : "Frontpanel Device (FCU/EFIS)";
        var description = new TextBlock
        {
            Text = $"Device Type: {deviceType}",
            Margin = new Thickness(0, 2, 0, 2),
            FontWeight = FontWeights.Bold
        };

        deviceInfoStack.Children.Add(description);

        return deviceInfoStack;
    }
}