using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MagicalDeckbuilder.Cards;
using NewGame.UI.Views;
using MagicalDeckbuilder.Combining;
using MagicalDeckbuilder.Decks;
using MagicalDeckbuilder.Game;
using MagicalDeckbuilder.Logging;
using MagicalDeckbuilder.Storage;
using Card = MagicalDeckbuilder.Cards.Card;
using CardType = MagicalDeckbuilder.Cards.CardType;
using EffectType = MagicalDeckbuilder.Cards.EffectType;
using TargetType = MagicalDeckbuilder.Cards.TargetType;
using WeaponTargetType = MagicalDeckbuilder.Cards.WeaponTargetType;
using WeaponCard = MagicalDeckbuilder.Cards.WeaponCard;
using PileType = MagicalDeckbuilder.Decks.PileType;
using CreatureSlot = MagicalDeckbuilder.Decks.CreatureSlot;
using SavedDeckType = MagicalDeckbuilder.Storage.DeckType;

namespace NewGame.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly CardCombiner _cardCombiner = new();
    private OpponentAI? _opponentAI;
    private DifficultyLevel _difficulty = DifficultyLevel.Journeyman;
    private bool _hasUnsavedDeckChanges;
    private string? _currentDeckId;
    private string? _selectedBattleDeckId;

    private string _currentView = "Menu";
    private CardViewModel? _selectedCard;
    private CardViewModel? _zoomedCard;
    private CardViewModel? _comboCard1;
    private CardViewModel? _comboCard2;
    private CardViewModel? _comboResult;
    private string _statusMessage = "";
    private bool _canCombine;
    private bool _isComboLocked;

    private int _playerHealth = 30;
    private int _playerMaxHealth = 30;
    private int _playerMana = 2;
    private int _playerMaxMana = 10;
    private int _opponentHealth = 30;
    private int _opponentMaxHealth = 30;
    private int _turnCount = 1;
    private bool _isPlayerTurn = true;
    private int _playerWeaponBonus = 0;
    private int _playerArmorBonus = 0;
    private int _opponentWeaponBonus = 0;
    private int _opponentArmorBonus = 0;

    public MainViewModel()
    {
        PlayerDeck = new DeckManager();
        OpponentDeck = new DeckManager();

        InitializeGameCommand = new RelayCommand(InitializeGame);
        StartGameCommand = new RelayCommand(StartGame);
        EndTurnCommand = new RelayCommand(EndTurn, () => _isPlayerTurn);
        CombineCardsCommand = new RelayCommand(ExecuteCombine, () => _canCombine);
        ClearComboCommand = new RelayCommand(ClearCombo);
        PlayCardCommand = new RelayCommand<CardViewModel>(PlayCard);
        NavigateCommand = new RelayCommand<string>(Navigate);
        RemoveCardFromDeckCommand = new RelayCommand<CardViewModel>(RemoveCardFromDeck);
        ClearDeckCommand = new RelayCommand(ClearDeck);
        CloseZoomCommand = new RelayCommand(CloseZoom);
        CloseCommand = new RelayCommand(Close);
        EquipWeaponCommand = new RelayCommand<CardViewModel>(EquipWeapon);
        EquipArmorCommand = new RelayCommand<CardViewModel>(EquipArmor);
        UnequipWeaponCommand = new RelayCommand(UnequipWeapon);
        UnequipArmorCommand = new RelayCommand(UnequipArmor);
        
        // Deck management commands
        SelectDeckCommand = new RelayCommand<SavedDeckViewModel>(SelectDeck);
        EditDeckCommand = new RelayCommand<SavedDeckViewModel>(EditDeck);
        DeleteDeckCommand = new RelayCommand<SavedDeckViewModel>(DeleteDeck);
        NewDeckCommand = new RelayCommand(NewDeck);
        SaveDeckCommand = new RelayCommand(SaveDeck, () => _hasUnsavedDeckChanges);

        var allCards = CardFactory.CreateStarterDeck();
        AvailableCards = new ObservableCollection<CardViewModel>(
            allCards.Select(c => new CardViewModel(c)));

        MenuCards = new ObservableCollection<CardViewModel>(
            CardFactory.CreateStarterDeck().Select(c => new CardViewModel(c)));
        
        // Load saved decks
        LoadSavedDecks();
    }
    
    private async void LoadSavedDecks()
    {
        PlayerDecks.Clear();
        
        // Load full deck data for each player deck to enable element/type breakdown display
        var deckIds = DeckStorageService.Instance.GetPlayerDecks();
        foreach (var entry in deckIds)
        {
            var fullDeck = await DeckStorageService.Instance.LoadDeckAsync(entry.Id);
            if (fullDeck != null)
            {
                PlayerDecks.Add(new SavedDeckViewModel(fullDeck));
            }
        }
    }
    
    private async void SelectDeck(SavedDeckViewModel? deck)
    {
        if (deck == null) return;
        
        // Load the full deck data
        var fullDeck = await DeckStorageService.Instance.LoadDeckAsync(deck.Id);
        if (fullDeck != null)
        {
            LoadDeckIntoBuilder(fullDeck);
            _currentDeckId = fullDeck.Id;
            _selectedBattleDeckId = fullDeck.Id;
            _hasUnsavedDeckChanges = false;
        }
        
        // Navigate to deck builder
        CurrentView = "DeckBuilder";
    }
    
    private void LoadDeckIntoBuilder(SavedDeck deck)
    {
        PlayerDeckCards.Clear();
        
        foreach (var savedCard in deck.Cards)
        {
            var card = CardFactory.GetCardByTemplateId(savedCard.CardTemplateId);
            if (card != null)
            {
                PlayerDeckCards.Add(new CardViewModel(card));
            }
        }
        
        OnPropertyChanged(nameof(PlayerDeckCardCount));
        OnPropertyChanged(nameof(DeckCountText));
    }
    
    private void EditDeck(SavedDeckViewModel? deck)
    {
        if (deck == null) return;
        SelectDeck(deck);
    }
    
    private async void DeleteDeck(SavedDeckViewModel? deck)
    {
        if (deck == null) return;
        
        var result = MessageBox.Show(
            $"Are you sure you want to delete '{deck.Name}'?",
            "Delete Deck",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result == MessageBoxResult.Yes)
        {
            await DeckStorageService.Instance.DeleteDeckAsync(deck.Id);
            PlayerDecks.Remove(deck);
        }
    }
    
    private void NewDeck()
    {
        ClearDeck();
        _currentDeckId = null;
        _hasUnsavedDeckChanges = false;
        CurrentView = "DeckBuilder";
    }
    
    private async void SaveDeck()
    {
        if (PlayerDeckCards.Count == 0)
        {
            MessageBox.Show("Cannot save an empty deck.", "Save Deck", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Get current deck name for default value (using dialog now)
        string? currentName = null;
        if (!string.IsNullOrEmpty(_currentDeckId))
        {
            currentName = DeckStorageService.Instance.GetDeckIndex()
                .FirstOrDefault(d => d.Id == _currentDeckId)?.Name;
        }

        // Create and show the deck name dialog
        var dialog = new DeckNameDialog(currentName ?? "NewDeck");
        
        if (dialog.ShowDialog() != true)
        {
            return; // User cancelled
        }

        var deckName = dialog.DeckName;
        
        if (string.IsNullOrWhiteSpace(deckName)) return;
        
        // Convert CardViewModels to Cards
        var cards = PlayerDeckCards.Select(cvm => cvm.Card).ToList();
        
        // If editing existing deck, update it; otherwise create new
        if (!string.IsNullOrEmpty(_currentDeckId))
        {
            await DeckStorageService.Instance.UpdateDeckAsync(_currentDeckId, cards);
        }
        else
        {
            var savedDeck = await DeckStorageService.Instance.SaveDeckAsync(deckName, cards, DeckType.Player, Difficulty);
            _currentDeckId = savedDeck.Id;
        }

        _hasUnsavedDeckChanges = false;
        
        // Refresh the deck list
        LoadSavedDecks();
        
        MessageBox.Show($"Deck '{deckName}' saved successfully!", "Save Deck",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private string? PromptForDeckName(string? currentName)
    {
        // Simple input dialog - could be enhanced with a proper dialog window
        var input = currentName ?? "NewDeck";
        
        // For now, use a simple approach - generate unique name if needed
        if (string.IsNullOrWhiteSpace(input))
        {
            input = "NewDeck";
        }
        
        // Check if we need to generate a unique name
        var existingDecks = DeckStorageService.Instance.GetPlayerDecks();
        var existingNames = existingDecks.Select(d => d.Name).ToHashSet();
        
        if (existingNames.Contains(input) && input != currentName)
        {
            // Generate a unique name
            int counter = 1;
            string baseName = input;
            while (existingNames.Contains(input))
            {
                input = $"{baseName}{counter++}";
            }
        }
        
        return input;
    }
    
    public void MarkDeckAsChanged()
    {
        _hasUnsavedDeckChanges = true;
        OnPropertyChanged(nameof(HasUnsavedDeckChanges));
    }
    
    public bool HasUnsavedDeckChanges => _hasUnsavedDeckChanges;

    public ObservableCollection<CardViewModel> AvailableCards { get; }
    public ObservableCollection<CardViewModel> MenuCards { get; }
    public ObservableCollection<CardViewModel> PlayerHand { get; } = new();
    public ObservableCollection<CardViewModel> CustomCards { get; } = new();
    public ObservableCollection<CardViewModel> PlayerDeckCards { get; } = new();
    public DeckManager PlayerDeck { get; }
    public DeckManager OpponentDeck { get; private set; }

    public int PlayerDeckCardCount => PlayerDeckCards.Count;

    public CardViewModel?[] FieldSlots { get; } = new CardViewModel?[12];

    public CardViewModel? PlayerWeapon { get; private set; }
    public CardViewModel? PlayerArmor { get; private set; }
    public CardViewModel? OpponentWeapon { get; private set; }
    public CardViewModel? OpponentArmor { get; private set; }

    // Artifact slots (3 slots for player artifacts)
    private CardViewModel?[] _playerArtifactSlots = new CardViewModel?[3];
    public CardViewModel?[] PlayerArtifactSlots => _playerArtifactSlots;

    // Event slot (1 slot for player events)
    public CardViewModel? PlayerEventSlot { get; private set; }

    // Opponent artifact slots (3 slots)
    private CardViewModel?[] _opponentArtifactSlots = new CardViewModel?[3];
    public CardViewModel?[] OpponentArtifactSlots => _opponentArtifactSlots;

    // Opponent event slot (1 slot)
    public CardViewModel? OpponentEventSlot { get; private set; }

    // Cached arrays for UI binding - include property change notifications
    private CardViewModel?[] _cachedOpponentCreatureSlots = new CardViewModel?[6];
    private CardViewModel?[] _cachedPlayerCreatureSlots = new CardViewModel?[6];
    
    // Additional observable collections for WPF binding
    public System.Collections.ObjectModel.ObservableCollection<CardViewModel?> OpponentCreatureSlotsObs { get; } = new();
    public System.Collections.ObjectModel.ObservableCollection<CardViewModel?> PlayerCreatureSlotsObs { get; } = new();
    public System.Collections.ObjectModel.ObservableCollection<CardViewModel?> PlayerArtifactSlotsObs { get; } = new();
    public System.Collections.ObjectModel.ObservableCollection<CardViewModel?> OpponentArtifactSlotsObs { get; } = new();
    
    // Debug file logging
    private static readonly string DebugLogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
        "NewGame_debug.txt");
    
    private static void LogToFile(string msg)
    {
        try { System.IO.File.AppendAllText(DebugLogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n"); }
        catch { }
    }
    
    // Computed properties for UI binding - use cached arrays
    public CardViewModel?[] OpponentCreatureSlots => _cachedOpponentCreatureSlots;
    public CardViewModel?[] PlayerCreatureSlots => _cachedPlayerCreatureSlots;
    
    private void RefreshCreatureSlotCaches()
    {
        // Log the full state of FieldSlots before refreshing - with stack trace for debugging
        LogToFile("[RefreshCaches] FieldSlots state before refresh - Stack trace:");
        LogToFile(Environment.StackTrace);
        
        for (int i = 0; i < 12; i++)
        {
            LogToFile($"  FieldSlots[{i}] = {FieldSlots[i]?.Name ?? "null"}");
        }
        
        LogToFile("[RefreshCaches] Before refresh:");
        for (int i = 0; i < 6; i++)
        {
            _cachedOpponentCreatureSlots[i] = FieldSlots[i];
            _cachedPlayerCreatureSlots[i] = FieldSlots[i + 6];
            LogToFile($"  [{i}] Opp: {FieldSlots[i]?.Name ?? "null"}, Player: {FieldSlots[i+6]?.Name ?? "null"}");
        }
        
        // Create NEW arrays to force WPF to detect the change
        var newOppSlots = new CardViewModel?[6];
        var newPlayerSlots = new CardViewModel?[6];
        for (int i = 0; i < 6; i++)
        {
            newOppSlots[i] = _cachedOpponentCreatureSlots[i];
            newPlayerSlots[i] = _cachedPlayerCreatureSlots[i];
        }
        _cachedOpponentCreatureSlots = newOppSlots;
        _cachedPlayerCreatureSlots = newPlayerSlots;
        
        LogToFile("[RefreshCaches] After refresh: PlayerSlots[0]=" + (_cachedPlayerCreatureSlots[0]?.Name ?? "null") + 
            ", OppSlots[0]=" + (_cachedOpponentCreatureSlots[0]?.Name ?? "null"));
        
        // Log FieldSlots state after refresh - check if creatures are still there
        LogToFile("[RefreshCaches] FieldSlots after refresh:");
        for (int i = 6; i < 12; i++)
        {
            LogToFile($"  FieldSlots[{i}] = {FieldSlots[i]?.Name ?? "null"}");
        }
        
        // Update ObservableCollections for UI binding
        OpponentCreatureSlotsObs.Clear();
        PlayerCreatureSlotsObs.Clear();
        for (int i = 0; i < 6; i++)
        {
            OpponentCreatureSlotsObs.Add(_cachedOpponentCreatureSlots[i]);
            PlayerCreatureSlotsObs.Add(_cachedPlayerCreatureSlots[i]);
        }
        LogToFile($"[RefreshCaches] Obs collections updated, Obs[0]: Opp={OpponentCreatureSlotsObs[0]?.Name ?? "null"}, Player={PlayerCreatureSlotsObs[0]?.Name ?? "null"}");
        
        // Notify UI of changes
        OnPropertyChanged(nameof(OpponentCreatureSlots));
        OnPropertyChanged(nameof(PlayerCreatureSlots));
    }

    public string CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public CardViewModel? SelectedCard
    {
        get => _selectedCard;
        set => SetProperty(ref _selectedCard, value);
    }

    public CardViewModel? ZoomedCard
    {
        get => _zoomedCard;
        set
        {
            // Don't update if same value - prevents binding cycles
            if (EqualityComparer<CardViewModel?>.Default.Equals(_zoomedCard, value))
            {
                ErrorLogger.Instance.Debug("MainViewModel", $"[ZoomedCard setter] SKIPPED - already set");
                return;
            }
            ErrorLogger.Instance.Debug("MainViewModel", $"[ZoomedCard setter] old={_zoomedCard?.Name}, new={value?.Name}");
            _zoomedCard = value;
            OnPropertyChanged(nameof(ZoomedCard));
            OnPropertyChanged(nameof(IsCardZoomed));
        }
    }

    public bool IsCardZoomed => _zoomedCard != null;

    public CardViewModel? ComboCard1
    {
        get => _comboCard1;
        set => SetProperty(ref _comboCard1, value);
    }

    public CardViewModel? ComboCard2
    {
        get => _comboCard2;
        set => SetProperty(ref _comboCard2, value);
    }

    public CardViewModel? ComboResult
    {
        get => _comboResult;
        set => SetProperty(ref _comboResult, value);
    }

    public bool CanCombine
    {
        get => _canCombine;
        set => SetProperty(ref _canCombine, value);
    }

    public bool IsComboLocked
    {
        get => _isComboLocked;
        set => SetProperty(ref _isComboLocked, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public int PlayerHealth
    {
        get => _playerHealth;
        set => SetProperty(ref _playerHealth, value);
    }

    public int PlayerMaxHealth
    {
        get => _playerMaxHealth;
        set => SetProperty(ref _playerMaxHealth, value);
    }

    public int PlayerMana
    {
        get => _playerMana;
        set => SetProperty(ref _playerMana, value);
    }

    public int PlayerMaxMana
    {
        get => _playerMaxMana;
        set => SetProperty(ref _playerMaxMana, value);
    }

    public int OpponentHealth
    {
        get => _opponentHealth;
        set => SetProperty(ref _opponentHealth, value);
    }

    public int OpponentMaxHealth
    {
        get => _opponentMaxHealth;
        set => SetProperty(ref _opponentMaxHealth, value);
    }

    public int PlayerWeaponBonus
    {
        get => _playerWeaponBonus;
        set => SetProperty(ref _playerWeaponBonus, value);
    }

    public int PlayerArmorBonus
    {
        get => _playerArmorBonus;
        set => SetProperty(ref _playerArmorBonus, value);
    }

    public int OpponentWeaponBonus
    {
        get => _opponentWeaponBonus;
        set => SetProperty(ref _opponentWeaponBonus, value);
    }

    public int OpponentArmorBonus
    {
        get => _opponentArmorBonus;
        set => SetProperty(ref _opponentArmorBonus, value);
    }

    public int TurnCount
    {
        get => _turnCount;
        set => SetProperty(ref _turnCount, value);
    }

    public bool IsPlayerTurn
    {
        get => _isPlayerTurn;
        set => SetProperty(ref _isPlayerTurn, value);
    }

    public DifficultyLevel Difficulty
    {
        get => _difficulty;
        set => SetProperty(ref _difficulty, value);
    }

    public ICommand InitializeGameCommand { get; }
    public ICommand StartGameCommand { get; }
    public ICommand EndTurnCommand { get; }
    public ICommand CombineCardsCommand { get; }
    public ICommand ClearComboCommand { get; }
    public ICommand PlayCardCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand RemoveCardFromDeckCommand { get; }
    public ICommand ClearDeckCommand { get; }
    public ICommand CloseZoomCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand EquipWeaponCommand { get; }
    public ICommand EquipArmorCommand { get; }
    public ICommand UnequipWeaponCommand { get; }
    public ICommand UnequipArmorCommand { get; }
    public ICommand? SelectBattleCommand { get; }
    public ICommand SelectDeckCommand { get; }
    public ICommand EditDeckCommand { get; }
    public ICommand DeleteDeckCommand { get; }
    public ICommand NewDeckCommand { get; }
    public ICommand SaveDeckCommand { get; }
    public ICommand RefreshDecksCommand { get; private set; }
    
    public ObservableCollection<SavedDeckViewModel> PlayerDecks { get; } = new();

    private void Close()
    {
        Application.Current.Shutdown();
    }

    private async void InitializeGame()
    {
        ErrorLogger.Instance.Debug("MainViewModel", "[Operation: InitializeGame] Starting game initialization");
        LogToFile("[InitializeGame] CALLED - Stack trace:");
        LogToFile(Environment.StackTrace);
        
        try
        {
            // Clear all field slots
            for (int i = 0; i < FieldSlots.Length; i++)
            {
                FieldSlots[i] = null;
            }
            
            // Refresh cached slot arrays
            RefreshCreatureSlotCaches();
            LogToFile("[InitializeGame] Calling OnPropertyChanged for slots");
            OnPropertyChanged(nameof(FieldSlots));
            OnPropertyChanged(nameof(OpponentCreatureSlots));
            OnPropertyChanged(nameof(PlayerCreatureSlots));
            
            // Initialize opponent AI and deck
            _opponentAI = new OpponentAI(_difficulty);
            var newOpponentDeck = OpponentAI.CreateDeckForDifficulty(_difficulty);
            OpponentDeck = newOpponentDeck;

            // Generate and initialize player deck
            List<Card> playerCards;
            
            if (!string.IsNullOrEmpty(_selectedBattleDeckId))
            {
                // Load the saved deck
                var savedDeck = await DeckStorageService.Instance.LoadDeckAsync(_selectedBattleDeckId);
                if (savedDeck != null && savedDeck.Cards.Count > 0)
                {
                    playerCards = savedDeck.Cards
                        .Select(c => CardFactory.GetCardByTemplateId(c.CardTemplateId))
                        .Where(c => c != null)
                        .Cast<Card>()
                        .ToList();
                    ErrorLogger.Instance.Info("MainViewModel", $"[InitializeGame] Loaded saved deck with {playerCards.Count} cards");
                }
                else
                {
                    // Fallback to random deck if saved deck not found
                    playerCards = CardFactory.GenerateRandomDeck();
                    ErrorLogger.Instance.Warning("MainViewModel", "[InitializeGame] Saved deck not found, using random deck");
                }
            }
            else
            {
                // No deck selected - use random deck as fallback
                playerCards = CardFactory.GenerateRandomDeck();
                ErrorLogger.Instance.Warning("MainViewModel", "[InitializeGame] No deck selected, using random deck");
            }
            
            PlayerDeck.InitializeDeck(playerCards);
            PlayerDeck.DrawCards(PlayerDeck.StartingHandSize);

            // Draw cards for opponent
            OpponentDeck.DrawCards(4);

            // Reset game state
            PlayerHealth = 30;
            PlayerMaxHealth = 30;
            PlayerMana = 2;
            PlayerMaxMana = 10;
            OpponentHealth = 30;
            TurnCount = 1;
            IsPlayerTurn = true;
            StatusMessage = "";

            // Refresh UI bindings
            RefreshHand();
            OnPropertyChanged(nameof(FieldSlots));
            OnPropertyChanged(nameof(PlayerHand));
            
            // Change view to game
            CurrentView = "Game";
            
            ErrorLogger.Instance.Info("MainViewModel", "[Operation: InitializeGame] Game initialized successfully");
        }
        catch (Exception ex)
        {
            ErrorLogger.Instance.Error("MainViewModel", "[Operation: InitializeGame] Failed to initialize game", ex);
            StatusMessage = $"Error: {ex.Message}";
            // Don't throw - try to stay in current view
            CurrentView = "Menu";
        }
    }

    public void AddCardToDeck(CardViewModel card)
    {
        if (card == null) return;
        PlayerDeckCards.Add(card);
        OnPropertyChanged(nameof(PlayerDeckCardCount));
        OnPropertyChanged(nameof(DeckCountText));
        MarkDeckAsChanged();
    }

    public void RemoveCardFromDeck(CardViewModel? card)
    {
        if (card == null) return;
        PlayerDeckCards.Remove(card);
        OnPropertyChanged(nameof(PlayerDeckCardCount));
        OnPropertyChanged(nameof(DeckCountText));
        MarkDeckAsChanged();
    }

    public void ClearDeck()
    {
        PlayerDeckCards.Clear();
        OnPropertyChanged(nameof(PlayerDeckCardCount));
        OnPropertyChanged(nameof(DeckCountText));
        MarkDeckAsChanged();
    }

    public string DeckCountText => $"{PlayerDeckCards.Count} cards";

    private void StartGame()
    {
        CurrentView = "Game";
    }

    private void RefreshHand()
    {
        PlayerHand.Clear();
        foreach (var card in PlayerDeck.Hand)
        {
            PlayerHand.Add(new CardViewModel(card));
        }
    }

    private void EndTurn()
    {
        if (!IsPlayerTurn) return;

        ResolveCombat();

        if (OpponentHealth <= 0 || PlayerHealth <= 0)
        {
            return;
        }

        // Player weapon deals damage at end of turn
        ResolvePlayerWeaponDamage();

        if (OpponentHealth <= 0 || PlayerHealth <= 0)
        {
            return;
        }

        IsPlayerTurn = false;
        ExecuteOpponentTurn();

        if (OpponentHealth <= 0 || PlayerHealth <= 0)
        {
            return;
        }

        TurnCount++;
        PlayerMana = Math.Min(TurnCount, 10);
        IsPlayerTurn = true;
        PlayerDeck.DrawCards(1);
        RefreshHand();
    }

    private void ExecuteOpponentTurn()
    {
        if (_opponentAI == null) return;

        LogToFile("[ExecuteOpponentTurn] START - FieldSlots before:");
        for (int i = 6; i < 12; i++)
            LogToFile($"  FieldSlots[{i}] = {FieldSlots[i]?.Name ?? "null"}");
        
        var playerSlots = Enumerable.Range(0, 6).Select(i => new CreatureSlot
        {
            SlotIndex = i,
            Creature = FieldSlots[i]?.Card
        }).ToList();

        int opponentMana = Math.Min(TurnCount, 10);
        bool keepPlaying = true;

        while (keepPlaying && opponentMana > 0)
        {
            var decision = _opponentAI.DecideAction(OpponentDeck, opponentMana, playerSlots);

            switch (decision.Type)
            {
                case OpponentAI.AIDecisionType.PlayCreature:
                    if (decision.CardId != null && decision.SlotIndex.HasValue)
                    {
                        var card = OpponentDeck.Hand.FirstOrDefault(c => c.Id == decision.CardId);
                        if (card != null && card.Type == CardType.Creature)
                        {
                            OpponentDeck.PlayCreatureToSlot(card.Id, decision.SlotIndex.Value);
                            opponentMana -= card.ManaCost;
                            var cardVm = new CardViewModel(card) { IsOnField = true };
                            FieldSlots[decision.SlotIndex.Value] = cardVm;
                            LogToFile($"[ExecuteOpponentTurn] Played {card.Name} to slot {decision.SlotIndex.Value}, FieldSlots now: " + 
                                string.Join(", ", Enumerable.Range(0, 6).Select(i => $"{i}:{FieldSlots[i]?.Name ?? "null"}")));
                            
                            // Update status message
                            StatusMessage = $"Opponent summons {card.Name}!";
                            
                            RefreshCreatureSlotCaches();
                            OnPropertyChanged(nameof(FieldSlots));
                            OnPropertyChanged(nameof(OpponentCreatureSlots));
                            OnPropertyChanged(nameof(PlayerCreatureSlots));
                            UpdateOpponentSlotDisplay(decision.SlotIndex.Value, card);
                        }
                        else
                        {
                            keepPlaying = false;
                        }
                    }
                    else
                    {
                        keepPlaying = false;
                    }
                    break;

                case OpponentAI.AIDecisionType.PlaySpell:
                case OpponentAI.AIDecisionType.PlayWeapon:
                case OpponentAI.AIDecisionType.PlayArtifact:
                    if (decision.CardId != null)
                    {
                        var card = OpponentDeck.Hand.FirstOrDefault(c => c.Id == decision.CardId);
                        if (card != null && card.ManaCost <= opponentMana)
                        {
                            OpponentDeck.PlayCard(card.Id);
                            opponentMana -= card.ManaCost;
                            LogToFile($"[ExecuteOpponentTurn] Playing {card.Name} (type={card.Type})");
                            
                            // Update status message to show opponent action
                            string cardTypeStr = card.Type.ToString();
                            StatusMessage = $"Opponent plays {card.Name}!";
                            
                            ApplyOpponentCardEffects(card);
                        }
                        else
                        {
                            keepPlaying = false;
                        }
                    }
                    else
                    {
                        keepPlaying = false;
                    }
                    break;

                case OpponentAI.AIDecisionType.Attack:
                    ResolveOpponentCombat();
                    break;

                default:
                    keepPlaying = false;
                    break;
            }
        }

        LogToFile("[ExecuteOpponentTurn] Before ResolveOpponentCombat - FieldSlots:");
        for (int i = 6; i < 12; i++)
            LogToFile($"  FieldSlots[{i}] = {FieldSlots[i]?.Name ?? "null"}");
            
        ResolveOpponentCombat();
        ResolveOpponentWeaponDamage();
        OpponentDeck.DrawCards(1);
        
        LogToFile("[ExecuteOpponentTurn] END - FieldSlots:");
        for (int i = 6; i < 12; i++)
            LogToFile($"  FieldSlots[{i}] = {FieldSlots[i]?.Name ?? "null"}");
    }

    private void UpdateOpponentSlotDisplay(int slotIndex, Card card)
    {
    }

    private void ApplyOpponentCardEffects(Card card)
    {
        LogToFile($"[ApplyOpponentCardEffects] Processing card: {card.Name}, Effects count: {card.Effects.Count}");
        
        foreach (var effect in card.Effects)
        {
            LogToFile($"[ApplyOpponentCardEffects] Effect: {effect.Type}, Target: {effect.Target}, Value: {effect.Value}");
            
            if (effect.Type == EffectType.Damage)
            {
                if (effect.Target == TargetType.Enemy)
                {
                    var playerCreatures = FieldSlots.Skip(6).Where(s => s != null).ToList();
                    LogToFile($"[ApplyOpponentCardEffects] Player creatures found: {playerCreatures.Count}");
                    
                    if (playerCreatures.Count > 0)
                    {
                        var target = playerCreatures.First();
                        if (target != null)
                        {
                            LogToFile($"[ApplyOpponentCardEffects] Applying {effect.Value} damage to {target.Name}, current HP: {target.Health}");
                            
                            target.ApplyDamage(effect.Value);
                            target.IsOnField = true;
                            
                            LogToFile($"[ApplyOpponentCardEffects] After damage: {target.Name} HP = {target.Health}");
                            
                            if (target.Health <= 0)
                            {
                                LogToFile($"[ApplyOpponentCardEffects] Creature died! Removing from slot.");
                                target.ResetDamage();
                                target.ClearStatusEffects();
                                var idx = Array.IndexOf(FieldSlots, target);
                                LogToFile($"[ApplyOpponentCardEffects] Found at index {idx}");
                                if (idx >= 0) 
                                {
                                    FieldSlots[idx] = null;
                                    RefreshCreatureSlotCaches();
                                }
                                OnPropertyChanged(nameof(FieldSlots));
                            }
                        }
                    }
                    else
                    {
                        LogToFile($"[ApplyOpponentCardEffects] No player creatures - applying {effect.Value} direct damage to player");
                        PlayerHealth -= effect.Value;
                    }
                }
            }
        }
    }

    private void ResolveOpponentCombat()
    {
        LogToFile("[ResolveOpponentCombat] START - FieldSlots before:");
        for (int i = 6; i < 12; i++)
            LogToFile($"  FieldSlots[{i}] = {FieldSlots[i]?.Name ?? "null"}");
            
        for (int i = 0; i < 6; i++)
        {
            var opponentCreature = FieldSlots[i]?.Card;
            var playerCreature = FieldSlots[i + 6]?.Card;

            if (opponentCreature != null && opponentCreature.Power > 0)
            {
                LogToFile($"[ResolveOpponentCombat] Opponent creature {opponentCreature.Name} (Power={opponentCreature.Power})");
                
                if (playerCreature != null)
                {
                    LogToFile($"[ResolveOpponentCombat] Attacking player creature {playerCreature.Name} (HP={playerCreature.Health})");
                    playerCreature.Health -= opponentCreature.Power;
                    if (playerCreature.Health <= 0)
                    {
                        LogToFile($"[ResolveOpponentCombat] Player creature died! Removing from slot {i + 6}");
                        FieldSlots[i + 6] = null;
                        RefreshCreatureSlotCaches();
                        PlayerHealth -= opponentCreature.Power;
                        StatusMessage = $"Your {playerCreature.Name} was destroyed by {opponentCreature.Name}!";
                    }
                    else
                    {
                        StatusMessage = $"{opponentCreature.Name} attacks your {playerCreature.Name} for {opponentCreature.Power} damage!";
                    }
                }
                else
                {
                    PlayerHealth -= opponentCreature.Power;
                    StatusMessage = $"{opponentCreature.Name} deals {opponentCreature.Power} damage to you!";
                }
                OnPropertyChanged(nameof(FieldSlots));
            }
        }
        
        LogToFile("[ResolveOpponentCombat] END - FieldSlots after:");
        for (int i = 6; i < 12; i++)
            LogToFile($"  FieldSlots[{i}] = {FieldSlots[i]?.Name ?? "null"}");
    }

    /// <summary>
    /// Resolve opponent weapon damage at end of turn
    /// </summary>
    private void ResolveOpponentWeaponDamage()
    {
        if (OpponentWeapon == null) return;

        var weapon = OpponentWeapon.Card as WeaponCard;
        if (weapon == null) return;

        if (weapon.TargetType == WeaponTargetType.DamageToOpponent)
        {
            // Direct damage to player
            int damage = weapon.Power;
            PlayerHealth -= damage;
            StatusMessage = $"Opponent's {weapon.Name} deals {damage} damage to you!";
            LogToFile($"[ResolveOpponentWeaponDamage] {weapon.Name} deals {damage} direct damage to player");
        }
        else if (weapon.TargetType == WeaponTargetType.DamageToCreatures)
        {
            // Attack player creatures, starting from slot 0 (leftmost) - slot 6 in FieldSlots
            var target = FindPlayerWeaponTarget();

            if (target != null)
            {
                int damage = weapon.Power;
                target.ApplyDamage(damage);
                target.IsOnField = true;

                // Check if target creature died
                if (target.Health <= 0)
                {
                    target.ResetDamage();
                    target.ClearStatusEffects();
                    // Find and remove the creature from player slots (indices 6-11)
                    int slotIndex = Array.FindIndex(FieldSlots, s => s?.Id == target.Id);
                    if (slotIndex >= 6)
                    {
                        FieldSlots[slotIndex] = null;
                    }
                    RefreshCreatureSlotCaches();
                    StatusMessage = $"Your {target.Name} was destroyed by {weapon.Name}!";
                }
                else
                {
                    StatusMessage = $"Opponent's {weapon.Name} attacks your {target.Name} for {damage} damage!";
                }

                LogToFile($"[ResolveOpponentWeaponDamage] {weapon.Name} attacks {target.Name} for {damage} damage, remaining HP: {target.Health}");
            }
            else
            {
                LogToFile($"[ResolveOpponentWeaponDamage] {weapon.Name} attacks but no player creatures found");
            }
        }

        OnPropertyChanged(nameof(PlayerHealth));
        OnPropertyChanged(nameof(FieldSlots));
        OnPropertyChanged(nameof(PlayerCreatureSlots));
    }

    /// <summary>
    /// Find a target for opponent weapon that damages creatures
    /// Starts at slot 6 (leftmost player slot) and scans
    /// </summary>
    private CardViewModel? FindPlayerWeaponTarget()
    {
        // Player creature slots are at indices 6-11 in FieldSlots
        for (int i = 6; i < 12; i++)
        {
            if (FieldSlots[i] != null)
            {
                return FieldSlots[i];
            }
        }
        return null;
    }

    private void ResolveCombat()
    {
        for (int i = 0; i < 6; i++)
        {
            var playerCreatureVm = FieldSlots[i + 6];
            var opponentCreatureVm = FieldSlots[i];

            if (playerCreatureVm != null && playerCreatureVm.Power > 0)
            {
                if (opponentCreatureVm != null)
                {
                    // Damage opponent creature through CardViewModel tracking
                    int damage = playerCreatureVm.Power;
                    opponentCreatureVm.ApplyDamage(damage);
                    opponentCreatureVm.IsOnField = true;
                    
                    if (opponentCreatureVm.Health <= 0)
                    {
                        opponentCreatureVm.ResetDamage();
                        opponentCreatureVm.ClearStatusEffects();
                        FieldSlots[i] = null;
                        RefreshCreatureSlotCaches();
                        OpponentHealth -= damage;
                    }
                }
                else
                {
                    OpponentHealth -= playerCreatureVm.Power;
                }
            }
        }
    }

    /// <summary>
    /// Resolve player weapon damage at end of turn
    /// </summary>
    private void ResolvePlayerWeaponDamage()
    {
        if (PlayerWeapon == null) return;

        var weapon = PlayerWeapon.Card as WeaponCard;
        if (weapon == null) return;

        if (weapon.TargetType == WeaponTargetType.DamageToOpponent)
        {
            // Direct damage to opponent
            int damage = weapon.Power;
            OpponentHealth -= damage;
            StatusMessage = $"{weapon.Name} deals {damage} damage to opponent!";
            LogToFile($"[ResolvePlayerWeaponDamage] {weapon.Name} deals {damage} direct damage to opponent");
        }
        else if (weapon.TargetType == WeaponTargetType.DamageToCreatures)
        {
            // Attack enemy creatures, starting from slot 0 (leftmost)
            var target = FindWeaponTarget(OpponentCreatureSlots);

            if (target != null)
            {
                int damage = weapon.Power;
                target.ApplyDamage(damage);
                target.IsOnField = true;
                StatusMessage = $"{weapon.Name} attacks {target.Name} for {damage} damage!";

                // Check if target creature died
                if (target.Health <= 0)
                {
                    target.ResetDamage();
                    target.ClearStatusEffects();
                    // Find and remove the creature from slots
                    int slotIndex = Array.FindIndex(OpponentCreatureSlots, s => s?.Id == target.Id);
                    if (slotIndex >= 0)
                    {
                        FieldSlots[slotIndex] = null;
                    }
                    RefreshCreatureSlotCaches();
                    StatusMessage = $"{target.Name} was destroyed!";
                }

                LogToFile($"[ResolvePlayerWeaponDamage] {weapon.Name} attacks {target.Name} for {damage} damage, remaining HP: {target.Health}");
            }
            else
            {
                LogToFile($"[ResolvePlayerWeaponDamage] {weapon.Name} attacks but no enemy creatures found");
            }
        }

        OnPropertyChanged(nameof(OpponentHealth));
        OnPropertyChanged(nameof(FieldSlots));
        OnPropertyChanged(nameof(OpponentCreatureSlots));
    }

    /// <summary>
    /// Find a target for a weapon that damages creatures
    /// Starts at slot 0 (leftmost) and scans left-to-right
    /// </summary>
    private CardViewModel? FindWeaponTarget(CardViewModel?[] opponentSlots)
    {
        for (int i = 0; i < opponentSlots.Length; i++)
        {
            if (opponentSlots[i] != null)
            {
                return opponentSlots[i];
            }
        }
        return null;
    }

    private void ExecuteCombine()
    {
        if (ComboCard1 == null || ComboCard2 == null) return;

        var result = _cardCombiner.Combine(ComboCard1.Card, ComboCard2.Card);
        if (!result.Success || result.ResultCard == null)
        {
            StatusMessage = result.Message;
            return;
        }

        PlayerDeck.DiscardFromHand(ComboCard1.Id);
        PlayerDeck.DiscardFromHand(ComboCard2.Id);

        var card1ToRemove = PlayerHand.FirstOrDefault(c => c.Id == ComboCard1.Id);
        var card2ToRemove = PlayerHand.FirstOrDefault(c => c.Id == ComboCard2.Id);
        if (card1ToRemove != null) PlayerHand.Remove(card1ToRemove);
        if (card2ToRemove != null) PlayerHand.Remove(card2ToRemove);

        ComboResult = new CardViewModel(result.ResultCard);
        CustomCards.Add(ComboResult);
        IsComboLocked = true;
        CanCombine = false;

        StatusMessage = $"Created {result.ResultCard.Name}!";
        OnPropertyChanged(nameof(PlayerHand));
    }

    private void ClearCombo()
    {
        ComboCard1 = null;
        ComboCard2 = null;
        ComboResult = null;
        CanCombine = false;
        IsComboLocked = false;
    }

    public void TakeComboResult()
    {
        if (ComboResult == null || !IsComboLocked) return;

        PlayerDeck.Hand.Add(ComboResult.Card);
        PlayerHand.Add(ComboResult);

        StatusMessage = $"Added {ComboResult.Card.Name} to hand!";
        ClearCombo();
        OnPropertyChanged(nameof(PlayerHand));
    }

    public void SetComboCard(CardViewModel card, int slot)
    {
        if (slot == 1)
            ComboCard1 = card;
        else
            ComboCard2 = card;

        UpdateComboPreview();
    }

    private void UpdateComboPreview()
    {
        if (ComboCard1 == null || ComboCard2 == null)
        {
            ComboResult = null;
            CanCombine = false;
            return;
        }

        var result = _cardCombiner.Combine(ComboCard1.Card, ComboCard2.Card);
        if (result.Success && result.ResultCard != null)
        {
            ComboResult = new CardViewModel(result.ResultCard);
            CanCombine = true;
        }
        else
        {
            ComboResult = null;
            CanCombine = false;
        }
    }

    public void PlayCardToSlot(CardViewModel card, int slotIndex)
    {
        LogToFile($"[PlayCardToSlot] card={card?.Name}, Type={card?.Card?.Type}, slotIndex={slotIndex}, Mana={PlayerMana}, Cost={card?.Card?.ManaCost}");
        
        if (slotIndex < 6 || slotIndex > 11) 
        {
            LogToFile("[PlayCardToSlot] FAIL: invalid slot index");
            return;
        }
        
        if (card.Card.Type != CardType.Creature)
        {
            StatusMessage = "Only creature cards can be played to the field!";
            LogToFile("[PlayCardToSlot] FAIL: not a creature");
            return;
        }
        if (card.Card.ManaCost > PlayerMana)
        {
            StatusMessage = "Not enough mana!";
            LogToFile("[PlayCardToSlot] FAIL: not enough mana");
            return;
        }
        if (FieldSlots[slotIndex] != null) 
        {
            LogToFile("[PlayCardToSlot] FAIL: slot occupied");
            return;
        }

        PlayerMana -= card.Card.ManaCost;
        PlayerDeck.PlayCard(card.Id);

        // Find CardViewModel by ID, not reference
        var cardInHand = PlayerHand.FirstOrDefault(c => c.Id == card.Id);
        if (cardInHand != null)
        {
            PlayerHand.Remove(cardInHand);
        }

        // Mark card as being on the field for damage tracking
        card.IsOnField = true;
        card.ResetDamage();
        card.ClearStatusEffects();
        
        FieldSlots[slotIndex] = card;
        RefreshCreatureSlotCaches();

        LogToFile($"[PlayCardToSlot] SUCCESS: FieldSlots[{slotIndex}]={card.Name}");
        
        // Force refresh by creating new array instances to trigger UI update
        OnPropertyChanged(nameof(FieldSlots));
        OnPropertyChanged(nameof(OpponentCreatureSlots));
        OnPropertyChanged(nameof(PlayerCreatureSlots));
        OnPropertyChanged(nameof(PlayerHand));
    }

    public void MoveCardToSlot(CardViewModel card, int targetSlotIndex)
    {
        LogToFile($"[MoveCardToSlot] card={card?.Name}, Type={card?.Card?.Type}, targetSlotIndex={targetSlotIndex}");
        
        if (targetSlotIndex < 6 || targetSlotIndex > 11) 
        {
            LogToFile("[MoveCardToSlot] FAIL: invalid slot index");
            return;
        }
        
        int sourceSlotIndex = -1;
        for (int i = 6; i < 12; i++)
        {
            if (FieldSlots[i]?.Id == card.Id)
            {
                sourceSlotIndex = i;
                break;
            }
        }

        if (sourceSlotIndex == -1)
        {
            PlayCardToSlot(card, targetSlotIndex);
            return;
        }

        if (sourceSlotIndex == targetSlotIndex) return;

        if (FieldSlots[targetSlotIndex] == null)
        {
            FieldSlots[targetSlotIndex] = card;
            FieldSlots[sourceSlotIndex] = null;
            RefreshCreatureSlotCaches();
        }

        OnPropertyChanged(nameof(FieldSlots));
    }

    public void SelectCardFromHand(CardViewModel card)
    {
        SelectedCard = card;
    }

    public void SelectCardFromField(CardViewModel card)
    {
        SelectedCard = card;
    }

    private void PlayCard(CardViewModel? card)
    {
        if (card == null) return;
        SelectedCard = card;
    }

    private void Navigate(string? view)
    {
        if (string.IsNullOrEmpty(view)) return;

        // Check for unsaved changes when leaving deck builder
        if (CurrentView == "DeckBuilder" && _hasUnsavedDeckChanges && view != "DeckBuilder")
        {
            var dialog = new Views.UnsavedChangesDialog();
            dialog.ShowDialog();

            switch (dialog.Result)
            {
                case Views.UnsavedChangesDialog.UnsavedDialogResult.Save:
                    // Save and then navigate
                    SaveDeck();
                    break;
                case Views.UnsavedChangesDialog.UnsavedDialogResult.Discard:
                    // Just continue navigating (discard changes)
                    break;
                case Views.UnsavedChangesDialog.UnsavedDialogResult.Cancel:
                default:
                    // Cancel navigation
                    return;
            }
        }

        CurrentView = view;
    }
    
    /// <summary>
    /// Select difficulty and initialize a new game with automatic deck generation
    /// </summary>
    public void SelectDifficultyAndStart(DifficultyLevel difficulty)
    {
        _difficulty = difficulty;
        InitializeGame();
    }

    /// <summary>
    /// Close the zoomed card overlay
    /// </summary>
    public void CloseZoom()
    {
        ErrorLogger.Instance.Debug("MainViewModel", "[CloseZoom] called");
        _zoomedCard = null;
        OnPropertyChanged(nameof(ZoomedCard));
        OnPropertyChanged(nameof(IsCardZoomed));
        ErrorLogger.Instance.Debug("MainViewModel", $"[CloseZoom] complete, IsCardZoomed={IsCardZoomed}");
    }

    /// <summary>
    /// Zoom in on a card to show full details
    /// </summary>
    public void ZoomCard(CardViewModel? card)
    {
        if (card == null) return;
        ZoomedCard = card;
    }

    public void EquipWeapon(CardViewModel? card)
    {
        if (card == null || card.Card.Type != CardType.Weapon) return;
        
        if (card.Card.ManaCost > PlayerMana)
        {
            StatusMessage = "Not enough mana!";
            return;
        }
        
        PlayerMana -= card.Card.ManaCost;
        PlayerWeapon = card;
        PlayerWeaponBonus = card.Card.Power;
        
        // Find CardViewModel by ID and remove from hand
        var cardInHand = PlayerHand.FirstOrDefault(c => c.Id == card.Id);
        if (cardInHand != null)
        {
            PlayerHand.Remove(cardInHand);
        }
        
        OnPropertyChanged(nameof(PlayerWeapon));
        OnPropertyChanged(nameof(PlayerWeaponBonus));
        OnPropertyChanged(nameof(PlayerHand));
    }

    public void EquipArmor(CardViewModel? card)
    {
        if (card == null || card.Card.Type != CardType.Armor) return;
        
        if (card.Card.ManaCost > PlayerMana)
        {
            StatusMessage = "Not enough mana!";
            return;
        }
        
        PlayerMana -= card.Card.ManaCost;
        PlayerArmor = card;
        PlayerArmorBonus = card.Card.Health;
        
        // Find CardViewModel by ID and remove from hand
        var cardInHand = PlayerHand.FirstOrDefault(c => c.Id == card.Id);
        if (cardInHand != null)
        {
            PlayerHand.Remove(cardInHand);
        }
        
        OnPropertyChanged(nameof(PlayerArmor));
        OnPropertyChanged(nameof(PlayerArmorBonus));
        OnPropertyChanged(nameof(PlayerHand));
    }

    public void UnequipWeapon()
    {
        PlayerWeapon = null;
        PlayerWeaponBonus = 0;
        OnPropertyChanged(nameof(PlayerWeapon));
        OnPropertyChanged(nameof(PlayerWeaponBonus));
    }

    public void UnequipArmor()
    {
        PlayerArmor = null;
        PlayerArmorBonus = 0;
        OnPropertyChanged(nameof(PlayerArmor));
        OnPropertyChanged(nameof(PlayerArmorBonus));
    }

    /// <summary>
    /// Play an artifact card to one of the artifact slots
    /// </summary>
    public void PlayArtifactToSlot(CardViewModel card, int slotIndex)
    {
        LogToFile($"[PlayArtifactToSlot] Enter: card={card?.Name}, Type={card?.Card?.Type}, slotIndex={slotIndex}");
        
        if (card == null || card.Card.Type != CardType.Artifact)
        {
            LogToFile($"[PlayArtifactToSlot] FAIL: card null={card == null}, Type={card?.Card?.Type}");
            StatusMessage = "Only artifact cards can be played to artifact slots!";
            return;
        }
        
        if (slotIndex < 0 || slotIndex >= 3)
        {
            LogToFile($"[PlayArtifactToSlot] FAIL: invalid slot index {slotIndex}");
            StatusMessage = "Invalid artifact slot!";
            return;
        }
        
        if (card.Card.ManaCost > PlayerMana)
        {
            LogToFile($"[PlayArtifactToSlot] FAIL: not enough mana. Cost={card.Card.ManaCost}, Available={PlayerMana}");
            StatusMessage = "Not enough mana!";
            return;
        }
        
        if (PlayerArtifactSlots[slotIndex] != null)
        {
            LogToFile($"[PlayArtifactToSlot] FAIL: slot {slotIndex} occupied");
            StatusMessage = "Artifact slot is already occupied!";
            return;
        }

        LogToFile($"[PlayArtifactToSlot] All checks passed, playing card");
        
        PlayerMana -= card.Card.ManaCost;
        
        // Remove from hand
        var cardInHand = PlayerHand.FirstOrDefault(c => c.Id == card.Id);
        if (cardInHand != null)
        {
            PlayerHand.Remove(cardInHand);
            LogToFile($"[PlayArtifactToSlot] Removed from hand: {card.Name}");
        }
        else
        {
            LogToFile($"[PlayArtifactToSlot] WARNING: Card not found in hand!");
        }

        PlayerArtifactSlots[slotIndex] = card;

        // Create new array reference to trigger UI update
        var newArray = new CardViewModel?[3];
        for (int i = 0; i < 3; i++)
            newArray[i] = _playerArtifactSlots[i];
        _playerArtifactSlots = newArray;

        // Update ObservableCollection for additional UI binding
        PlayerArtifactSlotsObs.Clear();
        for (int i = 0; i < 3; i++)
            PlayerArtifactSlotsObs.Add(_playerArtifactSlots[i]);

        OnPropertyChanged(nameof(PlayerArtifactSlots));
        OnPropertyChanged(nameof(PlayerArtifactSlotsObs));
        OnPropertyChanged(nameof(PlayerHand));
        StatusMessage = $"Played {card.Name} to artifact slot!";
        LogToFile($"[PlayArtifactToSlot] SUCCESS");
    }

    /// <summary>
    /// Cast a spell card (no slot needed, just mana cost and removes from hand)
    /// </summary>
    public void CastSpell(CardViewModel card)
    {
        if (card == null || card.Card.Type != CardType.Spell)
        {
            StatusMessage = "Only spell cards can be cast!";
            return;
        }
        
        if (card.Card.ManaCost > PlayerMana)
        {
            StatusMessage = "Not enough mana!";
            return;
        }
        
        // Check if spell needs a target - for now cast as global
        PlayerMana -= card.Card.ManaCost;
        
        // Remove from hand
        var cardInHand = PlayerHand.FirstOrDefault(c => c.Id == card.Id);
        if (cardInHand != null)
        {
            PlayerHand.Remove(cardInHand);
        }
        
        // Apply spell effects (global - affects all)
        ApplySpellEffects(card.Card, null);
        
        OnPropertyChanged(nameof(PlayerHand));
        StatusMessage = $"Cast {card.Name}!";
    }

    /// <summary>
    /// Cast a spell targeting a specific creature slot
    /// </summary>
    public void CastSpellOnCreature(CardViewModel card, int targetSlotIndex)
    {
        if (card == null || card.Card.Type != CardType.Spell)
        {
            StatusMessage = "Only spell cards can be cast!";
            return;
        }
        
        if (card.Card.ManaCost > PlayerMana)
        {
            StatusMessage = "Not enough mana!";
            return;
        }
        
        // Validate target - creature slots are 0-5 for opponent, 6-11 for player
        if (targetSlotIndex < 0 || targetSlotIndex > 11 || FieldSlots[targetSlotIndex] == null)
        {
            StatusMessage = "No creature in target slot!";
            return;
        }
        
        PlayerMana -= card.Card.ManaCost;
        
        // Remove from hand
        var cardInHand = PlayerHand.FirstOrDefault(c => c.Id == card.Id);
        if (cardInHand != null)
        {
            PlayerHand.Remove(cardInHand);
        }
        
        // Apply spell effects to target creature
        ApplySpellEffects(card.Card, targetSlotIndex);
        
        OnPropertyChanged(nameof(PlayerHand));
        StatusMessage = $"Cast {card.Name} on {FieldSlots[targetSlotIndex]?.Name}!";
    }

    /// <summary>
    /// Apply spell effects based on card effects
    /// </summary>
    private void ApplySpellEffects(Card spellCard, int? targetSlotIndex)
    {
        foreach (var effect in spellCard.Effects)
        {
            switch (effect.Type)
            {
                case EffectType.Damage:
                    if (targetSlotIndex.HasValue)
                    {
                        // Damage single creature
                        var target = FieldSlots[targetSlotIndex.Value];
                        if (target != null)
                        {
                            target.ApplyDamage(effect.Value);
                            target.IsOnField = true;
                            LogToFile($"[Spell] Damaged {target.Name} for {effect.Value}");
                            if (target.Health <= 0)
                            {
                                // Creature dies
                                target.ResetDamage();
                                target.ClearStatusEffects();
                                FieldSlots[targetSlotIndex.Value] = null;
                                RefreshCreatureSlotCaches();
                            }
                        }
                    }
                    else
                    {
                        // Global damage to all opponent creatures
                        for (int i = 0; i < 6; i++)
                        {
                            var oppCreature = FieldSlots[i];
                            if (oppCreature != null)
                            {
                                oppCreature.ApplyDamage(effect.Value);
                                oppCreature.IsOnField = true;
                                if (oppCreature.Health <= 0)
                                {
                                    oppCreature.ResetDamage();
                                    oppCreature.ClearStatusEffects();
                                    FieldSlots[i] = null;
                                }
                            }
                        }
                        RefreshCreatureSlotCaches();
                    }
                    break;
                    
                case EffectType.Heal:
                    if (targetSlotIndex.HasValue)
                    {
                        // Heal single creature
                        var target = FieldSlots[targetSlotIndex.Value];
                        if (target != null)
                        {
                            target.HealDamage(effect.Value);
                            LogToFile($"[Spell] Healed {target.Name} for {effect.Value}");
                        }
                    }
                    else
                    {
                        // Global heal to all player creatures
                        for (int i = 6; i < 12; i++)
                        {
                            var playerCreature = FieldSlots[i];
                            if (playerCreature != null)
                            {
                                playerCreature.HealDamage(effect.Value);
                            }
                        }
                    }
                    break;
                    
                case EffectType.Debuff:
                    // Direct damage to opponent
                    OpponentHealth -= effect.Value;
                    LogToFile($"[Spell] Damage to opponent: {effect.Value}");
                    break;
                    
                case EffectType.Buff:
                    // Direct heal to player
                    PlayerHealth = Math.Min(PlayerHealth + effect.Value, PlayerMaxHealth);
                    LogToFile($"[Spell] Heal to player: {effect.Value}");
                    break;
                    
                case EffectType.ManaGain:
                    PlayerMana += effect.Value;
                    LogToFile($"[Spell] Mana gained: {effect.Value}");
                    break;
                    
                case EffectType.DrawCard:
                    // Draw extra cards
                    PlayerDeck.DrawCards(effect.Value);
                    RefreshHand();
                    LogToFile($"[Spell] Drew {effect.Value} cards");
                    break;
                    
                case EffectType.Destroy:
                    // Destroy target artifact
                    if (targetSlotIndex.HasValue)
                    {
                        PlayerArtifactSlots[targetSlotIndex.Value - 6] = null;
                    }
                    break;
                    
                default:
                    LogToFile($"[Spell] Unhandled effect type: {effect.Type}");
                    break;
            }
        }
    }

    /// <summary>
    /// Play an event card to the event slot
    /// </summary>
    public void PlayEventToSlot(CardViewModel card)
    {
        if (card == null || card.Card.Type != CardType.Event)
        {
            StatusMessage = "Only event cards can be played to the event slot!";
            return;
        }
        
        if (card.Card.ManaCost > PlayerMana)
        {
            StatusMessage = "Not enough mana!";
            return;
        }
        
        if (PlayerEventSlot != null)
        {
            StatusMessage = "Event slot is already occupied!";
            return;
        }

        PlayerMana -= card.Card.ManaCost;
        
        // Remove from hand
        var cardInHand = PlayerHand.FirstOrDefault(c => c.Id == card.Id);
        if (cardInHand != null)
        {
            PlayerHand.Remove(cardInHand);
        }

        PlayerEventSlot = card;

        OnPropertyChanged(nameof(PlayerEventSlot));
        OnPropertyChanged(nameof(PlayerHand));
        StatusMessage = $"Played {card.Name} to event slot!";
    }
}
