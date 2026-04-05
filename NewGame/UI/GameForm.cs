using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Reflection;
using MagicalDeckbuilder.Decks;
using MagicalDeckbuilder.Game;
using MagicalDeckbuilder.UI;
using Card = MagicalDeckbuilder.Cards.Card;
using static MagicalDeckbuilder.Decks.PileType;
using static MagicalDeckbuilder.Cards.CardType;

namespace MagicalDeckbuilder.UI;

/// <summary>
/// Custom Panel that properly handles mouse and click events
/// Standard Panel doesn't have ControlStyles.StandardClick enabled by default
/// </summary>
public class ClickablePanel : Panel
{
    public ClickablePanel()
    {
        // Enable standard click style so mouse events fire
        SetStyle(ControlStyles.StandardClick, true);
    }
}

/// <summary>
/// Main game form with playing field, opponent area, and player area
/// </summary>
public class GameForm : Form
{
    private readonly Panel _opponentArea;
    private readonly Panel _playingField;
    private readonly Panel _playerArea;
    private readonly Label _opponentLabel;
    private readonly Label _playerLabel;
    private readonly Label _turnIndicator;
    private readonly Label _manaLabel;
    private readonly Label _statusLabel;  // For temporary status messages
    private readonly Button _passTurnButton;
    private Action<string, Color>? _showStatus;  // Helper to show status messages
    
    private readonly DeckManager _playerDeck;
    private readonly DeckManager _opponentDeck;
    private FlowLayoutPanel _playerCardsPanel;
    private FlowLayoutPanel _opponentCardsPanel;
    
    // Field slots for cards in play (6 opponent slots at indices 0-5, 6 player slots at indices 6-11)
    private readonly Panel[] _fieldSlots;
    private readonly Card?[] _cardsInSlots;
    
    // Turn system
    private bool _isPlayerTurn = true;
    private int _currentMana = 2;
    private int _maxMana = 10;
    private int _turnCount = 1;
    private int _opponentCurrentMana = 2;
    private int _opponentMaxMana = 10;
    
    // Drag and drop - store card being dragged for direct reference
    private Card? _draggedCard;
    private bool _isDragging = false;  // Track if we're currently dragging
    
    // Cancellation token for async operations to ensure proper cleanup
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    
    public GameForm()
    {
        // Initialize cancellation token for async operations
        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationToken = _cancellationTokenSource.Token;
        
        // Initialize deck managers
        _playerDeck = new DeckManager();
        _opponentDeck = new DeckManager();
        
        // Generate and shuffle player deck
        var playerCards = CardFactory.GenerateRandomDeck();
        _playerDeck.InitializeDeck(playerCards);
        
        // Generate and shuffle opponent deck
        var opponentCards = CardFactory.GenerateRandomDeck();
        _opponentDeck.InitializeDeck(opponentCards);
        
        // Initialize field slots arrays (12 total: 6 opponent + 6 player)
        _fieldSlots = new Panel[12];
        _cardsInSlots = new Card?[12];
        
        // Initialize mana for first turn (starting at 2, max 10)
        _currentMana = 2;
        
        // Form setup
        Text = "Card Game";
        Size = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 40);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        
        // Enable drag-drop on the form itself
        AllowDrop = true;
        
        // Handle form-level drag events
        DragEnter += Form_DragEnter;
        DragOver += (s, e) => e.Effect = DragDropEffects.Move;
        
