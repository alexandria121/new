using Card = MagicalDeckbuilder.Cards.Card;
using CardType = MagicalDeckbuilder.Cards.CardType;
using EffectType = MagicalDeckbuilder.Cards.EffectType;
using TargetType = MagicalDeckbuilder.Cards.TargetType;
using CreatureSlot = MagicalDeckbuilder.Decks.CreatureSlot;
using MagicalDeckbuilder.Decks;
using MagicalDeckbuilder.Combining;
using MagicalDeckbuilder.Game;

namespace MagicalDeckbuilder.Game;

/// <summary>
/// Represents a player or opponent in the game
/// </summary>
public class Character
{
    public string Name { get; set; } = "Character";
    public int Health { get; set; } = 30;
    public int MaxHealth { get; set; } = 30;
    public int Mana { get; set; } = 0;
    public int MaxMana { get; set; } = 0;
    public int Turn { get; set; } = 1;
    public DeckManager Deck { get; set; } = new();
    public bool IsPlayer { get; set; } = true;
    public List<Card> CustomCreatedCards { get; set; } = new();
    
    // Damage tracking for visual display
    public int LastDamageTaken { get; set; } = 0;
    public int LastDamageDealt { get; set; } = 0;
    public bool ShowDamageIndicator { get; set; } = false;
    
    /// <summary>
    /// Take damage and track it for display
    /// </summary>
    public void TakeDamage(int amount)
    {
        LastDamageTaken = amount;
        ShowDamageIndicator = true;
        Health = Math.Max(0, Health - amount);
    }
    
    /// <summary>
    /// Reset damage indicators after display
    /// </summary>
    public void ClearDamageIndicator()
    {
        ShowDamageIndicator = false;
        LastDamageTaken = 0;
        LastDamageDealt = 0;
    }
    
    /// <summary>
    /// Generate a visual health bar with optional damage indicator
    /// </summary>
    public string GetHealthBar()
    {
        const int barLength = 20;
        int filled = (int)((double)Health / MaxHealth * barLength);
        string bar = new string('█', filled) + new string('░', barLength - filled);
        
        // Add damage indicator if recently damaged
        if (ShowDamageIndicator && LastDamageTaken > 0)
        {
            return $"[{bar}] -{LastDamageTaken}";
        }
        return $"[{bar}]";
    }
    
    /// <summary>
    /// Generate a visual mana bar
    /// </summary>
    public string GetManaBar()
    {
        const int barLength = 10;
        int filled = (int)((double)Mana / MaxMana * barLength);
        string bar = new string('◆', filled) + new string('◇', barLength - filled);
        return $"[{bar}]";
    }
}

/// <summary>
/// Main game class that handles game loop and player interaction
/// </summary>
public class Game
{
    private Character _player;
    private Character _opponent;
    private CardCombiner _combiner;
    private bool _gameRunning;
    private List<string> _gameLog = new();
    private Random _random = new();
    
    public Game()
    {
        _player = new Character { Name = "Player", IsPlayer = true };
        _opponent = new Character { Name = "Enemy", IsPlayer = false };
        _combiner = new CardCombiner();
    }
    
