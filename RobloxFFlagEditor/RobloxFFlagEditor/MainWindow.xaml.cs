using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Text.Json;

namespace RobloxSettingsEditor
{
    public partial class MainWindow : Window
    {
        // Fixed: Made the string nullable to resolve the CS8618 warning.
        private string? _settingsFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private string GetRobloxVersionsPath()
        {
            // Dynamically finds the C:\Users\Username\AppData\Local\Roblox\Versions path.
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Roblox", "Versions");
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Set the initial directory to the Roblox Versions folder for easy navigation
            string initialDirectory = GetRobloxVersionsPath();
            if (Directory.Exists(initialDirectory))
            {
                openFileDialog.InitialDirectory = initialDirectory;
            }
            else
            {
                // Fallback if the path is not found
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            // Filter for the possible settings file names
            openFileDialog.Filter = "Roblox Settings Files|IxpSettings.json;ClientAppSettings.json|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                _settingsFilePath = openFileDialog.FileName;
                LoadFileContent(_settingsFilePath);
            }
        }

        private void LoadFileContent(string filePath)
        {
            try
            {
                // Ensure the file is writable before reading
                SetFileReadWriteAttribute(filePath, isReadOnly: false);

                string content = File.ReadAllText(filePath);

                // Try to pretty-print the JSON for readability
                try
                {
                    using (var doc = JsonDocument.Parse(content))
                    {
                        content = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
                    }
                }
                catch (JsonException)
                {
                    // If content isn't valid JSON, just load the raw text
                }

                SettingsTextBox.Text = content;
                FilePathLabel.Text = $"Current File: {filePath}";
                StatusLabel.Text = "Status: File loaded successfully.";
                ToggleReadOnlyButton.IsEnabled = true;
                UpdateToggleReadOnlyButtonText(filePath);
            }
            catch (FileNotFoundException)
            {
                StatusLabel.Text = "ERROR: File not found. Select a new file.";
                FilePathLabel.Text = "Current File: Not Selected";
                ToggleReadOnlyButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"ERROR loading file: {ex.Message}";
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            // Check for null/empty path before attempting save
            if (string.IsNullOrEmpty(_settingsFilePath))
            {
                StatusLabel.Text = "ERROR: Please select a file first.";
                return;
            }

            try
            {
                // 1. Ensure the file is writable (unlock the file)
                SetFileReadWriteAttribute(_settingsFilePath!, isReadOnly: false);

                // 2. Save the content from the textbox
                File.WriteAllText(_settingsFilePath!, SettingsTextBox.Text);

                StatusLabel.Text = "SUCCESS: Changes saved to file.";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"ERROR saving file: {ex.Message}";
            }
        }

        private void ToggleReadOnly_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_settingsFilePath))
            {
                StatusLabel.Text = "ERROR: No file is selected.";
                return;
            }

            try
            {
                FileAttributes attributes = File.GetAttributes(_settingsFilePath!);
                bool isCurrentlyReadOnly = (attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;

                // Toggle the attribute: if ReadOnly, make Writable; if Writable, make ReadOnly.
                SetFileReadWriteAttribute(_settingsFilePath!, !isCurrentlyReadOnly);

                UpdateToggleReadOnlyButtonText(_settingsFilePath!);
                StatusLabel.Text = isCurrentlyReadOnly
                    ? "Status: File is now Writable (UNLOCKED)."
                    : "Status: File is now Read-Only (LOCKED).";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"ERROR toggling attribute: {ex.Message}";
            }
        }

        // Helper function to set the ReadOnly attribute
        private void SetFileReadWriteAttribute(string filePath, bool isReadOnly)
        {
            FileAttributes attributes = File.GetAttributes(filePath);

            if (isReadOnly)
            {
                // Set ReadOnly attribute
                File.SetAttributes(filePath, attributes | FileAttributes.ReadOnly);
            }
            else
            {
                // Remove ReadOnly attribute (make it writable)
                File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
            }
        }

        // Helper function to update the button's text based on file state
        private void UpdateToggleReadOnlyButtonText(string filePath)
        {
            FileAttributes attributes = File.GetAttributes(filePath);
            bool isReadOnly = (attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;

            ToggleReadOnlyButton.Content = isReadOnly
                ? "Toggle Read-Only (LOCKED)"
                : "Toggle Read-Only (UNLOCKED)";
        }
    }
}