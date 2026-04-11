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
    private readonly CombinationLookup _combinationLookup = new();
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

    // Quest/Search token tracking
    private int _questTokens = 0;
    private readonly List<Card> _questTableCards = new();

    // Ability targeting state
    private CardAbility? _activeAbility;
    private CardViewModel? _abilitySourceCard;
    private CardViewModel? _buddingFirstTarget; // For two-stage targeting (Budding)
    private bool _waitingForSecondTarget; // For two-stage targeting

    // Card sorting options
    private string _selectedSortOption = "Element";
    private readonly List<string> _sortOptions = new() { "Element", "Type" };
    private readonly List<CardViewModel> _allAvailableCards = new();

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
        SortCardsCommand = new RelayCommand(SortCards);

        var allCards = CardFactory.CreateStarterDeck();
        var cardViewModels = allCards.Select(c => new CardViewModel(c)).ToList();
        _allAvailableCards.AddRange(cardViewModels);
        AvailableCards = new ObservableCollection<CardViewModel>(cardViewModels);
        SortCards(); // Apply initial sort

        // Add quest table cards to MenuCards (for galleries/menus) but NOT AvailableCards (deck builder)
        var questTableCardsList = CardFactory.GetQuestTableCards();
        _questTableCards.AddRange(questTableCardsList);
        var questTableCardVMs = questTableCardsList
            .Select(c => new CardViewModel(c))
            .ToList();
        
        MenuCards = new ObservableCollection<CardViewModel>(
            cardViewModels.Select(c => c.Clone()).Concat(questTableCardVMs));
        
        // Load pre-made combination cards into lookup
        // Cards with names starting with "combine_" will be used for combinations
        var customCards = CardFactory.GetCustomCombinationCards();
        _combinationLookup.LoadCombinationCards(customCards);
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

    /// <summary>
    /// Quest/Search token count - when 3 tokens accumulated, draw a random quest table card
    /// </summary>
    public int QuestTokens
    {
        get => _questTokens;
        set
        {
            var oldTokens = _questTokens;
            SetProperty(ref _questTokens, value);
            
            // Check if 3 tokens reached - draw random quest table card
            if (oldTokens < 3 && value >= 3)
            {
                DrawQuestTableCard();
                _questTokens = 0; // Reset after drawing
                OnPropertyChanged(nameof(QuestTokens));
            }
        }
    }

    /// <summary>
    /// Currently active ability awaiting target selection (e.g., after clicking a card to use its ability)
    /// </summary>
    public bool IsSelectingTarget => _activeAbility != null;

    /// <summary>
    /// Start targeting for an ability - player clicked a card's ability button
    /// </summary>
    public void StartAbilityTargeting(CardAbility ability, CardViewModel sourceCard)
    {
        // For Budding, cost is the spell cost (stored separately)
        int cost = ability.ManaCost > 0 ? ability.ManaCost : sourceCard.ManaCost;
        
        if (PlayerMana < cost)
        {
            AddBattleLog($"Not enough mana for {ability.Name}!", BattleLogEntryType.Info);
            return;
        }

        _activeAbility = ability;
        _abilitySourceCard = sourceCard;
        PlayerMana -= cost;
        
        // Handle two-stage targeting (Budding)
        if (ability.EffectType == EffectType.Duplicate && ability.Name == "Budding")
        {
            _buddingFirstTarget = null;
            _waitingForSecondTarget = false;
            AddBattleLog("Select PLAYER owned creature to duplicate...", BattleLogEntryType.Info);
            OnPropertyChanged(nameof(IsSelectingTarget));
        }
        // Determine targeting mode based on ability
        else if (ability.RequiresTarget)
        {
            AddBattleLog($"Select target for {ability.Name}...", BattleLogEntryType.Info);
            OnPropertyChanged(nameof(IsSelectingTarget));
        }
        else
        {
            // Execute immediately for non-targeted abilities (like Search)
            ExecuteAbility(ability, sourceCard, null);
            _activeAbility = null;
            _abilitySourceCard = null;
            OnPropertyChanged(nameof(IsSelectingTarget));
        }
    }

    /// <summary>
    /// Execute the active ability on a target
    /// </summary>
    public void ExecuteAbilityOnTarget(CardViewModel target)
    {
        if (_activeAbility == null || _abilitySourceCard == null) return;

        // Handle two-stage targeting (Budding)
        if (_activeAbility.EffectType == EffectType.Duplicate && _activeAbility.Name == "Budding")
        {
            if (!_waitingForSecondTarget)
            {
                // First target selected (player creature)
                _buddingFirstTarget = target;
                _waitingForSecondTarget = true;
                AddBattleLog("Now, select OPPONENT owned creature to duplicate...", BattleLogEntryType.Info);
                return; // Wait for second target
            }
            else
            {
                // Second target selected - execute with both
                ExecuteBudding(_buddingFirstTarget!, target);
                _activeAbility = null;
                _abilitySourceCard = null;
                _buddingFirstTarget = null;
                _waitingForSecondTarget = false;
                OnPropertyChanged(nameof(IsSelectingTarget));
                return;
            }
        }

        ExecuteAbility(_activeAbility, _abilitySourceCard, target);
        
        _activeAbility = null;
        _abilitySourceCard = null;
        OnPropertyChanged(nameof(IsSelectingTarget));
    }

    /// <summary>
    /// Execute Budding ability (duplicate two creatures)
    /// </summary>
    private void ExecuteBudding(CardViewModel playerCreature, CardViewModel opponentCreature)
    {
        // Find empty slots and duplicate both creatures
        int emptyPlayerSlot = FindEmptyPlayerSlot();
        int emptyOppSlot = FindEmptyOpponentSlot();
        
        if (emptyPlayerSlot >= 0)
        {
            var clone = playerCreature.Clone();
            // Add to player's field (simplified - would need actual game state update)
            AddBattleLog($"Duplicated {playerCreature.Name} to player slot!", BattleLogEntryType.PlayerAction);
        }
        
        if (emptyOppSlot >= 0)
        {
            var clone = opponentCreature.Clone();
            // Add to opponent's field (simplified)
            AddBattleLog($"Duplicated {opponentCreature.Name} to opponent slot!", BattleLogEntryType.OpponentAction);
        }
    }

    private int FindEmptyPlayerSlot()
    {
        for (int i = 6; i < 12; i++)
        {
            if (FieldSlots[i] == null) return i;
        }
        return -1;
    }

    private int FindEmptyOpponentSlot()
    {
        for (int i = 0; i < 6; i++)
        {
            if (FieldSlots[i] == null) return i;
        }
        return -1;
    }

    /// <summary>
    /// Execute an ability
    /// </summary>
    private void ExecuteAbility(CardAbility ability, CardViewModel source, CardViewModel? target)
    {
        switch (ability.EffectType)
        {
            case EffectType.Search:
                // Add quest token
                QuestTokens += ability.EffectValue;
                AddBattleLog($"{source.Name} searches and gains {ability.EffectValue} quest token(s)!", BattleLogEntryType.Info);
                break;

            case EffectType.Damage:
            case EffectType.Debuff:
                if (target != null)
                {
                    ApplyDamageToTarget(ability, source, target);
                }
                break;

            case EffectType.Buff:
            case EffectType.BuffPower:
            case EffectType.BuffHealth:
                if (target != null)
                {
                    ApplyBuffToTarget(ability, source, target);
                }
                break;

            case EffectType.DebuffPower:
            case EffectType.DebuffHealth:
                if (target != null)
                {
                    ApplyDebuffToTarget(ability, source, target);
                }
                break;

            case EffectType.DamageToAll:
            case EffectType.DamageToAllCreatures:
            case EffectType.DamageToAllEnemyCreatures:
                ApplyDamageToAll(ability, source);
                break;

            case EffectType.ManaRegen:
                PlayerMaxMana += ability.EffectValue;
                PlayerMana = Math.Min(PlayerMana + ability.EffectValue, PlayerMaxMana);
                AddBattleLog($"{source.Name} grants +{ability.EffectValue} max mana!", BattleLogEntryType.Info);
                break;

            case EffectType.DisableAttack:
                if (target != null)
                {
                    ApplyDisableAttack(ability, source, target);
                }
                break;

            case EffectType.Infect:
                // Infection tokens - track separately for now, just add status effect
                if (target != null)
                {
                    target.AddStatusEffect("Infected");
                    AddBattleLog($"{source.Name} adds {ability.EffectValue} infection counter(s) to {target.Name}!", BattleLogEntryType.Debuff);
                }
                break;

            default:
                AddBattleLog($"{ability.Name} effect not implemented yet!", BattleLogEntryType.Info);
                break;
        }
    }

    /// <summary>
    /// Apply disable attack effect (Astral Eviction)
    /// </summary>
    private void ApplyDisableAttack(CardAbility ability, CardViewModel source, CardViewModel target)
    {
        target.AddStatusEffect("CannotAttack");
        AddBattleLog($"{source.Name} disables {target.Name}'s attacks!", BattleLogEntryType.Debuff);
        
        // If it's temporary, schedule removal
        if (ability.IsTemporary && ability.Duration > 0)
        {
            // Would need turn-based tracking to remove after duration
            AddBattleLog($"(Effect lasts {ability.Duration} turns)", BattleLogEntryType.Info);
        }
    }

    /// <summary>
    /// Apply damage to all (handles Whiteout with element check)
    /// </summary>
    private void ApplyDamageToAll(CardAbility ability, CardViewModel source)
    {
        int damage = ability.EffectValue;
        
        // Handle Whiteout - check if it's this ability
        if (ability.Name == "Whiteout")
        {
            // Add turn-based effect tracking
            AddBattleLog($"Whiteout begins! {damage} damage to all creatures for {ability.Duration} turns.", BattleLogEntryType.Info);
            
            // Apply first tick immediately
            ApplyWhiteoutTick(source, damage);
            return;
        }
        
        if (ability.EffectType == EffectType.DamageToAllEnemyCreatures)
        {
            // Damage all opponent creatures
            for (int i = 0; i < 6; i++)
            {
                if (OpponentCreatureSlots[i] != null)
                {
                    OpponentCreatureSlots[i].ApplyDamage(damage);
                }
            }
            AddBattleLog($"{source.Name} deals {damage} damage to all enemy creatures!", BattleLogEntryType.Damage);
        }
    }

    /// <summary>
    /// Apply Whiteout damage tick with element check
    /// </summary>
    private void ApplyWhiteoutTick(CardViewModel source, int baseDamage)
    {
        int damage = baseDamage;
        
        // Check each creature's element - reduce damage for RAD, TOX, THE
        for (int i = 0; i < 6; i++)
        {
            var oppCreature = OpponentCreatureSlots[i];
            if (oppCreature != null)
            {
                var element = oppCreature.Element;
                // Damage halved (rounded down) for RAD, TOX, THE - minimum 1
                if (element == ElementType.Radioactivity || 
                    element == ElementType.Toxin || 
                    element == ElementType.Thermodynamics)
                {
                    damage = Math.Max(1, baseDamage / 2);
                }
                else
                {
                    damage = baseDamage;
                }
                
                oppCreature.ApplyDamage(damage);
            }
            
            var playerCreature = PlayerCreatureSlotsObs[i];
            if (playerCreature != null)
            {
                var element = playerCreature.Element;
                if (element == ElementType.Radioactivity || 
                    element == ElementType.Toxin || 
                    element == ElementType.Thermodynamics)
                {
                    damage = Math.Max(1, baseDamage / 2);
                }
                else
                {
                    damage = baseDamage;
                }
                
                playerCreature.ApplyDamage(damage);
            }
        }
        
        AddBattleLog($"Whiteout deals {damage} damage (reduced for RAD/TOX/THE)!", BattleLogEntryType.Damage);
    }

    private void ApplyDamageToTarget(CardAbility ability, CardViewModel source, CardViewModel target)
    {
        int damage = ability.EffectValue;
        
        // Find target in field slots and apply damage
        bool foundTarget = false;
        for (int i = 0; i < FieldSlots.Length; i++)
        {
            if (FieldSlots[i]?.Id == target.Id)
            {
                target.ApplyDamage(damage);
                foundTarget = true;
                AddBattleLog($"{source.Name} deals {damage} damage to {target.Name}!", BattleLogEntryType.Damage);
                break;
            }
        }

        // Also check opponent creatures
        if (!foundTarget)
        {
            // For now, apply to opponent directly or their creatures
            OpponentHealth -= damage;
            AddBattleLog($"{source.Name} deals {damage} damage to opponent!", BattleLogEntryType.Damage);
        }
    }

    private void ApplyBuffToTarget(CardAbility ability, CardViewModel source, CardViewModel target)
    {
        int value = ability.EffectValue;
        
        // Find target in field slots and apply buff
        for (int i = 0; i < FieldSlots.Length; i++)
        {
            if (FieldSlots[i]?.Id == target.Id)
            {
                if (ability.EffectType == EffectType.Buff || ability.EffectType == EffectType.BuffPower)
                {
                    target.AddStatusEffect("Buffed");
                    target.HealDamage(-value); // Negative damage = buff
                }
                if (ability.EffectType == EffectType.Buff || ability.EffectType == EffectType.BuffHealth)
                {
                    target.HealDamage(-value);
                }
                AddBattleLog($"{source.Name} buffs {target.Name} by +{value}!", BattleLogEntryType.PlayerAction);
                break;
            }
        }
    }

    private void ApplyDebuffToTarget(CardAbility ability, CardViewModel source, CardViewModel target)
    {
        int value = ability.EffectValue;
        
        // Find target in field slots and apply debuff
        for (int i = 0; i < FieldSlots.Length; i++)
        {
            if (FieldSlots[i]?.Id == target.Id)
            {
                if (ability.EffectType == EffectType.Debuff || ability.EffectType == EffectType.DebuffPower)
                {
                    target.AddStatusEffect("Weakened");
                }
                AddBattleLog($"{source.Name} debuffs {target.Name} by -{value}!", BattleLogEntryType.OpponentAction);
                break;
            }
        }
    }

    /// <summary>
    /// Cancel ability targeting
    /// </summary>
    public void CancelAbilityTargeting()
    {
        if (_activeAbility != null)
        {
            // Refund mana
            PlayerMana += _activeAbility.ManaCost;
            _activeAbility = null;
            _abilitySourceCard = null;
            OnPropertyChanged(nameof(IsSelectingTarget));
            AddBattleLog("Ability cancelled.", BattleLogEntryType.Info);
        }
    }

    /// <summary>
    /// Draw a random card from the quest table when 3 tokens accumulated
    /// </summary>
    private void DrawQuestTableCard()
    {
        if (_questTableCards.Count == 0) return;
        
        // Use weighted random based on card rarity percentages
        // C: percentages: 26%, 25%, 20%, 17%, 10%, 2% = 100%
        var roll = _random.Next(100);
        Card selectedCard;
        
        if (roll < 26) selectedCard = _questTableCards[0]; // Dwarf Star Spawn (0-25)
        else if (roll < 51) selectedCard = _questTableCards[1]; // Malformed Summon (26-50)
        else if (roll < 71) selectedCard = _questTableCards[2]; // Eater of Hope (51-70)
        else if (roll < 88) selectedCard = _questTableCards[3]; // Greater Star Spawn (71-87)
        else if (roll < 98) selectedCard = _questTableCards[4]; // Herald of Jaa'aird'thuun (88-97)
        else selectedCard = _questTableCards[5]; // Jaa'aird'thuun (98-99)
        
        var questCardVm = new CardViewModel(selectedCard.Clone());
        PlayerHand.Add(questCardVm);
        
        AddBattleLog($"Quest complete! Summoned: {questCardVm.Name}", BattleLogEntryType.Spell);
    }

    /// <summary>
    /// Add a quest token - called when Search ability is activated
    /// </summary>
    public void AddQuestToken()
    {
        QuestTokens++;
        AddBattleLog($"Quest token added! ({QuestTokens}/3)", BattleLogEntryType.Info);
    }

    private readonly Random _random = new();

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
    
    // Battle log for game events
    public ObservableCollection<BattleLogEntry> BattleLog { get; } = new();
    
    /// <summary>
    /// Represents a single entry in the battle log
    /// </summary>
    public class BattleLogEntry
    {
        public int Turn { get; set; }
        public string Message { get; set; } = "";
        public BattleLogEntryType Type { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string FormattedEntry => $"[Turn {Turn}] {Message}";
    }
    
    public enum BattleLogEntryType
    {
        Info,
        PlayerAction,
        OpponentAction,
        Damage,
        CreatureDeath,
        Spell,
        Combat,
        Debuff
    }
    
    private void AddBattleLog(string message, BattleLogEntryType type = BattleLogEntryType.Info)
    {
        var entry = new BattleLogEntry
        {
            Turn = TurnCount,
            Message = message,
            Type = type,
            Timestamp = DateTime.Now
        };
        BattleLog.Add(entry);
        LogToFile($"[BattleLog] Turn {TurnCount}: {message}");
    }
    
    /// <summary>
    /// Clear the battle log at the start of a new game
    /// </summary>
    public void ClearBattleLog()
    {
        BattleLog.Clear();
        AddBattleLog("Game started!", BattleLogEntryType.Info);
    }
    
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
    public ICommand SortCardsCommand { get; private set; }
    
    public string SelectedSortOption
    {
        get => _selectedSortOption;
        set
        {
            if (SetProperty(ref _selectedSortOption, value))
            {
                SortCards();
            }
        }
    }
    
    public List<string> SortOptions => _sortOptions;
    
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
            
            // Clear artifact slots
            for (int i = 0; i < _opponentArtifactSlots.Length; i++)
            {
                _opponentArtifactSlots[i] = null;
            }
            for (int i = 0; i < _playerArtifactSlots.Length; i++)
            {
                _playerArtifactSlots[i] = null;
            }
            OpponentArtifactSlotsObs.Clear();
            PlayerArtifactSlotsObs.Clear();
            
            // Populate artifact slots with null placeholders (3 slots each)
            for (int i = 0; i < 3; i++)
            {
                OpponentArtifactSlotsObs.Add(null);
                PlayerArtifactSlotsObs.Add(null);
            }
            
            // Clear equipment slots
            OpponentWeapon = null;
            OpponentArmor = null;
            OpponentEventSlot = null;
            PlayerWeapon = null;
            PlayerArmor = null;
            PlayerEventSlot = null;
            
            // Notify UI of artifact slot changes
            OnPropertyChanged(nameof(OpponentArtifactSlots));
            OnPropertyChanged(nameof(PlayerArtifactSlots));
            OnPropertyChanged(nameof(OpponentArtifactSlotsObs));
            OnPropertyChanged(nameof(PlayerArtifactSlotsObs));
            OnPropertyChanged(nameof(OpponentWeapon));
            OnPropertyChanged(nameof(OpponentArmor));
            OnPropertyChanged(nameof(OpponentEventSlot));
            OnPropertyChanged(nameof(PlayerWeapon));
            OnPropertyChanged(nameof(PlayerArmor));
            OnPropertyChanged(nameof(PlayerEventSlot));
            
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
            
            // Clear and initialize battle log
            ClearBattleLog();

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

    /// <summary>
    /// Sorts the available cards based on the selected sort option
    /// </summary>
    private void SortCards()
    {
        var sorted = _selectedSortOption switch
        {
            "Element" => _allAvailableCards.OrderBy(c => c.Element.ToString()).ThenBy(c => c.Name).ToList(),
            "Type" => _allAvailableCards.OrderBy(c => c.Type.ToString()).ThenBy(c => c.Element.ToString()).ThenBy(c => c.Name).ToList(),
            _ => _allAvailableCards.ToList()
        };
        
        AvailableCards.Clear();
        foreach (var card in sorted)
        {
            AvailableCards.Add(card);
        }
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
                            
                            // Add to battle log
                            int displaySlot = decision.SlotIndex.Value + 1;
                            AddBattleLog($"Opponent summons {card.Name} to slot {displaySlot}", BattleLogEntryType.OpponentAction);
                            
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
                    if (decision.CardId != null)
                    {
                        var card = OpponentDeck.Hand.FirstOrDefault(c => c.Id == decision.CardId);
                        if (card != null && card.ManaCost <= opponentMana)
                        {
                            // Check if this is an Event card that should go to the event slot
                            if (card.Type == CardType.Event)
                            {
                                var cardVm = new CardViewModel(card);
                                OpponentDeck.PlayCard(card.Id);
                                opponentMana -= cardVm.ManaCost;
                                OpponentEventSlot = cardVm;
                                LogToFile($"[ExecuteOpponentTurn] Plays event: {cardVm.Name}");
                                StatusMessage = $"Opponent plays {cardVm.Name}!";
                                AddBattleLog($"Opponent plays {cardVm.Name}", BattleLogEntryType.Spell);
                                OnPropertyChanged(nameof(OpponentEventSlot));
                            }
                            else
                            {
                                // Regular spell/enchantment - apply effects
                                OpponentDeck.PlayCard(card.Id);
                                opponentMana -= card.ManaCost;
                                LogToFile($"[ExecuteOpponentTurn] Playing {card.Name} (type={card.Type})");
                                
                                // Update status message to show opponent action
                                string cardTypeStr = card.Type.ToString();
                                StatusMessage = $"Opponent plays {card.Name}!";
                                
                                // Add to battle log
                                AddBattleLog($"Opponent plays {card.Name} ({cardTypeStr})", BattleLogEntryType.Spell);
                                
                                ApplyOpponentCardEffects(card);
                            }
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

                case OpponentAI.AIDecisionType.PlayWeapon:
                    if (decision.CardId != null)
                    {
                        var card = OpponentDeck.Hand.FirstOrDefault(c => c.Id == decision.CardId);
                        if (card != null && card.ManaCost <= opponentMana)
                        {
                            var cardVm = new CardViewModel(card);
                            OpponentDeck.PlayCard(cardVm.Id);
                            opponentMana -= cardVm.ManaCost;
                            OpponentWeapon = cardVm;
                            LogToFile($"[ExecuteOpponentTurn] Equipped weapon: {cardVm.Name}");

                            StatusMessage = $"Opponent equips {cardVm.Name}!";
                            AddBattleLog($"Opponent equips {cardVm.Name}", BattleLogEntryType.OpponentAction);
                            OnPropertyChanged(nameof(OpponentWeapon));
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

                case OpponentAI.AIDecisionType.PlayArtifact:
                    if (decision.CardId != null)
                    {
                        var card = OpponentDeck.Hand.FirstOrDefault(c => c.Id == decision.CardId);
                        if (card != null && card.ManaCost <= opponentMana)
                        {
                            var cardVm = new CardViewModel(card);
                            OpponentDeck.PlayCard(cardVm.Id);
                            opponentMana -= cardVm.ManaCost;
                            
                            // Handle based on card type
                            if (cardVm.Type == CardType.Weapon)
                            {
                                OpponentWeapon = cardVm;
                                LogToFile($"[ExecuteOpponentTurn] Equipped weapon: {cardVm.Name}");
                                StatusMessage = $"Opponent equips {cardVm.Name}!";
                                AddBattleLog($"Opponent equips {cardVm.Name}", BattleLogEntryType.OpponentAction);
                                OnPropertyChanged(nameof(OpponentWeapon));
                            }
                            else if (cardVm.Type == CardType.Armor)
                            {
                                OpponentArmor = cardVm;
                                LogToFile($"[ExecuteOpponentTurn] Equipped armor: {cardVm.Name}");
                                StatusMessage = $"Opponent equips {cardVm.Name}!";
                                AddBattleLog($"Opponent equips {cardVm.Name}", BattleLogEntryType.OpponentAction);
                                OnPropertyChanged(nameof(OpponentArmor));
                            }
                            else if (cardVm.Type == CardType.Event)
                            {
                                OpponentEventSlot = cardVm;
                                LogToFile($"[ExecuteOpponentTurn] Plays event: {cardVm.Name}");
                                StatusMessage = $"Opponent plays {cardVm.Name}!";
                                AddBattleLog($"Opponent plays {cardVm.Name}", BattleLogEntryType.Spell);
                                OnPropertyChanged(nameof(OpponentEventSlot));
                            }
                            else if (cardVm.Type == CardType.Artifact)
                            {
                                // Place artifact in first available artifact slot
                                for (int i = 0; i < _opponentArtifactSlots.Length; i++)
                                {
                                    if (_opponentArtifactSlots[i] == null)
                                    {
                                        _opponentArtifactSlots[i] = cardVm;
                                        LogToFile($"[ExecuteOpponentTurn] Plays artifact: {cardVm.Name} to slot {i}");
                                        StatusMessage = $"Opponent plays {cardVm.Name}!";
                                        AddBattleLog($"Opponent plays {cardVm.Name}", BattleLogEntryType.Spell);
                                        break;
                                    }
                                }
                                // Update observable collection
                                OpponentArtifactSlotsObs.Clear();
                                for (int i = 0; i < _opponentArtifactSlots.Length; i++)
                                {
                                    OpponentArtifactSlotsObs.Add(_opponentArtifactSlots[i]);
                                }
                                OnPropertyChanged(nameof(OpponentArtifactSlots));
                                OnPropertyChanged(nameof(OpponentArtifactSlotsObs));
                            }
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
                        AddBattleLog($"Your {playerCreature.Name} was destroyed by {opponentCreature.Name}!", BattleLogEntryType.CreatureDeath);
                    }
                    else
                    {
                        StatusMessage = $"{opponentCreature.Name} attacks your {playerCreature.Name} for {opponentCreature.Power} damage!";
                        AddBattleLog($"{opponentCreature.Name} attacks your {playerCreature.Name} for {opponentCreature.Power} damage", BattleLogEntryType.Damage);
                    }
                }
                else
                {
                    PlayerHealth -= opponentCreature.Power;
                    StatusMessage = $"{opponentCreature.Name} deals {opponentCreature.Power} damage to you!";
                    AddBattleLog($"{opponentCreature.Name} deals {opponentCreature.Power} damage to you", BattleLogEntryType.Damage);
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
            AddBattleLog($"Opponent's {weapon.Name} deals {damage} damage to you", BattleLogEntryType.Damage);
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
                    AddBattleLog($"Your {target.Name} was destroyed by {weapon.Name}!", BattleLogEntryType.CreatureDeath);
                }
                else
                {
                    StatusMessage = $"Opponent's {weapon.Name} attacks your {target.Name} for {damage} damage!";
                    AddBattleLog($"Opponent's {weapon.Name} attacks your {target.Name} for {damage} damage", BattleLogEntryType.Damage);
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

        // Look up pre-made combination card
        var resultCard = _combinationLookup.FindCombination(ComboCard1.Card, ComboCard2.Card);
        
        if (resultCard == null)
        {
            StatusMessage = "No pre-made combination available for these cards.";
            return;
        }

        // Create a new instance to avoid reference issues
        var newCard = CreateCardCopy(resultCard);
        
        PlayerDeck.DiscardFromHand(ComboCard1.Id);
        PlayerDeck.DiscardFromHand(ComboCard2.Id);

        var card1ToRemove = PlayerHand.FirstOrDefault(c => c.Id == ComboCard1.Id);
        var card2ToRemove = PlayerHand.FirstOrDefault(c => c.Id == ComboCard2.Id);
        if (card1ToRemove != null) PlayerHand.Remove(card1ToRemove);
        if (card2ToRemove != null) PlayerHand.Remove(card2ToRemove);

        ComboResult = new CardViewModel(newCard);
        CustomCards.Add(ComboResult);
        IsComboLocked = true;
        CanCombine = false;

        StatusMessage = $"Created {newCard.Name}!";
        OnPropertyChanged(nameof(PlayerHand));
    }

    /// <summary>
    /// Create a deep copy of a card with a new ID
    /// </summary>
    private Card CreateCardCopy(Card source)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        var copy = System.Text.Json.JsonSerializer.Deserialize<Card>(json);
        if (copy != null)
        {
            copy.Id = Guid.NewGuid().ToString();
        }
        return copy ?? source;
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

        // Look up pre-made combination card
        var resultCard = _combinationLookup.FindCombination(ComboCard1.Card, ComboCard2.Card);
        
        if (resultCard != null)
        {
            ComboResult = new CardViewModel(resultCard);
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
        
        // Add to battle log
        int displaySlot = slotIndex - 5; // Convert 6-11 to 1-6
        AddBattleLog($"You summon {card.Name} to slot {displaySlot}", BattleLogEntryType.PlayerAction);
        
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
