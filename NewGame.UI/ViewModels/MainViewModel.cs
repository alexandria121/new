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

/// <summary>
/// Tracks a temporary effect that expires after a certain number of turns
/// </summary>
public class TemporaryEffect
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EffectName { get; set; } = "";
    public string TargetCardId { get; set; } = ""; // Card being affected
    public string TargetCardName { get; set; } = "";
    public string StatusEffect { get; set; } = ""; // e.g., "CannotAttack", "Weakened", "Buffed"
    public int RemainingTurns { get; set; } // How many turns remain (0 = expires)
    public bool IsFromOpponent { get; set; } // true if effect came from opponent
    public CardAbility? SourceAbility { get; set; } // The ability that created this effect
}

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
    
    // Temporary effect tracking
    private readonly List<TemporaryEffect> _temporaryEffects = new();

    // Jar of Eyes effect - tracks if opponent's hand is visible
    private bool _opponentHandVisible = false;
    
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
        SaveDeckCommand = new RelayCommand(SaveDeck, () => PlayerDeckCards.Count > 0);
        SortCardsCommand = new RelayCommand(SortCards);

        var allCards = CardFactory.CreateStarterDeck();
        var cardViewModels = allCards.Select(c => new CardViewModel(c)).ToList();
        
        // Filter out combo-only cards from deck builder (they can only be obtained via combining)
        var deckBuilderCards = cardViewModels.Where(c => !c.IsComboOnly).ToList();
        
        _allAvailableCards.AddRange(deckBuilderCards);
        AvailableCards = new ObservableCollection<CardViewModel>(deckBuilderCards);
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
    public ObservableCollection<CardViewModel> OpponentHand { get; } = new();
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
        // Check if ability was already used this turn
        if (sourceCard.AbilityUsedThisTurn)
        {
            AddBattleLog($"{ability.Name} was already used this turn!", BattleLogEntryType.Info);
            return;
        }
        
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
            // Custom targeting message based on effect type
            string targetMessage = ability.EffectType switch
            {
                EffectType.Infect => $"Click on OPPONENT creature to infect with {ability.Name}!",
                EffectType.Damage => $"Click on target creature for {ability.Name}...",
                EffectType.Debuff => $"Click on target creature for {ability.Name}...",
                EffectType.Heal => $"Click on target creature for {ability.Name}...",
                EffectType.DisableAttack => $"Click on target creature for {ability.Name}...",
                _ => $"Select target for {ability.Name}..."
            };
            AddBattleLog(targetMessage, BattleLogEntryType.Info);
            OnPropertyChanged(nameof(IsSelectingTarget));
        }
        else
        {
            // Execute immediately for non-targeted abilities (like Search)
            ExecuteAbility(ability, sourceCard, null);
            sourceCard.AbilityUsedThisTurn = true;
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

        // For Infect ability, require targeting an opponent creature
        if (_activeAbility.EffectType == EffectType.Infect)
        {
            // Find target's slot index - FieldSlots[0-5] are opponent, [6-11] are player
            int targetSlotIndex = -1;
            for (int i = 0; i < FieldSlots.Length; i++)
            {
                if (FieldSlots[i]?.Id == target.Id)
                {
                    targetSlotIndex = i;
                    break;
                }
            }
            
            // Must target opponent creature (slots 0-5)
            if (targetSlotIndex < 0 || targetSlotIndex > 5)
            {
                AddBattleLog("Select an OPPONENT creature to infect!", BattleLogEntryType.Info);
                return; // Don't execute, keep targeting mode
            }
        }

        ExecuteAbility(_activeAbility, _abilitySourceCard, target);
        _abilitySourceCard.AbilityUsedThisTurn = true;
        
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
            
            case EffectType.EldritchSummon:
                // Call of the Deep - track duration, summon on last turn
                _temporaryEffects.Add(new TemporaryEffect
                {
                    EffectName = ability.Name,
                    TargetCardId = source.Id,
                    TargetCardName = source.Name,
                    StatusEffect = "EldritchSummon",
                    RemainingTurns = ability.Duration,
                    IsFromOpponent = false,
                    SourceAbility = ability
                });
                AddBattleLog($"{source.Name} begins the ritual! Eldritch creature will be summoned in {ability.Duration} turn(s).", BattleLogEntryType.Info);
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
            
            case EffectType.RevealHand:
                // Jar of Eyes - makes opponent's hand visible for one turn
                OpponentHandVisible = true;
                _temporaryEffects.Add(new TemporaryEffect
                {
                    EffectName = ability.Name,
                    TargetCardId = "PLAYER",
                    TargetCardName = "Opponent Hand",
                    StatusEffect = "RevealHand",
                    RemainingTurns = 1,
                    IsFromOpponent = false,
                    SourceAbility = ability
                });
                AddBattleLog($"{source.Name} reveals the opponent's hand!", BattleLogEntryType.Info);
                break;
                
            case EffectType.Heal:
            case EffectType.HealSelf:
                // Heal target creature or self
                if (target != null)
                {
                    target.HealDamage(-ability.EffectValue); // Negative = heal
                    AddBattleLog($"{source.Name} heals {target.Name} for {ability.EffectValue} HP!", BattleLogEntryType.Info);
                }
                else if (ability.EffectType == EffectType.HealSelf)
                {
                    // Heal self (source creature)
                    source.HealDamage(-ability.EffectValue);
                    AddBattleLog($"{source.Name} heals itself for {ability.EffectValue} HP!", BattleLogEntryType.Info);
                }
                break;
                
            case EffectType.DrawCard:
                // Draw extra cards to hand
                PlayerDeck.DrawCards(ability.EffectValue);
                RefreshHand();
                AddBattleLog($"{source.Name} draws {ability.EffectValue} card(s)!", BattleLogEntryType.Info);
                break;
                
            case EffectType.ManaGain:
                // Gain mana immediately
                PlayerMana += ability.EffectValue;
                AddBattleLog($"{source.Name} gains {ability.EffectValue} mana!", BattleLogEntryType.Info);
                break;
                
            case EffectType.Shield:
            case EffectType.ShieldSelf:
                // Add shield status effect
                if (target != null)
                {
                    target.AddStatusEffect("Shielded");
                    AddBattleLog($"{source.Name} shields {target.Name}!", BattleLogEntryType.Info);
                }
                else if (ability.EffectType == EffectType.ShieldSelf)
                {
                    source.AddStatusEffect("Shielded");
                    AddBattleLog($"{source.Name} shields itself!", BattleLogEntryType.Info);
                }
                break;
                
            case EffectType.Destroy:
                // Destroy target (remove from field)
                if (target != null)
                {
                    AddBattleLog($"{source.Name} destroys {target.Name}!", BattleLogEntryType.Damage);
                    // Mark for removal - actual removal happens in game loop
                    target.ApplyDamage(999); // High damage to ensure death
                }
                break;
                
            case EffectType.Transform:
                // Transform target into something else (Dwarf Star Spawn -> Greater Star Spawn)
                // For Dwarf Star Spawn: creates a delayed transformation after 2 turns
                _temporaryEffects.Add(new TemporaryEffect
                {
                    EffectName = ability.Name,
                    TargetCardId = source.Id,
                    TargetCardName = source.Name,
                    StatusEffect = "Transforming",
                    RemainingTurns = 2, // Transformation happens after 2 turns
                    IsFromOpponent = false,
                    SourceAbility = ability
                });
                AddBattleLog($"{source.Name} begins to transform! Will become Greater Star Spawn in 2 turns.", BattleLogEntryType.Info);
                break;
                
            case EffectType.Biteback:
                // Counterattack when attacked - handled in combat resolution
                AddBattleLog($"{source.Name} has biteback ability!", BattleLogEntryType.Info);
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
        
        // If it's temporary, track for removal
        if (ability.IsTemporary && ability.Duration > 0)
        {
            _temporaryEffects.Add(new TemporaryEffect
            {
                EffectName = ability.Name,
                TargetCardId = target.Id,
                TargetCardName = target.Name,
                StatusEffect = "CannotAttack",
                RemainingTurns = ability.Duration,
                IsFromOpponent = false,
                SourceAbility = ability
            });
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
            
            // Track this as a temporary effect (applied each turn)
            if (ability.IsTemporary && ability.Duration > 0)
            {
                _temporaryEffects.Add(new TemporaryEffect
                {
                    EffectName = "Whiteout",
                    TargetCardId = "ALL_CREATURES", // Special - affects all
                    TargetCardName = "All Creatures",
                    StatusEffect = "WhiteoutDamage",
                    RemainingTurns = ability.Duration,
                    IsFromOpponent = true,
                    SourceAbility = ability
                });
            }
            
            // Apply first tick immediately
            ApplyWhiteoutTick(source, damage);
            return;
        }
        
        if (ability.EffectType == EffectType.DamageToAllEnemyCreatures || ability.EffectType == EffectType.DamageToAllCreatures)
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
            
            // Track as temporary effect if applicable
            if (ability.IsTemporary && ability.Duration > 0)
            {
                _temporaryEffects.Add(new TemporaryEffect
                {
                    EffectName = ability.Name,
                    TargetCardId = "OPPONENT_CREATURES",
                    TargetCardName = "Opponent Creatures",
                    StatusEffect = "DamageAura",
                    RemainingTurns = ability.Duration,
                    IsFromOpponent = false,
                    SourceAbility = ability
                });
                AddBattleLog($"(Effect lasts {ability.Duration} turns)", BattleLogEntryType.Info);
            }
        }
    }

    /// <summary>
    /// Apply Whiteout damage tick with element check
    /// </summary>
    private void ApplyWhiteoutTick(CardViewModel source, int baseDamage)
    {
        int totalDamageDealt = 0;
        int damageThisCreature;
        
        // Check each creature's element - reduce damage for RAD, TOX, THE
        for (int i = 0; i < 6; i++)
        {
            // Process opponent creature
            var oppCreature = OpponentCreatureSlots[i];
            if (oppCreature != null)
            {
                var element = oppCreature.Element;
                // Damage halved (rounded down) for RAD, TOX, THE - minimum 1
                if (element == ElementType.Radioactivity || 
                    element == ElementType.Toxin || 
                    element == ElementType.Thermodynamics)
                {
                    damageThisCreature = Math.Max(1, baseDamage / 2);
                }
                else
                {
                    damageThisCreature = baseDamage;
                }
                
                oppCreature.ApplyDamage(damageThisCreature);
                totalDamageDealt += damageThisCreature;
            }
            
            // Process player creature at same index
            var playerCreature = PlayerCreatureSlotsObs[i];
            if (playerCreature != null)
            {
                var element = playerCreature.Element;
                if (element == ElementType.Radioactivity || 
                    element == ElementType.Toxin || 
                    element == ElementType.Thermodynamics)
                {
                    damageThisCreature = Math.Max(1, baseDamage / 2);
                }
                else
                {
                    damageThisCreature = baseDamage;
                }
                
                playerCreature.ApplyDamage(damageThisCreature);
                totalDamageDealt += damageThisCreature;
            }
        }
        
        // Log the base damage and note about reduction
        AddBattleLog($"Whiteout deals {baseDamage} damage to all creatures (RAD/TOX/THE: half)!", BattleLogEntryType.Damage);
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
            ApplyDamageToOpponent(damage, source.Name);
        }
    }

    private void ApplyBuffToTarget(CardAbility ability, CardViewModel source, CardViewModel target)
    {
        int value = ability.EffectValue;
        int duration = ability.Duration;
        
        // Check for Temperature Gradient - doubles buff amount and duration
        // Check if any creature on the field has Temperature Gradient that matches the buff type
        value = ApplyTemperatureGradientBonus(value, ability, source, out int bonusDuration);
        if (bonusDuration > 0)
        {
            duration = bonusDuration;
            value *= 2; // Double the buff amount
        }
        
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
                
                // Track temporary buffs
                if (ability.IsTemporary && duration > 0)
                {
                    _temporaryEffects.Add(new TemporaryEffect
                    {
                        EffectName = ability.Name,
                        TargetCardId = target.Id,
                        TargetCardName = target.Name,
                        StatusEffect = "Buffed",
                        RemainingTurns = duration,
                        IsFromOpponent = false,
                        SourceAbility = ability
                    });
                    AddBattleLog($"(Temporary buff lasts {duration} turns)", BattleLogEntryType.Info);
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// Check if any creature has Temperature Gradient and apply bonus (doubling buff amount and duration)
    /// </summary>
    private int ApplyTemperatureGradientBonus(int baseValue, CardAbility ability, CardViewModel source, out int bonusDuration)
    {
        bonusDuration = ability.Duration;
        
        // Check all creatures on field for Temperature Gradient
        for (int i = 0; i < FieldSlots.Length; i++)
        {
            var creature = FieldSlots[i];
            if (creature?.Card == null) continue;
            
            string? gradient = creature.Card.TemperatureGradient;
            if (string.IsNullOrEmpty(gradient)) continue;
            
            // Heat Mote ("up") doubles Power buffs
            // Cold Mote ("down") doubles Health buffs
            // TempMote ("both") doubles both
            bool applyBonus = false;
            
            if (ability.EffectType == EffectType.BuffPower && (gradient == "up" || gradient == "both"))
            {
                applyBonus = true;
            }
            else if (ability.EffectType == EffectType.BuffHealth && (gradient == "down" || gradient == "both"))
            {
                applyBonus = true;
            }
            else if (ability.EffectType == EffectType.Buff && (gradient == "both" || gradient == "up" || gradient == "down"))
            {
                // Generic buff - apply if "both"
                applyBonus = (gradient == "both");
            }
            
            if (applyBonus)
            {
                LogToFile($"[ApplyTemperatureGradientBonus] {creature.Name} has {gradient}, doubling buff!");
                AddBattleLog($"{creature.Name}'s Temperature Gradient doubles the buff!", BattleLogEntryType.Info);
                return baseValue * 2;
            }
        }
        
        return baseValue;
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
                if (ability.EffectType == EffectType.Debuff || ability.EffectType == EffectType.DebuffHealth)
                {
                    target.AddStatusEffect("Weakened");
                }
                AddBattleLog($"{source.Name} debuffs {target.Name} by -{value}!", BattleLogEntryType.OpponentAction);
                
                // Track temporary debuffs
                if (ability.IsTemporary && ability.Duration > 0)
                {
                    _temporaryEffects.Add(new TemporaryEffect
                    {
                        EffectName = ability.Name,
                        TargetCardId = target.Id,
                        TargetCardName = target.Name,
                        StatusEffect = "Weakened",
                        RemainingTurns = ability.Duration,
                        IsFromOpponent = true,
                        SourceAbility = ability
                    });
                    AddBattleLog($"(Temporary debuff lasts {ability.Duration} turns)", BattleLogEntryType.Info);
                }
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
    /// Apply damage to player, accounting for armor damage reduction and special effects
    /// </summary>
    private void ApplyDamageToPlayer(int damage, string sourceName)
    {
        var armorCard = PlayerArmor?.Card as ArmorCard;
        
        // Check for NegateDamage (Suspicious Suit, Aegis of Jaa'aird'thuun)
        if (armorCard?.NegatesAllDamage == true)
        {
            AddBattleLog($"{sourceName}'s attack was negated by {PlayerArmor?.Name}!", BattleLogEntryType.Info);
            LogToFile($"[ApplyDamageToPlayer] Damage negated by {PlayerArmor?.Name}");
            return;
        }
        
        int actualDamage = Math.Max(0, damage - PlayerArmorBonus);
        PlayerHealth -= actualDamage;
        
        if (PlayerArmorBonus > 0)
        {
            AddBattleLog($"{sourceName} deals {damage} damage (reduced by {PlayerArmorBonus} to {actualDamage} by armor)!", BattleLogEntryType.Damage);
        }
        else
        {
            AddBattleLog($"{sourceName} deals {actualDamage} damage to you!", BattleLogEntryType.Damage);
        }
        
        // Handle Biteback (Graphite Cladding)
        if (armorCard?.HasBiteback == true && actualDamage > 0)
        {
            int bitebackDamage = armorCard.BitebackDamage;
            OpponentHealth -= bitebackDamage;
            AddBattleLog($"{PlayerArmor?.Name} deals {bitebackDamage} damage back to attacker!", BattleLogEntryType.Damage);
            LogToFile($"[ApplyDamageToPlayer] Biteback deals {bitebackDamage} damage");
        }
        
        // Handle OnHitDebuff (Virulent Vambrace)
        if (armorCard?.HasOnHitDebuff == true && actualDamage > 0)
        {
            // Would need to apply debuff to attacking creature - simplified for now
            AddBattleLog($"{PlayerArmor?.Name} inflicts a debuff on the attacker!", BattleLogEntryType.Info);
            LogToFile($"[ApplyDamageToPlayer] OnHitDebuff applied");
        }
        
        // Handle OnHitSpawn (Disintegrating Regalia - 50% chance)
        if (armorCard?.HasOnHitSpawn == true && actualDamage > 0)
        {
            var random = new Random();
            if (random.Next(100) < armorCard.SpawnChance)
            {
                // Spawn Mycelium Spore on player's field
                var sporeCard = CardFactory.GetCardByTemplateId("Mycelium Spore_Creature_Fungus");
                if (sporeCard != null)
                {
                    var sporeVm = new CardViewModel(sporeCard.Clone());
                    
                    // Find empty slot
                    for (int i = 6; i < 12; i++)
                    {
                        if (FieldSlots[i] == null)
                        {
                            FieldSlots[i] = sporeVm;
                            AddBattleLog($"{PlayerArmor?.Name} spawns a Mycelium Spore!", BattleLogEntryType.Info);
                            LogToFile($"[ApplyDamageToPlayer] Spawned Mycelium Spore at slot {i}");
                            break;
                        }
                    }
                }
            }
        }
        
        // Handle Intimidation (Sinew Shield)
        if (armorCard?.HasIntimidation == true && actualDamage > 0)
        {
            // Apply Intimidated status to all opponent creatures with power/health <= threshold
            int powerThreshold = armorCard.IntimidationPowerThreshold;
            int healthThreshold = armorCard.IntimidationHealthThreshold;
            
            for (int i = 0; i < 6; i++)
            {
                var oppCreature = FieldSlots[i];
                if (oppCreature != null && oppCreature.Power <= powerThreshold && oppCreature.Health <= healthThreshold)
                {
                    oppCreature.AddStatusEffect("Intimidated");
                    AddBattleLog($"{oppCreature.Name} is too intimidated to attack!", BattleLogEntryType.Info);
                    LogToFile($"[ApplyDamageToPlayer] Intimidated {oppCreature.Name}");
                }
            }
            
            AddBattleLog($"{PlayerArmor?.Name} radiates intimidation!", BattleLogEntryType.Info);
        }
    }
    
    /// <summary>
    /// Apply damage to opponent, accounting for armor damage reduction
    /// </summary>
    private void ApplyDamageToOpponent(int damage, string sourceName)
    {
        int actualDamage = Math.Max(0, damage - OpponentArmorBonus);
        OpponentHealth -= actualDamage;
        
        if (OpponentArmorBonus > 0)
        {
            AddBattleLog($"{sourceName} deals {damage} damage (reduced by {OpponentArmorBonus} to {actualDamage} by armor)!", BattleLogEntryType.Damage);
        }
        else
        {
            AddBattleLog($"{sourceName} deals {actualDamage} damage to opponent!", BattleLogEntryType.Damage);
        }
    }
    
    /// <summary>
    /// Update opponent armor bonus when they equip armor
    /// </summary>
    private void UpdateOpponentArmorBonus()
    {
        if (OpponentArmor?.Card is ArmorCard armorCard)
        {
            OpponentArmorBonus = armorCard.DamageReduction;
        }
        else
        {
            OpponentArmorBonus = 0;
        }
        OnPropertyChanged(nameof(OpponentArmorBonus));
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
    
    /// <summary>
    /// Whether opponent's hand is visible (from Jar of Eyes effect)
    /// </summary>
    public bool OpponentHandVisible
    {
        get => _opponentHandVisible;
        set => SetProperty(ref _opponentHandVisible, value);
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
                    // Must have a saved deck selected - show error
                    System.Windows.MessageBox.Show(
                        "Please create a deck and select it first!",
                        "No Deck Selected",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    ErrorLogger.Instance.Warning("MainViewModel", "[InitializeGame] Saved deck not found");
                    return;
                }
            }
            else
            {
                // Must have a deck selected - show error
                System.Windows.MessageBox.Show(
                    "Please create a deck and select it first!",
                    "No Deck Selected",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                ErrorLogger.Instance.Warning("MainViewModel", "[InitializeGame] No deck selected");
                return;
            }
            
            PlayerDeck.InitializeDeck(playerCards);
            PlayerDeck.DrawCards(PlayerDeck.StartingHandSize);

            // Draw cards for opponent
            OpponentDeck.DrawCards(4);
            RefreshOpponentHand();

            // Reset game state
            PlayerHealth = 30;
            PlayerMaxHealth = 30;
            PlayerMana = 2;
            PlayerMaxMana = 10;
            OpponentHealth = 30;
            TurnCount = 1;
            IsPlayerTurn = true;
            StatusMessage = "";
            _temporaryEffects.Clear();
            
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
    
    private void RefreshOpponentHand()
    {
        OpponentHand.Clear();
        foreach (var card in OpponentDeck.Hand)
        {
            OpponentHand.Add(new CardViewModel(card));
        }
        OnPropertyChanged(nameof(OpponentHand));
    }

    private void EndTurn()
    {
        if (!IsPlayerTurn) return;

        ResolveCombat();

        if (OpponentHealth <= 0 || PlayerHealth <= 0)
        {
            // Show game end window
            bool playerWon = OpponentHealth <= 0;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var endWindow = new Views.GameEndWindow(playerWon);
                endWindow.ShowDialog();
                // Return to menu after game ends
                CurrentView = "Menu";
            });
            return;
        }

        // Player weapon deals damage at end of turn
        ResolvePlayerWeaponDamage();

        if (OpponentHealth <= 0 || PlayerHealth <= 0)
        {
            // Show game end window
            bool playerWon = OpponentHealth <= 0;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var endWindow = new Views.GameEndWindow(playerWon);
                endWindow.ShowDialog();
                CurrentView = "Menu";
            });
            return;
        }

        IsPlayerTurn = false;
        ResetOpponentAbilityUsageFlags();
        ExecuteOpponentTurn();

        if (OpponentHealth <= 0 || PlayerHealth <= 0)
        {
            // Show game end window
            bool playerWon = OpponentHealth <= 0;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var endWindow = new Views.GameEndWindow(playerWon);
                endWindow.ShowDialog();
                CurrentView = "Menu";
            });
            return;
        }

        TurnCount++;
        PlayerMana = Math.Min(TurnCount, 10);
        IsPlayerTurn = true;
        PlayerDeck.DrawCards(1);
        RefreshHand();
        
        // Reset ability usage flags for all player cards in hand and field
        ResetAbilityUsageFlags();
        
        // Process temporary effects (decrement durations)
        ProcessTemporaryEffects();
        
        // Execute passive abilities at start of player's turn
        ExecutePassiveAbilities();
    }

    /// <summary>
    /// Reset ability usage flags at start of turn
    /// </summary>
    private void ResetAbilityUsageFlags()
    {
        // Reset for player hand
        foreach (var card in PlayerHand)
        {
            card.AbilityUsedThisTurn = false;
        }
        
        // Reset for player field creatures (slots 6-11)
        for (int i = 6; i < 12; i++)
        {
            if (FieldSlots[i] != null)
            {
                FieldSlots[i].AbilityUsedThisTurn = false;
            }
        }
        
        LogToFile("Abilities refreshed for new turn!");
    }

    /// <summary>
    /// Reset opponent ability usage flags at start of opponent's turn
    /// </summary>
    private void ResetOpponentAbilityUsageFlags()
    {
        // Reset for opponent field creatures (slots 0-5)
        for (int i = 0; i < 6; i++)
        {
            if (FieldSlots[i] != null)
            {
                FieldSlots[i].AbilityUsedThisTurn = false;
            }
        }
    }

    /// <summary>
    /// Process temporary effects - decrement durations and apply turn-based effects
    /// </summary>
    private void ProcessTemporaryEffects()
    {
        // Decrement all durations and apply effects
        for (int i = _temporaryEffects.Count - 1; i >= 0; i--)
        {
            var effect = _temporaryEffects[i];
            
            // Apply the effect for this turn if it's a damage aura
            if (effect.StatusEffect == "WhiteoutDamage" || effect.StatusEffect == "DamageAura")
            {
                ApplyTemporaryEffectDamage(effect);
            }
            
            effect.RemainingTurns--;
            
            if (effect.RemainingTurns <= 0)
            {
                // Handle EldritchSummon - when duration expires, summon from Eldritch table
                if (effect.SourceAbility?.EffectType == EffectType.EldritchSummon)
                {
                    SummonFromEldritchTable();
                }
                
                // Handle Transformation (Dwarf Star Spawn -> Greater Star Spawn)
                if (effect.StatusEffect == "Transforming")
                {
                    TransformDwarfStarSpawn(effect.TargetCardId, effect.TargetCardName);
                }
                
                // Handle RevealHand expiring (Jar of Eyes)
                if (effect.StatusEffect == "RevealHand")
                {
                    OpponentHandVisible = false;
                    AddBattleLog("The opponent's hand is no longer visible.", BattleLogEntryType.Info);
                }
                
                // Find the target card and remove the status effect
                if (effect.TargetCardId != "ALL_CREATURES" && effect.TargetCardId != "OPPONENT_CREATURES" && effect.TargetCardId != "PLAYER_CREATURES")
                {
                    var targetCard = FindCardById(effect.TargetCardId);
                    if (targetCard != null)
                    {
                        targetCard.RemoveStatusEffect(effect.StatusEffect);
                    }
                }
                
                AddBattleLog($"{effect.EffectName} on {effect.TargetCardName} expires!", BattleLogEntryType.Info);
                _temporaryEffects.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Summon a random creature from the Eldritch table to player's hand
    /// Similar to quest system but for Eldritch cards
    /// </summary>
    private void SummonFromEldritchTable()
    {
        var eldritchCards = CardFactory.GetQuestTableCards()
            .Where(c => c.Type == CardType.Creature && c.Element == ElementType.Eldritch)
            .ToList();
        
        if (eldritchCards.Count == 0)
        {
            AddBattleLog("Something stirs from the deep, but nothing appears...", BattleLogEntryType.Info);
            return;
        }
        
        // Pick random eldritch creature
        var random = new Random();
        var summonedCard = eldritchCards[random.Next(eldritchCards.Count)].Clone();
        var summonedVm = new CardViewModel(summonedCard);
        
        PlayerHand.Add(summonedVm);
        AddBattleLog($"Call of the Deep summons {summonedVm.Name} to your hand!", BattleLogEntryType.Spell);
        OnPropertyChanged(nameof(PlayerHand));
        LogToFile($"[SummonFromEldritchTable] Summoned {summonedVm.Name}");
    }
    
    /// <summary>
    /// Transform Dwarf Star Spawn into Greater Star Spawn
    /// </summary>
    private void TransformDwarfStarSpawn(string cardId, string cardName)
    {
        var oldCard = FindCardById(cardId);
        if (oldCard == null)
        {
            AddBattleLog($"{cardName} transforms, but the transformation fails!", BattleLogEntryType.Info);
            return;
        }
        
        // Find the slot
        int slotIndex = Array.FindIndex(FieldSlots, s => s?.Id == cardId);
        
        // Get the transformation target
        var newCard = CardFactory.GetCardByTemplateId("Greater Star Spawn_Creature_Eldritch");
        if (newCard == null)
        {
            AddBattleLog($"{cardName} transforms into Greater Star Spawn!", BattleLogEntryType.Info);
            return;
        }
        
        var newVm = new CardViewModel(newCard.Clone());
        
        // Replace in slot if found
        if (slotIndex >= 0)
        {
            FieldSlots[slotIndex] = newVm;
            RefreshCreatureSlotCaches();
        }
        
        AddBattleLog($"{cardName} transforms into Greater Star Spawn!", BattleLogEntryType.Info);
        LogToFile($"[TransformDwarfStarSpawn] Transformed {cardName} to Greater Star Spawn");
    }
    
    /// <summary>
    /// Apply damage from temporary effect
    /// </summary>
    private void ApplyTemporaryEffectDamage(TemporaryEffect effect)
    {
        if (effect.SourceAbility == null) return;
        
        int damage = effect.SourceAbility.EffectValue;
        
        if (effect.StatusEffect == "WhiteoutDamage")
        {
            // Whiteout has element-specific damage reduction
            ApplyWhiteoutTick(null!, damage);
        }
        else if (effect.StatusEffect == "DamageAura")
        {
            // Regular damage aura
            if (effect.TargetCardId == "OPPONENT_CREATURES")
            {
                for (int i = 0; i < 6; i++)
                {
                    if (OpponentCreatureSlots[i] != null)
                    {
                        OpponentCreatureSlots[i].ApplyDamage(damage);
                    }
                }
            }
            else if (effect.TargetCardId == "PLAYER_CREATURES")
            {
                for (int i = 6; i < 12; i++)
                {
                    if (FieldSlots[i] != null)
                    {
                        FieldSlots[i].ApplyDamage(damage);
                    }
                }
            }
            AddBattleLog($"{effect.EffectName} deals {damage} damage!", BattleLogEntryType.Damage);
        }
    }
    
    /// <summary>
    /// Execute passive abilities for all player creatures on the field
    /// </summary>
    private void ExecutePassiveAbilities()
    {
        // Check each player creature slot (indices 6-11)
        for (int i = 6; i < 12; i++)
        {
            var creature = FieldSlots[i];
            if (creature == null) continue;
            
            // Check each ability on the creature
            foreach (var ability in creature.Abilities)
            {
                if (ability.IsPassive)
                {
                    ExecutePassiveAbility(ability, creature);
                }
            }
        }
        
        // Also execute opponent passive abilities during their turn (called separately)
    }
    
    /// <summary>
    /// Execute a single passive ability
    /// </summary>
    private void ExecutePassiveAbility(CardAbility ability, CardViewModel source)
    {
        // Skip if already used this turn (we could track this)
        // For now, just execute passive abilities
        
        switch (ability.EffectType)
        {
            case EffectType.Buff:
            case EffectType.BuffPower:
            case EffectType.BuffHealth:
                // Passive buffs are permanent - apply once at start
                // Check if already has the buff status
                if (!source.HasStatusEffects || !source.StatusEffects.Contains("Buffed"))
                {
                    // Apply permanent buff
                    int value = ability.EffectValue;
                    if (value > 0)
                    {
                        source.HealDamage(-value); // Negative heals as buff
                        source.AddStatusEffect("Buffed");
                        AddBattleLog($"{source.Name}'s {ability.Name} grants +{value} permanent buff!", BattleLogEntryType.PlayerAction);
                    }
                }
                break;
                
            case EffectType.Debuff:
            case EffectType.DebuffPower:
            case EffectType.DebuffHealth:
                // Some passive debuffs affect all enemies - apply at start
                if (ability.Name == "Brain Fog")
                {
                    // Brain Fog: 25% chance to deal damage to owner, 75% to target
                    var random = new Random();
                    if (random.Next(4) == 0)
                    {
                        // 25% - damage to self
                        source.ApplyDamage(ability.EffectValue);
                        AddBattleLog($"{source.Name}'s Brain Fog backfires! Deals {ability.EffectValue} damage to owner!", BattleLogEntryType.Debuff);
                    }
                }
                break;
                
            case EffectType.DamageToAllCreatures:
            case EffectType.DamageToAllEnemyCreatures:
                // Some passives deal damage each turn
                ApplyPassiveDamageAura(ability, source);
                break;
                
            case EffectType.ManaRegen:
                // Passive mana regen
                PlayerMaxMana += ability.EffectValue;
                PlayerMana = Math.Min(PlayerMana + ability.EffectValue, PlayerMaxMana);
                AddBattleLog($"{source.Name}'s {ability.Name} grants +{ability.EffectValue} max mana!", BattleLogEntryType.Info);
                break;
                
            default:
                // Other passive abilities - log that they're active
                AddBattleLog($"{source.Name}'s {ability.Name} is active.", BattleLogEntryType.Info);
                break;
        }
    }
    
    /// <summary>
    /// Apply passive damage aura (for passives that damage each turn)
    /// </summary>
    private void ApplyPassiveDamageAura(CardAbility ability, CardViewModel source)
    {
        int damage = ability.EffectValue;
        bool isEnemyOnly = ability.EffectType == EffectType.DamageToAllEnemyCreatures;
        
        // Apply to opponent creatures
        for (int i = 0; i < 6; i++)
        {
            var target = FieldSlots[i];
            if (target != null)
            {
                target.ApplyDamage(damage);
            }
        }
        
        if (!isEnemyOnly)
        {
            // Also apply to player creatures if not enemy-only
            for (int i = 6; i < 12; i++)
            {
                var target = FieldSlots[i];
                if (target != null)
                {
                    target.ApplyDamage(damage);
                }
            }
        }
        
        AddBattleLog($"{source.Name}'s {ability.Name} deals {damage} damage!", BattleLogEntryType.Damage);
    }
    
    /// <summary>
    /// Execute passive abilities for all opponent creatures on the field
    /// </summary>
    private void ExecuteOpponentPassiveAbilities()
    {
        // Check each opponent creature slot (indices 0-5)
        for (int i = 0; i < 6; i++)
        {
            var creature = FieldSlots[i];
            if (creature == null) continue;
            
            // Check each ability on the creature
            foreach (var ability in creature.Abilities)
            {
                if (ability.IsPassive)
                {
                    ExecuteOpponentPassiveAbility(ability, creature);
                }
            }
        }
    }
    
    /// <summary>
    /// Execute a single opponent passive ability
    /// </summary>
    private void ExecuteOpponentPassiveAbility(CardAbility ability, CardViewModel source)
    {
        switch (ability.EffectType)
        {
            case EffectType.Buff:
            case EffectType.BuffPower:
            case EffectType.BuffHealth:
                // Passive buffs - apply if not already applied
                if (!source.HasStatusEffects || !source.StatusEffects.Contains("Buffed"))
                {
                    int value = ability.EffectValue;
                    if (value > 0)
                    {
                        source.HealDamage(-value);
                        source.AddStatusEffect("Buffed");
                        AddBattleLog($"{source.Name}'s {ability.Name} grants +{value} permanent buff!", BattleLogEntryType.OpponentAction);
                    }
                }
                break;
                
            case EffectType.Debuff:
            case EffectType.DebuffPower:
            case EffectType.DebuffHealth:
                if (ability.Name == "Brain Fog")
                {
                    var random = new Random();
                    if (random.Next(4) == 0)
                    {
                        source.ApplyDamage(ability.EffectValue);
                        AddBattleLog($"{source.Name}'s Brain Fog backfires! Deals {ability.EffectValue} damage to owner!", BattleLogEntryType.Debuff);
                    }
                }
                break;
                
            case EffectType.DamageToAllCreatures:
            case EffectType.DamageToAllEnemyCreatures:
                ApplyPassiveDamageAura(ability, source);
                break;
                
            case EffectType.ManaRegen:
                // Opponent mana regen (we track separately)
                AddBattleLog($"{source.Name}'s {ability.Name} is active.", BattleLogEntryType.Info);
                break;
                
            default:
                AddBattleLog($"{source.Name}'s {ability.Name} is active.", BattleLogEntryType.Info);
                break;
        }
    }
    
    /// <summary>
    /// Execute an opponent activated ability on a target
    /// </summary>
    private void ExecuteOpponentAbilityOnTarget(CardAbility ability, CardViewModel source, CardViewModel? target)
    {
        switch (ability.EffectType)
        {
            case EffectType.Damage:
            case EffectType.Debuff:
                if (target != null)
                {
                    target.ApplyDamage(ability.EffectValue);
                    AddBattleLog($"{source.Name}'s {ability.Name} deals {ability.EffectValue} damage to {target.Name}!", BattleLogEntryType.Damage);
                }
                else
                {
                    ApplyDamageToPlayer(ability.EffectValue, $"{source.Name}'s {ability.Name}");
                }
                break;
                
            case EffectType.Buff:
            case EffectType.BuffPower:
            case EffectType.BuffHealth:
                source.HealDamage(-ability.EffectValue);
                source.AddStatusEffect("Buffed");
                AddBattleLog($"{source.Name}'s {ability.Name} buffs itself by +{ability.EffectValue}!", BattleLogEntryType.OpponentAction);
                break;
                
            case EffectType.DebuffPower:
            case EffectType.DebuffHealth:
                if (target != null)
                {
                    target.AddStatusEffect("Weakened");
                    AddBattleLog($"{source.Name}'s {ability.Name} debuffs {target.Name}!", BattleLogEntryType.OpponentAction);
                }
                break;
                
            case EffectType.DamageToAll:
            case EffectType.DamageToAllEnemyCreatures:
                for (int i = 6; i < 12; i++)
                {
                    if (FieldSlots[i] != null)
                    {
                        FieldSlots[i].ApplyDamage(ability.EffectValue);
                    }
                }
                AddBattleLog($"{source.Name}'s {ability.Name} deals {ability.EffectValue} damage to all your creatures!", BattleLogEntryType.Damage);
                break;
                
            case EffectType.DisableAttack:
                if (target != null)
                {
                    target.AddStatusEffect("CannotAttack");
                    AddBattleLog($"{source.Name} disables {target.Name}'s attacks!", BattleLogEntryType.Debuff);
                }
                break;
            
            case EffectType.RevealHand:
                // Jar of Eyes - makes opponent's hand visible for one turn
                OpponentHandVisible = true;
                _temporaryEffects.Add(new TemporaryEffect
                {
                    EffectName = ability.Name,
                    TargetCardId = "PLAYER",
                    TargetCardName = "Opponent Hand",
                    StatusEffect = "RevealHand",
                    RemainingTurns = 1,
                    IsFromOpponent = false,
                    SourceAbility = ability
                });
                AddBattleLog($"{source.Name} reveals the opponent's hand!", BattleLogEntryType.Info);
                break;
                
            case EffectType.Heal:
            case EffectType.HealSelf:
                source.HealDamage(-ability.EffectValue);
                AddBattleLog($"{source.Name} heals for {ability.EffectValue} HP!", BattleLogEntryType.Info);
                break;
                
            case EffectType.ManaGain:
                // Would track separately for opponent
                AddBattleLog($"{source.Name} gains {ability.EffectValue} mana!", BattleLogEntryType.Info);
                break;
                
            case EffectType.Infect:
                if (target != null)
                {
                    target.AddStatusEffect("Infected");
                    AddBattleLog($"{source.Name} adds infection to {target.Name}!", BattleLogEntryType.Debuff);
                }
                break;
                
            default:
                AddBattleLog($"{source.Name}'s {ability.Name} effect not fully implemented!", BattleLogEntryType.Info);
                break;
        }
    }
    
    /// <summary>
    /// Find a card by ID in the field slots
    /// </summary>
    private CardViewModel? FindCardById(string cardId)
    {
        for (int i = 0; i < FieldSlots.Length; i++)
        {
            if (FieldSlots[i]?.Id == cardId)
                return FieldSlots[i];
        }
        return null;
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
            // Build opponent creature slots for AI to use
            var opponentSlots = new List<CreatureSlot>();
            for (int i = 0; i < 6; i++)
            {
                if (FieldSlots[i] != null)
                {
                    var slot = new CreatureSlot { Creature = FieldSlots[i].Card, SlotIndex = i };
                    opponentSlots.Add(slot);
                }
                else
                {
                    opponentSlots.Add(new CreatureSlot { SlotIndex = i });
                }
            }
            
            var decision = _opponentAI.DecideAction(OpponentDeck, opponentMana, playerSlots, opponentSlots);

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
                            RefreshOpponentHand();
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
                                RefreshOpponentHand();
                            }
                            else
                            {
                                // Regular spell/enchantment - apply effects
                                OpponentDeck.PlayCard(card.Id);
                                RefreshOpponentHand();
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
                            RefreshOpponentHand();
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
                                UpdateOpponentArmorBonus();
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
                            RefreshOpponentHand();
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
                
                case OpponentAI.AIDecisionType.UseAbility:
                    if (decision.SlotIndex.HasValue && decision.AbilityIndex.HasValue)
                    {
                        var oppCreature = FieldSlots[decision.SlotIndex.Value];
                        if (oppCreature != null && oppCreature.Abilities.Count > decision.AbilityIndex.Value)
                        {
                            var ability = oppCreature.Abilities[decision.AbilityIndex.Value];
                            
                            // Check mana cost
                            int abilityCost = ability.ManaCost > 0 ? ability.ManaCost : oppCreature.ManaCost;
                            if (opponentMana >= abilityCost)
                            {
                                opponentMana -= abilityCost;
                                oppCreature.AbilityUsedThisTurn = true;
                                
                                // Find target if needed
                                CardViewModel? targetVm = null;
                                if (decision.TargetCard != null)
                                {
                                    // Find target in player slots
                                    for (int i = 6; i < 12; i++)
                                    {
                                        if (FieldSlots[i]?.Id == decision.TargetCard.Id)
                                        {
                                            targetVm = FieldSlots[i];
                                            break;
                                        }
                                    }
                                }
                                
                                // Execute the ability
                                ExecuteOpponentAbilityOnTarget(ability, oppCreature, targetVm);
                                
                                AddBattleLog($"Opponent {oppCreature.Name} uses {ability.Name}!", BattleLogEntryType.OpponentAction);
                            }
                        }
                    }
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
        RefreshOpponentHand();
        
        // Execute opponent passive abilities
        ExecuteOpponentPassiveAbilities();
        
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
                        ApplyDamageToPlayer(effect.Value, "Opponent spell");
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
            var opponentCreature = FieldSlots[i];
            var playerCreature = FieldSlots[i + 6];

            // Check if opponent creature is Intimidated
            if (opponentCreature?.StatusEffects.Contains("Intimidated") == true)
            {
                AddBattleLog($"{opponentCreature.Name} is too intimidated to attack!", BattleLogEntryType.Info);
                LogToFile($"[ResolveOpponentCombat] {opponentCreature.Name} is Intimidated, skipping attack");
                continue;
            }

            if (opponentCreature != null && opponentCreature.Power > 0)
            {
                LogToFile($"[ResolveOpponentCombat] Opponent creature {opponentCreature.Name} (Power={opponentCreature.Power})");
                
                if (playerCreature != null)
                {
                    LogToFile($"[ResolveOpponentCombat] Attacking player creature {playerCreature.Name} (HP={playerCreature.Health})");
                    playerCreature.ApplyDamage(opponentCreature.Power);
                    if (playerCreature.Health <= 0)
                    {
                        LogToFile($"[ResolveOpponentCombat] Player creature died! Removing from slot {i + 6}");
                        FieldSlots[i + 6] = null;
                        RefreshCreatureSlotCaches();
                        ApplyDamageToPlayer(opponentCreature.Power, opponentCreature.Name);
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
                    ApplyDamageToPlayer(opponentCreature.Power, opponentCreature.Name);
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
            ApplyDamageToPlayer(damage, $"Opponent's {weapon.Name}");
            StatusMessage = $"Opponent's {weapon.Name} deals {damage} damage to you!";
            LogToFile($"[ResolveOpponentWeaponDamage] {weapon.Name} deals {damage} direct damage to player");
            
            // Handle power growth
            if (weapon.PowerGrowth > 0)
            {
                weapon.HitCount++;
                weapon.Power += weapon.PowerGrowth;
                OpponentWeapon.Card.Power = weapon.Power;
                OnPropertyChanged(nameof(OpponentWeapon));
                AddBattleLog($"Opponent's {weapon.Name} grows! Power is now {weapon.Power}!", BattleLogEntryType.Info);
            }
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

                // Handle power growth
                if (weapon.PowerGrowth > 0)
                {
                    weapon.HitCount++;
                    weapon.Power += weapon.PowerGrowth;
                    OpponentWeapon.Card.Power = weapon.Power;
                    OnPropertyChanged(nameof(OpponentWeapon));
                    AddBattleLog($"Opponent's {weapon.Name} grows! Power is now {weapon.Power}!", BattleLogEntryType.Info);
                }

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

            // Check if player creature is Intimidated
            if (playerCreatureVm != null && playerCreatureVm.StatusEffects.Contains("Intimidated"))
            {
                AddBattleLog($"{playerCreatureVm.Name} is too intimidated to attack!", BattleLogEntryType.Info);
                LogToFile($"[ResolveCombat] {playerCreatureVm.Name} is Intimidated, skipping attack");
                continue;
            }

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
                        ApplyDamageToOpponent(damage, playerCreatureVm.Name);
                    }
                }
                else
                {
                    ApplyDamageToOpponent(playerCreatureVm.Power, playerCreatureVm.Name);
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
            ApplyDamageToOpponent(damage, weapon.Name);
            StatusMessage = $"{weapon.Name} deals {damage} damage to opponent!";
            LogToFile($"[ResolvePlayerWeaponDamage] {weapon.Name} deals {damage} direct damage to opponent");
            
            // Handle power growth (Disgusting! mechanic)
            if (weapon.PowerGrowth > 0)
            {
                weapon.HitCount++;
                weapon.Power += weapon.PowerGrowth;
                // Update the underlying card directly
                PlayerWeapon.Card.Power = weapon.Power;
                OnPropertyChanged(nameof(PlayerWeapon));
                AddBattleLog($"{weapon.Name} grows! Power is now {weapon.Power}!", BattleLogEntryType.Info);
                LogToFile($"[ResolvePlayerWeaponDamage] {weapon.Name} power growth: +{weapon.PowerGrowth}, new power: {weapon.Power}");
            }
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

                // Handle power growth (Disgusting! mechanic)
                if (weapon.PowerGrowth > 0)
                {
                    weapon.HitCount++;
                    weapon.Power += weapon.PowerGrowth;
                    PlayerWeapon.Card.Power = weapon.Power;
                    OnPropertyChanged(nameof(PlayerWeapon));
                    AddBattleLog($"{weapon.Name} grows! Power is now {weapon.Power}!", BattleLogEntryType.Info);
                    LogToFile($"[ResolvePlayerWeaponDamage] {weapon.Name} power growth: +{weapon.PowerGrowth}, new power: {weapon.Power}");
                }

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
        // Only allow combinable cards (base creature cards) in combo slots
        if (card == null || !card.IsCombinable)
        {
            return;
        }

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
        
        // Handle special actions
        if (view == "Quit")
        {
            QuitGame();
        }
    }

    private void QuitGame()
    {
        // Save any pending data, then exit
        System.Windows.Application.Current.Shutdown();
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
        
        // Get damage reduction from the armor card
        var armorCard = card.Card as ArmorCard;
        PlayerArmorBonus = armorCard?.DamageReduction ?? 0;
        
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
                    ApplyDamageToOpponent(effect.Value, "Your spell");
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
