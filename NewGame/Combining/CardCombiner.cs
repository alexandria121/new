using Card = MagicalDeckbuilder.Cards.Card;
using CardType = MagicalDeckbuilder.Cards.CardType;
using ElementType = MagicalDeckbuilder.Cards.ElementType;
using EffectType = MagicalDeckbuilder.Cards.EffectType;
using TargetType = MagicalDeckbuilder.Cards.TargetType;
using MagicalDeckbuilder.Cards;

namespace MagicalDeckbuilder.Combining;

/// <summary>
/// Result of attempting to combine cards
/// </summary>
public class CombinationResult
{
    public bool Success { get; set; }
    public Card? ResultCard { get; set; }
    public string Message { get; set; } = "";
    public List<CardEffect> NewEffects { get; set; } = new();
}

/// <summary>
/// Handles card combination and creation mechanics
/// </summary>
public class CardCombiner
{
    private readonly Random _random = new();
    
    // Elemental synergy bonuses
    private static readonly Dictionary<(ElementType, ElementType), string> ElementSynergies = new()
    {
        { (ElementType.Fire, ElementType.Air), "Explosive" },
        { (ElementType.Fire, ElementType.Water), "Steam" },
        { (ElementType.Fire, ElementType.Earth), "Magma" },
        { (ElementType.Water, ElementType.Earth), "Nature" },
        { (ElementType.Water, ElementType.Air), "Storm" },
        { (ElementType.Earth, ElementType.Air), "Dust" },
        { (ElementType.Light, ElementType.Dark), "Twilight" },
        { (ElementType.Light, ElementType.Arcane), "Cosmic" },
        { (ElementType.Dark, ElementType.Arcane), "Void" },
        { (ElementType.Nature, ElementType.Water), "Life" },
        { (ElementType.Nature, ElementType.Fire), "Wildfire" },
    };
    
    /// <summary>
    /// Combine two cards to create a new custom card
    /// </summary>
    public CombinationResult Combine(Card card1, Card card2)
    {
        var result = new CombinationResult();
        
        // Check if cards can be combined
        if (card1.Type == CardType.Creature && card2.Type == CardType.Creature)
        {
            return CombineCreatures(card1, card2);
        }
        else if (card1.Type == CardType.Spell && card2.Type == CardType.Spell)
        {
            return CombineSpells(card1, card2);
        }
        else if (card1.Type == CardType.Artifact && card2.Type == CardType.Artifact)
        {
            return CombineArtifacts(card1, card2);
        }
        else if (card1.Type == CardType.Event && card2.Type == CardType.Event)
        {
            return CombineEvents(card1, card2);
        }
        else if (card1.Type == CardType.Blank || card2.Type == CardType.Blank)
        {
            return CombineWithBlank(card1.Type == CardType.Blank ? card2 : card1, 
                                   card1.Type == CardType.Blank ? card1 : card2);
        }
        else if (card1.Type == CardType.Spell && card2.Type == CardType.Creature ||
                 card1.Type == CardType.Creature && card2.Type == CardType.Spell)
        {
            return EnchantCreature(card1.Type == CardType.Spell ? card1 : card2, 
                                   card2.Type == CardType.Creature ? card2 : card1);
        }
        
        result.Success = false;
        result.Message = "These cards cannot be combined.";
        return result;
    }
    
    /// <summary>
    /// Combine two creatures into a hybrid creature
    /// </summary>
    private CombinationResult CombineCreatures(Card card1, Card card2)
    {
        var result = new CombinationResult();
        
        // Only same element or complementary elements can combine
        if (card1.Element != card2.Element && !HasSynergy(card1.Element, card2.Element))
        {
            result.Success = false;
            result.Message = "These creatures have no elemental synergy.";
            return result;
        }
        
        var creature1 = (MagicalDeckbuilder.Cards.CreatureCard)card1;
        var creature2 = (MagicalDeckbuilder.Cards.CreatureCard)card2;
        
        // Create hybrid creature
        var hybrid = new MagicalDeckbuilder.Cards.CreatureCard
        {
            Name = $"Hybrid {card1.Name}/{card2.Name}",
            Description = $"A powerful fusion of {card1.Name} and {card2.Name}.",
            Element = GetSynergyElement(card1.Element, card2.Element),
            ManaCost = Math.Max(card1.ManaCost, card2.ManaCost) + 1,
            Power = (card1.Power + card2.Power) / 2 + _random.Next(1, 4),
            Health = (card1.Health + card2.Health) / 2 + _random.Next(1, 3),
            Rarity = Math.Max(card1.Rarity, card2.Rarity) + 1,
            IsLegendary = card1.IsLegendary || card2.IsLegendary
        };
        
        // Combine effects
        hybrid.Effects.AddRange(card1.Effects);
        hybrid.Effects.AddRange(card2.Effects);
        
        result.Success = true;
        result.ResultCard = hybrid;
        result.Message = $"Created {hybrid.Name}! Power: {hybrid.Power}, Health: {hybrid.Health}";
        return result;
    }
    
