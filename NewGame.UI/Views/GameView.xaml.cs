using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NewGame.UI.ViewModels;

namespace NewGame.UI.Views;

public partial class GameView : UserControl
{
    public GameView()
    {
        InitializeComponent();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void OnSlotDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            var slotIndex = (int)(slot.Tag ?? -1);
            if (slotIndex >= 6 && slotIndex <= 11 && ViewModel?.FieldSlots[slotIndex] == null)
            {
                slot.Background = new SolidColorBrush(Color.FromRgb(80, 120, 80));
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        e.Handled = true;
    }

    private void OnSlotDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(70, 80, 70));
        }
        e.Handled = true;
    }

    private void OnSlotDrop(object sender, DragEventArgs e)
    {
        if (sender is Border slot && e.Data.GetDataPresent("CardViewModel"))
        {
            var card = e.Data.GetData("CardViewModel") as CardViewModel;
            var slotIndex = (int)(slot.Tag ?? -1);

            slot.Background = new SolidColorBrush(Color.FromRgb(70, 80, 70));

            if (card != null && slotIndex >= 6 && slotIndex <= 11)
            {
                ViewModel?.PlayCardToSlot(card, slotIndex);
            }
        }
        e.Handled = true;
    }

    private void OnComboSlotDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(70, 70, 90));
        }
        e.Handled = true;
    }

    private void OnComboSlotDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(70, 70, 70));
        }
        e.Handled = true;
    }

    private void OnComboSlot1Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("CardViewModel"))
        {
            var card = e.Data.GetData("CardViewModel") as CardViewModel;
            if (card != null)
            {
                ViewModel?.SetComboCard(card, 1);
            }
        }
        e.Handled = true;
    }

    private void OnComboSlot2Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("CardViewModel"))
        {
            var card = e.Data.GetData("CardViewModel") as CardViewModel;
            if (card != null)
            {
                ViewModel?.SetComboCard(card, 2);
            }
        }
        e.Handled = true;
    }
}
