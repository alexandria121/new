using System.Collections.ObjectModel;
using System.Windows.Input;
using MagicalDeckbuilder.Cards;
using MagicalDeckbuilder.Combining;
using MagicalDeckbuilder.Decks;
using MagicalDeckbuilder.Game;
using Card = MagicalDeckbuilder.Cards.Card;
using CardType = MagicalDeckbuilder.Cards.CardType;
using PileType = MagicalDeckbuilder.Decks.PileType;

namespace NewGame.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly CardCombiner _cardCombiner = new();

    private string _currentView = "Menu";
    private CardViewModel? _selectedCard;
    private CardViewModel? _comboCard1;
    private CardViewModel? _comboCard2;
    private CardViewModel? _comboResult;
    private string _statusMessage = "";
    private bool _canCombine;

    private int _playerHealth = 30;
    private int _playerMaxHealth = 30;
    private int _playerMana = 2;
    private int _playerMaxMana = 10;
    private int _opponentHealth = 30;
    private int _turnCount = 1;
    private bool _isPlayerTurn = true;

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
    public DeckManager OpponentDeck { get; }

    public int PlayerDeckCardCount => PlayerDeckCards.Count;

    public CardViewModel?[] FieldSlots { get; } = new CardViewModel?[12];

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

    public ICommand InitializeGameCommand { get; }
    public ICommand StartGameCommand { get; }
    public ICommand EndTurnCommand { get; }
    public ICommand CombineCardsCommand { get; }
    public ICommand ClearComboCommand { get; }
    public ICommand PlayCardCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand RemoveCardFromDeckCommand { get; }
    public ICommand ClearDeckCommand { get; }

    private void InitializeGame()
    {
        var playerCards = CardFactory.GenerateRandomDeck();
        PlayerDeck.InitializeDeck(playerCards);

        var opponentCards = CardFactory.GenerateRandomDeck();
        OpponentDeck.InitializeDeck(opponentCards);

        PlayerDeck.DrawCards(4);

        PlayerHealth = 30;
        PlayerMaxHealth = 30;
        PlayerMana = 2;
        PlayerMaxMana = 10;
        OpponentHealth = 30;
        TurnCount = 1;
        IsPlayerTurn = true;

        RefreshHand();
        CurrentView = "Game";
    }

    public void AddCardToDeck(CardViewModel card)
    {
        if (card == null) return;
        PlayerDeckCards.Add(card);
        OnPropertyChanged(nameof(PlayerDeckCardCount));
        OnPropertyChanged(nameof(DeckCountText));
    }

    public void RemoveCardFromDeck(CardViewModel card)
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
        IsPlayerTurn = false;

        PlayerDeck.DrawCards(1);
        RefreshHand();
        TurnCount++;
        PlayerMana = Math.Min(_turnCount, 10);
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

        PlayerDeck.PlayCard(ComboCard1.Id);
        PlayerDeck.PlayCard(ComboCard2.Id);

        var newCard = new CardViewModel(result.ResultCard);
        PlayerDeck.Hand.Add(result.ResultCard);
        PlayerHand.Add(newCard);
        CustomCards.Add(newCard);

        StatusMessage = $"Created {result.ResultCard.Name}!";
        ClearCombo();
        OnPropertyChanged(nameof(PlayerHand));
    }

    private void ClearCombo()
    {
        ComboCard1 = null;
        ComboCard2 = null;
        ComboResult = null;
        CanCombine = false;
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
        if (card.Card.Type != CardType.Creature) return;
        if (card.Card.ManaCost > PlayerMana) return;
        if (FieldSlots[slotIndex] != null) return;

        PlayerMana -= card.Card.ManaCost;
        PlayerDeck.PlayCard(card.Id);

        FieldSlots[slotIndex] = card;
        PlayerHand.Remove(card);

        OnPropertyChanged(nameof(FieldSlots));
        OnPropertyChanged(nameof(PlayerHand));
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
}