    /// <summary>
    /// Start a new game
    /// </summary>
    public void Start()
    {
        Console.WriteLine("╔════════════════════════════════════════════════╗");
        Console.WriteLine("║           MAGICAL DECKBUILDER v1.1              ║");
        Console.WriteLine("║     Combine Cards, Build Your Deck, Win!       ║");
        Console.WriteLine("╚════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        // Initialize player deck
        var playerDeck = CardFactory.CreateSimpleDeck();
        _player.Deck.InitializeDeck(playerDeck);
        _player.MaxMana = 1;
        _player.Mana = _player.MaxMana;
        
        // Initialize opponent deck (slightly different for variety)
        var opponentDeck = CardFactory.CreateSimpleDeck();
        _opponent.Deck.InitializeDeck(opponentDeck);
        _opponent.MaxMana = 1;
        _opponent.Mana = _opponent.MaxMana;
        
        // Draw initial hands
        _player.Deck.DrawCards(_player.Deck.StartingHandSize);
        _opponent.Deck.DrawCards(_opponent.Deck.StartingHandSize);
        
        _gameRunning = true;
        _gameLog.Add("Game started!");
        
        MainLoop();
    }
    
    /// <summary>
    /// Main game loop
    /// </summary>
    private void MainLoop()
    {
        while (_gameRunning)
        {
            ShowGameState();
            
            // Check for game over
            if (_player.Health <= 0)
            {
                GameOver(false);
                return;
            }
            if (_opponent.Health <= 0)
            {
                GameOver(true);
                return;
            }
            
            ShowMainMenu();
            
            var input = Console.ReadLine()?.Trim().ToLower() ?? "";
            
            Console.WriteLine();
            switch (input)
            {
                case "1":
                    PlayCardFromHand();
                    break;
                case "2":
                    CombineCards();
                    break;
                case "3":
                    EndTurn();
                    break;
                case "4":
                    ShowDeckStatus();
                    break;
                case "5":
                    ShowCustomCards();
                    break;
                case "6":
                    ShowGameLog();
                    break;
                case "q":
                    _gameRunning = false;
                    Console.WriteLine("Thanks for playing!");
                    break;
                default:
                    Console.WriteLine("Invalid option. Try again.");
                    break;
            }
            
            Console.WriteLine();
            
            // Check for game over after player action
            if (_player.Health <= 0)
            {
                GameOver(false);
                return;
            }
            if (_opponent.Health <= 0)
            {
                GameOver(true);
                return;
            }
        }
    }
    
    /// <summary>
    /// Display current game state with health/mana bars
    /// </summary>
    private void ShowGameState()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        // Opponent status (top)
        Console.WriteLine($"👹 OPPONENT: {_opponent.Name}");
        Console.WriteLine($"   Health: {_opponent.GetHealthBar()} {_opponent.Health}/{_opponent.MaxHealth}");
        Console.WriteLine($"   Mana:   {_opponent.GetManaBar()} {_opponent.Mana}/{_opponent.MaxMana}");
        Console.WriteLine($"   Hand:   {_opponent.Deck.Hand.Count} cards");
        Console.WriteLine("───────────────────────────────────────────────────────────────");
        
        // Turn info
        Console.WriteLine($"                        ⚔️  TURN {_player.Turn}  ⚔️");
        
        Console.WriteLine("───────────────────────────────────────────────────────────────");
        
        // Player status (bottom)
        Console.WriteLine($"🎮 YOU: {_player.Name}");
        Console.WriteLine($"   Health: {_player.GetHealthBar()} {_player.Health}/{_player.MaxHealth}");
        Console.WriteLine($"   Mana:   {_player.GetManaBar()} {_player.Mana}/{_player.MaxMana}");
        
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("📚 DRAW PILE: " + _player.Deck.GetPileCount(PileType.DrawPile));
        Console.WriteLine("🗑️  DISCARD:  " + _player.Deck.GetPileCount(PileType.DiscardPile));
        Console.WriteLine();
        Console.WriteLine("🎴 YOUR HAND:");
        Console.WriteLine(_player.Deck.DisplayHand());
        Console.WriteLine("⚔️  IN PLAY:");
        Console.WriteLine(_player.Deck.DisplayInPlay());
        Console.WriteLine();
    }
    
    /// <summary>
    /// Display main menu
    /// </summary>
    private void ShowMainMenu()
    {
        Console.WriteLine("┌─────────────────────────────────────────┐");
        Console.WriteLine("│ MAIN MENU                               │");
        Console.WriteLine("├─────────────────────────────────────────┤");
        Console.WriteLine("│ 1. Play a card from hand                │");
        Console.WriteLine("│ 2. Combine cards                        │");
        Console.WriteLine("│ 3. End turn (Attack!)                   │");
        Console.WriteLine("│ 4. Show deck status                     │");
        Console.WriteLine("│ 5. Show custom cards                    │");
        Console.WriteLine("│ 6. Show game log                        │");
        Console.WriteLine("│ Q. Quit game                           │");
        Console.WriteLine("└─────────────────────────────────────────┘");
        Console.Write("Select option: ");
    }
    
