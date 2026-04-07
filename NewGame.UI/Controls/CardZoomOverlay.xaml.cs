using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NewGame.UI.ViewModels;

namespace NewGame.UI.Controls;

public partial class CardZoomOverlay : UserControl
{
    private MainViewModel? _mainViewModel;
    
    public CardZoomOverlay()
    {
        InitializeComponent();
        Focusable = true;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Find and store the MainViewModel for closing - don't overwrite DataContext
        _mainViewModel = FindMainViewModel();
        Focus();
    }

    private MainViewModel? FindMainViewModel()
    {
        DependencyObject current = this;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is MainViewModel vm)
            {
                return vm;
            }
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
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

    private void OnOverlayRightClick(object sender, MouseButtonEventArgs e)
    {
        CloseOverlay();
        e.Handled = true;
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
        // Try cached reference first, then search if needed
        if (_mainViewModel == null)
        {
            _mainViewModel = FindMainViewModel();
        }
        _mainViewModel?.CloseZoom();
        
        // Manually hide this overlay since we removed the binding
        // This ensures it closes immediately when X is clicked
        Visibility = Visibility.Collapsed;
    }
}
