using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Effects;
using NewGame.UI.ViewModels;
using MagicalDeckbuilder.Game;

namespace NewGame.UI.Views;

public partial class BattleSelectView : UserControl
{
    public BattleSelectView()
    {
        InitializeComponent();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void OnDifficultyClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string difficultyStr)
        {
            // Parse difficulty string to enum
            if (Enum.TryParse<DifficultyLevel>(difficultyStr, out var difficulty))
            {
                ViewModel?.SelectDifficultyAndStart(difficulty);
            }
        }
    }
}
