using Microsoft.Win32;
using System;
using System.IO;
using System.Net;
using System.Windows;

namespace WWCduDcsBiosBridge.Config;

public partial class ConfigWindow : Window
{
    public DcsBiosConfig Config { get; private set; }

    public ConfigWindow(DcsBiosConfig config)
    {
        InitializeComponent();
        Config = new DcsBiosConfig
        {
            ReceiveFromIpUdp = config.ReceiveFromIpUdp,
            SendToIpUdp = config.SendToIpUdp,
            ReceivePortUdp = config.ReceivePortUdp,
            SendPortUdp = config.SendPortUdp,
            DcsBiosJsonLocation = config.DcsBiosJsonLocation
        };
        
        LoadConfigToUI();
    }

    private void LoadConfigToUI()
    {
        ReceiveIpTextBox.Text = Config.ReceiveFromIpUdp;
        SendIpTextBox.Text = Config.SendToIpUdp;
        ReceivePortTextBox.Text = Config.ReceivePortUdp.ToString();
        SendPortTextBox.Text = Config.SendPortUdp.ToString();
        JsonLocationTextBox.Text = Config.DcsBiosJsonLocation;
    }

    private bool ValidateAndUpdateConfig()
    {
        try
        {
            // Validate IP addresses
            if (!IPAddress.TryParse(ReceiveIpTextBox.Text.Trim(), out var receiveIp))
            {
                MessageBox.Show("Receive IP address is not valid.", "Validation Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                ReceiveIpTextBox.Focus();
                return false;
            }

            if (!IPAddress.TryParse(SendIpTextBox.Text.Trim(), out var sendIp))
            {
                MessageBox.Show("Send IP address is not valid.", "Validation Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                SendIpTextBox.Focus();
                return false;
            }

            // Validate ports
            if (!int.TryParse(ReceivePortTextBox.Text.Trim(), out int receivePort) || 
                receivePort < 1 || receivePort > 65535)
            {
                MessageBox.Show("Receive port must be between 1 and 65535.", "Validation Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                ReceivePortTextBox.Focus();
                return false;
            }

            if (!int.TryParse(SendPortTextBox.Text.Trim(), out int sendPort) || 
                sendPort < 1 || sendPort > 65535)
            {
                MessageBox.Show("Send port must be between 1 and 65535.", "Validation Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                SendPortTextBox.Focus();
                return false;
            }

            // Validate JSON location
            string jsonLocation = JsonLocationTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(jsonLocation))
            {
                MessageBox.Show("DCS-BIOS JSON location cannot be empty.", "Validation Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                JsonLocationTextBox.Focus();
                return false;
            }

            if (!Directory.Exists(jsonLocation))
            {
                MessageBox.Show($"The specified directory does not exist: {jsonLocation}", "Validation Error", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                JsonLocationTextBox.Focus();
                return false;
            }

            // Update config
            Config.ReceiveFromIpUdp = receiveIp.ToString();
            Config.SendToIpUdp = sendIp.ToString();
            Config.ReceivePortUdp = receivePort;
            Config.SendPortUdp = sendPort;
            Config.DcsBiosJsonLocation = jsonLocation;

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Validation error: {ex.Message}", "Validation Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select DCS-BIOS JSON Files Directory"
        };

        if (!string.IsNullOrWhiteSpace(JsonLocationTextBox.Text) && Directory.Exists(JsonLocationTextBox.Text))
        {
            dialog.InitialDirectory = JsonLocationTextBox.Text;
        }

        if (dialog.ShowDialog() == true)
        {
            JsonLocationTextBox.Text = dialog.FolderName;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (ValidateAndUpdateConfig())
        {
            try
            {
                ConfigManager.Save(Config);
                MessageBox.Show("Configuration saved successfully!", "Success", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save configuration: {ex.Message}", "Save Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ResetToDefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to reset all settings to default values?", 
                                    "Reset to Defaults", 
                                    MessageBoxButton.YesNo, 
                                    MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            Config = new DcsBiosConfig();
            LoadConfigToUI();
        }
    }
}