    /// <summary>
    /// Combine two spells into a more powerful spell
    /// </summary>
    private CombinationResult CombineSpells(Card card1, Card card2)
    {
        var result = new CombinationResult();
        
        var combinedSpell = new MagicalDeckbuilder.Cards.SpellCard
        {
            Name = $"Greater {card1.Name}",
            Description = $"A powerful combination of {card1.Name} and {card2.Name}.",
            Element = GetSynergyElement(card1.Element, card2.Element),
            ManaCost = Math.Max(card1.ManaCost, card2.ManaCost) + 2,
            Power = card1.Power + card2.Power + _random.Next(1, 5),
            Rarity = Math.Max(card1.Rarity, card2.Rarity) + 1,
            IsLegendary = card1.IsLegendary && card2.IsLegendary
        };
        
        // Combine effects with bonus
        foreach (var effect in card1.Effects)
        {
            combinedSpell.Effects.Add(new CardEffect
            {
                Name = effect.Name,
                Description = effect.Description,
                Type = effect.Type,
                Value = effect.Value + (effect.Value / 2),
                Target = effect.Target,
                IsTemporary = effect.IsTemporary
            });
        }
        
        foreach (var effect in card2.Effects)
        {
            combinedSpell.Effects.Add(new CardEffect
            {
                Name = effect.Name,
                Description = effect.Description,
                Type = effect.Type,
                Value = effect.Value + (effect.Value / 2),
                Target = effect.Target,
                IsTemporary = effect.IsTemporary
            });
        }
        
        result.Success = true;
        result.ResultCard = combinedSpell;
        result.Message = $"Created {combinedSpell.Name}! Power: {combinedSpell.Power}";
        return result;
    }
    
    /// <summary>
    /// Combine two artifacts into a more powerful artifact
    /// </summary>
    private CombinationResult CombineArtifacts(Card card1, Card card2)
    {
        var result = new CombinationResult();
        
        var combinedArtifact = new MagicalDeckbuilder.Cards.ArtifactCard
        {
            Name = $"Enhanced {card1.Name}",
            Description = $"A superior version combining {card1.Name} and {card2.Name}.",
            Element = GetSynergyElement(card1.Element, card2.Element),
            ManaCost = Math.Max(card1.ManaCost, card2.ManaCost),
            Rarity = Math.Max(card1.Rarity, card2.Rarity) + 1,
            IsLegendary = card1.IsLegendary || card2.IsLegendary
        };
        
        // Combine effects
        combinedArtifact.Effects.AddRange(card1.Effects);
        combinedArtifact.Effects.AddRange(card2.Effects);
        
        result.Success = true;
        result.ResultCard = combinedArtifact;
        result.Message = $"Created {combinedArtifact.Name}!";
        return result;
    }
    
    /// <summary>
    /// Combine two event cards into a more powerful event
    /// </summary>
    private CombinationResult CombineEvents(Card card1, Card card2)
    {
        var result = new CombinationResult();
        
        var combinedEvent = new MagicalDeckbuilder.Cards.EventCard
        {
            Name = $"Enhanced {card1.Name}",
            Description = $"A powerful combination of {card1.Name} and {card2.Name}.",
            Element = GetSynergyElement(card1.Element, card2.Element),
            ManaCost = Math.Max(card1.ManaCost, card2.ManaCost) + 1,
            Power = card1.Power + card2.Power + _random.Next(1, 3),
            Rarity = Math.Max(card1.Rarity, card2.Rarity) + 1,
            IsLegendary = card1.IsLegendary && card2.IsLegendary
        };
        
        // Combine effects with bonus
        foreach (var effect in card1.Effects)
        {
            combinedEvent.Effects.Add(new CardEffect
            {
                Name = effect.Name,
                Description = effect.Description,
                Type = effect.Type,
                Value = effect.Value + (effect.Value / 2),
                Target = effect.Target,
                IsTemporary = effect.IsTemporary
            });
        }
        
        foreach (var effect in card2.Effects)
        {
            combinedEvent.Effects.Add(new CardEffect
            {
                Name = effect.Name,
                Description = effect.Description,
                Type = effect.Type,
                Value = effect.Value + (effect.Value / 2),
                Target = effect.Target,
                IsTemporary = effect.IsTemporary
            });
        }
        
        result.Success = true;
        result.ResultCard = combinedEvent;
        result.Message = $"Created {combinedEvent.Name}! Power: {combinedEvent.Power}";
        return result;
    }
    
    /// <summary>
    /// Combine a blank card with another card to enhance it
    /// </summary>
    private CombinationResult CombineWithBlank(Card normalCard, Card blankCard)
    {
        var result = new CombinationResult();
        
        // Blank adds a random boost to the other card
        var boostedCard = normalCard.Clone();
        boostedCard.Name = $"Enhanced {normalCard.Name}";
        boostedCard.Description = $"{normalCard.Description} Infused with additional power!";
        
        // Add random bonus based on blank's element
        var bonus = _random.Next(1, 4);
        
        if (boostedCard.Type == CardType.Creature)
        {
            boostedCard.Power += bonus;
            boostedCard.Health += bonus;
        }
        else
        {
            boostedCard.Power += bonus;
        }
        
        boostedCard.Rarity = Math.Min(boostedCard.Rarity + 1, 4);
        
        result.Success = true;
        result.ResultCard = boostedCard;
        result.Message = $"Created {boostedCard.Name}! +{bonus} to stats";
        return result;
    }
    