    /// <summary>
    /// Play a card from hand - includes card effects
    /// </summary>
    private void PlayCardFromHand()
    {
        if (_player.Deck.Hand.Count == 0)
        {
            Console.WriteLine("Your hand is empty!");
            return;
        }
        
        // Show available slots for creatures
        Console.WriteLine("\nAvailable creature slots:");
        for (int i = 0; i < _player.Deck.CreatureSlots.Count; i++)
        {
            var slot = _player.Deck.CreatureSlots[i];
            if (slot.Creature != null)
                Console.WriteLine($"  [{i + 1}] {slot.Creature.Name}");
            else
                Console.WriteLine($"  [{i + 1}] (Empty)");
        }
        
        Console.WriteLine("\nSelect card number to play:");
        var input = Console.ReadLine();
        
        if (int.TryParse(input, out int cardIndex) && cardIndex >= 1 && cardIndex <= _player.Deck.Hand.Count)
        {
            var card = _player.Deck.Hand[cardIndex - 1];
            
            if (card.ManaCost <= _player.Mana)
            {
                // For creatures, ask which slot to play in
                if (card.Type == CardType.Creature)
                {
                    Console.WriteLine($"Select slot number (1-{_player.Deck.CreatureSlots.Count}) for {card.Name}:");
                    var slotInput = Console.ReadLine();
                    
                    if (int.TryParse(slotInput, out int slotNum) && slotNum >= 1 && slotNum <= _player.Deck.CreatureSlots.Count)
                    {
                        var slotIndex = slotNum - 1;
                        if (!_player.Deck.CreatureSlots[slotIndex].IsEmpty)
                        {
                            Console.WriteLine("That slot is occupied! Choose another.");
                            return;
                        }
                        
                        _player.Mana -= card.ManaCost;
                        _player.Deck.PlayCreatureToSlot(card.Id, slotIndex);
                        Console.WriteLine($"✓ Played {card.Name} in slot {slotNum}!");
                        _gameLog.Add($"Played {card.Name} in slot {slotNum}");
                        
                        // Apply card effects (initial attack is handled in combat phase)
                    }
                    else
                    {
                        Console.WriteLine("Invalid slot selection.");
                        return;
                    }
                }
                else
                {
                    // Non-creature cards play normally
                    _player.Mana -= card.ManaCost;
                    _player.Deck.PlayCard(card.Id);
                    Console.WriteLine($"✓ Played {card.Name}!");
                    _gameLog.Add($"Played {card.Name}");
                    
                    // Apply card effects
                    ApplyCardEffects(card, _player, _opponent);
                }
            }
            else
            {
                Console.WriteLine($"Not enough mana! Need {card.ManaCost}, have {_player.Mana}");
            }
        }
        else
        {
            Console.WriteLine("Invalid selection.");
        }
    }
    
