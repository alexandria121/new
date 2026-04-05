using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NewGame.UI.ViewModels;

namespace NewGame.UI.Controls;

public partial class CardControl : UserControl
{
    public static readonly RoutedEvent CardDroppedEvent = EventManager.RegisterRoutedEvent(
        "CardDropped", RoutingStrategy.Bubble, typeof(EventHandler<CardDroppedEventArgs>), typeof(CardControl));

    public event EventHandler<CardDroppedEventArgs> CardDropped
    {
        add { AddHandler(CardDroppedEvent, value); }
        remove { RemoveHandler(CardDroppedEvent, value); }
    }

    private Point _startPoint;
    private bool _isDragging;

    public CardControl()
    {
        InitializeComponent();
    }

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is CardViewModel card)
        {
            card.IsSelected = !card.IsSelected;
        }
    }

    private void OnCardMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
        {
            var diff = e.GetPosition(this) - _startPoint;
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isDragging = true;
                if (DataContext is CardViewModel card)
                {
                    card.IsDragging = true;
                    var data = new DataObject("CardViewModel", card);
                    DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                    card.IsDragging = false;
                }
                _isDragging = false;
            }
        }
    }

    private void OnCardMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(this);
        base.OnMouseLeftButtonDown(e);
    }
}

public class CardDroppedEventArgs : RoutedEventArgs
{
    public CardViewModel Card { get; }
    public int SlotIndex { get; }

    public CardDroppedEventArgs(RoutedEvent routedEvent, CardViewModel card, int slotIndex)
        : base(routedEvent)
    {
        Card = card;
        SlotIndex = slotIndex;
    }
}