    /// <summary>
    /// Enchant a creature with a spell
    /// </summary>
    private CombinationResult EnchantCreature(Card spell, Card creature)
    {
        var result = new CombinationResult();
        
        var enchantedCreature = new MagicalDeckbuilder.Cards.CreatureCard
        {
            Name = $"Enchanted {creature.Name}",
            Description = $"{creature.Description} Now enhanced with {spell.Name}!",
            Element = GetSynergyElement(creature.Element, spell.Element),
            ManaCost = creature.ManaCost + spell.ManaCost,
            Power = creature.Power + spell.Power,
            Health = creature.Health,
            Rarity = Math.Max(creature.Rarity, spell.Rarity) + 1,
            IsLegendary = creature.IsLegendary || spell.IsLegendary
        };
        
        // Add creature's original effects plus spell effects as buffs
        foreach (var effect in creature.Effects)
        {
            enchantedCreature.Effects.Add(effect);
        }
        
        foreach (var effect in spell.Effects)
        {
            enchantedCreature.Effects.Add(new CardEffect
            {
                Name = $"Enchanted: {effect.Name}",
                Description = effect.Description,
                Type = EffectType.Buff,
                Value = effect.Value,
                Target = TargetType.Self,
                IsTemporary = false
            });
        }
        
        result.Success = true;
        result.ResultCard = enchantedCreature;
        result.Message = $"Created {enchantedCreature.Name}! Power: {enchantedCreature.Power}";
        return result;
    }
    
    /// <summary>
    /// Check if two elements have synergy
    /// </summary>
    private bool HasSynergy(ElementType elem1, ElementType elem2)
    {
        return ElementSynergies.ContainsKey((elem1, elem2)) || 
               ElementSynergies.ContainsKey((elem2, elem1));
    }
    
    /// <summary>
    /// Get the synergy element when combining
    /// </summary>
    private ElementType GetSynergyElement(ElementType elem1, ElementType elem2)
    {
        if (ElementSynergies.TryGetValue((elem1, elem2), out var synergy))
        {
            return GetElementFromSynergy(synergy);
        }
        if (ElementSynergies.TryGetValue((elem2, elem1), out synergy))
        {
            return GetElementFromSynergy(synergy);
        }
        return elem1; // Default to first element
    }
    
    private ElementType GetElementFromSynergy(string synergy)
    {
        return synergy switch
        {
            "Explosive" => ElementType.Fire,
            "Steam" => ElementType.Water,
            "Magma" => ElementType.Fire,
            "Nature" => ElementType.Nature,
            "Storm" => ElementType.Water,
            "Dust" => ElementType.Earth,
            "Twilight" => ElementType.Dark,
            "Cosmic" => ElementType.Arcane,
            "Void" => ElementType.Dark,
            "Life" => ElementType.Nature,
            "Wildfire" => ElementType.Fire,
            _ => ElementType.Arcane
        };
    }
    
    /// <summary>
    /// Display potential combinations for a list of cards
    /// </summary>
    public List<(Card, Card, string)> GetPotentialCombinations(List<Card> cards)
    {
        var combinations = new List<(Card, Card, string)>();
        
        for (int i = 0; i < cards.Count; i++)
        {
            for (int j = i + 1; j < cards.Count; j++)
            {
                string description = GetCombinationDescription(cards[i], cards[j]);
                if (description != "")
                {
                    combinations.Add((cards[i], cards[j], description));
                }
            }
        }
        
        return combinations;
    }
    
    private string GetCombinationDescription(Card card1, Card card2)
    {
        // Check for blank first
        if (card1.Type == CardType.Blank || card2.Type == CardType.Blank)
        {
            var normalCard = card1.Type == CardType.Blank ? card2 : card1;
            return $"Enhance {normalCard.Name} with Blank";
        }
        
        if (card1.Type == CardType.Creature && card2.Type == CardType.Creature)
        {
            return HasSynergy(card1.Element, card2.Element) 
                ? $"Fusion: {card1.Name} + {card2.Name}" 
                : "";
        }
        else if (card1.Type == CardType.Spell && card2.Type == CardType.Spell)
        {
            return "Combine Spells";
        }
        else if (card1.Type == CardType.Artifact && card2.Type == CardType.Artifact)
        {
            return "Combine Artifacts";
        }
        else if (card1.Type == CardType.Event && card2.Type == CardType.Event)
        {
            return "Combine Events";
        }
        else if ((card1.Type == CardType.Spell && card2.Type == CardType.Creature) ||
                 (card1.Type == CardType.Creature && card2.Type == CardType.Spell))
        {
            var spell = card1.Type == CardType.Spell ? card1 : card2;
            var creature = card1.Type == CardType.Creature ? card1 : card2;
            return $"Enchant {creature.Name} with {spell.Name}";
        }
        
        return "";
    }
}