    /// <summary>
    /// Apply a card's effects to targets
    /// </summary>
    private void ApplyCardEffects(Card card, Character caster, Character target)
    {
        foreach (var effect in card.Effects)
        {
            switch (effect.Type)
            {
                case EffectType.Damage:
                    if (effect.Target == TargetType.Enemy || effect.Target == TargetType.Any)
                    {
                        int damage = effect.Value;
                        
                        // For spells, check for enemy creatures
                        if (card.Type == CardType.Spell || card.Type == CardType.Weapon)
                        {
                            var enemyCreatures = target.Deck.GetCreaturesInSlots();
                            if (enemyCreatures.Count > 0)
                            {
                                // Attack first creature
                                var enemyCreature = enemyCreatures.First();
                                enemyCreature.Health -= damage;
                                Console.WriteLine($"  ⚔️ {card.Name} attacks {enemyCreature.Name} for {damage} damage!");
                                
                                if (enemyCreature.Health <= 0)
                                {
                                    // Find and remove the creature from slots
                                    var slotIndex = target.Deck.CreatureSlots
                                        .FindIndex(s => s.Creature?.Id == enemyCreature.Id);
                                    if (slotIndex >= 0)
                                        target.Deck.RemoveCreatureFromSlot(slotIndex);
                                    Console.WriteLine($"  💀 {enemyCreature.Name} was destroyed!");
                                }
                            }
                            else
                            {
                                // Direct attack
                                target.Health -= damage;
                                Console.WriteLine($"  💥 {card.Name} deals {damage} damage to {target.Name}!");
                            }
                        }
                        else
                        {
                            // Other card types deal direct damage
                            target.TakeDamage(damage);
                            Console.WriteLine($"  💥 {card.Name} deals {damage} damage to {target.Name}!");
                        }
                    }
                    break;
                    
                case EffectType.Heal:
                    if (effect.Target == TargetType.Self || effect.Target == TargetType.Ally)
                    {
                        caster.Health = Math.Min(caster.Health + effect.Value, caster.MaxHealth);
                        Console.WriteLine($"  💚 {card.Name} heals {caster.Name} for {effect.Value} HP!");
                    }
                    break;
                    
                case EffectType.Buff:
                    if (effect.Target == TargetType.Self || effect.Target == TargetType.AllAllies)
                    {
                        caster.Health += effect.Value;
                        Console.WriteLine($"  🛡️ {card.Name} gives {effect.Value} temporary HP to {caster.Name}!");
                    }
                    break;
                    
                case EffectType.DrawCard:
                    int drawCount = effect.Value > 0 ? effect.Value : 1;
                    caster.Deck.DrawCards(drawCount);
                    Console.WriteLine($"  🎴 {card.Name} draws {drawCount} card(s)!");
                    break;
                    
                case EffectType.ManaGain:
                    caster.MaxMana += effect.Value;
                    caster.Mana = Math.Min(caster.Mana + effect.Value, caster.MaxMana);
                    Console.WriteLine($"  ✨ {card.Name} gives +{effect.Value} max mana!");
                    break;
                    
                default:
                    Console.WriteLine($"  ⚡ {card.Name} has effect: {effect.Name}");
                    break;
            }
        }
        
        // Creatures stay in play (in slots), spells and events are discarded
        if (card.Type == CardType.Spell || card.Type == CardType.Event)
        {
            _player.Deck.DiscardCard(card.Id);
        }
    }
    
    /// <summary>
    /// Combine two cards
    /// </summary>
    private void CombineCards()
    {
        if (_player.Deck.Hand.Count < 2)
        {
            Console.WriteLine("You need at least 2 cards to combine!");
            return;
        }
        
        Console.WriteLine("Select first card to combine:");
        var input1 = Console.ReadLine();
        
        if (!int.TryParse(input1, out int index1) || index1 < 1 || index1 > _player.Deck.Hand.Count)
        {
            Console.WriteLine("Invalid first selection.");
            return;
        }
        
        Console.WriteLine("Select second card to combine:");
        var input2 = Console.ReadLine();
        
        if (!int.TryParse(input2, out int index2) || index2 < 1 || index2 > _player.Deck.Hand.Count)
        {
            Console.WriteLine("Invalid second selection.");
            return;
        }
        
        if (index1 == index2)
        {
            Console.WriteLine("You must select two different cards!");
            return;
        }
        
        // Get cards (adjust for 1-based indexing)
        var card1 = _player.Deck.Hand[index1 - 1];
        var card2 = _player.Deck.Hand[index2 - 1];
        
        Console.WriteLine($"\nCombining {card1.Name} with {card2.Name}...");
        
        var result = _combiner.Combine(card1, card2);
        
        if (result.Success && result.ResultCard != null)
        {
            // Remove original cards
            _player.Deck.DiscardFromHand(card1.Id);
            _player.Deck.DiscardFromHand(card2.Id);
            
            // Add new card to hand (as a free card)
            var newCard = result.ResultCard;
            _player.Deck.Hand.Add(newCard);
            
            // Also save to custom cards collection
            _player.CustomCreatedCards.Add(newCard);
            
            Console.WriteLine($"✓ {result.Message}");
            Console.WriteLine($"  New Card: {newCard.Name} ({newCard.Type}) | {newCard.ManaCost} Mana | PWR:{newCard.Power} HP:{newCard.Health}");
            _gameLog.Add($"Created {newCard.Name} via combination");
        }
        else
        {
            Console.WriteLine($"✗ {result.Message}");
        }
    }
    
