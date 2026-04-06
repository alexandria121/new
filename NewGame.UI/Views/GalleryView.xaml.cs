using System.Windows.Controls;
using System.Windows.Input;
using NewGame.UI.ViewModels;

namespace NewGame.UI.Views;

public partial class GalleryView : UserControl
{
    public GalleryView()
    {
        InitializeComponent();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void OnCardRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is CardViewModel card)
        {
            ViewModel?.ZoomCard(card);
            e.Handled = true;
        }
    }
}
