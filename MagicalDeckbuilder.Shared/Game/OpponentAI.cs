using Card = MagicalDeckbuilder.Cards.Card;
using CardType = MagicalDeckbuilder.Cards.CardType;
using MagicalDeckbuilder.Decks;
using MagicalDeckbuilder.Logging;

namespace MagicalDeckbuilder.Game;

public enum DifficultyLevel
{
    Apprentice,
    Journeyman,
    Expert,
    Grandmaster
}

public class OpponentAI
{
    private readonly DifficultyLevel _difficulty;
    private readonly Random _random = new();

    public OpponentAI(DifficultyLevel difficulty)
    {
        _difficulty = difficulty;
    }

    public DifficultyLevel Difficulty => _difficulty;

    public struct AIDecision
    {
        public string? CardId;
        public int? SlotIndex;
        public Card? TargetCard;
        public AIDecisionType Type;
        
        // For UseAbility decisions
        public int? AbilityIndex; // Which ability to use (index in Abilities list)
        public Card? AbilityTarget; // Target for the ability
    }

    public enum AIDecisionType
    {
        PlayCreature,
        PlaySpell,
        PlayArtifact,
        PlayWeapon,
        Attack,
        UseAbility,
        EndTurn,
        Pass
    }

    public AIDecision DecideAction(DeckManager deck, int currentMana, List<CreatureSlot> playerSlots, List<CreatureSlot>? opponentSlots = null)
    {
        // First check if any creature can use an ability
        var abilityDecision = TryChooseAbilityDecision(deck, currentMana, playerSlots, opponentSlots);
        if (abilityDecision.Type != AIDecisionType.Pass)
            return abilityDecision;
        
        return _difficulty switch
        {
            DifficultyLevel.Apprentice => ApprenticeLogic(deck, currentMana, playerSlots),
            DifficultyLevel.Journeyman => JourneymanLogic(deck, currentMana, playerSlots),
            DifficultyLevel.Expert => ExpertLogic(deck, currentMana, playerSlots),
            DifficultyLevel.Grandmaster => GrandmasterLogic(deck, currentMana, playerSlots),
            _ => ApprenticeLogic(deck, currentMana, playerSlots)
        };
    }
    
    /// <summary>
    /// Try to find a creature with an ability that can be used
    /// </summary>
    private AIDecision TryChooseAbilityDecision(DeckManager deck, int currentMana, List<CreatureSlot> playerSlots, List<CreatureSlot>? opponentSlots)
    {
        if (opponentSlots == null) return new AIDecision { Type = AIDecisionType.Pass };
        
        // Check opponent's creatures for usable abilities
        for (int i = 0; i < opponentSlots.Count; i++)
        {
            var slot = opponentSlots[i];
            if (slot?.Creature == null) continue;
            
            var card = slot.Creature;
            
            // Skip if already used ability this turn
            if (card.Abilities == null || card.Abilities.Count == 0) continue;
            
            // Check each ability
            for (int a = 0; a < card.Abilities.Count; a++)
            {
                var ability = card.Abilities[a];
                if (ability.IsPassive) continue; // Skip passives
                if (ability.ManaCost > currentMana) continue; // Not enough mana
                
                // Have ability - find target
                Card? target = null;
                
                // Simple targeting: pick a random player creature as target if ability needs one
                if (ability.RequiresTarget && playerSlots.Count > 0)
                {
                    var availableTargets = playerSlots.Where(s => s?.Creature != null).ToList();
                    if (availableTargets.Count > 0)
                    {
                        target = availableTargets[_random.Next(availableTargets.Count)].Creature;
                    }
                }
                
                return new AIDecision
                {
                    Type = AIDecisionType.UseAbility,
                    CardId = card.Id,
                    SlotIndex = i,
                    AbilityIndex = a,
                    TargetCard = target
                };
            }
        }
        
        return new AIDecision { Type = AIDecisionType.Pass };
    }

