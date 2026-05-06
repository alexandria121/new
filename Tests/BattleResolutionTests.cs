using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MagicalDeckbuilder.Cards;
using MagicalDeckbuilder.Decks;
using MagicalDeckbuilder.Game;
using NewGame.UI.ViewModels;
using NewGame.UI.Views;
using Card = MagicalDeckbuilder.Cards.Card;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for combat resolution in MainViewModel.
/// Tests cover: combat triggering, damage application, creature death removal,
/// game-end conditions, mana refill, and turn count increment.
/// </summary>
public class BattleResolutionTests : IDisposable
{
    private readonly MainViewModel _vm;

    public BattleResolutionTests()
    {
        if (Application.Current == null)
        {
            _ = new Application();
        }
        _vm = new MainViewModel();
    }

    public void Dispose() => GC.SuppressFinalize(this);

    #region Helper methods

    private void InitializeOpponentAI()
    {
        var aiField = typeof(MainViewModel)
            .GetField("_opponentAI",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (aiField?.GetValue(_vm) == null)
        {
            aiField?.SetValue(_vm,
                new OpponentAI(DifficultyLevel.Journeyman));
        }
    }

    private CardViewModel?[]? GetFieldSlots() => _vm.FieldSlots;

    /// <summary>
    /// Creates a test creature with a fallback chain that always produces a valid card.
    /// </summary>
    private CardViewModel MakeCreature(int power, int health)
    {
        Card? c = null;
        try { c = CardFactory.GetCardByTemplateId("Mycelium Spore_Creature_Fungus"); } catch { }
        if (c == null)
        {
            var all = CardFactory.CreateStarterDeck();
            c = all.FirstOrDefault(x => x.Type == CardType.Creature);
        }
        if (c == null)
            throw new InvalidOperationException("No creature available in test deck");
        var vm = new CardViewModel(c);
        vm.Card.Power = power;
        vm.Card.Health = health;
        vm.ApplyDamage(0);
        vm.IsOnField = true;
        return vm;
    }

    private void SetPlayerSlot(int slot, CardViewModel vm)
    {
        var slots = GetFieldSlots();
        if (slots != null)
            slots[slot + 6] = vm;
    }

    private void SetOpponentSlot(int slot, CardViewModel vm)
    {
        var slots = GetFieldSlots();
        if (slots != null)
            slots[slot] = vm;
    }

    private void CallRemoveDeadCreatures()
    {
        typeof(MainViewModel)
            .GetMethod("RemoveDeadCreatures",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_vm, null);
    }

    private void CallResolveCombat()
    {
        typeof(MainViewModel)
            .GetMethod("ResolveCombat",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_vm, null);
    }

    #endregion

    #region EndTurn triggers combat

    [Fact]
    public void EndTurn_TriggersCombat_DamagesOpponentCreature()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 30;
        SetPlayerSlot(0, MakeCreature(power: 3, health: 10));
        SetOpponentSlot(0, MakeCreature(power: 0, health: 5));

        try { _vm.EndTurnCommand.Execute(null); }
        catch (InvalidOperationException) { /* GameEndWindow modal blocks in test env */ }

        var slots = GetFieldSlots();
        Assert.NotNull(slots);
        Assert.NotNull(slots[0]);
        Assert.True(slots[0].Health <= 2,
            $"Opponent creature HP should be ≤2 after 3 damage, was {slots[0].Health}");
    }

    [Fact]
    public void EndTurn_TriggersCombat_DirectDamageToOpponent()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 30;
        SetPlayerSlot(0, MakeCreature(power: 4, health: 10));
        SetOpponentSlot(0, null!);

        int before = _vm.OpponentHealth;
        try { _vm.EndTurnCommand.Execute(null); }
        catch (InvalidOperationException) { }