    /// <summary>
    /// End current turn - opponent takes their turn
    /// </summary>
    private void EndTurn()
    {
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("🎮 Player turn ended. Enemy turn begins...");
        Console.WriteLine("═══════════════════════════════════════════════");
        
        _gameLog.Add($"Turn {_player.Turn} ended");
        
        // Player's end of turn effects
        foreach (var card in _player.Deck.InPlay.ToList())
        {
            if (card.Type == CardType.Artifact)
            {
                // Artifacts persist - check for end-of-turn effects
            }
        }
        
        // COMBAT PHASE: Player creatures attack
        Console.WriteLine("\n⚔️  COMBAT PHASE: Your creatures attack!");
        ResolveCombat(_player, _opponent);
        
        // Check for game over after combat
        if (_opponent.Health <= 0 || _player.Health <= 0)
        {
            return;
        }
        
        // Opponent's turn
        OpponentTurn();
        
        // Check for game over after opponent turn
        if (_player.Health <= 0 || _opponent.Health <= 0)
        {
            return;
        }
        
        // Start next player turn
        _player.Turn++;
        _player.MaxMana = Math.Min(_player.Turn, 10);
        _player.Mana = _player.MaxMana;
        
        // Draw new hand for player
        var previousHand = _player.Deck.Hand.Count;
        _player.Deck.DrawCards(_player.Deck.StartingHandSize);
        
        Console.WriteLine($"\n═══════════════════════════════════════════════");
        Console.WriteLine($"⚔️  Turn {_player.Turn} started! Mana: {_player.Mana}/{_player.MaxMana}");
        Console.WriteLine($"   You drew {_player.Deck.Hand.Count - previousHand} new card(s)");
        Console.WriteLine("═══════════════════════════════════════════════");
        _gameLog.Add($"Turn {_player.Turn} started");
    }
    
    /// <summary>
    /// Resolve combat - all creatures attack according to slot rules
    /// </summary>
    private void ResolveCombat(Character attacker, Character defender)
    {
        var attackerSlots = attacker.Deck.CreatureSlots;
        var defenderSlots = defender.Deck.CreatureSlots;
        
        // Process each slot - creatures attack in slot order
        for (int slotIndex = 0; slotIndex < attackerSlots.Count; slotIndex++)
        {
            var attackerCreature = attackerSlots[slotIndex].Creature;
            if (attackerCreature == null)
                continue;
            
            if (attackerCreature.Type != CardType.Creature)
                continue;
            
            // Find target using the targeting logic
            var target = FindCombatTarget(slotIndex, defenderSlots, defender);
            
            if (target != null)
            {
                // Attack the creature
                int damage = attackerCreature.Power;
                target.Health -= damage;
                Console.WriteLine($"  ⚔️ {attackerCreature.Name} (Slot {slotIndex + 1}) attacks {target.Name} for {damage} damage!");
                
                // Check if target creature died
                if (target.Health <= 0)
                {
                    // Remove the target from its slot
                    defender.Deck.RemoveCreatureFromSlot(
                        defender.Deck.CreatureSlots.FindIndex(s => s.Creature?.Id == target.Id));
                    Console.WriteLine($"  💀 {target.Name} was destroyed!");
                }
            }
            else
            {
                // Direct attack to opponent
                int damage = attackerCreature.Power;
                defender.TakeDamage(damage);
                Console.WriteLine($"  ⚔️ {attackerCreature.Name} (Slot {slotIndex + 1}) attacks {defender.Name} directly for {damage} damage!");
            }
        }
        
        // Remove dead creatures from slots (health <= 0)
        for (int slotIndex = 0; slotIndex < attackerSlots.Count; slotIndex++)
        {
            var creature = attackerSlots[slotIndex].Creature;
            if (creature != null && creature.Health <= 0)
            {
                attacker.Deck.RemoveCreatureFromSlot(slotIndex);
                Console.WriteLine($"  💀 Your {creature.Name} in slot {slotIndex + 1} was destroyed!");
            }
        }
        
        // Clear damage indicators after display
        defender.ClearDamageIndicator();
    }
    