        // Use nested Panels with Dock instead of TableLayoutPanel
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        
        // Set up layout container for vertical stacking - use percentage-based sizes
        var layoutContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(0)
        };
        // Use percentage-based sizes for rows
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Row 0 - Top (AutoSize for turn panel)
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));  // Row 1 - Opponent (30%)
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Row 2 - Playing field label (AutoSize)
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));  // Row 3 - Playing field (40%)
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));  // Row 4 - Player (30%)
        
        // Turn indicator panel - use FlowLayoutPanel for proper layout with AutoSize
        var turnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.FromArgb(50, 50, 60),
            Padding = new Padding(10, 10, 10, 10),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        
        // Positioned controls
        _turnIndicator = new Label
        {
            Text = "Your Turn",
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.LimeGreen,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Margin = new Padding(0, 0, 20, 0)
        };
        turnPanel.Controls.Add(_turnIndicator);
        
        // Status label for temporary messages (like "Insufficient mana") - takes remaining space
        _statusLabel = new Label
        {
            Text = "",
            Font = new Font("Arial", 9, FontStyle.Bold),
            ForeColor = Color.Orange,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Margin = new Padding(0, 0, 20, 0)
        };
        
        // Helper to show temporary status message
        _showStatus = async (message, color) =>
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = color;
            
            try
            {
                // Clear after 2 seconds - with cancellation check
                await Task.Delay(2000, _cancellationToken).ConfigureAwait(false);
                
                // Only clear if form is still visible and message hasn't changed
                if (!IsDisposed && _statusLabel.Text == message)
                {
                    _statusLabel.Text = "";
                }
            }
            catch (TaskCanceledException)
            {
                // Expected - task was cancelled on form close
            }
        };
        
        turnPanel.Controls.Add(_statusLabel);
        
        // Mana counter - positioned in the middle
        _manaLabel = new Label
        {
            Text = $"Mana: {_currentMana}/{_maxMana}",
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.Cyan,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Margin = new Padding(0, 0, 20, 0)
        };
        turnPanel.Controls.Add(_manaLabel);
        
        // End Turn button - positioned on the far right
        _passTurnButton = new Button
        {
            Text = "End Turn",
            Font = new Font("Arial", 9, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(200, 100, 0),
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(15, 8, 15, 8)
        };
        _passTurnButton.FlatAppearance.BorderSize = 2;
        _passTurnButton.Click += PassTurnButton_Click;
        turnPanel.Controls.Add(_passTurnButton);
        
        layoutContainer.Controls.Add(turnPanel, 0, 0);
        
        // Opponent area (top) - cards hidden
        _opponentArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 50),
            BorderStyle = BorderStyle.FixedSingle
        };
        
        _opponentLabel = new Label
        {
            Text = "Opponent's Hand",
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 80,
            AutoSize = false,
            Padding = new Padding(5, 0, 5, 0)
        };
        _opponentArea.Controls.Add(_opponentLabel);
        
        // Add opponent card backs and deck info
        var opponentTopPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            RowCount = 1,
            ColumnCount = 2,
            Height = 40
        };
        opponentTopPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        opponentTopPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        
        var opponentDeckInfo = new Label
        {
            Text = $"Deck: {_opponentDeck.GetPileCount(PileType.DrawPile)} cards",
            ForeColor = Color.LightGray,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 0, 0, 0)
        };
        opponentTopPanel.Controls.Add(opponentDeckInfo, 0, 0);
        
        var opponentHandInfo = new Label
        {
            Text = $"Hand: {_opponentDeck.GetPileCount(PileType.Hand)} cards",
            ForeColor = Color.LightGray,
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 10, 0)
        };
        opponentTopPanel.Controls.Add(opponentHandInfo, 1, 0);
        _opponentArea.Controls.Add(opponentTopPanel);
        
        // Add placeholder card backs for opponent
        _opponentCardsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };
        
        // Add 5 hidden card backs for opponent (starting hand)
        _opponentDeck.DrawCards(5);
        UpdateOpponentDisplay();
        _opponentArea.Controls.Add(_opponentCardsPanel);
        
        layoutContainer.Controls.Add(_opponentArea, 0, 1);
        
        // Playing field label row
        var fieldLabelPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 90,
            BackColor = Color.FromArgb(45, 70, 45),
            Padding = new Padding(5, 10, 5, 10)
        };
        
        var fieldLabel = new Label
        {
            Text = "Playing Field - Drag cards here to play them",
            Font = new Font("Arial", 7, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        fieldLabelPanel.Controls.Add(fieldLabel);
        
        layoutContainer.Controls.Add(fieldLabelPanel, 0, 2);
        
        // Playing field (middle) - 6 opponent slots at top, 6 player slots at bottom
        _playingField = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(50, 80, 50), // Green felt-like color
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10)
        };
        
        // Create slots container with 2 rows (opponent row, player row) and 6 columns
        var slotsContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 6,
            Margin = new Padding(10)
        };
        
        // Both rows share the available space equally
        slotsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        slotsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        
        // 6 equal columns for the 6 slots
        for (int i = 0; i < 6; i++)
        {
            slotsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.67f));
        }
        
        _playingField.Controls.Add(slotsContainer);
        
        // Create 12 slots (6 opponent at top indices 0-5, 6 player at bottom indices 6-11)
        for (int i = 0; i < 12; i++)
        {
            bool isOpponentSlot = i < 6;
            
            var slot = new Panel
            {
                BackColor = isOpponentSlot ? Color.FromArgb(80, 50, 50) : Color.FromArgb(50, 70, 50),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(3),
                Tag = i,
                AllowDrop = true
            };
            
            // Add slot label
            var slotLabel = new Label
            {
                Text = isOpponentSlot ? $"Opp {i + 1}" : $"Your {i - 5}",
                ForeColor = Color.FromArgb(180, 180, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            slot.Controls.Add(slotLabel);
            
            // Add drag handlers for player slots (indices 6-11)
            if (!isOpponentSlot)
            {
                slot.DragEnter += Slot_DragEnter;
                slot.DragDrop += Slot_DragDrop;
                slot.DragLeave += Slot_DragLeave;
            }
            
            _fieldSlots[i] = slot;
            
            // Add to table layout: row 0 for opponent (0-5), row 1 for player (6-11)
            int col = i % 6;
            int row = i < 6 ? 0 : 1;
            slotsContainer.Controls.Add(slot, col, row);
        }
        
        layoutContainer.Controls.Add(_playingField, 0, 3);
        
        // Player area (bottom) - player's drawn cards
        _playerArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 50),
            BorderStyle = BorderStyle.FixedSingle
        };
        
        _playerLabel = new Label
        {
            Text = "Your Hand - Click for details, drag to play",
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 80,
            AutoSize = false,
            Padding = new Padding(5, 0, 5, 0)
        };
        _playerArea.Controls.Add(_playerLabel);
        
        // Add player deck info
        var playerTopPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            RowCount = 1,
            ColumnCount = 2,
            Height = 40
        };
        playerTopPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        playerTopPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        
        var deckInfoLabel = new Label
        {
            Text = $"Deck: {_playerDeck.GetPileCount(DrawPile)} cards",
            ForeColor = Color.LightGray,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 0, 0, 0)
        };
        playerTopPanel.Controls.Add(deckInfoLabel, 0, 0);
        
        var turnInfoLabel = new Label
        {
            Text = $"Turn {_turnCount}",
            ForeColor = Color.LightGray,
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 10, 0)
        };
        playerTopPanel.Controls.Add(turnInfoLabel, 1, 0);
        _playerArea.Controls.Add(playerTopPanel);
        
        // Add player cards panel
        _playerCardsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10),
            AllowDrop = true  // Enable drag-drop on the card panel
        };
        
        // Also enable drag-drop on the player area
        _playerArea.AllowDrop = true;
        
        // Draw initial hand (4 cards)
        _playerDeck.DrawCards(4);
        UpdatePlayerDisplay();
        _playerArea.Controls.Add(_playerCardsPanel);
        
        layoutContainer.Controls.Add(_playerArea, 0, 4);
        
        mainPanel.Controls.Add(layoutContainer);
        Controls.Add(mainPanel);
        
        // Handle resize to scale elements
        Resize += GameForm_Resize;
        
        // Handle form closing to properly cancel async operations
        FormClosing += GameForm_FormClosing;
    }
    
    /// <summary>
    /// Handle form closing - cancel all async operations to ensure clean shutdown
    /// </summary>
    private void GameForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // Cancel all async operations
        _cancellationTokenSource?.Cancel();
        
        // Dispose the cancellation token source
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        
        // Force immediate exit - don't wait for message pump
        Environment.Exit(0);
    }
    
    /// <summary>
    /// Handle drag enter on the form - set effect based on what's being dragged
    /// </summary>
    private void Form_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.Text))
        {
            e.Effect = DragDropEffects.Move;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }
    
    /// <summary>
    /// Handle drag enter on a player slot (indices 6-11) - highlight the slot
    /// </summary>
    private void Slot_DragEnter(object? sender, DragEventArgs e)
    {
        if (sender is Panel slot && slot.Tag is int slotIndex)
        {
            // Only player slots (indices 6-11) accept drops
            if (slotIndex >= 6 && slotIndex <= 11)
            {
                // Check if slot is empty
                if (_cardsInSlots[slotIndex] == null)
                {
                    slot.BackColor = Color.FromArgb(100, 150, 100);
                    e.Effect = DragDropEffects.Move;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
    }
    
    /// <summary>
    /// Handle drag leave on a slot - reset slot color
    /// </summary>
    private void Slot_DragLeave(object? sender, EventArgs e)
    {
        if (sender is Panel slot && slot.Tag is int slotIndex)
        {
            // Reset to appropriate color based on slot type
            if (slotIndex < 6)
            {
                slot.BackColor = Color.FromArgb(80, 50, 50);  // Opponent slot color
            }
            else
            {
                slot.BackColor = Color.FromArgb(50, 70, 50);  // Player slot color
            }
        }
    }
    
    /// <summary>
    /// Handle drop on a player slot - play the card
    /// </summary>
    private void Slot_DragDrop(object? sender, DragEventArgs e)
    {
        if (sender is Panel slot && slot.Tag is int slotIndex)
        {
            // Reset slot color based on slot type
            if (slotIndex < 6)
            {
                slot.BackColor = Color.FromArgb(80, 50, 50);  // Opponent slot color
            }
            else
            {
                slot.BackColor = Color.FromArgb(50, 70, 50);  // Player slot color
            }
            
            // Only player slots (6-11) accept drops
            if (slotIndex < 6 || slotIndex > 11)
                return;
            
            // Check if slot is already occupied
            if (_cardsInSlots[slotIndex] != null)
            {
                MessageBox.Show("This slot is already occupied!", "Slot Full", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Get card data from the currently dragged card
            var card = _draggedCard;
            if (card == null)
                return;
            
            // Verify the card is still in hand
            if (!_playerDeck.Hand.Any(c => c.Id == card.Id))
                return;
            
            // Check mana cost
            if (card.ManaCost > _currentMana)
            {
                _showStatus?.Invoke($"Insufficient mana! Need {card.ManaCost}, have {_currentMana}", Color.Orange);
                return;
            }
            
            // Play the card
            PlayCardToSlot(card, slotIndex);
        }
    }
    
    /// <summary>
    /// Play a card to a specific slot on the field
    /// </summary>
    private void PlayCardToSlot(Card card, int slotIndex)
    {
        // Deduct mana cost
        _currentMana -= card.ManaCost;
        
        // Move card from hand to in-play pile
        _playerDeck.PlayCard(card.Id);
        
        // Store card in slot
        _cardsInSlots[slotIndex] = card;
        
        // Update the slot display
        UpdateSlotDisplay(slotIndex);
        
        // Update mana display
        UpdateManaDisplay();
        
        // Update player hand display
        UpdatePlayerDisplay();
        
        Console.WriteLine($"Played {card.Name} to slot {slotIndex}");
    }
    
    /// <summary>
    /// Update the display of a slot with the card in it
    /// </summary>
    private void UpdateSlotDisplay(int slotIndex)
    {
        var slot = _fieldSlots[slotIndex];
        var card = _cardsInSlots[slotIndex];
        
        // Clear existing controls
        slot.Controls.Clear();
        
        if (card != null)
        {
            // Create a card panel for display in the slot
            var cardPanel = CreateCardPanel(card);
            cardPanel.Dock = DockStyle.Fill;
            slot.Controls.Add(cardPanel);
        }
        else
        {
            // Show empty slot label
            bool isOpponentSlot = slotIndex < 6;
            var slotLabel = new Label
            {
                Text = isOpponentSlot ? $"Opp {slotIndex + 1}" : $"Your {slotIndex - 5}",
                ForeColor = Color.FromArgb(180, 180, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            slot.Controls.Add(slotLabel);
        }
    }
    
    private async void PassTurnButton_Click(object? sender, EventArgs e)
    {
        if (_isPlayerTurn)
        {
            // Switch to opponent turn
            _isPlayerTurn = false;
            _turnIndicator.Text = "Opponent's Turn";
            _turnIndicator.ForeColor = Color.Orange;
            _passTurnButton.Enabled = false;
            
            // Opponent refills 1 mana (up to max of 10)
            _opponentCurrentMana = Math.Min(_opponentMaxMana, _opponentCurrentMana + 1);
            
            // Opponent draws a card
            _opponentDeck.DrawCards(1);
            UpdateOpponentDisplay();
            
            // Simple opponent AI - pass after a delay
            try
            {
                await Task.Delay(1500, _cancellationToken).ConfigureAwait(false);
                
                if (!IsDisposed && IsHandleCreated)
                {
                    // Marshal back to UI thread safely
                    try
                    {
                        if (!IsDisposed)
                        {
                            BeginInvoke(StartPlayerTurn);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Form was disposed - ignore
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Expected - task was cancelled on form close
            }
        }
    }
    
    private void StartPlayerTurn()
    {
        // Start player turn
        _isPlayerTurn = true;
        _turnCount++;
        
        // Refill 1 mana (up to max of 10)
        _currentMana = Math.Min(_maxMana, _currentMana + 1);
        
        // Draw a card
        _playerDeck.DrawCards(1);
        
        // Update UI
        _turnIndicator.Text = "Your Turn";
        _turnIndicator.ForeColor = Color.LimeGreen;
        _passTurnButton.Enabled = true;
        
        UpdatePlayerDisplay();
        UpdateManaDisplay();
    }
    
    private void UpdateManaDisplay()
    {
        _manaLabel.Text = $"Mana: {_currentMana}/{_maxMana}";
    }
    
    private void UpdatePlayerDisplay()
    {
        _playerCardsPanel.Controls.Clear();
        
        foreach (var card in _playerDeck.Hand)
        {
            var playerCard = CreateCardPanel(card);
            
            // Make cards clickable to show detail form
            playerCard.Click += (s, e) => ShowCardDetail(card);
            playerCard.Cursor = Cursors.Hand;
            
            // Add drag support - start drag on left mouse button
            playerCard.MouseDown += (s, e) => 
            {
                if (e.Button == MouseButtons.Left)
                {
                    StartDrag(card, e);
                }
            };
            
            // Also add handlers to ALL child controls (labels)
            foreach (Control child in playerCard.Controls)
            {
                child.Cursor = Cursors.Hand;
                child.Click += (s, e) => ShowCardDetail(card);
                
                child.MouseDown += (s, e) => 
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        StartDrag(card, e);
                    }
                };
            }
            
            // Add tooltip showing if playable
            if (card.ManaCost <= _currentMana)
            {
                playerCard.BorderStyle = BorderStyle.FixedSingle;
            }
            
            _playerCardsPanel.Controls.Add(playerCard);
        }
    }
    
    /// <summary>
    /// Start dragging a card
    /// </summary>
    private void StartDrag(Card card, MouseEventArgs e)
    {
        // Only allow drag if it's player's turn (but allow regardless of mana cost)
        if (!_isPlayerTurn)
        {
            MessageBox.Show("Wait for your turn!", "Not Your Turn", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        // Store reference to the card being dragged
        _draggedCard = card;
        
        // Create a DataObject with a simple text format to trigger drag
        // The actual card data is stored in _draggedCard field
        var data = new DataObject();
        data.SetData(DataFormats.Text, card.Id);
        
        try
        {
            // Start drag-drop operation
            DoDragDrop(data, DragDropEffects.Move);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DragDrop error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Show the detail form for a card when clicked
    /// </summary>
    private void ShowCardDetail(Card card)
    {
        var detailForm = new MagicalDeckbuilder.UI.CardDetailForm(card);
        detailForm.Owner = this;
        detailForm.ShowDialog();
    }
    
    private void UpdateOpponentDisplay()
    {
        _opponentCardsPanel.Controls.Clear();
        
        // Show hidden card backs for opponent's hand
        foreach (var card in _opponentDeck.Hand)
        {
            var cardBack = new Panel
            {
                Width = 175,
                Height = 245,
                BackColor = Color.FromArgb(139, 69, 19), // Brown card back
                Margin = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            var cardLabel = new Label
            {
                Text = "?",
                ForeColor = Color.FromArgb(200, 200, 200),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Arial", 24, FontStyle.Bold)
            };
            cardBack.Controls.Add(cardLabel);
            _opponentCardsPanel.Controls.Add(cardBack);
        }
    }
    
    private Panel CreateCardPanel(Card card)
    {
        // Use custom ClickablePanel that has ControlStyles.StandardClick enabled
        var cardPanel = new ClickablePanel
        {
            Width = 263,
            Height = 368,
            BackColor = GetCardColor(card.Element.ToString()),
            Margin = new Padding(5),
            BorderStyle = BorderStyle.FixedSingle
        };
        
        // Card name at top
        var nameLabel = new Label
        {
            Text = card.Name,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.TopCenter,
            Dock = DockStyle.Top,
            Height = 50,
            Font = new Font("Arial", 10, FontStyle.Bold),
            Padding = new Padding(2)
        };
        
        cardPanel.Controls.Add(nameLabel);
        
        // Card type in middle
        var typeLabel = new Label
        {
            Text = card.Type.ToString(),
            ForeColor = Color.FromArgb(200, 200, 200),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Font = new Font("Arial", 12)
        };
        cardPanel.Controls.Add(typeLabel);
        
        // Mana cost at bottom
        var manaLabel = new Label
        {
            Text = $"{card.ManaCost} Mana",
            ForeColor = Color.FromArgb(100, 200, 255),
            TextAlign = ContentAlignment.BottomCenter,
            Dock = DockStyle.Bottom,
            Height = 40,
            Font = new Font("Arial", 12, FontStyle.Bold)
        };
        cardPanel.Controls.Add(manaLabel);
        
        return cardPanel;
    }
    
    private Color GetCardColor(string element)
    {
        return element switch
        {
            "Fire" => Color.FromArgb(180, 50, 50),
            "Water" => Color.FromArgb(50, 80, 180),
            "Earth" => Color.FromArgb(100, 80, 50),
            "Air" => Color.FromArgb(180, 180, 200),
            "Light" => Color.FromArgb(255, 255, 150),
            "Dark" => Color.FromArgb(80, 50, 80),
            "Arcane" => Color.FromArgb(150, 50, 180),
            "Nature" => Color.FromArgb(50, 150, 80),
            _ => Color.FromArgb(70, 70, 90)
        };
    }
    
    private void GameForm_Resize(object? sender, EventArgs e)
    {
        // Scale font sizes based on form dimensions (reduced by 25%)
        float scaleFactor = Math.Min(Width / 1200f, Height / 800f);
        scaleFactor = Math.Max(0.5f, Math.Min(2f, scaleFactor));
        
        _turnIndicator.Font = new Font("Arial", (float)(10 * scaleFactor), FontStyle.Bold);
        _manaLabel.Font = new Font("Arial", (float)(10 * scaleFactor), FontStyle.Bold);
        _opponentLabel.Font = new Font("Arial", (float)(10 * scaleFactor), FontStyle.Bold);
        _playerLabel.Font = new Font("Arial", (float)(10 * scaleFactor), FontStyle.Bold);
    }
}