    private AIDecision ApprenticeLogic(DeckManager deck, int currentMana, List<CreatureSlot> playerSlots)
    {
        var playableCards = deck.Hand
            .Where(c => c.ManaCost <= currentMana)
            .ToList();

        if (playableCards.Count == 0)
            return new AIDecision { Type = AIDecisionType.Pass };

        var randomCard = playableCards[_random.Next(playableCards.Count)];

        if (randomCard.Type == CardType.Creature)
        {
            var emptySlot = FindEmptySlot(deck.CreatureSlots);
            if (emptySlot >= 0)
            {
                return new AIDecision
                {
                    Type = AIDecisionType.PlayCreature,
                    CardId = randomCard.Id,
                    SlotIndex = emptySlot
                };
            }
        }

        // Return proper decision type for equipment
        var decisionType = randomCard.Type switch
        {
            CardType.Weapon => AIDecisionType.PlayWeapon,
            CardType.Armor => AIDecisionType.PlayArtifact,
            CardType.Artifact => AIDecisionType.PlayArtifact,
            CardType.Event => AIDecisionType.PlaySpell,
            _ => AIDecisionType.PlaySpell
        };
        
        return new AIDecision
        {
            Type = decisionType,
            CardId = randomCard.Id
        };
    }

    private AIDecision JourneymanLogic(DeckManager deck, int currentMana, List<CreatureSlot> playerSlots)
    {
        var playableCards = deck.Hand
            .Where(c => c.ManaCost <= currentMana)
            .OrderByDescending(c => c.Power)
            .ToList();

        if (playableCards.Count == 0)
            return new AIDecision { Type = AIDecisionType.Pass };

        var bestCard = playableCards.First();

        if (bestCard.Type == CardType.Creature)
        {
            var emptySlot = FindEmptySlot(deck.CreatureSlots);
            if (emptySlot >= 0)
            {
                return new AIDecision
                {
                    Type = AIDecisionType.PlayCreature,
                    CardId = bestCard.Id,
                    SlotIndex = emptySlot
                };
            }
            return new AIDecision { Type = AIDecisionType.Pass };
        }

        if (bestCard.Type == CardType.Spell && playerSlots.Any(s => s.Creature != null))
        {
            var targetCreature = playerSlots[_random.Next(playerSlots.Count(s => s.Creature != null))].Creature;
            return new AIDecision
            {
                Type = AIDecisionType.PlaySpell,
                CardId = bestCard.Id,
                TargetCard = targetCreature
            };
        }

        // Return proper decision type for equipment
        var journeymanDecisionType = bestCard.Type switch
        {
            CardType.Weapon => AIDecisionType.PlayWeapon,
            CardType.Armor => AIDecisionType.PlayArtifact,
            CardType.Artifact => AIDecisionType.PlayArtifact,
            CardType.Event => AIDecisionType.PlaySpell,
            _ => AIDecisionType.PlaySpell
        };
        
        return new AIDecision
        {
            Type = journeymanDecisionType,
            CardId = bestCard.Id
        };
    }

    private AIDecision ExpertLogic(DeckManager deck, int currentMana, List<CreatureSlot> playerSlots)
    {
        var playableCards = deck.Hand
            .Where(c => c.ManaCost <= currentMana)
            .OrderByDescending(c => GetCardValue(c, currentMana, playerSlots))
            .ToList();

        if (playableCards.Count == 0)
            return new AIDecision { Type = AIDecisionType.Pass };

        var bestCard = playableCards.First();

        if (bestCard.Type == CardType.Creature)
        {
            var emptySlot = FindBestSlotForCreature(deck, bestCard, playerSlots);
            if (emptySlot >= 0)
            {
                return new AIDecision
                {
                    Type = AIDecisionType.PlayCreature,
                    CardId = bestCard.Id,
                    SlotIndex = emptySlot
                };
            }
            return new AIDecision { Type = AIDecisionType.Pass };
        }

        if (bestCard.Type == CardType.Spell)
        {
            var target = FindBestSpellTarget(bestCard, playerSlots, deck);
            return new AIDecision
            {
                Type = AIDecisionType.PlaySpell,
                CardId = bestCard.Id,
                TargetCard = target
            };
        }

        var decisionType = bestCard.Type switch
        {
            CardType.Weapon => AIDecisionType.PlayWeapon,
            CardType.Armor => AIDecisionType.PlayArtifact,
            CardType.Artifact => AIDecisionType.PlayArtifact,
            CardType.Event => AIDecisionType.PlaySpell,
            _ => AIDecisionType.PlaySpell
        };
        
        return new AIDecision
        {
            Type = decisionType,
            CardId = bestCard.Id
        };
    }

