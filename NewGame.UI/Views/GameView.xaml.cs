using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NewGame.UI.ViewModels;
using System.IO;
using CardType = MagicalDeckbuilder.Cards.CardType;

namespace NewGame.UI.Views;

public partial class GameView : UserControl
{
    private Point _clickStartPoint;
    private bool _isDragging;
    private const double DragThreshold = 15.0;
    private int _lastHoveredPlayerSlotIndex = -1;
    private int _lastHoveredArtifactSlotIndex = -1;
    
    // Debug file logging
    private static readonly string DebugLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
        "NewGame_debug.txt");
    
    private static void LogToFile(string msg)
    {
        try { File.AppendAllText(DebugLogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n"); }
        catch { }
    }
    
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
                LogToFile($"[DRAG] Starting drag for card: {_pendingDragCard?.Name}, Type={_pendingDragCard?.Card?.Type}, hash={_pendingDragCard?.GetHashCode()}");
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
        // Only clear if drag didn't complete
        if (_pendingDragCard != null)
        {
            // Reset the slot index in case drag ended without drop
            _lastHoveredPlayerSlotIndex = -1;
            _lastHoveredArtifactSlotIndex = -1;
        }
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

                if (card != null)
                {
                    // Check if this is a spell card
                    if (card.Card.Type == CardType.Spell && slotIndex >= 0 && slotIndex <= 11)
                    {
                        // Cast spell on the creature at this slot
                        LogToFile($"[SPELL] OnSlotDrop: Casting {card.Name} on slot {slotIndex}");
                        ViewModel?.CastSpellOnCreature(card, slotIndex);
                        return;
                    }
                    
                    // Regular card - move to slot
                    if (slotIndex >= 6 && slotIndex <= 11)
                    {
                        ViewModel?.MoveCardToSlot(card, slotIndex);
                    }
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
            ShowCardZoom();
            e.Handled = true;
        }
    }

    private void OnFieldCardRightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Border cardBorder && cardBorder.Tag is CardViewModel card)
        {
            ViewModel?.ZoomCard(card);
            ShowCardZoom();
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
            ShowCardZoom();
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

    // ========== Player Creature Slot Handlers ==========
    private void OnPlayerSlotDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            // Get global position relative to this GameView
            var pos = e.GetPosition(this);
            
            // Player slots start at around column index 0 of itemscontrol - actual positions vary
            // Better: use slot's actual position relative to GameView
            var slotPos = slot.TranslatePoint(new Point(40, 35), this);
            int slotWidth = 86;
            int index = (int)(slotPos.X / slotWidth);
            index = Math.Clamp(index, 0, 5);
            
            LogToFile($"[DRAGENTER] PlayerSlot: slotPos.X={slotPos.X}, index={index}");
            
            _lastHoveredPlayerSlotIndex = index;
            
            if (ViewModel?.FieldSlots[index + 6] == null)
            {
                slot.Background = new SolidColorBrush(Color.FromRgb(80, 120, 80));
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
    }

    private void OnPlayerSlotDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(70, 80, 70));
        }
        _lastHoveredPlayerSlotIndex = -1;
        e.Handled = true;
    }

    private void OnPlayerSlotDrop(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            int slotIndex = GetPlayerSlotIndex(slot);
            
            LogToFile($"[DROP] PlayerSlot: slotIndex={slotIndex}, lastHovered={_lastHoveredPlayerSlotIndex}, hasCardData={e.Data.GetDataPresent("CardViewModel")}");
            
            if (e.Data.GetDataPresent("CardViewModel"))
            {
                var card = e.Data.GetData("CardViewModel") as CardViewModel;
                slot.Background = new SolidColorBrush(Color.FromRgb(70, 80, 70));
                
                LogToFile($"[DROP] card={card?.Name}, Type={card?.Card?.Type}, hash={card?.GetHashCode()}");

                if (card != null)
                {
                    // Check if this is a spell card
                    if (card.Card.Type == CardType.Spell)
                    {
                        // Cast spell on the creature in this slot
                        if (slotIndex >= 0 && slotIndex < 6 && ViewModel?.FieldSlots[slotIndex + 6] != null)
                        {
                            LogToFile($"[SPELL] Casting {card.Name} on player creature at slot {slotIndex + 6}");
                            ViewModel?.CastSpellOnCreature(card, slotIndex + 6);
                        }
                        else if (slotIndex >= 0 && slotIndex < 6)
                        {
                            // No creature there - try to cast on opponent creature
                            if (ViewModel?.FieldSlots[slotIndex] != null)
                            {
                                LogToFile($"[SPELL] Casting {card.Name} on opponent creature at slot {slotIndex}");
                                ViewModel?.CastSpellOnCreature(card, slotIndex);
                            }
                            else
                            {
                                LogToFile($"[SPELL] No creature at slot to target");
                            }
                        }
                        return;
                    }
                    
                    // Regular creature card - move to slot
                    if (slotIndex >= 0 && slotIndex < 6)
                    {
                        LogToFile($"[DROP] Calling MoveCardToSlot with {slotIndex + 6}");
                        ViewModel?.MoveCardToSlot(card, slotIndex + 6);
                        LogToFile($"[DROP] After call, PlayerSlots[0] hash={ViewModel?.PlayerCreatureSlots?[0]?.GetHashCode()}");
                    }
                    else
                    {
                        LogToFile($"[DROP] FAILED: card={card != null}, slotIndex={slotIndex}");
                    }
                }
            }
            else
            {
                LogToFile("[DROP] No card data present!");
            }
            
            _lastHoveredPlayerSlotIndex = -1;
        }
        e.Handled = true;
    }

    private void OnPlayerSlotMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && sender is Border slotBorder)
        {
            if (slotBorder.Tag is CardViewModel card)
            {
                DragDrop.DoDragDrop(slotBorder, card, DragDropEffects.Move);
            }
        }
    }

    private int GetPlayerSlotIndex(Border slot)
    {
        // First check cached index from DragEnter
        if (_lastHoveredPlayerSlotIndex >= 0)
        {
            LogToFile($"[GetPlayerSlotIndex] Using cached: {_lastHoveredPlayerSlotIndex}");
            return _lastHoveredPlayerSlotIndex;
        }
        
        // Try Panel.Children.IndexOf
        if (slot.Parent is Panel panel)
        {
            int idx = panel.Children.IndexOf(slot);
            LogToFile($"[GetPlayerSlotIndex] Panel.IndexOf: {idx}");
            return idx;
        }
        
        // Try Tag as last resort
        if (slot.Tag is int tagInt)
            return tagInt;
        
        if (slot.Tag is string tagStr && int.TryParse(tagStr, out int parsed))
            return parsed;
        
        LogToFile("[GetPlayerSlotIndex] Failed to find index");
        return -1;
    }

    // ========== Opponent Event Slot Handlers ==========
    private void OnOpponentEventSlotDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border slot && ViewModel?.OpponentEventSlot == null)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(160, 60, 60));
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnOpponentEventSlotDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(80, 50, 50));
        }
        e.Handled = true;
    }

    private void OnOpponentEventSlotDrop(object sender, DragEventArgs e)
    {
        // Opponent event slots are read-only for the player
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
            // Artifact slots appear at roughly X = 500-700
            var slotPos = slot.TranslatePoint(new Point(40, 35), this);
            
            // Map X=500-700 to index 0-2
            int index = (int)((slotPos.X - 500) / 80);
            index = Math.Clamp(index, 0, 2);
            
            LogToFile($"[DRAGENTER] Artifact Slot: slotPos.X={slotPos.X}, index={index}");
            
            _lastHoveredArtifactSlotIndex = index;
            
            if (ViewModel?.PlayerArtifactSlots[index] == null)
            {
                slot.Background = new SolidColorBrush(Color.FromRgb(180, 180, 80));
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
    }

    private void OnArtifactSlotDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border slot)
        {
            slot.Background = new SolidColorBrush(Color.FromRgb(200, 200, 100));
        }
        _lastHoveredArtifactSlotIndex = -1;
        e.Handled = true;
    }

    private void OnArtifactSlotDrop(object sender, DragEventArgs e)
    {
        if (sender is Border slot && e.Data.GetDataPresent("CardViewModel"))
        {
            var card = e.Data.GetData("CardViewModel") as CardViewModel;
            slot.Background = new SolidColorBrush(Color.FromRgb(200, 200, 100));

            // Check if this is a spell card - spells can destroy artifacts
            if (card?.Card?.Type == CardType.Spell)
            {
                LogToFile($"[SPELL] Dropped on artifact slot - casting {card.Name}");
                // For now, cast as global spell
                ViewModel?.CastSpell(card);
                _lastHoveredArtifactSlotIndex = -1;
                e.Handled = true;
                return;
            }

            // Use cached index from DragEnter
            int slotIndex = _lastHoveredArtifactSlotIndex;
            
            // Fallback: calculate from Tag if not available
            if (slotIndex < 0)
            {
                if (slot.Tag is string tagStr && int.TryParse(tagStr, out int parsed))
                    slotIndex = parsed - 20;
                else if (slot.Tag is int tagInt)
                    slotIndex = tagInt - 20;
            }

            if (card != null && slotIndex >= 0 && slotIndex < 3)
            {
                ViewModel?.PlayArtifactToSlot(card, slotIndex);
            }
            
            _lastHoveredArtifactSlotIndex = -1;
        }
        e.Handled = true;
    }

    private void OnArtifactSlotMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && sender is Border slotBorder)
        {
            if (slotBorder.Tag is CardViewModel card)
            {
                DragDrop.DoDragDrop(slotBorder, card, DragDropEffects.Move);
            }
        }
    }

    // ========== Manual Card Zoom Control ==========
    private void ShowCardZoom()
    {
        if (CardZoomPopup != null && ViewModel?.ZoomedCard != null)
        {
            CardZoomPopup.DataContext = ViewModel.ZoomedCard;
            CardZoomPopup.Visibility = Visibility.Visible;
        }
    }

    public void HideCardZoom()
    {
        if (CardZoomPopup != null)
        {
            CardZoomPopup.Visibility = Visibility.Collapsed;
            CardZoomPopup.DataContext = null;
        }
    }

    // ========== Spell Drag Drop Handlers ==========
    private void OnSpellDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("CardViewModel"))
        {
            var card = e.Data.GetData("CardViewModel") as CardViewModel;
            if (card?.Card?.Type == CardType.Spell)
            {
                LogToFile("[SPELL] DragEnter spell: " + card.Name);
                // Show status message
                if (ViewModel != null)
                {
                    ViewModel.StatusMessage = $"Drag {card.Name} to target or release to cast globally";
                }
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        e.Handled = true;
    }

    private void OnSpellDragLeave(object sender, DragEventArgs e)
    {
        LogToFile("[SPELL] DragLeave");
        // Clear status message when leaving
        if (ViewModel != null && ViewModel.StatusMessage.Contains("Drag"))
        {
            ViewModel.StatusMessage = "";
        }
        e.Handled = true;
    }

    private void OnSpellDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("CardViewModel"))
        {
            var card = e.Data.GetData("CardViewModel") as CardViewModel;
            LogToFile("[SPELL] Drop: " + card?.Name + " Type=" + card?.Card?.Type);
            
            if (card != null && card.Card.Type == CardType.Spell)
            {
                // Global spell cast - no target
                if (ViewModel?.PlayerMana >= card.Card.ManaCost)
                {
                    ViewModel?.CastSpell(card);
                    LogToFile("[SPELL] Cast successfully: " + card.Name);
                }
                else
                {
                    LogToFile("[SPELL] Not enough mana!");
                }
            }
        }
        e.Handled = true;
    }
}
