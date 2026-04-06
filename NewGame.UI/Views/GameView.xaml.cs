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
    private const double DragThreshold = 15.0;
    
    // Track what was clicked for drag operations
    private CardViewModel? _pendingDragCard;
    private Border? _pendingDragSource;

    public GameView()
    {
        InitializeComponent();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    // Handle preview mouse down on hand cards - track what's being clicked
    private void OnHandCardPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _clickStartPoint = e.GetPosition(this);
        _isDragging = false;
        
        if (sender is Border cardBorder && cardBorder.Tag is CardViewModel card)
        {
            _pendingDragCard = card;
            _pendingDragSource = cardBorder;
        }
        else
        {
            _pendingDragCard = null;
            _pendingDragSource = null;
        }
    }
    
    // Handle preview mouse move - initiate drag when threshold exceeded
    private void OnHandCardPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_pendingDragCard == null || _pendingDragSource == null)
            return;
            
        if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
        {
            var currentPos = e.GetPosition(this);
            var diff = currentPos - _clickStartPoint;
            
            if (Math.Abs(diff.X) > DragThreshold || Math.Abs(diff.Y) > DragThreshold)
            {
                _isDragging = true;
                
                // Use explicit DataObject to ensure proper data format
                var dataObject = new DataObject("CardViewModel", _pendingDragCard);
                DragDrop.DoDragDrop(_pendingDragSource, dataObject, DragDropEffects.Move);
                
                _isDragging = false;
                _pendingDragCard = null;
                _pendingDragSource = null;
            }
        }
    }

    // Clean up when mouse is released
    private void OnHandCardPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        _pendingDragCard = null;
        _pendingDragSource = null;
    }

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
            // Handle Tag which may be string or int
            int slotIndex = -1;
            if (slot.Tag is int tagInt)
                slotIndex = tagInt;
            else if (slot.Tag is string tagStr && int.TryParse(tagStr, out int parsed))
                slotIndex = parsed;
                
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
        if (sender is Border slot)
        {
            // Handle Tag which may be string or int
            int slotIndex = -1;
            if (slot.Tag is int tagInt)
                slotIndex = tagInt;
            else if (slot.Tag is string tagStr && int.TryParse(tagStr, out int parsed))
                slotIndex = parsed;
            
            if (e.Data.GetDataPresent("CardViewModel"))
            {
                var card = e.Data.GetData("CardViewModel") as CardViewModel;
                slot.Background = new SolidColorBrush(Color.FromRgb(70, 80, 70));

                if (card != null && slotIndex >= 6 && slotIndex <= 11)
                {
                    ViewModel?.MoveCardToSlot(card, slotIndex);
                }
            }
        }
        e.Handled = true;
    }

    private void OnComboSlotDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border slot && ViewModel?.IsComboLocked != true)
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
        if (ViewModel?.IsComboLocked == true) return;

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
        if (ViewModel?.IsComboLocked == true) return;

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

    private void OnHandCardRightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Border cardBorder && cardBorder.Tag is CardViewModel card)
        {
            ViewModel?.ZoomCard(card);
            e.Handled = true;
        }
    }

    private void OnFieldCardRightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Border cardBorder && cardBorder.Tag is CardViewModel card)
        {
            ViewModel?.ZoomCard(card);
            e.Handled = true;
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

    private void OnWeaponSlotDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(100, 80, 60));
            e.Effects = DragDropEffects.Move;
        }
        e.Handled = true;
    }

    private void OnWeaponSlotDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(70, 80, 70));
        }
        e.Handled = true;
    }

    private void OnWeaponSlotDrop(object sender, DragEventArgs e)
    {
        if (sender is Border slot && e.Data.GetDataPresent("CardViewModel"))
        {
            var card = e.Data.GetData("CardViewModel") as CardViewModel;
            slot.Background = new SolidColorBrush(Color.FromRgb(70, 80, 70));

            if (card != null)
            {
                ViewModel?.EquipWeapon(card);
            }
        }
        e.Handled = true;
    }

    private void OnArmorSlotDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(60, 80, 100));
            e.Effects = DragDropEffects.Move;
        }
        e.Handled = true;
    }

    private void OnArmorSlotDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(70, 80, 70));
        }
        e.Handled = true;
    }

    private void OnArmorSlotDrop(object sender, DragEventArgs e)
    {
        if (sender is Border slot && e.Data.GetDataPresent("CardViewModel"))
        {
            var card = e.Data.GetData("CardViewModel") as CardViewModel;
            slot.Background = new SolidColorBrush(Color.FromRgb(70, 80, 70));

            if (card != null)
            {
                ViewModel?.EquipArmor(card);
            }
        }
        e.Handled = true;
    }

    private void OnEquipmentRightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Border cardBorder && cardBorder.Tag is CardViewModel card)
        {
            ViewModel?.ZoomCard(card);
            e.Handled = true;
        }
    }

    private void OnComboResultMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && 
            sender is Border comboSlot && 
            ViewModel?.IsComboLocked == true)
        {
            if (comboSlot.Tag is CardViewModel card)
            {
                DragDrop.DoDragDrop(comboSlot, card, DragDropEffects.Move);
            }
        }
    }

    private void OnComboResultDrop(object sender, DragEventArgs e)
    {
        if (sender is Border comboSlot && ViewModel?.IsComboLocked == true)
        {
            ViewModel?.TakeComboResult();
        }
        e.Handled = true;
    }

    // ========== Event Slot Handlers ==========
    private void OnEventSlotDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border slot && ViewModel?.PlayerEventSlot == null)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(160, 130, 60));
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnEventSlotDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(200, 160, 80));
        }
        e.Handled = true;
    }

    private void OnEventSlotDrop(object sender, DragEventArgs e)
    {
        if (sender is Border slot && e.Data.GetDataPresent("CardViewModel"))
        {
            var card = e.Data.GetData("CardViewModel") as CardViewModel;
            slot.Background = new SolidColorBrush(Color.FromRgb(200, 160, 80));

            if (card != null)
            {
                // Find slot index (only 1 event slot, so just use 0)
                ViewModel?.PlayEventToSlot(card);
            }
        }
        e.Handled = true;
    }

    // ========== Artifact Slot Handlers ==========
    private void OnArtifactSlotDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            // Determine slot index from Tag
            int slotIndex = 0;
            if (slot.Tag is string tagStr && int.TryParse(tagStr, out int parsed))
            {
                slotIndex = parsed - 20; // 20, 21, 22 -> 0, 1, 2
            }
            else if (slot.Tag is int tagInt)
            {
                slotIndex = tagInt - 20;
            }
            
            if (slotIndex >= 0 && slotIndex < 3 && ViewModel?.PlayerArtifactSlots[slotIndex] == null)
            {
                slot.Background = new SolidColorBrush(Color.FromRgb(180, 180, 80));
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        e.Handled = true;
    }

    private void OnArtifactSlotDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(200, 200, 100));
        }
        e.Handled = true;
    }

    private void OnArtifactSlotDrop(object sender, DragEventArgs e)
    {
        if (sender is Border slot && e.Data.GetDataPresent("CardViewModel"))
        {
            var card = e.Data.GetData("CardViewModel") as CardViewModel;
            slot.Background = new SolidColorBrush(Color.FromRgb(200, 200, 100));

            // Determine slot index from Tag
            int slotIndex = 0;
            if (slot.Tag is string tagStr && int.TryParse(tagStr, out int parsed))
            {
                slotIndex = parsed - 20; // 20, 21, 22 -> 0, 1, 2
            }
            else if (slot.Tag is int tagInt)
            {
                slotIndex = tagInt - 20;
            }

            if (card != null && slotIndex >= 0 && slotIndex < 3)
            {
                ViewModel?.PlayArtifactToSlot(card, slotIndex);
            }
        }
        e.Handled = true;
    }
}
