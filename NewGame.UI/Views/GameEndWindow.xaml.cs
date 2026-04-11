using System.Windows;

namespace NewGame.UI.Views;

/// <summary>
/// Window displayed when the game ends (win or lose)
/// </summary>
public partial class GameEndWindow : Window
{
    public GameEndWindow(bool playerWon)
    {
        InitializeComponent();
        
        Owner = Application.Current.MainWindow;
        
        if (playerWon)
        {
            TitleText.Text = "You Won!";
            TitleText.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
            SubtitleText.Text = "Victory! More progression coming soon!";
        }
        else
        {
            TitleText.Text = "Game Over";
            TitleText.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
            SubtitleText.Text = "So sorry! More progression coming soon!";
        }
    }

    private void OnMenuClick(object sender, RoutedEventArgs e)
    {
        // Close this window and return to menu
        DialogResult = true;
        Close();
    }
}