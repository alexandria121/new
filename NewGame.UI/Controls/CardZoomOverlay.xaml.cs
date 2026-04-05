using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NewGame.UI.ViewModels;

namespace NewGame.UI.Controls;

public partial class CardZoomOverlay : UserControl
{
    public CardZoomOverlay()
    {
        InitializeComponent();
        Focusable = true;
        Loaded += (s, e) => Focus();
    }

    private void OnOverlayClick(object sender, MouseButtonEventArgs e)
    {
        // Don't close if clicking on the card popup itself
        if (e.OriginalSource is Border border && border.Name == "")
        {
            return;
        }
        
        // Close when clicking on the overlay background
        if (sender is Border)
        {
            CloseOverlay();
        }
    }

    private void OnOverlayBackgroundClick(object sender, MouseButtonEventArgs e)
    {
        CloseOverlay();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        CloseOverlay();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape || e.Key == Key.Enter || e.Key == Key.Space)
        {
            CloseOverlay();
            e.Handled = true;
        }
    }

    private void CloseOverlay()
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.CloseZoom();
        }
    }
}
