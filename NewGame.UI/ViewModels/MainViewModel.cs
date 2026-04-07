using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MagicalDeckbuilder.Cards;
using MagicalDeckbuilder.Combining;
using MagicalDeckbuilder.Decks;
using MagicalDeckbuilder.Game;
using MagicalDeckbuilder.Logging;
using MagicalDeckbuilder.Storage;
using Card = MagicalDeckbuilder.Cards.Card;
using CardType = MagicalDeckbuilder.Cards.CardType;
using EffectType = MagicalDeckbuilder.Cards.EffectType;
using TargetType = MagicalDeckbuilder.Cards.TargetType;
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
    
    private void LoadSavedDecks()
    {
        PlayerDecks.Clear();
        var savedDecks = DeckStorageService.Instance.GetPlayerDecks();
        foreach (var deck in savedDecks)
        {
            PlayerDecks.Add(new SavedDeckViewModel(deck));
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
        
        var deckName = PromptForDeckName(_currentDeckId != null ? 
            DeckStorageService.Instance.GetDeckIndex().FirstOrDefault(d => d.Id == _currentDeckId)?.Name : null);
        
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
            await DeckStorageService.Instance.SaveDeckAsync(deckName, cards, DeckType.Player, Difficulty);
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
    public CardViewModel?[] PlayerArtifactSlots { get; } = new CardViewModel?[3];

    // Event slot (1 slot for player events)
    public CardViewModel? PlayerEventSlot { get; private set; }

    // Opponent artifact slots (3 slots)
    public CardViewModel?[] OpponentArtifactSlots { get; } = new CardViewModel?[3];

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
        
        LogToFile("[RefreshCaches] After refresh: PlayerSlots[0]=" + (_cachedPlayerCreatureSlots[0]?.Name ?? "null"));
        
        // Update ObservableCollections for UI binding
        OpponentCreatureSlotsObs.Clear();
        PlayerCreatureSlotsObs.Clear();
        for (int i = 0; i < 6; i++)
        {
            OpponentCreatureSlotsObs.Add(_cachedOpponentCreatureSlots[i]);
            PlayerCreatureSlotsObs.Add(_cachedPlayerCreatureSlots[i]);
        }
        
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

    private void InitializeGame()
    {
        ErrorLogger.Instance.Debug("MainViewModel", "[Operation: InitializeGame] Starting game initialization");
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
            var playerCards = CardFactory.GenerateRandomDeck();
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
                            FieldSlots[decision.SlotIndex.Value] = new CardViewModel(card);
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

        ResolveOpponentCombat();
        OpponentDeck.DrawCards(1);
    }

    private void UpdateOpponentSlotDisplay(int slotIndex, Card card)
    {
    }

    private void ApplyOpponentCardEffects(Card card)
    {
        foreach (var effect in card.Effects)
        {
            if (effect.Type == EffectType.Damage)
            {
                if (effect.Target == TargetType.Enemy)
                {
                    var playerCreatures = FieldSlots.Skip(6).Where(s => s != null).ToList();
                    if (playerCreatures.Count > 0)
                    {
                        var target = playerCreatures.First();
                        if (target != null)
                        {
                            target.Card.Health -= effect.Value;
                            if (target.Card.Health <= 0)
                            {
                                var idx = Array.IndexOf(FieldSlots, target);
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
                        PlayerHealth -= effect.Value;
                    }
                }
            }
        }
    }

    private void ResolveOpponentCombat()
    {
        for (int i = 0; i < 6; i++)
        {
            var opponentCreature = FieldSlots[i]?.Card;
            var playerCreature = FieldSlots[i + 6]?.Card;

            if (opponentCreature != null && opponentCreature.Power > 0)
            {
                if (playerCreature != null)
                {
                    playerCreature.Health -= opponentCreature.Power;
                    if (playerCreature.Health <= 0)
                    {
                        FieldSlots[i + 6] = null;
                        RefreshCreatureSlotCaches();
                        PlayerHealth -= opponentCreature.Power;
                    }
                }
                else
                {
                    PlayerHealth -= opponentCreature.Power;
                }
                OnPropertyChanged(nameof(FieldSlots));
            }
        }
    }

    private void ResolveCombat()
    {
        for (int i = 0; i < 6; i++)
        {
            var playerCreature = FieldSlots[i + 6]?.Card;
            var opponentCreature = FieldSlots[i]?.Card;

            if (playerCreature != null && playerCreature.Power > 0)
            {
                if (opponentCreature != null)
                {
                    opponentCreature.Health -= playerCreature.Power;
                    if (opponentCreature.Health <= 0)
                    {
                        FieldSlots[i] = null;
                        RefreshCreatureSlotCaches();
                        OpponentHealth -= playerCreature.Power;
                    }
                }
                else
                {
                    OpponentHealth -= playerCreature.Power;
                }
            }
        }
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
        if (card == null || card.Card.Type != CardType.Artifact)
        {
            StatusMessage = "Only artifact cards can be played to artifact slots!";
            return;
        }
        
        if (slotIndex < 0 || slotIndex >= 3)
        {
            StatusMessage = "Invalid artifact slot!";
            return;
        }
        
        if (card.Card.ManaCost > PlayerMana)
        {
            StatusMessage = "Not enough mana!";
            return;
        }
        
        if (PlayerArtifactSlots[slotIndex] != null)
        {
            StatusMessage = "Artifact slot is already occupied!";
            return;
        }

        PlayerMana -= card.Card.ManaCost;
        
        // Remove from hand
        var cardInHand = PlayerHand.FirstOrDefault(c => c.Id == card.Id);
        if (cardInHand != null)
        {
            PlayerHand.Remove(cardInHand);
        }

        PlayerArtifactSlots[slotIndex] = card;

        // Update ObservableCollection for WPF binding
        PlayerArtifactSlotsObs.Clear();
        for (int i = 0; i < 3; i++)
            PlayerArtifactSlotsObs.Add(PlayerArtifactSlots[i]);

        OnPropertyChanged(nameof(PlayerArtifactSlots));
        OnPropertyChanged(nameof(PlayerHand));
        StatusMessage = $"Played {card.Name} to artifact slot!";
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
                        if (target?.Card != null)
                        {
                            target.Card.Health -= effect.Value;
                            LogToFile($"[Spell] Damaged {target.Name} for {effect.Value}");
                            if (target.Card.Health <= 0)
                            {
                                // Creature dies
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
                            if (oppCreature?.Card != null)
                            {
                                oppCreature.Card.Health -= effect.Value;
                                if (oppCreature.Card.Health <= 0)
                                {
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
                        if (target?.Card != null)
                        {
                            target.Card.Health += effect.Value;
                            LogToFile($"[Spell] Healed {target.Name} for {effect.Value}");
                        }
                    }
                    else
                    {
                        // Global heal to all player creatures
                        for (int i = 6; i < 12; i++)
                        {
                            var playerCreature = FieldSlots[i];
                            if (playerCreature?.Card != null)
                            {
                                playerCreature.Card.Health += effect.Value;
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