    private AIDecision GrandmasterLogic(DeckManager deck, int currentMana, List<CreatureSlot> playerSlots)
    {
        var playableCards = deck.Hand
            .Where(c => c.ManaCost <= currentMana)
            .ToList();

        if (playableCards.Count == 0)
            return new AIDecision { Type = AIDecisionType.Pass };

        var threats = playerSlots
            .Where(s => s.Creature != null)
            .Select(s => s.Creature!)
            .OrderByDescending(c => c.Power)
            .ToList();

        var myEmptySlots = GetEmptySlotCount(deck.CreatureSlots);
        var opponentEmptySlots = GetEmptySlotCount(playerSlots);

        foreach (var card in playableCards.OrderByDescending(c => GetCardValue(c, currentMana, playerSlots)))
        {
            if (card.Type == CardType.Creature && myEmptySlots > 0)
            {
                if (card.Power >= 4 || threats.Count == 0)
                {
                    var slot = FindBestSlotForCreature(deck, card, playerSlots);
                    if (slot >= 0)
                    {
                        return new AIDecision
                        {
                            Type = AIDecisionType.PlayCreature,
                            CardId = card.Id,
                            SlotIndex = slot
                        };
                    }
                }
            }
            else if (card.Type == CardType.Spell && threats.Count > 0)
            {
                var killableThreat = threats.FirstOrDefault(t => t.Health <= card.Power);
                if (killableThreat != null)
                {
                    return new AIDecision
                    {
                        Type = AIDecisionType.PlaySpell,
                        CardId = card.Id,
                        TargetCard = killableThreat
                    };
                }
            }
        }

        var cheapCreatures = playableCards
            .Where(c => c.Type == CardType.Creature && c.ManaCost <= 2)
            .ToList();

        if (cheapCreatures.Count > 0 && myEmptySlots > 0)
        {
            var best = cheapCreatures.First();
            var slot = FindEmptySlot(deck.CreatureSlots);
            return new AIDecision
            {
                Type = AIDecisionType.PlayCreature,
                CardId = best.Id,
                SlotIndex = slot
            };
        }

        return new AIDecision { Type = AIDecisionType.Pass };
    }

    private double GetCardValue(Card card, int currentMana, List<CreatureSlot> playerSlots)
    {
        double baseValue = card.Power + (card.Health / 2.0);

        if (card.Type == CardType.Creature)
        {
            if (currentMana >= card.ManaCost + 3)
                baseValue += 3;
        }
        else if (card.Type == CardType.Spell)
        {
            var threats = playerSlots.Where(s => s.Creature != null).ToList();
            if (threats.Count > 0)
            {
                var killCount = threats.Count(s => s.Creature!.Health <= card.Power);
                baseValue += killCount * 5;
            }
            baseValue += 2;
        }

        return baseValue;
    }