    /// <summary>
    /// Find combat target for a creature in the given slot
    /// Rules:
    /// 1. First try the opposite slot
    /// 2. If empty, scan left-to-right starting from slot 0
    /// 3. If all slots empty, return null (direct attack)
    /// </summary>
    private Card? FindCombatTarget(int attackerSlotIndex, List<CreatureSlot> defenderSlots, Character defender)
    {
        // First, check the opposite slot
        if (attackerSlotIndex < defenderSlots.Count)
        {
            var oppositeCreature = defenderSlots[attackerSlotIndex].Creature;
            if (oppositeCreature != null)
            {
                return oppositeCreature;
            }
        }
        
        // If opposite is empty, scan left-to-right (like reading a book)
        for (int i = 0; i < defenderSlots.Count; i++)
        {
            if (i == attackerSlotIndex)
                continue; // Skip the opposite slot we already checked
            
            var creature = defenderSlots[i].Creature;
            if (creature != null)
            {
                return creature;
            }
        }
        
        // No creatures in any slot - direct attack
        return null;
    }
    
    /// <summary>
    /// Simple AI opponent turn
    /// </summary>
    private void OpponentTurn()
    {
        Console.WriteLine($"\n👹 {_opponent.Name} is thinking...");
        
        // Opponent plays cards
        var playableCards = _opponent.Deck.Hand
            .Where(c => c.ManaCost <= _opponent.Mana)
            .OrderByDescending(c => c.Power)
            .ToList();
        
        foreach (var card in playableCards)
        {
            if (card.ManaCost <= _opponent.Mana)
            {
                // For creatures, find an empty slot
                if (card.Type == CardType.Creature)
                {
                    var emptySlot = -1;
                    for (int i = 0; i < _opponent.Deck.CreatureSlots.Count; i++)
                    {
                        if (_opponent.Deck.CreatureSlots[i].IsEmpty)
                        {
                            emptySlot = i;
                            break;
                        }
                    }
                    
                    if (emptySlot >= 0)
                    {
                        _opponent.Mana -= card.ManaCost;
                        _opponent.Deck.PlayCreatureToSlot(card.Id, emptySlot);
                        Console.WriteLine($"   👹 Enemy played {card.Name} in slot {emptySlot + 1}!");
                        _gameLog.Add($"Enemy played {card.Name} in slot {emptySlot + 1}");
                    }
                }
                else
                {
                    _opponent.Mana -= card.ManaCost;
                    _opponent.Deck.PlayCard(card.Id);
                    Console.WriteLine($"   👹 Enemy played {card.Name}!");
                    
                    // Apply effects to player
                    ApplyCardEffects(card, _opponent, _player);
                    
                    _gameLog.Add($"Enemy played {card.Name}");
                }
            }
        }
        
        // Discard hand and draw new
        foreach (var card in _opponent.Deck.Hand.ToList())
        {
            _opponent.Deck.DiscardFromHand(card.Id);
        }
        
        // NON-CREATURES in play get discarded (creatures stay in slots)
        foreach (var card in _opponent.Deck.InPlay.ToList())
        {
            if (card.Type != CardType.Creature)
            {
                _opponent.Deck.DiscardCard(card.Id);
            }
        }
        
        // Combat phase: opponent creatures attack!
        Console.WriteLine("\n⚔️  COMBAT PHASE: Enemy creatures attack!");
        ResolveCombat(_opponent, _player);
        
        // Check for game over after opponent combat
        if (_player.Health <= 0)
        {
            return;
        }
        
        // Next opponent turn
        _opponent.Turn++;
        _opponent.MaxMana = Math.Min(_opponent.Turn, 10);
        _opponent.Mana = _opponent.MaxMana;
        
        _opponent.Deck.DrawCards(_opponent.Deck.StartingHandSize);
        
        Console.WriteLine($"   👹 Enemy turn complete. Hand: {_opponent.Deck.Hand.Count} cards");
    }
    