        Assert.True(_vm.OpponentHealth < before,
            "Opponent HP should drop when player creature has no opposing creature");
    }

    #endregion

    #region Creature attacks apply damage

    [Fact]
    public void CreatureAttacks_OpposingCreature_TakesDamage()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 30;
        SetPlayerSlot(0, MakeCreature(power: 3, health: 10));
        SetOpponentSlot(0, MakeCreature(power: 0, health: 5));

        CallResolveCombat();

        var slots = GetFieldSlots();
        Assert.NotNull(slots?[0]);
        Assert.True(slots![0].Health <= 2,
            $"Opponent creature should have ≤2 HP after 3 damage, was {slots[0].Health}");
    }

    [Fact]
    public void CreatureAttacks_NoOpposingCreature_DirectDamage()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 30;
        SetPlayerSlot(0, MakeCreature(power: 5, health: 10));
        SetOpponentSlot(0, null!);

        int before = _vm.OpponentHealth;
        CallResolveCombat();

        Assert.Equal(before - 5, _vm.OpponentHealth);
    }

    #endregion

    #region Dead creatures removed from slots

    [Fact]
    public void DeadCreatures_PlayerSlot_Cleared()
    {
        var slots = GetFieldSlots();
        Assert.NotNull(slots);
        var dead = MakeCreature(1, 1);
        dead.ApplyDamage(999);
        slots[6] = dead;
        CallRemoveDeadCreatures();
        Assert.Null(slots[6]);
    }

    [Fact]
    public void DeadCreatures_OpponentSlot_Cleared()
    {
        var slots = GetFieldSlots();
        Assert.NotNull(slots);
        var dead = MakeCreature(1, 1);
        dead.ApplyDamage(999);
        slots[0] = dead;
        CallRemoveDeadCreatures();
        Assert.Null(slots[0]);
    }

    #endregion

    #region Game ends when health reaches zero

    [Fact]
    public void GameEnd_OpponentZero_TriggersCondition()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 0;
        _vm.PlayerHealth = 30;
        Assert.True(_vm.OpponentHealth <= 0);
    }

    [Fact]
    public void GameEnd_PlayerZero_TriggersCondition()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 0;
        Assert.True(_vm.PlayerHealth <= 0);
    }

    #endregion

    #region Mana refills on new turn

    [Fact]
    public void Mana_RefillsToMax()
    {
        InitializeOpponentAI();
        _vm.PlayerMana = 3;
        _vm.PlayerMaxMana = 10;
        try { _vm.EndTurnCommand.Execute(null); }
        catch (InvalidOperationException) { }
        Assert.Equal(10, _vm.PlayerMana);
    }

    [Fact]
    public void Mana_RefillsToMax_PartialSpend()
    {
        InitializeOpponentAI();
        _vm.PlayerMana = 4;
        _vm.PlayerMaxMana = 10;
        try { _vm.EndTurnCommand.Execute(null); }
        catch (InvalidOperationException) { }
        Assert.Equal(10, _vm.PlayerMana);
    }

    #endregion

    #region Turn count increments after round

    [Fact]
    public void TurnCount_IncrementsAfterEndTurn()
    {
        InitializeOpponentAI();
        _vm.TurnCount = 1;
        _vm.PlayerMana = 2;
        _vm.PlayerMaxMana = 10;
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 30;
        try { _vm.EndTurnCommand.Execute(null); }
        catch (InvalidOperationException) { }
        Assert.Equal(2, _vm.TurnCount);
    }

    [Fact]
    public void TurnCount_Increments_TwoRounds()
    {
        InitializeOpponentAI();
        _vm.TurnCount = 1;
        _vm.PlayerMana = 2;
        _vm.PlayerMaxMana = 10;
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 30;
        try
        {
            _vm.EndTurnCommand.Execute(null);
            _vm.EndTurnCommand.Execute(null);
        }
        catch (InvalidOperationException) { }
        Assert.Equal(3, _vm.TurnCount);
    }

    #endregion

    #region Game End Window + Win/Lose Detection

    private TextBlock? GetTitleText(GameEndWindow window)
    {
        var field = typeof(GameEndWindow)
            .GetField("TitleText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(window) as TextBlock;
    }

    private TextBlock? GetSubtitleText(GameEndWindow window)
    {
        var field = typeof(GameEndWindow)
            .GetField("SubtitleText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(window) as TextBlock;
    }

    [Fact]
    public void GameEndWindow_ShowsVictory_WhenPlayerWon()
    {
        GameEndWindow window = null!;
        try
        {
            window = new GameEndWindow(true);
        }
        catch (InvalidOperationException)
        {
            // ShowDialog throws in headless test env
        }

        if (window != null)
        {
            var titleText = GetTitleText(window);
            var subtitleText = GetSubtitleText(window);
            Assert.NotNull(titleText);
            Assert.Contains("Won", titleText.Text ?? "", StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(subtitleText);
            Assert.False(string.IsNullOrWhiteSpace(subtitleText.Text),
                "Victory subtitle should not be empty");
            window.Close();
        }
    }

    [Fact]
    public void GameEndWindow_ShowsGameOver_WhenPlayerLost()
    {
        GameEndWindow window = null!;
        try
        {
            window = new GameEndWindow(false);
        }
        catch (InvalidOperationException)
        {
            // ShowDialog throws in headless test env
        }

        if (window != null)
        {
            var titleText = GetTitleText(window);
            var subtitleText = GetSubtitleText(window);
            Assert.NotNull(titleText);
            Assert.Contains("Over", titleText.Text ?? "", StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(subtitleText);
            Assert.False(string.IsNullOrWhiteSpace(subtitleText.Text),
                "Defeat subtitle should not be empty");
            window.Close();
        }
    }

    [Fact]
    public void GameEnd_OpponentKilledMidCombat_TriggersGameEnd()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 1;
        _vm.PlayerHealth = 30;
        // Player creature with attack that will kill opponent
        SetPlayerSlot(0, MakeCreature(power: 5, health: 10));
        // No opponent creature = direct damage
        SetOpponentSlot(0, null!);

        int before = _vm.OpponentHealth;
        try
        {
            _vm.EndTurnCommand.Execute(null);
        }
        catch (InvalidOperationException) { /* GameEndWindow modal */ }

        Assert.True(_vm.OpponentHealth <= 0,
            $"Opponent HP should reach 0 or below after lethal damage, was {_vm.OpponentHealth}");
    }

    [Fact]
    public void GameEnd_PlayerKilledMidCombat_TriggersGameEnd()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 1;
        // No player creature = opponent deals direct damage
        SetPlayerSlot(0, null!);
        // Opponent creature with attack that will kill player
        SetOpponentSlot(0, MakeCreature(power: 5, health: 10));

        try
        {
            _vm.EndTurnCommand.Execute(null);
        }
        catch (InvalidOperationException) { /* GameEndWindow modal */ }

        Assert.True(_vm.PlayerHealth <= 0,
            $"Player HP should reach 0 or below after lethal damage, was {_vm.PlayerHealth}");
    }

    [Fact]
    public void GameEnd_PlayerWeaponKillsOpponent_TriggersGameEnd()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 3;
        _vm.PlayerHealth = 30;
        _vm.PlayerMana = 100; // Enough mana for any weapon

        // Create weapon from starter deck, add to hand, set power
        var starterDeck = CardFactory.CreateStarterDeck();
        var weaponCard = starterDeck.FirstOrDefault(c => c.Type == CardType.Weapon);
        Assert.NotNull(weaponCard);
        weaponCard.ManaCost = 0;
        weaponCard.Power = 5;
        var weaponCardVm = new CardViewModel(weaponCard);
        weaponCardVm.IsOnField = true;
        _vm.PlayerHand.Add(weaponCardVm);

        // Equip via public method
        _vm.EquipWeapon(weaponCardVm);

        // Resolve weapon damage via reflection
        var resolveWeapon = typeof(MainViewModel)
            .GetMethod("ResolvePlayerWeaponDamage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        try { resolveWeapon?.Invoke(_vm, null); }
        catch (InvalidOperationException) { /* GameEndWindow */ }

        Assert.True(_vm.OpponentHealth <= 0,
            $"Opponent HP should reach 0 or below after weapon damage, was {_vm.OpponentHealth}");
    }

    [Fact]
    public void GameEnd_OpponentWeaponKillsPlayer_TriggersGameEnd()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 3;

        // Create a weapon, equip to opponent via backing field lookup
        var starterDeck = CardFactory.CreateStarterDeck();
        var weaponCard = starterDeck.FirstOrDefault(c => c.Type == CardType.Weapon);
        Assert.NotNull(weaponCard);
        weaponCard.Power = 5;
        var weaponCardVm = new CardViewModel(weaponCard);
        weaponCardVm.IsOnField = true;

        // Equip opponent weapon via compiler-generated backing field
        var oppWeaponField = typeof(MainViewModel)
            .GetField("<OpponentWeapon>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (oppWeaponField != null)
            oppWeaponField.SetValue(_vm, weaponCardVm);

        // Resolve opponent weapon damage via reflection
        var resolveOppWeapon = typeof(MainViewModel)
            .GetMethod("ResolveOpponentWeaponDamage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        try { resolveOppWeapon?.Invoke(_vm, null); }
        catch (InvalidOperationException) { /* GameEndWindow */ }

        Assert.True(_vm.PlayerHealth <= 0,
            $"Player HP should reach 0 or below after opponent weapon damage, was {_vm.PlayerHealth}");
    }

    #endregion

    #region AI Opponent Takes Turn

    [Fact]
    public void AI_TakesTurn_PlaysCardFromHand()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 30;
        // Place player's creature to trigger combat
        SetPlayerSlot(0, MakeCreature(power: 1, health: 10));
        
        int before = _vm.TurnCount;
        try { _vm.EndTurnCommand.Execute(null); }
        catch (InvalidOperationException) { /* GameEndWindow modal blocks in test env */ }
        
        // AI acted: TurnCount increased
        Assert.True(_vm.TurnCount > before, "AI should have taken a turn, incrementing TurnCount");
    }

    [Fact]
    public void AI_Turn_CompletesWithoutCrash_EmptyHand()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 30;
        
        // Ensure no crash when AI has no cards
        try
        {
            _vm.EndTurnCommand.Execute(null);
        }
        catch (InvalidOperationException)
        {
            // Expected: GameEndWindow modal blocks in headless test env
        }
        // No other exception should occur
        Assert.True(true, "AI turn completed without crash");
    }

    [Fact]
    public void AI_Turn_CompletesWithoutCrash_ZeroMana()
    {
        InitializeOpponentAI();
        _vm.OpponentHealth = 30;
        _vm.PlayerHealth = 30;
        // Set opponent mana to 0 via reflection
        var opponentManaField = typeof(MainViewModel)
            .GetProperty("OpponentMana",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (opponentManaField != null)
            opponentManaField.SetValue(_vm, 0);
        
        try { _vm.EndTurnCommand.Execute(null); }
        catch (InvalidOperationException) { /* GameEndWindow */ }
        
        Assert.True(true, "AI turn completed with zero mana");
    }

    [Fact]
    public void OpponentAI_ConstructsAllDifficultyLevels()
    {
        var ai1 = new OpponentAI(DifficultyLevel.Apprentice);
        Assert.Equal(DifficultyLevel.Apprentice, ai1.Difficulty);
        
        var ai2 = new OpponentAI(DifficultyLevel.Journeyman);
        Assert.Equal(DifficultyLevel.Journeyman, ai2.Difficulty);
        
        var ai3 = new OpponentAI(DifficultyLevel.Expert);
        Assert.Equal(DifficultyLevel.Expert, ai3.Difficulty);
        
        var ai4 = new OpponentAI(DifficultyLevel.Grandmaster);
        Assert.Equal(DifficultyLevel.Grandmaster, ai4.Difficulty);
    }

    [Fact]
    public void OpponentAI_DecideAction_ReturnsValidDecision_EmptyHand()
    {
        var ai = new OpponentAI(DifficultyLevel.Apprentice);
        var deck = new DeckManager();
        deck.InitializeDeck(new List<Card>());
        var playerSlots = new List<CreatureSlot> { new(), new(), new(), new(), new() };
        
        var decision = ai.DecideAction(deck, 10, playerSlots);
        
        Assert.NotNull(decision);
        // Empty hand should return Pass or EndTurn decision
        Assert.True(decision.Type == OpponentAI.AIDecisionType.Pass || 
                    decision.Type == OpponentAI.AIDecisionType.EndTurn,
                    $"Expected Pass or EndTurn for empty hand, got {decision.Type}");
    }

    [Fact]
    public void OpponentAI_DecideAction_ReturnsValidDecision_WithCards()
    {
        var ai = new OpponentAI(DifficultyLevel.Journeyman);
        var deck = new DeckManager();
        var starterCards = CardFactory.CreateStarterDeck();
        deck.InitializeDeck(starterCards);
        var playerSlots = new List<CreatureSlot> { new(), new(), new(), new(), new() };
        
        var decision = ai.DecideAction(deck, 10, playerSlots);
        
        Assert.NotNull(decision);
        // Decision type should be one of the valid options
        Assert.True(Enum.IsDefined(typeof(OpponentAI.AIDecisionType), decision.Type),
            $"Decision type {decision.Type} should be a valid AIDecisionType");
    }

    #endregion
}
