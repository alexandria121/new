using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NewGame.UI.ViewModels;

namespace NewGame.UI.Views;

public partial class GameView : UserControl
{
    private Point _clickStartPoint;
    private bool _isDragging;
    private const double DragThreshold = 5.0;

    public GameView()
    {
        InitializeComponent();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _clickStartPoint = e.GetPosition(this);
        _isDragging = false;
        base.OnMouseLeftButtonDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
        {
            var currentPos = e.GetPosition(this);
            var diff = currentPos - _clickStartPoint;
            if (Math.Abs(diff.X) > DragThreshold || Math.Abs(diff.Y) > DragThreshold)
            {
                _isDragging = true;
            }
        }
        base.OnMouseMove(e);
    }

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
                ViewModel?.MoveCardToSlot(card, slotIndex);
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

    private void OnHandCardMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && sender is Border cardBorder)
        {
            if (cardBorder.Tag is CardViewModel card)
            {
                DragDrop.DoDragDrop(cardBorder, card, DragDropEffects.Move);
            }
        }
    }

    private void OnHandCardClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Border cardBorder && cardBorder.Tag is CardViewModel card)
        {
            // Only zoom if it wasn't a drag
            if (!_isDragging)
            {
                ViewModel?.ZoomCard(card);
            }
            _isDragging = false;
        }
    }

    private void OnSlotMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && sender is Border slotBorder)
        {
            if (slotBorder.Tag is CardViewModel card)
            {
                DragDrop.DoDragDrop(slotBorder, card, DragDropEffects.Move);
            }
        }
    }

    private void OnFieldCardClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Border cardBorder && cardBorder.Tag is CardViewModel card)
        {
            // Only zoom if it wasn't a drag
            if (!_isDragging)
            {
                ViewModel?.ZoomCard(card);
            }
            _isDragging = false;
        }
        e.Handled = true;
    }
}