    /// <summary>
    /// Handle game over
    /// </summary>
    private void GameOver(bool playerWon)
    {
        _gameRunning = false;
        
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════╗");
        
        if (playerWon)
        {
            Console.WriteLine("║           🎉 VICTORY! 🎉                        ║");
            Console.WriteLine("║                                                ║");
            Console.WriteLine("║    You have defeated the enemy!                ║");
            Console.WriteLine($"║    Final Health: {_player.Health}/{_player.MaxHealth}                      ║");
        }
        else
        {
            Console.WriteLine("║           💀 DEFEAT 💀                          ║");
            Console.WriteLine("║                                                ║");
            Console.WriteLine("║    You have been defeated...                  ║");
            Console.WriteLine($"║    Enemy Health: {_opponent.Health}/{_opponent.MaxHealth}                      ║");
        }
        
        Console.WriteLine("╚════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Game Over! Thanks for playing!");
    }
    
    /// <summary>
    /// Show deck status
    /// </summary>
    private void ShowDeckStatus()
    {
        Console.WriteLine("═══ DECK STATUS ═══");
        Console.WriteLine($"Draw Pile:   {_player.Deck.GetPileCount(PileType.DrawPile)} cards");
        Console.WriteLine($"Hand:        {_player.Deck.GetPileCount(PileType.Hand)} cards");
        Console.WriteLine($"In Play:     {_player.Deck.GetPileCount(PileType.InPlay)} cards");
        Console.WriteLine($"Discard:     {_player.Deck.GetPileCount(PileType.DiscardPile)} cards");
        Console.WriteLine($"Custom Cards: {_player.CustomCreatedCards.Count} created");
        Console.WriteLine("═══════════════════");
    }
    
    /// <summary>
    /// Show custom created cards
    /// </summary>
    private void ShowCustomCards()
    {
        Console.WriteLine("═══ CUSTOM CREATED CARDS ═══");
        
        if (_player.CustomCreatedCards.Count == 0)
        {
            Console.WriteLine("No custom cards yet. Combine cards to create some!");
        }
        else
        {
            foreach (var card in _player.CustomCreatedCards)
            {
                Console.WriteLine($"• {card.Name} ({card.Type}) | {card.Element} | {card.ManaCost} Mana | PWR:{card.Power} HP:{card.Health}");
            }
        }
        
        Console.WriteLine("════════════════════════════");
    }
    
    /// <summary>
    /// Show game log
    /// </summary>
    private void ShowGameLog()
    {
        Console.WriteLine("═══ GAME LOG ═══");
        
        if (_gameLog.Count == 0)
        {
            Console.WriteLine("No events yet.");
        }
        else
        {
            foreach (var entry in _gameLog.TakeLast(10))
            {
                Console.WriteLine($"• {entry}");
            }
        }
        
        Console.WriteLine("════════════════");
    }
}