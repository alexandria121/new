using System.Windows;

namespace NewGame.UI.Views;

/// <summary>
/// Dialog for prompting user when they have unsaved changes
/// </summary>
public partial class UnsavedChangesDialog : Window
{
    /// <summary>
    /// Result of the dialog
    /// </summary>
    public enum UnsavedDialogResult
    {
        Save,
        Discard,
        Cancel
    }
    
    public UnsavedDialogResult Result { get; private set; } = UnsavedDialogResult.Cancel;
    
    public UnsavedChangesDialog()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
    }
    
    private void SaveAndExit_Click(object sender, RoutedEventArgs e)
    {
        Result = UnsavedDialogResult.Save;
        Close();
    }
    
    private void Discard_Click(object sender, RoutedEventArgs e)
    {
        Result = UnsavedDialogResult.Discard;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = UnsavedDialogResult.Cancel;
        Close();
    }
}
