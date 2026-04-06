using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using MagicalDeckbuilder.Storage;

namespace NewGame.UI.Views;

public partial class DeckNameDialog : Window, INotifyPropertyChanged
{
    private string _deckName = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DeckName
    {
        get => _deckName;
        set
        {
            _deckName = value;
            OnPropertyChanged();
        }
    }

    public DeckNameDialog()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        DataContext = this;
        DeckNameTextBox.Focus();
    }

    public DeckNameDialog(string suggestedName) : this()
    {
        DeckName = suggestedName;
        DeckNameTextBox.SelectAll();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Validate deck name
        if (!DeckStorageService.IsValidDeckName(DeckName, out var errorMessage))
        {
            MessageBox.Show(errorMessage, "Invalid Deck Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}