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

        // LED state (declared early so it can be used in brightness controls and LED test)
        var ledState = new FcuEfisLeds();

        // Brightness controls
        var brightnessGroup = new GroupBox
        {
            Header = "Brightness Controls",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 5, 0, 10)
        };

        var brightnessStack = new StackPanel();

        byte currentPanel = 80, currentLcd = 255, currentLed = 255;

        // Panel Backlight
        var panelBrightnessPanel = CreateBrightnessSlider("Panel Backlight:", currentPanel, (value) =>
        {
            try
            {
                currentPanel = (byte)value;
                frontpanel.SetBrightness(currentPanel, currentLcd, currentLed);
                // Also set EXPED yellow LED to match panel brightness
                ledState.ExpedYellowBrightness = currentPanel;
                frontpanel.UpdateLeds(ledState);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set panel brightness: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
        brightnessStack.Children.Add(panelBrightnessPanel);


        // LCD Backlight
        var lcdBrightnessPanel = CreateBrightnessSlider("LCD Backlight:", currentLcd, (value) =>
        {
            try
            {
                currentLcd = (byte)value;
                frontpanel.SetBrightness(currentPanel, currentLcd, currentLed);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set LCD brightness: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
        brightnessStack.Children.Add(lcdBrightnessPanel);

        // LED Backlight
        var ledBrightnessPanel = CreateBrightnessSlider("LED Backlight:", currentLed, (value) =>
        {
            try
            {
                currentLed = (byte)value;
                frontpanel.SetBrightness(currentPanel, currentLcd, currentLed);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set LED brightness: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
        brightnessStack.Children.Add(ledBrightnessPanel);
        

        brightnessGroup.Content = brightnessStack;
        mainStack.Children.Add(brightnessGroup);

        // Display test controls
        var displayGroup = new GroupBox
        {
            Header = "Display Test",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 5, 0, 10)
        };

        var displayStack = new StackPanel();
        var fcuState = new FcuEfisState();

        // Text mode indicators section
        var textIndicatorsPanel = new GroupBox
        {
            Header = "Mode Indicators (Radio Buttons = Mutually Exclusive)",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 0, 0, 10)
        };
        
        var textStack = new StackPanel();
        
        // SPD/MACH radio buttons
        var spdMachPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 5) };
        spdMachPanel.Children.Add(new TextBlock { Text = "Speed:", Width = 80, VerticalAlignment = VerticalAlignment.Center });
        
        var spdRadio = new RadioButton { Content = "SPD", GroupName = "Speed", IsChecked = true, Margin = new Thickness(5, 0, 10, 0) };
        var machRadio = new RadioButton { Content = "MACH", GroupName = "Speed", IsChecked = false, Margin = new Thickness(5, 0, 10, 0) };
        
        spdRadio.Checked += (s, e) => { fcuState.SpeedIsMach = false; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        machRadio.Checked += (s, e) => { fcuState.SpeedIsMach = true; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        
        spdMachPanel.Children.Add(spdRadio);
        spdMachPanel.Children.Add(machRadio);
        textStack.Children.Add(spdMachPanel);
        
        // HDG/TRK radio buttons
        var hdgTrkPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 5) };
        hdgTrkPanel.Children.Add(new TextBlock { Text = "Heading:", Width = 80, VerticalAlignment = VerticalAlignment.Center });
        
        var hdgRadio = new RadioButton { Content = "HDG", GroupName = "Heading", IsChecked = true, Margin = new Thickness(5, 0, 10, 0) };
        var trkRadio = new RadioButton { Content = "TRK", GroupName = "Heading", IsChecked = false, Margin = new Thickness(5, 0, 10, 0) };
        
        hdgRadio.Checked += (s, e) => { fcuState.HeadingIsTrack = false; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        trkRadio.Checked += (s, e) => { fcuState.HeadingIsTrack = true; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        
        hdgTrkPanel.Children.Add(hdgRadio);
        hdgTrkPanel.Children.Add(trkRadio);
        textStack.Children.Add(hdgTrkPanel);
        
        // V/S/FPA radio buttons (right side)
        var vsFpaPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 5) };
        vsFpaPanel.Children.Add(new TextBlock { Text = "Vertical (right):", Width = 80, VerticalAlignment = VerticalAlignment.Center });
        
        var vsRadio = new RadioButton { Content = "V/S", GroupName = "Vertical", IsChecked = true, Margin = new Thickness(5, 0, 10, 0) };
        var fpaRadio = new RadioButton { Content = "FPA", GroupName = "Vertical", IsChecked = false, Margin = new Thickness(5, 0, 10, 0) };
        
        vsRadio.Checked += (s, e) => { fcuState.VsIsFpa = false; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        fpaRadio.Checked += (s, e) => { fcuState.VsIsFpa = true; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        
        vsFpaPanel.Children.Add(vsRadio);
        vsFpaPanel.Children.Add(fpaRadio);
        textStack.Children.Add(vsFpaPanel);
        
        // LAT checkbox (independent)
        var latPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
        latPanel.Children.Add(new TextBlock { Text = "Other:", Width = 80, VerticalAlignment = VerticalAlignment.Center });
        var latCb = new CheckBox { Content = "LAT", IsChecked = false, Margin = new Thickness(5, 0, 10, 0) };
        latCb.Checked += (s, e) => { fcuState.LatIndicator = true; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        latCb.Unchecked += (s, e) => { fcuState.LatIndicator = false; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        latPanel.Children.Add(latCb);
        textStack.Children.Add(latPanel);
        
        textIndicatorsPanel.Content = textStack;
        displayStack.Children.Add(textIndicatorsPanel);

        // Managed mode dots section
        var managedPanel = new GroupBox
        {
            Header = "Managed Mode Dots (Round Indicators)",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 0, 0, 10)
        };
        
        var managedStack = new WrapPanel();
        
        var spdDotCb = new CheckBox { Content = "SPD Dot", IsChecked = false, Margin = new Thickness(5, 2, 5, 2) };
        spdDotCb.Checked += (s, e) => { fcuState.SpeedManaged = true; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        spdDotCb.Unchecked += (s, e) => { fcuState.SpeedManaged = false; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        managedStack.Children.Add(spdDotCb);
        
        var hdgDotCb = new CheckBox { Content = "HDG Dot", IsChecked = false, Margin = new Thickness(5, 2, 5, 2) };
        hdgDotCb.Checked += (s, e) => { fcuState.HeadingManaged = true; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        hdgDotCb.Unchecked += (s, e) => { fcuState.HeadingManaged = false; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        managedStack.Children.Add(hdgDotCb);
        
        var altDotCb = new CheckBox { Content = "ALT Dot", IsChecked = false, Margin = new Thickness(5, 2, 5, 2) };
        altDotCb.Checked += (s, e) => { fcuState.AltitudeManaged = true; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        altDotCb.Unchecked += (s, e) => { fcuState.AltitudeManaged = false; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        managedStack.Children.Add(altDotCb);
        
        managedPanel.Content = managedStack;
        displayStack.Children.Add(managedPanel);

        // Middle section indicators (HDG/TRACK VS/FPA)
        var middleIndicatorsPanel = new GroupBox
        {
            Header = "Middle Section Indicators (Between ALT and V/S displays)",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 0, 0, 10)
        };
        
        var middleStack = new StackPanel();
        
        // LVL/CH checkboxes (independent)
        var lvlPanel = new WrapPanel { Margin = new Thickness(0, 5, 0, 0) };
        lvlPanel.Children.Add(new TextBlock { Text = "LVL/CH (independent):", Width = 150, VerticalAlignment = VerticalAlignment.Center });
        
        var lvlLeftCb = new CheckBox { Content = "LVL [ ", IsChecked = false, Margin = new Thickness(5, 2, 5, 2) };
        lvlLeftCb.Checked += (s, e) => { fcuState.LvlLeftBracket = true; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        lvlLeftCb.Unchecked += (s, e) => { fcuState.LvlLeftBracket = false; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        lvlPanel.Children.Add(lvlLeftCb);
        var lvlCb = new CheckBox { Content = "LVL", IsChecked = false, Margin = new Thickness(5, 2, 5, 2) };
        lvlCb.Checked += (s, e) => { fcuState.LvlIndicator = true; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        lvlCb.Unchecked += (s, e) => { fcuState.LvlIndicator = false; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        lvlPanel.Children.Add(lvlCb);

        var lvlRightCb = new CheckBox { Content = "LVL ]", IsChecked = false, Margin = new Thickness(5, 2, 5, 2) };
        lvlRightCb.Checked += (s, e) => { fcuState.LvlRightBracket = true; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        lvlRightCb.Unchecked += (s, e) => { fcuState.LvlRightBracket = false; try { frontpanel.UpdateDisplay(fcuState); } catch { } };
        lvlPanel.Children.Add(lvlRightCb);
        
        
        middleStack.Children.Add(lvlPanel);
        
        middleIndicatorsPanel.Content = middleStack;
        displayStack.Children.Add(middleIndicatorsPanel);

        // Initialize FCU to default state (SPD=0, HDG=0, ALT=0, V/S=0 with correct mode indicators)
        try {
            fcuState.Speed = 0;
            fcuState.Heading = 0;
            fcuState.Altitude = 0;
            fcuState.VerticalSpeed = 0;
            fcuState.SpeedIsMach = false;
            fcuState.HeadingIsTrack = false;
            fcuState.VsIsFpa = false;
            frontpanel.UpdateDisplay(fcuState);
        } catch (Exception ex) {
            MessageBox.Show($"Failed to initialize FCU displays: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Speed control
        var speedPanel = CreateNumericInput("Speed:", 0, 999, 0, (value) =>
        {
            try
            {
                fcuState.Speed = value;
                frontpanel.UpdateDisplay(fcuState);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
        displayStack.Children.Add(speedPanel);

        // Heading control
        var headingPanel = CreateNumericInput("Heading:", 0, 359, 0, (value) =>
        {
            try
            {
                fcuState.Heading = value;
                frontpanel.UpdateDisplay(fcuState);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
        displayStack.Children.Add(headingPanel);

        // Altitude control
        var altitudePanel = CreateNumericInput("Altitude:", 0, 99999, 0, (value) =>
        {
            try
            {
                fcuState.Altitude = value;
                frontpanel.UpdateDisplay(fcuState);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
        displayStack.Children.Add(altitudePanel);

        // Vertical Speed control
        var vsPanel = CreateNumericInput("V/S:", -9999, 9999, 0, (value) =>
        {
            try
            {
                fcuState.VerticalSpeed = value;
                frontpanel.UpdateDisplay(fcuState);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
        displayStack.Children.Add(vsPanel);

        // Clear display button
        var clearDisplayBtn = new Button
        {
            Content = "Clear All Displays & Indicators",
            Margin = new Thickness(0, 10, 0, 0),
            Padding = new Thickness(10, 5, 10, 5)
        };
        clearDisplayBtn.Click += (s, e) =>
        {
            try
            {
                // Clear display values
                fcuState.Speed = null;
                fcuState.Heading = null;
                fcuState.Altitude = null;
                fcuState.VerticalSpeed = null;
                
                // Clear all indicators
                fcuState.SpeedIsMach = false;
                fcuState.HeadingIsTrack = false;
                fcuState.VsIsFpa = false;
                fcuState.SpeedManaged = false;
                fcuState.HeadingManaged = false;
                fcuState.AltitudeManaged = false;
                fcuState.LatIndicator = false;
                fcuState.LvlIndicator = false;
                fcuState.LvlLeftBracket = false;
                fcuState.LvlRightBracket = false;
                fcuState.VsHorzIndicator = false;
                
                frontpanel.UpdateDisplay(fcuState);
                
                // Reset all radio buttons and checkboxes
                spdRadio.IsChecked = false;
                machRadio.IsChecked = false;
                hdgRadio.IsChecked = false;
                trkRadio.IsChecked = false;
                vsRadio.IsChecked = false;
                fpaRadio.IsChecked = false;
                latCb.IsChecked = false;
                spdDotCb.IsChecked = false;
                hdgDotCb.IsChecked = false;
                altDotCb.IsChecked = false;
                lvlCb.IsChecked = false;
                lvlLeftCb.IsChecked = false;
                lvlRightCb.IsChecked = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clear displays: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        displayStack.Children.Add(clearDisplayBtn);

        displayGroup.Content = displayStack;
        mainStack.Children.Add(displayGroup);

        // EFIS Baro display test controls
        var efisGroup = new GroupBox
        {
            Header = "EFIS Barometric Display Test",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 5, 0, 10)
        };

        var efisStack = new StackPanel();

        // Left EFIS Baro controls
        if (frontpanel.DeviceId.Device == Device.WinWingFcuLeftEfis || 
            frontpanel.DeviceId.Device == Device.WinWingFcuBothEfis)
        {
            var leftEfisHeader = new TextBlock 
            { 
                Text = "Left EFIS:", 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 0, 0, 5) 
            };
            efisStack.Children.Add(leftEfisHeader);

            // Unit selection for left EFIS
            var leftUnitPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            leftUnitPanel.Children.Add(new TextBlock { Text = "Unit:", Width = 100, VerticalAlignment = VerticalAlignment.Center });
            
            var leftHpaRadio = new RadioButton { Content = "hPa (870-1085)", GroupName = "LeftUnit", IsChecked = true, Margin = new Thickness(5, 0, 10, 0) };
            var leftInHgRadio = new RadioButton { Content = "inHg (25.70-32.00)", GroupName = "LeftUnit", IsChecked = false, Margin = new Thickness(5, 0, 10, 0) };
            
            leftUnitPanel.Children.Add(leftHpaRadio);
            leftUnitPanel.Children.Add(leftInHgRadio);
            efisStack.Children.Add(leftUnitPanel);

            // Left baro pressure input - will be updated based on unit selection
            var leftBaroPanel = CreateNumericInput("Baro Pressure:", 870, 1085, 1013, (value) =>
            {
                try
                {
                    fcuState.LeftBaroPressure = value;
                    frontpanel.UpdateDisplay(fcuState);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update left EFIS display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
            efisStack.Children.Add(leftBaroPanel);

            // Update range when unit changes
            leftHpaRadio.Checked += (s, e) => {
                // Switch back to hPa mode with integer input (870-1085)
                var parentStack = leftBaroPanel.Parent as StackPanel;
                if (parentStack != null)
                {
                    var index = parentStack.Children.IndexOf(leftBaroPanel);
                    parentStack.Children.RemoveAt(index);
                    
                    // Create new panel with integer input
                    var newBaroPanel = CreateNumericInput("Baro Pressure:", 870, 1085, 1013, (value) =>
                    {
                        try
                        {
                            fcuState.LeftBaroPressure = value;
                            frontpanel.UpdateDisplay(fcuState);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to update left EFIS display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                    
                    parentStack.Children.Insert(index, newBaroPanel);
                    leftBaroPanel = newBaroPanel;
                }
            };
            
            leftInHgRadio.Checked += (s, e) => {
                // Switch to inHg mode with double input (25.70-32.00)
                var parentStack = leftBaroPanel.Parent as StackPanel;
                if (parentStack != null)
                {
                    var index = parentStack.Children.IndexOf(leftBaroPanel);
                    parentStack.Children.RemoveAt(index);
                    
                    // Create new panel with double input (user enters 29.92, hardware receives 2992)
                    var newBaroPanel = CreateNumericInput("Baro Pressure:", 25.70, 32.00, 29.92, (value) =>
                    {
                        try
                        {
                            fcuState.LeftBaroPressure = value;
                            frontpanel.UpdateDisplay(fcuState);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to update left EFIS display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }, 100);  // multiplier=100 to convert 29.92 to 2992
                    
                    parentStack.Children.Insert(index, newBaroPanel);
                    leftBaroPanel = newBaroPanel;
                }
            };

            // QNH/QFE radio buttons for left
            var leftQnhQfePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 10) };
            leftQnhQfePanel.Children.Add(new TextBlock { Text = "Mode:", Width = 100, VerticalAlignment = VerticalAlignment.Center });
            
            var leftQnhRadio = new RadioButton { Content = "QNH", GroupName = "LeftBaro", IsChecked = true, Margin = new Thickness(5, 0, 10, 0) };
            var leftQfeRadio = new RadioButton { Content = "QFE", GroupName = "LeftBaro", IsChecked = false, Margin = new Thickness(5, 0, 10, 0) };
            var leftOffRadio = new RadioButton { Content = "OFF", GroupName = "LeftBaro", IsChecked = false, Margin = new Thickness(5, 0, 10, 0) };
            
            leftQnhRadio.Checked += (s, e) => { 
                fcuState.LeftBaroQnh = true; 
                fcuState.LeftBaroQfe = false; 
                try { frontpanel.UpdateDisplay(fcuState); } catch { } 
            };
            leftQfeRadio.Checked += (s, e) => { 
                fcuState.LeftBaroQnh = false; 
                fcuState.LeftBaroQfe = true; 
                try { frontpanel.UpdateDisplay(fcuState); } catch { } 
            };
            leftOffRadio.Checked += (s, e) => { 
                fcuState.LeftBaroQnh = false; 
                fcuState.LeftBaroQfe = false; 
                try { frontpanel.UpdateDisplay(fcuState); } catch { } 
            };
            
            leftQnhQfePanel.Children.Add(leftQnhRadio);
            leftQnhQfePanel.Children.Add(leftQfeRadio);
            leftQnhQfePanel.Children.Add(leftOffRadio);
            efisStack.Children.Add(leftQnhQfePanel);
        }

        // Right EFIS Baro controls
        if (frontpanel.DeviceId.Device == Device.WinWingFcuRightEfis || 
            frontpanel.DeviceId.Device == Device.WinWingFcuBothEfis)
        {
            var rightEfisHeader = new TextBlock 
            { 
                Text = "Right EFIS:", 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 5, 0, 5) 
            };
            efisStack.Children.Add(rightEfisHeader);

            // Unit selection for right EFIS
            var rightUnitPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            rightUnitPanel.Children.Add(new TextBlock { Text = "Unit:", Width = 100, VerticalAlignment = VerticalAlignment.Center });
            
            var rightHpaRadio = new RadioButton { Content = "hPa (870-1085)", GroupName = "RightUnit", IsChecked = true, Margin = new Thickness(5, 0, 10, 0) };
            var rightInHgRadio = new RadioButton { Content = "inHg (25.70-32.00)", GroupName = "RightUnit", IsChecked = false, Margin = new Thickness(5, 0, 10, 0) };
            
            rightUnitPanel.Children.Add(rightHpaRadio);
            rightUnitPanel.Children.Add(rightInHgRadio);
            efisStack.Children.Add(rightUnitPanel);

            // Right baro pressure input
            var rightBaroPanel = CreateNumericInput("Baro Pressure:", 870, 1085, 1013, (value) =>
            {
                try
                {
                    fcuState.RightBaroPressure = value;
                    frontpanel.UpdateDisplay(fcuState);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update right EFIS display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
            efisStack.Children.Add(rightBaroPanel);

            // Update range when unit changes
            rightHpaRadio.Checked += (s, e) => {
                // Switch back to hPa mode with integer input (870-1085)
                var parentStack = rightBaroPanel.Parent as StackPanel;
                if (parentStack != null)
                {
                    var index = parentStack.Children.IndexOf(rightBaroPanel);
                    parentStack.Children.RemoveAt(index);
                    
                    // Create new panel with integer input
                    var newBaroPanel = CreateNumericInput("Baro Pressure:", 870, 1085, 1013, (value) =>
                    {
                        try
                        {
                            fcuState.RightBaroPressure = value;
                            frontpanel.UpdateDisplay(fcuState);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to update right EFIS display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                    
                    parentStack.Children.Insert(index, newBaroPanel);
                    rightBaroPanel = newBaroPanel;
                }
            };
            
            rightInHgRadio.Checked += (s, e) => {
                // Switch to inHg mode with double input (25.70-32.00)
                var parentStack = rightBaroPanel.Parent as StackPanel;
                if (parentStack != null)
                {
                    var index = parentStack.Children.IndexOf(rightBaroPanel);
                    parentStack.Children.RemoveAt(index);
                    
                    // Create new panel with double input (user enters 29.92, hardware receives 2992)
                    var newBaroPanel = CreateNumericInput("Baro Pressure:", 25.70, 32.00, 29.92, (value) =>
                    {
                        try
                        {
                            fcuState.RightBaroPressure = value;
                            frontpanel.UpdateDisplay(fcuState);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to update right EFIS display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }, 100);  // multiplier=100 to convert 29.92 to 2992
                    
                    parentStack.Children.Insert(index, newBaroPanel);
                    rightBaroPanel = newBaroPanel;
                }
            };

            // QNH/QFE radio buttons for right
            var rightQnhQfePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 10) };
            rightQnhQfePanel.Children.Add(new TextBlock { Text = "Mode:", Width = 100, VerticalAlignment = VerticalAlignment.Center });
            
            var rightQnhRadio = new RadioButton { Content = "QNH", GroupName = "RightBaro", IsChecked = true, Margin = new Thickness(5, 0, 10, 0) };
            var rightQfeRadio = new RadioButton { Content = "QFE", GroupName = "RightBaro", IsChecked = false, Margin = new Thickness(5, 0, 10, 0) };
            var rightOffRadio = new RadioButton { Content = "OFF", GroupName = "RightBaro", IsChecked = false, Margin = new Thickness(5, 0, 10, 0) };
            
            rightQnhRadio.Checked += (s, e) => { 
                fcuState.RightBaroQnh = true; 
                fcuState.RightBaroQfe = false; 
                try { frontpanel.UpdateDisplay(fcuState); } catch { } 
            };
            rightQfeRadio.Checked += (s, e) => { 
                fcuState.RightBaroQnh = false; 
                fcuState.RightBaroQfe = true; 
                try { frontpanel.UpdateDisplay(fcuState); } catch { } 
            };
            rightOffRadio.Checked += (s, e) => { 
                fcuState.RightBaroQnh = false; 
                fcuState.RightBaroQfe = false; 
                try { frontpanel.UpdateDisplay(fcuState); } catch { } 
            };
            
            rightQnhQfePanel.Children.Add(rightQnhRadio);
            rightQnhQfePanel.Children.Add(rightQfeRadio);
            rightQnhQfePanel.Children.Add(rightOffRadio);
            efisStack.Children.Add(rightQnhQfePanel);
        }

        efisGroup.Content = efisStack;
        mainStack.Children.Add(efisGroup);

        // LED test controls
        var ledGroup = new GroupBox
        {
            Header = "LED Test",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 5, 0, 10)
        };

        var ledStack = new StackPanel();

        // FCU LEDs
        var fcuLedPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 5) };
        fcuLedPanel.Children.Add(new TextBlock { Text = "FCU: ", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 5, 10, 0) });
        fcuLedPanel.Children.Add(CreateLedCheckBox("LOC", v => { ledState.Loc = v; frontpanel.UpdateLeds(ledState); }));
        fcuLedPanel.Children.Add(CreateLedCheckBox("AP1", v => { ledState.Ap1 = v; frontpanel.UpdateLeds(ledState); }));
        fcuLedPanel.Children.Add(CreateLedCheckBox("AP2", v => { ledState.Ap2 = v; frontpanel.UpdateLeds(ledState); }));
        fcuLedPanel.Children.Add(CreateLedCheckBox("A/THR", v => { ledState.AThr = v; frontpanel.UpdateLeds(ledState); }));
        fcuLedPanel.Children.Add(CreateLedCheckBox("EXPED (Green)", v => { ledState.Exped = v; frontpanel.UpdateLeds(ledState); }));
        fcuLedPanel.Children.Add(CreateLedCheckBox("APPR", v => { ledState.Appr = v; frontpanel.UpdateLeds(ledState); }));
        ledStack.Children.Add(fcuLedPanel);

        // Left EFIS LEDs
        var leftEfisPanel = new WrapPanel { Margin = new Thickness(0, 5, 0, 5) };
        leftEfisPanel.Children.Add(new TextBlock { Text = "Left EFIS: ", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 5, 10, 0) });
        leftEfisPanel.Children.Add(CreateLedCheckBox("FD", v => { ledState.LeftFd = v; frontpanel.UpdateLeds(ledState); }));
        leftEfisPanel.Children.Add(CreateLedCheckBox("LS", v => { ledState.LeftLs = v; frontpanel.UpdateLeds(ledState); }));
        leftEfisPanel.Children.Add(CreateLedCheckBox("CSTR", v => { ledState.LeftCstr = v; frontpanel.UpdateLeds(ledState); }));
        leftEfisPanel.Children.Add(CreateLedCheckBox("WPT", v => { ledState.LeftWpt = v; frontpanel.UpdateLeds(ledState); }));
        leftEfisPanel.Children.Add(CreateLedCheckBox("VOR.D", v => { ledState.LeftVorD = v; frontpanel.UpdateLeds(ledState); }));
        leftEfisPanel.Children.Add(CreateLedCheckBox("NDB", v => { ledState.LeftNdb = v; frontpanel.UpdateLeds(ledState); }));
        leftEfisPanel.Children.Add(CreateLedCheckBox("ARPT", v => { ledState.LeftArpt = v; frontpanel.UpdateLeds(ledState); }));
        ledStack.Children.Add(leftEfisPanel);

        // Right EFIS LEDs
        var rightEfisPanel = new WrapPanel { Margin = new Thickness(0, 5, 0, 0) };
        rightEfisPanel.Children.Add(new TextBlock { Text = "Right EFIS: ", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 5, 10, 0) });
        rightEfisPanel.Children.Add(CreateLedCheckBox("FD", v => { ledState.RightFd = v; frontpanel.UpdateLeds(ledState); }));
        rightEfisPanel.Children.Add(CreateLedCheckBox("LS", v => { ledState.RightLs = v; frontpanel.UpdateLeds(ledState); }));
        rightEfisPanel.Children.Add(CreateLedCheckBox("CSTR", v => { ledState.RightCstr = v; frontpanel.UpdateLeds(ledState); }));
        rightEfisPanel.Children.Add(CreateLedCheckBox("WPT", v => { ledState.RightWpt = v; frontpanel.UpdateLeds(ledState); }));
        rightEfisPanel.Children.Add(CreateLedCheckBox("VOR.D", v => { ledState.RightVorD = v; frontpanel.UpdateLeds(ledState); }));
        rightEfisPanel.Children.Add(CreateLedCheckBox("NDB", v => { ledState.RightNdb = v; frontpanel.UpdateLeds(ledState); }));
        rightEfisPanel.Children.Add(CreateLedCheckBox("ARPT", v => { ledState.RightArpt = v; frontpanel.UpdateLeds(ledState); }));
        ledStack.Children.Add(rightEfisPanel);

        // Clear LEDs button
        var clearLedsBtn = new Button
        {
            Content = "Clear All LEDs",
            Margin = new Thickness(0, 10, 0, 0),
            Padding = new Thickness(10, 5, 10, 5)
        };
        clearLedsBtn.Click += (s, e) =>
        {
            try
            {
                ledState = new FcuEfisLeds();
                frontpanel.UpdateLeds(ledState);
                // Reset checkboxes
                foreach (var panel in new[] { fcuLedPanel, leftEfisPanel, rightEfisPanel })
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is CheckBox cb)
                            cb.IsChecked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clear LEDs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        ledStack.Children.Add(clearLedsBtn);

        ledGroup.Content = ledStack;
        mainStack.Children.Add(ledGroup);

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