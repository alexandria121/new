using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NewGame.UI.ViewModels;

namespace NewGame.UI.Views;

public partial class DeckBuilderView : UserControl
{
    private Point _startPoint;
    private CardViewModel? _draggedCard;

    public DeckBuilderView()
    {
        InitializeComponent();
    }

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
        
        // Set SelectedCard for preview
        if (sender is Border border && border.DataContext is CardViewModel card)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedCard = card;
            }
        }
    }

    private void Card_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var currentPosition = e.GetPosition(null);
        var diff = _startPoint - currentPosition;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            var border = sender as Border;
            var card = border?.DataContext as CardViewModel;

            if (card != null)
            {
                _draggedCard = card;
                var data = new DataObject("CardViewModel", card);
                DragDrop.DoDragDrop(border!, data, DragDropEffects.Copy);
            }
        }
    }

    private void Deck_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("CardViewModel"))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Deck_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("CardViewModel"))
        {
            var card = e.Data.GetData("CardViewModel") as CardViewModel;
            if (card != null && DataContext is MainViewModel vm)
            {
                vm.AddCardToDeck(card);
            }
        }
    }

    private void DeckCard_RightClick(object sender, MouseButtonEventArgs e)
    {
        var border = sender as Border;
        var card = border?.DataContext as CardViewModel;

        if (card != null && DataContext is MainViewModel vm)
        {
            vm.RemoveCardFromDeck(card);
        }
    }
}