    private int FindEmptySlot(List<CreatureSlot> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty)
                return i;
        }
        return -1;
    }

    private int GetEmptySlotCount(List<CreatureSlot> slots)
    {
        return slots.Count(s => s.IsEmpty);
    }

    private int FindBestSlotForCreature(DeckManager deck, Card creature, List<CreatureSlot> playerSlots)
    {
        var emptySlots = deck.CreatureSlots
            .Select((s, i) => new { Slot = s, Index = i })
            .Where(x => x.Slot.IsEmpty)
            .ToList();

        if (emptySlots.Count == 0)
            return -1;

        var opponentThreats = playerSlots
            .Select((s, i) => new { Slot = s, Index = i })
            .Where(x => x.Slot.Creature != null)
            .ToList();

        if (opponentThreats.Count == 0)
            return emptySlots.First().Index;

        foreach (var threat in opponentThreats)
        {
            if (creature.Power >= threat.Slot.Creature!.Health)
            {
                return threat.Index < emptySlots.Count ? emptySlots[threat.Index].Index : emptySlots.First().Index;
            }
        }

        return emptySlots.First().Index;
    }

    private Card? FindBestSpellTarget(Card spell, List<CreatureSlot> playerSlots, DeckManager deck)
    {
        var targets = playerSlots
            .Where(s => s.Creature != null)
            .Select(s => s.Creature!)
            .ToList();

        if (targets.Count == 0)
            return null;

        if (spell.Power > 0)
        {
            var killable = targets.FirstOrDefault(t => t.Health <= spell.Power);
            if (killable != null)
                return killable;

            return targets.OrderByDescending(t => t.Power).First();
        }

        return targets.First();
    }

    public int CalculateBestSlot(Card creature, List<CreatureSlot> playerSlots, List<CreatureSlot> mySlots)
    {
        var emptySlots = mySlots
            .Select((s, i) => new { Slot = s, Index = i })
            .Where(x => x.Slot.IsEmpty)
            .Select(x => x.Index)
            .ToList();

        if (emptySlots.Count == 0)
            return -1;

        var threats = playerSlots
            .Select((s, i) => new { Slot = s, Index = i })
            .Where(x => x.Slot.Creature != null)
            .ToList();

        if (threats.Count == 0)
            return emptySlots.First();

        foreach (var threat in threats.Where(t => t.Slot.Creature!.Health <= creature.Power))
        {
            if (threat.Index < emptySlots.Count)
                return emptySlots[threat.Index];
        }

        foreach (var threat in threats.OrderByDescending(t => t.Slot.Creature!.Power))
        {
            if (threat.Index < emptySlots.Count)
                return emptySlots[threat.Index];
        }

        return emptySlots.First();
    }

    public static DeckManager CreateDeckForDifficulty(DifficultyLevel difficulty)
    {
        ErrorLogger.Instance.Debug("OpponentAI", $"[Operation: CreateDeckForDifficulty] Creating deck for difficulty: {difficulty}");
        try
        {
            var deck = new DeckManager();
            var cards = CardFactory.CreateStarterDeck();
            var filtered = difficulty switch
            {
                DifficultyLevel.Apprentice => cards.Where(c => c.ManaCost <= 3).ToList(),
                DifficultyLevel.Journeyman => cards.Where(c => c.ManaCost <= 5).ToList(),
                DifficultyLevel.Expert => cards,
                DifficultyLevel.Grandmaster => cards,
                _ => cards
            };

            var random = new Random();
            var deckCards = new List<Card>();
            int size = difficulty switch
            {
                DifficultyLevel.Apprentice => random.Next(8, 12),
                DifficultyLevel.Journeyman => random.Next(10, 15),
                DifficultyLevel.Expert => random.Next(12, 18),
                DifficultyLevel.Grandmaster => random.Next(15, 22),
                _ => random.Next(11, 20)
            };

            for (int i = 0; i < size && i < filtered.Count * 3; i++)
            {
                var template = filtered[random.Next(filtered.Count)];
                deckCards.Add(template.Clone());
            }

            deck.InitializeDeck(deckCards);
            ErrorLogger.Instance.Info("OpponentAI", $"[Operation: CreateDeckForDifficulty] Created deck with {deckCards.Count} cards for {difficulty}");
            return deck;
        }
        catch (Exception ex)
        {
            ErrorLogger.Instance.Error("OpponentAI", $"[Operation: CreateDeckForDifficulty] Failed to create deck for {difficulty}", ex);
            throw;
        }
    }
}
