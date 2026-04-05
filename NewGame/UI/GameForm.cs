using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Reflection;
using MagicalDeckbuilder.Decks;
using MagicalDeckbuilder.Game;
using MagicalDeckbuilder.UI;
using MagicalDeckbuilder.Combining;
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
    
    // Health tracking labels in the top panel
    private readonly Label _playerHealthLabel;
    private readonly Label _opponentHealthLabel;
    private readonly Label _playerDamageLabel;
    private readonly Label _opponentDamageLabel;
    
    private readonly DeckManager _playerDeck;
    private readonly DeckManager _opponentDeck;
    private FlowLayoutPanel _playerCardsPanel;
    private FlowLayoutPanel _opponentCardsPanel;
    
    // Inline card detail panel
    private Panel? _cardDetailPanel;
    private Label? _cardDetailName;
    private Label? _cardDetailInfo;
    private Label? _cardDetailDescription;
    private Card? _selectedCard;
    
    // Field slots for cards in play (6 opponent slots at indices 0-5, 6 player slots at indices 6-11)
    private readonly Panel[] _fieldSlots;
    private readonly Card?[] _cardsInSlots;
    
    // Health tracking (default 30 HP each)
    private int _playerHealth = 30;
    private int _opponentHealth = 30;
    private int _playerLastDamage = 0;
    private int _opponentLastDamage = 0;
    
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
    
    // Card combiner for combining two cards
    private readonly CardCombiner _cardCombiner = new();
    
    // Combination UI - 60% hand (left), 40% combo (right)
    private Panel _comboArea;
    private Panel? _comboSlot1;
    private Panel? _comboSlot2;
    private Panel? _comboResultSlot;
    private Card? _comboCard1;
    private Card? _comboCard2;
    private Label? _comboResultLabel;
    private Panel? _comboPreviewPanel;
    private Button? _comboExecuteButton;
    private Label? _comboInstructionLabel;
    
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
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Row 0 - Top (turn panel + card details)
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));  // Row 1 - Opponent (20%)
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Row 2 - Playing field label
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));  // Row 3 - Playing field (35%)
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));  // Row 4 - Player (35%)
        
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
        
        // Player health label - positioned after status (center-left area)
        _playerHealthLabel = new Label
        {
            Text = $"You: {_playerHealth} HP",
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.LimeGreen,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Margin = new Padding(0, 0, 20, 0)
        };
        turnPanel.Controls.Add(_playerHealthLabel);
        
        // Player damage indicator (shows when damaged)
        _playerDamageLabel = new Label
        {
            Text = "",
            Font = new Font("Arial", 11, FontStyle.Bold),
            ForeColor = Color.Red,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Margin = new Padding(0, 0, 15, 0)
        };
        turnPanel.Controls.Add(_playerDamageLabel);
        
        // Opponent health label
        _opponentHealthLabel = new Label
        {
            Text = $"Enemy: {_opponentHealth} HP",
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.OrangeRed,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Margin = new Padding(0, 0, 20, 0)
        };
        turnPanel.Controls.Add(_opponentHealthLabel);
        
        // Opponent damage indicator (shows when damaged)
        _opponentDamageLabel = new Label
        {
            Text = "",
            Font = new Font("Arial", 11, FontStyle.Bold),
            ForeColor = Color.Red,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Margin = new Padding(0, 0, 15, 0)
        };
        turnPanel.Controls.Add(_opponentDamageLabel);
        
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
        
        // Card detail labels merged into turn panel
        _cardDetailName = new Label
        {
            Text = " [Click card]",
            Font = new Font("Arial", 7, FontStyle.Bold),
            ForeColor = Color.LightGray,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Margin = new Padding(10, 0, 0, 0)
        };
        turnPanel.Controls.Add(_cardDetailName);
        
        _cardDetailInfo = new Label
        {
            Text = "",
            Font = new Font("Arial", 7, FontStyle.Bold),
            ForeColor = Color.Cyan,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Margin = new Padding(5, 0, 5, 0)
        };
        turnPanel.Controls.Add(_cardDetailInfo);
        
        _cardDetailDescription = new Label
        {
            Text = "",
            Font = new Font("Arial", 6),
            ForeColor = Color.FromArgb(180, 180, 180),
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true,
            Margin = new Padding(0, 0, 10, 0)
        };
        turnPanel.Controls.Add(_cardDetailDescription);
        
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
            Dock = DockStyle.Top,
            Height = 35,
            BackColor = Color.FromArgb(45, 70, 45),
            Padding = new Padding(5, 5, 5, 5)
        };
        
        var fieldLabel = new Label
        {
            Text = "Drag creature cards here",
            Font = new Font("Arial", 6, FontStyle.Bold),
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
        
        // Both rows - opponent row gets 40%, player row gets 60%
        slotsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
        slotsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));
        
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
                Margin = new Padding(2),
                Tag = i,
                AllowDrop = true,
                MinimumSize = new Size(80, 100)
            };
            
            // Add slot label
            var slotLabel = new Label
            {
                Text = isOpponentSlot ? $"O{i + 1}" : $"P{i - 5}",
                ForeColor = Color.FromArgb(180, 180, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Arial", 7, FontStyle.Bold)
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
        
        // Player area: 60% hand (left), 40% combo (right) using TableLayoutPanel
        var playerContentTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2
        };
        // 60% for hand (left), 40% for combo (right)
        playerContentTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
        playerContentTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
        
        // Left side: player cards panel (hand) - 60%
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
        
        // Right side: combination area - 40%
        _comboArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 45)
        };
        
        // Combo instruction label at top
        _comboInstructionLabel = new Label
        {
            Text = "Drag cards to combine ->",
            Font = new Font("Arial", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 180, 180),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 30
        };
        _comboArea.Controls.Add(_comboInstructionLabel);
        
        // Combo slots - add directly to _comboArea without wrapper panel
        // (will add slots/button later in the method)
        
        // Combo slot 1 (first card to combine)
        _comboSlot1 = new Panel
        {
            Width = 90,
            Height = 80,
            BackColor = Color.FromArgb(60, 60, 70),
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(10, 10),
            Tag = "combo1",
            AllowDrop = true
        };
        _comboSlot1.DragEnter += ComboSlot_DragEnter;
        _comboSlot1.DragDrop += ComboSlot1_DragDrop;
        _comboSlot1.DragLeave += ComboSlot_DragLeave;
        
        var slot1Label = new Label
        {
            Text = "Slot 1",
            ForeColor = Color.FromArgb(150, 150, 150),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Font = new Font("Arial", 9, FontStyle.Bold)
        };
        _comboSlot1.Controls.Add(slot1Label);
        
        // Combo slot 2 (second card to combine)
        _comboSlot2 = new Panel
        {
            Width = 90,
            Height = 80,
            BackColor = Color.FromArgb(60, 60, 70),
            BorderStyle = BorderStyle.FixedSingle,
            Tag = "combo2",
            AllowDrop = true
        };
        _comboSlot2.DragEnter += ComboSlot_DragEnter;
        _comboSlot2.DragDrop += ComboSlot2_DragDrop;
        _comboSlot2.DragLeave += ComboSlot_DragLeave;
        
        var slot2Label = new Label
        {
            Text = "Slot 2",
            ForeColor = Color.FromArgb(150, 150, 150),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Font = new Font("Arial", 9, FontStyle.Bold)
        };
        _comboSlot2.Controls.Add(slot2Label);
        
        // Horizontal panel to hold both slots
        var slotsRow = new TableLayoutPanel
        {
            RowCount = 1,
            ColumnCount = 2,
            Width = 200,
            Height = 90
        };
        slotsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95f));
        slotsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95f));
        slotsRow.Controls.Add(_comboSlot1, 0, 0);
        slotsRow.Controls.Add(_comboSlot2, 1, 0);
        
        // Combo result slot (preview of combined card)
        _comboResultSlot = new Panel
        {
            Width = 90,
            Height = 45,
            BackColor = Color.FromArgb(50, 50, 60),
            BorderStyle = BorderStyle.FixedSingle
        };
        
        _comboResultLabel = new Label
        {
            Text = "Preview",
            ForeColor = Color.FromArgb(120, 120, 120),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Font = new Font("Arial", 8)
        };
        _comboResultSlot.Controls.Add(_comboResultLabel);
        
        // Combine button (wider and taller)
        _comboExecuteButton = new Button
        {
            Text = "Combine!",
            Font = new Font("Arial", 9, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(100, 80, 50),
            FlatStyle = FlatStyle.Flat,
            Width = 180,
            Height = 75,
            Enabled = false
        };
        _comboExecuteButton.FlatAppearance.BorderSize = 2;
        _comboExecuteButton.Click += ComboButton_Click;
        
        // Add all combo controls directly to _comboArea
        _comboArea.Controls.Add(_comboSlot1);
        _comboArea.Controls.Add(_comboSlot2);
        _comboArea.Controls.Add(_comboResultSlot);
        _comboArea.Controls.Add(_comboExecuteButton);
        
        // Add both panels to the content table
        playerContentTable.Controls.Add(_playerCardsPanel, 0, 0);
        playerContentTable.Controls.Add(_comboArea, 1, 0);
        
        _playerArea.Controls.Add(playerContentTable);
        
        layoutContainer.Controls.Add(_playerArea, 0, 4);
        
        mainPanel.Controls.Add(layoutContainer);
        Controls.Add(mainPanel);
        
        // Handle resize to scale elements
        Resize += GameForm_Resize;
        
        // Handle form closing to properly cancel async operations
        FormClosing += GameForm_FormClosing;
        
        // Small delay then initialize combo UI elements
        System.Windows.Forms.Timer initTimer = new System.Windows.Forms.Timer { Interval = 100 };
        initTimer.Tick += (s, e) =>
        {
            initTimer.Stop();
            initTimer.Dispose();
            CreateComboControls();
        };
        initTimer.Start();
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
    /// Create combo UI controls after form is loaded
    /// </summary>
    private void CreateComboControls()
    {
        if (_comboArea == null) return;
        
        // Clear any existing controls
        _comboArea.Controls.Clear();
        
        // Create instruction label
        var label = new Label
        {
            Text = "Drag cards here to combine:",
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 35
        };
        _comboArea.Controls.Add(label);
        
        // Use BUTTONS with horizontal layout - slots side by side
        // First clear any existing controls
        _comboArea.Controls.Clear();
        
        // Slot 1 - horizontal position
        var slot1Btn = new Button
        {
            Text = "1",
            Font = new Font("Arial", 14, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(90, 60, 60),
            Width = 80,
            Height = 60,
            FlatStyle = FlatStyle.Flat
        };
        slot1Btn.FlatAppearance.BorderSize = 3;
        slot1Btn.FlatAppearance.BorderColor = Color.LightCyan;
        slot1Btn.Location = new Point(5, 10);
        
        // Slot 2 - next to slot 1 horizontally
        var slot2Btn = new Button
        {
            Text = "2",
            Font = new Font("Arial", 14, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(90, 60, 60),
            Width = 80,
            Height = 60,
            FlatStyle = FlatStyle.Flat
        };
        slot2Btn.FlatAppearance.BorderSize = 3;
        slot2Btn.FlatAppearance.BorderColor = Color.LightCyan;
        slot2Btn.Location = new Point(95, 10);  // Right of slot1
        
        // Preview button below slots
        var previewBtn = new Button
        {
            Text = "PREVIEW",
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.Yellow,
            BackColor = Color.FromArgb(70, 70, 60),
            Width = 170,
            Height = 40,
            FlatStyle = FlatStyle.Flat,
            Enabled = false,
            Location = new Point(5, 80)
        };
        
        // Combine button below preview
        var combineBtn = new Button
        {
            Text = "COMBINE!",
            Font = new Font("Arial", 12, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(180, 80, 30),
            FlatStyle = FlatStyle.Flat,
            Width = 170,
            Height = 45,
            Location = new Point(5, 130)
        };
        combineBtn.FlatAppearance.BorderSize = 2;
        combineBtn.Click += ComboButton_Click;
        
        // Add controls
        _comboArea.Controls.Add(slot1Btn);
        _comboArea.Controls.Add(slot2Btn);
        _comboArea.Controls.Add(previewBtn);
        _comboArea.Controls.Add(combineBtn);
        
        // Store references
        if (_comboSlot1 == null) { } // Dummy reference to force field initialization
        
        System.Diagnostics.Debug.WriteLine("Combo UI created!");
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
            
            // Only creature cards can be played to slots (check using type name or is operator)
            var cardTypeName = card.GetType().Name;
            if (cardTypeName != "CreatureCard")
            {
                _showStatus?.Invoke("Only creatures can be played to the field!", Color.Orange);
                return;
            }
            
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
    
    // ========== COMBO SLOT HANDLERS ==========
    
    /// <summary>
    /// Handle drag enter on a combo slot - highlight the slot
    /// </summary>
    private void ComboSlot_DragEnter(object? sender, DragEventArgs e)
    {
        if (sender is Panel slot)
        {
            slot.BackColor = Color.FromArgb(90, 90, 110);
            e.Effect = DragDropEffects.Move;
        }
    }
    
    /// <summary>
    /// Handle drag leave on a combo slot - reset slot color
    /// </summary>
    private void ComboSlot_DragLeave(object? sender, EventArgs e)
    {
        if (sender is Panel slot)
        {
            slot.BackColor = Color.FromArgb(60, 60, 70);
        }
    }
    
    /// <summary>
    /// Handle drop on combo slot 1 - place card for combining
    /// </summary>
    private void ComboSlot1_DragDrop(object? sender, DragEventArgs e)
    {
        if (sender is Panel slot)
        {
            slot.BackColor = Color.FromArgb(60, 60, 70);
            
            var card = _draggedCard;
            if (card == null)
                return;
            
            // Verify card is in hand
            if (!_playerDeck.Hand.Any(c => c.Id == card.Id))
                return;
            
            // Set card in slot 1
            _comboCard1 = card;
            
            // Update slot display
            UpdateComboSlotDisplay(_comboSlot1, card);
            
            // Update preview
            UpdateComboPreview();
        }
    }
    
    /// <summary>
    /// Handle drop on combo slot 2 - place card for combining
    /// </summary>
    private void ComboSlot2_DragDrop(object? sender, DragEventArgs e)
    {
        if (sender is Panel slot)
        {
            slot.BackColor = Color.FromArgb(60, 60, 70);
            
            var card = _draggedCard;
            if (card == null)
                return;
            
            // Verify card is in hand
            if (!_playerDeck.Hand.Any(c => c.Id == card.Id))
                return;
            
            // Set card in slot 2
            _comboCard2 = card;
            
            // Update slot display
            UpdateComboSlotDisplay(_comboSlot2, card);
            
            // Update preview
            UpdateComboPreview();
        }
    }
    
    /// <summary>
    /// Update combo slot display with a card
    /// </summary>
    private void UpdateComboSlotDisplay(Panel slot, Card? card)
    {
        slot.Controls.Clear();
        
        if (card != null)
        {
            var cardPanel = CreateSlotCardPanel(card);
            cardPanel.Dock = DockStyle.Fill;
            slot.Controls.Add(cardPanel);
        }
        else
        {
            var slotLabel = new Label
            {
                Text = slot == _comboSlot1 ? "Slot 1" : "Slot 2",
                ForeColor = Color.FromArgb(150, 150, 150),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            slot.Controls.Add(slotLabel);
        }
    }
    
    /// <summary>
    /// Update combo preview - show the result of combining
    /// </summary>
    private void UpdateComboPreview()
    {
        // Check if both slots have cards
        if (_comboCard1 != null && _comboCard2 != null)
        {
            // Use the CardCombiner to get the preview
            var result = _cardCombiner.Combine(_comboCard1, _comboCard2);
            
            if (result.Success && result.ResultCard != null)
            {
                // Show preview card
                _comboResultSlot!.Controls.Clear();
                
                var previewPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = GetCardColor(result.ResultCard.Element.ToString()),
                    BorderStyle = BorderStyle.FixedSingle
                };
                
                var nameLabel = new Label
                {
                    Text = result.ResultCard.Name,
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.TopCenter,
                    Dock = DockStyle.Top,
                    Height = 30,
                    Font = new Font("Arial", 7, FontStyle.Bold)
                };
                previewPanel.Controls.Add(nameLabel);
                
                var statsLabel = new Label
                {
                    Text = result.Message,
                    ForeColor = Color.Cyan,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Font = new Font("Arial", 6)
                };
                previewPanel.Controls.Add(statsLabel);
                
                _comboResultSlot!.Controls.Add(previewPanel);
                
                // Enable combine button
                _comboExecuteButton!.Enabled = true;
                
                return;
            }
        }
        
        // No valid combination - show empty preview
        _comboResultSlot!.Controls.Clear();
        _comboResultLabel = new Label
        {
            Text = _comboCard1 != null && _comboCard2 != null ? "Cannot combine!" : "Preview",
            ForeColor = _comboCard1 != null && _comboCard2 != null ? Color.Red : Color.FromArgb(120, 120, 120),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Font = new Font("Arial", 8)
        };
        _comboResultSlot!.Controls.Add(_comboResultLabel);
        
        // Disable combine button
        _comboExecuteButton!.Enabled = false;
    }
    
    /// <summary>
    /// Handle click on combine button - execute the combination
    /// </summary>
    private void ComboButton_Click(object? sender, EventArgs e)
    {
        // Check if both slots have cards
        if (_comboCard1 == null || _comboCard2 == null)
            return;
        
        // Attempt to combine
        var result = _cardCombiner.Combine(_comboCard1, _comboCard2);
        
        if (!result.Success || result.ResultCard == null)
        {
            _showStatus?.Invoke(result.Message, Color.Red);
            return;
        }
        
        // Remove both cards from hand
        _playerDeck.PlayCard(_comboCard1.Id);
        _playerDeck.PlayCard(_comboCard2.Id);
        
        // Add the new card to hand
        _playerDeck.Hand.Add(result.ResultCard);
        
        // Clear combo slots
        _comboCard1 = null;
        _comboCard2 = null;
        
        // Reset slot displays
        UpdateComboSlotDisplay(_comboSlot1!, null);
        UpdateComboSlotDisplay(_comboSlot2!, null);
        
        // Clear preview
        _comboResultSlot!.Controls.Clear();
        _comboResultLabel = new Label
        {
            Text = "Preview",
            ForeColor = Color.FromArgb(120, 120, 120),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Font = new Font("Arial", 8)
        };
        _comboResultSlot!.Controls.Add(_comboResultLabel);
        
        // Disable button
        _comboExecuteButton!.Enabled = false;
        
        // Update displays
        UpdatePlayerDisplay();
        
        _showStatus?.Invoke($"Combined into {result.ResultCard.Name}!", Color.LimeGreen);
    }
    
    // ========== END COMBO SLOT HANDLERS ==========
    
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
            // Create a smaller card panel for display in the slot
            var cardPanel = CreateSlotCardPanel(card);
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
            // Resolve combat: Player creatures attack, opponent creatures counterattack
            ResolveCombat(isPlayerAttacking: true);
            
            // Check game over
            if (_opponentHealth <= 0 || _playerHealth <= 0)
            {
                CheckGameOver();
                return;
            }
            
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
            
            // Simple opponent AI - try to play creatures
            PlayOpponentCards();
            
            // Pass after a delay
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
    
    /// <summary>
    /// Simple AI for opponent to play creature cards
    /// </summary>
    private void PlayOpponentCards()
    {
        // Find creature cards in opponent hand
        var creatureCards = _opponentDeck.Hand
            .Where(c => c.GetType().Name == "CreatureCard")
            .OrderBy(c => c.ManaCost)  // Play cheaper cards first
            .ToList();
        
        foreach (var card in creatureCards)
        {
            // Check if we have enough mana
            if (card.ManaCost > _opponentCurrentMana)
                continue;
            
            // Find an empty slot
            int? emptySlot = FindEmptyOpponentSlot();
            if (emptySlot == null)
                break;  // No empty slots
            
            // Play the card to the slot
            _opponentCurrentMana -= card.ManaCost;
            _opponentDeck.PlayCard(card.Id);
            _cardsInSlots[emptySlot.Value] = card;
            UpdateSlotDisplay(emptySlot.Value);
            
            _showStatus?.Invoke($"Enemy played {card.Name}!", Color.Orange);
        }
        
        // Update mana display
        UpdateOpponentManaDisplay();
    }
    
    /// <summary>
    /// Find an empty opponent slot (indices 0-5)
    /// </summary>
    private int? FindEmptyOpponentSlot()
    {
        for (int i = 0; i < 6; i++)
        {
            if (_cardsInSlots[i] == null)
                return i;
        }
        return null;
    }
    
    /// <summary>
    /// Update opponent mana display
    /// </summary>
    private void UpdateOpponentManaDisplay()
    {
        // Could add a label for opponent mana if needed
    }
    
    /// <summary>
    /// Resolve combat between creatures on the field
    /// Combat order: Attacker's creatures attack first, then defender's creatures counterattack
    /// </summary>
    private void ResolveCombat(bool isPlayerAttacking)
    {
        // Slot indices: 0-5 are opponent, 6-11 are player
        // Combat pairs: slot 0 vs 6, 1 vs 7, 2 vs 8, 3 vs 9, 4 vs 10, 5 vs 11
        
        for (int i = 0; i < 6; i++)
        {
            int opponentSlot = i;
            int playerSlot = i + 6;
            
            var opponentCreature = _cardsInSlots[opponentSlot] as Card;
            var playerCreature = _cardsInSlots[playerSlot] as Card;
            
            // Skip if either slot is empty
            if (opponentCreature == null && playerCreature == null)
                continue;
            
            // Skip if both creatures are dead (health <= 0) - already handled in previous turns
            if (opponentCreature != null && opponentCreature.Health <= 0)
                opponentCreature = null;
            if (playerCreature != null && playerCreature.Health <= 0)
                playerCreature = null;
            
            if (isPlayerAttacking)
            {
                // Player creatures attack first, then opponent counterattacks
                if (playerCreature != null && playerCreature.Power > 0)
                {
                    if (opponentCreature != null)
                    {
                        // Player attacks opponent creature
                        int damage = playerCreature.Power;
                        opponentCreature.Health -= damage;
                        _showStatus?.Invoke($"{playerCreature.Name} attacks {opponentCreature.Name} for {damage}!", Color.Yellow);
                        
                        if (opponentCreature.Health <= 0)
                        {
                            // Opponent creature dies - remove it
                            _cardsInSlots[opponentSlot] = null;
                            UpdateSlotDisplay(opponentSlot);
                            _showStatus?.Invoke($"{opponentCreature.Name} was destroyed!", Color.Red);
                        }
                        else if (opponentCreature.Power > 0)
                        {
                            // Opponent counterattacks if still alive
                            int counterDamage = opponentCreature.Power;
                            playerCreature.Health -= counterDamage;
                            _showStatus?.Invoke($"{opponentCreature.Name} counterattacks for {counterDamage}!", Color.Orange);
                            
                            if (playerCreature.Health <= 0)
                            {
                                // Player creature dies
                                _cardsInSlots[playerSlot] = null;
                                UpdateSlotDisplay(playerSlot);
                                _showStatus?.Invoke($"{playerCreature.Name} was destroyed!", Color.Red);
                            }
                        }
                    }
                    else
                    {
                        // No opponent creature - attack player directly
                        int damage = playerCreature.Power;
                        ApplyOpponentDamage(damage);
                        _showStatus?.Invoke($"{playerCreature.Name} attacks for {damage} damage!", Color.Yellow);
                    }
                }
            }
            else
            {
                // Opponent creatures attack first, then player counterattacks
                if (opponentCreature != null && opponentCreature.Power > 0)
                {
                    if (playerCreature != null)
                    {
                        // Opponent attacks player creature
                        int damage = opponentCreature.Power;
                        playerCreature.Health -= damage;
                        _showStatus?.Invoke($"{opponentCreature.Name} attacks {playerCreature.Name} for {damage}!", Color.Orange);
                        
                        if (playerCreature.Health <= 0)
                        {
                            // Player creature dies
                            _cardsInSlots[playerSlot] = null;
                            UpdateSlotDisplay(playerSlot);
                            _showStatus?.Invoke($"{playerCreature.Name} was destroyed!", Color.Red);
                        }
                        else if (playerCreature.Power > 0)
                        {
                            // Player counterattacks if still alive
                            int counterDamage = playerCreature.Power;
                            opponentCreature.Health -= counterDamage;
                            _showStatus?.Invoke($"{playerCreature.Name} counterattacks for {counterDamage}!", Color.Yellow);
                            
                            if (opponentCreature.Health <= 0)
                            {
                                // Opponent creature dies
                                _cardsInSlots[opponentSlot] = null;
                                UpdateSlotDisplay(opponentSlot);
                                _showStatus?.Invoke($"{opponentCreature.Name} was destroyed!", Color.Red);
                            }
                        }
                    }
                    else
                    {
                        // No player creature - attack player directly
                        int damage = opponentCreature.Power;
                        ApplyPlayerDamage(damage);
                        _showStatus?.Invoke($"{opponentCreature.Name} attacks for {damage} damage!", Color.Orange);
                    }
                }
            }
        }
        
        // Update health display after combat
        UpdateHealthDisplay();
    }
    
    /// <summary>
    /// Check for game over condition
    /// </summary>
    private void CheckGameOver()
    {
        if (_opponentHealth <= 0)
        {
            MessageBox.Show("Victory! You defeated the opponent!", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        else if (_playerHealth <= 0)
        {
            MessageBox.Show("Defeat! You were defeated...", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
    }
    
    private void StartPlayerTurn()
    {
        // Resolve combat: Opponent creatures attack first, then player counterattacks
        ResolveCombat(isPlayerAttacking: false);
        
        // Check game over
        if (_opponentHealth <= 0 || _playerHealth <= 0)
        {
            CheckGameOver();
            return;
        }
        
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
        UpdateHealthDisplay();
    }
    
    private void UpdateManaDisplay()
    {
        _manaLabel.Text = $"Mana: {_currentMana}/{_maxMana}";
    }
    
    /// <summary>
    /// Update health display with damage indicators
    /// </summary>
    private void UpdateHealthDisplay()
    {
        // Update player health label
        _playerHealthLabel.Text = $"You: {_playerHealth} HP";
        
        // Show player damage indicator if recently damaged
        if (_playerLastDamage > 0)
        {
            _playerDamageLabel.Text = $"-{_playerLastDamage}";
        }
        else
        {
            _playerDamageLabel.Text = "";
        }
        
        // Update opponent health label
        _opponentHealthLabel.Text = $"Enemy: {_opponentHealth} HP";
        
        // Show opponent damage indicator if recently damaged
        if (_opponentLastDamage > 0)
        {
            _opponentDamageLabel.Text = $"-{_opponentLastDamage}";
        }
        else
        {
            _opponentDamageLabel.Text = "";
        }
        
        // Clear damage values after displaying them (they persist in labels)
        _playerLastDamage = 0;
        _opponentLastDamage = 0;
    }
    
    /// <summary>
    /// Apply damage to player and show indicator
    /// </summary>
    public void ApplyPlayerDamage(int damage)
    {
        _playerHealth = Math.Max(0, _playerHealth - damage);
        _playerLastDamage = damage;
        UpdateHealthDisplay();
    }
    
    /// <summary>
    /// Apply damage to opponent and show indicator
    /// </summary>
    public void ApplyOpponentDamage(int damage)
    {
        _opponentHealth = Math.Max(0, _opponentHealth - damage);
        _opponentLastDamage = damage;
        UpdateHealthDisplay();
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
    /// Show card details inline instead of popup
    /// </summary>
    private void ShowCardDetail(Card card)
    {
        // Update the inline panel instead of opening a popup
        UpdateCardDetailPanel(card);
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
            Width = 150,
            Height = 210,
            BackColor = GetCardColor(card.Element.ToString()),
            Margin = new Padding(3),
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

    /// <summary>
    /// Create a smaller card panel for display in a slot on the playing field
    /// </summary>
    private Panel CreateSlotCardPanel(Card card)
    {
        // Use custom ClickablePanel with smaller dimensions for slot display
        var cardPanel = new ClickablePanel
        {
            Width = 110,
            Height = 170,
            BackColor = GetCardColor(card.Element.ToString()),
            Margin = new Padding(2),
            BorderStyle = BorderStyle.FixedSingle
        };

        // Card name at top (smaller font)
        var nameLabel = new Label
        {
            Text = card.Name,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.TopCenter,
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font("Arial", 8, FontStyle.Bold),
            Padding = new Padding(2)
        };

        cardPanel.Controls.Add(nameLabel);

        // Card type in middle (smaller font)
        var typeLabel = new Label
        {
            Text = card.Type.ToString(),
            ForeColor = Color.FromArgb(200, 200, 200),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Font = new Font("Arial", 10)
        };
        cardPanel.Controls.Add(typeLabel);

        // Mana cost at bottom (smaller font)
        var manaLabel = new Label
        {
            Text = $"{card.ManaCost}",
            ForeColor = Color.FromArgb(100, 200, 255),
            TextAlign = ContentAlignment.BottomCenter,
            Dock = DockStyle.Bottom,
            Height = 25,
            Font = new Font("Arial", 10, FontStyle.Bold)
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
    
    /// <summary>
    /// Create the inline card detail panel
    /// </summary>
    private void CreateCardDetailPanel()
    {
        _cardDetailPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(45, 45, 55),
            BorderStyle = BorderStyle.FixedSingle
        };
        
        // Card name label (left side)
        _cardDetailName = new Label
        {
            Text = "Click card",
            Font = new Font("Arial", 8, FontStyle.Bold),
            ForeColor = Color.LightGray,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Left,
            Width = 150,
            Margin = new Padding(5, 0, 0, 0)
        };
        _cardDetailPanel.Controls.Add(_cardDetailName);
        
        // Card info label (middle - stats)
        _cardDetailInfo = new Label
        {
            Text = "",
            Font = new Font("Arial", 8, FontStyle.Bold),
            ForeColor = Color.Cyan,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        _cardDetailPanel.Controls.Add(_cardDetailInfo);
        
        // Card description (right side)
        _cardDetailDescription = new Label
        {
            Text = "",
            Font = new Font("Arial", 7),
            ForeColor = Color.FromArgb(180, 180, 180),
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Right,
            Width = 300,
            Margin = new Padding(0, 0, 5, 0)
        };
        _cardDetailPanel.Controls.Add(_cardDetailDescription);
    }
    
    /// <summary>
    /// Update the inline card details (now in turn panel)
    /// </summary>
    private void UpdateCardDetailPanel(Card card)
    {
        if (_cardDetailName == null || _cardDetailInfo == null || _cardDetailDescription == null)
            return;
        
        _selectedCard = card;
        
        // Update name with element indicator
        var elementEmoji = GetElementEmoji(card.Element.ToString());
        _cardDetailName.Text = $" [{elementEmoji} {card.Name}]";
        _cardDetailName.ForeColor = GetCardColor(card.Element.ToString());
        
        // Update stats info based on card type
        var cardTypeName = card.GetType().Name;
        var statsText = $"{card.Type} | {card.ManaCost} Mana";
        
        if (cardTypeName == "CreatureCard")
        {
            statsText += $" | PWR:{card.Power} HP:{card.Health}";
        }
        else if (cardTypeName == "SpellCard")
        {
            // Show effect info for spells
            if (card.Effects.Count > 0)
            {
                var effect = card.Effects[0];
                statsText += $" | {effect.Type}:{effect.Value}";
            }
        }
        else if (cardTypeName == "ArtifactCard")
        {
            statsText += " | Persistent";
            if (card.Effects.Count > 0)
            {
                statsText += $" | {card.Effects[0].Type}:{card.Effects[0].Value}";
            }
        }
        else if (cardTypeName == "WeaponCard")
        {
            statsText += $" | ATK:{card.Power}";
        }
        else if (cardTypeName == "ArmorCard")
        {
            statsText += $" | DEF:{card.Health}";
        }
        
        if (card.IsLegendary)
        {
            statsText += " | ★ LEGENDARY";
        }
        
        _cardDetailInfo.Text = statsText;
        
        // Update description
        _cardDetailDescription.Text = card.Description;
    }
    
    /// <summary>
    /// Get element emoji for display
    /// </summary>
    private string GetElementEmoji(string element)
    {
        return element switch
        {
            "Fire" => "🔥",
            "Water" => "💧",
            "Earth" => "🌍",
            "Air" => "💨",
            "Light" => "✨",
            "Dark" => "🌑",
            "Arcane" => "🔮",
            "Nature" => "🌿",
            _ => "🎴"
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