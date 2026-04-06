using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MagicalDeckbuilder.Cards;
using MagicalDeckbuilder.Combining;
using MagicalDeckbuilder.Decks;
using MagicalDeckbuilder.Game;
using MagicalDeckbuilder.Logging;
using Card = MagicalDeckbuilder.Cards.Card;
using CardType = MagicalDeckbuilder.Cards.CardType;
using EffectType = MagicalDeckbuilder.Cards.EffectType;
using TargetType = MagicalDeckbuilder.Cards.TargetType;
using PileType = MagicalDeckbuilder.Decks.PileType;
using CreatureSlot = MagicalDeckbuilder.Decks.CreatureSlot;

namespace NewGame.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly CardCombiner _cardCombiner = new();
    private OpponentAI? _opponentAI;
    private DifficultyLevel _difficulty = DifficultyLevel.Journeyman;

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

        var allCards = CardFactory.CreateStarterDeck();
        AvailableCards = new ObservableCollection<CardViewModel>(
            allCards.Select(c => new CardViewModel(c)));

        MenuCards = new ObservableCollection<CardViewModel>(
            CardFactory.CreateStarterDeck().Select(c => new CardViewModel(c)));
    }

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
            SetProperty(ref _zoomedCard, value);
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
    }

    public void RemoveCardFromDeck(CardViewModel? card)
    {
        if (card == null) return;
        PlayerDeckCards.Remove(card);
        OnPropertyChanged(nameof(PlayerDeckCardCount));
        OnPropertyChanged(nameof(DeckCountText));
    }

    public void ClearDeck()
    {
        PlayerDeckCards.Clear();
        OnPropertyChanged(nameof(PlayerDeckCardCount));
        OnPropertyChanged(nameof(DeckCountText));
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
                            OnPropertyChanged(nameof(FieldSlots));
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
                                if (idx >= 0) FieldSlots[idx] = null;
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
        if (slotIndex < 6 || slotIndex > 11) return;
        if (card.Card.Type != CardType.Creature)
        {
            StatusMessage = "Only creature cards can be played to the field!";
            return;
        }
        if (card.Card.ManaCost > PlayerMana)
        {
            StatusMessage = "Not enough mana!";
            return;
        }
        if (FieldSlots[slotIndex] != null) return;

        PlayerMana -= card.Card.ManaCost;
        PlayerDeck.PlayCard(card.Id);

        // Find CardViewModel by ID, not reference
        var cardInHand = PlayerHand.FirstOrDefault(c => c.Id == card.Id);
        if (cardInHand != null)
        {
            PlayerHand.Remove(cardInHand);
        }

        FieldSlots[slotIndex] = card;

        OnPropertyChanged(nameof(FieldSlots));
        OnPropertyChanged(nameof(PlayerHand));
    }

    public void MoveCardToSlot(CardViewModel card, int targetSlotIndex)
    {
        if (targetSlotIndex < 6 || targetSlotIndex > 11) 
            return;
        
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
        ZoomedCard = null;
    }

    /// <summary>
    /// Zoom in on a card to show full details
    /// </summary>
    public void ZoomCard(CardViewModel card)
    {
        ZoomedCard = card;
    }

    public void EquipWeapon(CardViewModel? card)
    {
        if (card == null || card.Card.Type != CardType.Weapon) return;
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

        OnPropertyChanged(nameof(PlayerArtifactSlots));
        OnPropertyChanged(nameof(PlayerHand));
        StatusMessage = $"Played {card.Name} to artifact slot!";
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
