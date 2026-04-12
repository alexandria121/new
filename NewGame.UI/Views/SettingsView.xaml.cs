using System.Windows.Controls;
using System.Windows;
using NewGame.UI.ViewModels;

namespace NewGame.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Set the dropdown to current setting value
        var currentHealth = AppSettings.Instance.StartingHealth;
        foreach (ComboBoxItem item in StartingHealthCombo.Items)
        {
            if (item.Tag is string tag && int.TryParse(tag, out int value) && value == currentHealth)
            {
                item.IsSelected = true;
                break;
            }
        }
    }

    private void StartingHealthCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StartingHealthCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            if (int.TryParse(tag, out int health))
            {
                AppSettings.Instance.StartingHealth = health;
                AppSettings.Instance.Save();
            }
        }
    }
}