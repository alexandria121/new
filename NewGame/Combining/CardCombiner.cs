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
/// Implements the combination system:
/// - Any card can combine with any card, EXCEPT events cannot be combined
/// - When two cards combine, they become better than before (but not 100% of both)
/// - Creature + Creature = Bigger creature
/// - Creature + Spell = Enchanted creature (buffs, negative effects on hit)
/// - Creature + Artifact = Equipped creature
/// - Spell + Spell = Augmented spell (positive+positive=better, pos+neg=double-edged, neg+neg=stronger)
/// - Spell + Artifact = Magical item that can cast the spell multiple times
/// - Artifact + Artifact = Mega artifact with combined effects
/// - Blank + Any other = That card type with extra effect from blank
/// - Blank + Blank = N/A (cannot combine)
/// - Event + Any = N/A (cannot combine)
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
    
    // Combination efficiency - results are better than either card alone but not 100% of both
    private const double CombinationEfficiency = 0.6; // 60% of each card's stats combined
    
    /// <summary>
    /// Combine two cards to create a new custom card
    /// Events cannot be combined with anything
    /// </summary>
    public CombinationResult Combine(Card card1, Card card2)
    {
        var result = new CombinationResult();
        
        // Rule: Events cannot be combined with ANY card
        if (card1.Type == CardType.Event || card2.Type == CardType.Event)
        {
            result.Success = false;
            result.Message = "Events cannot be combined.";
            return result;
        }
        
        // Rule: Blank + Blank = N/A (cannot combine)
        if (card1.Type == CardType.Blank && card2.Type == CardType.Blank)
        {
            result.Success = false;
            result.Message = "Blank cards cannot be combined with each other.";
            return result;
        }
        
        // Handle Blank + Other card
        if (card1.Type == CardType.Blank || card2.Type == CardType.Blank)
        {
            return CombineWithBlank(
                card1.Type == CardType.Blank ? card2 : card1,
                card1.Type == CardType.Blank ? card1 : card2
            );
        }
        
        // Creature + Creature = Bigger creature (hybrid)
        if (card1.Type == CardType.Creature && card2.Type == CardType.Creature)
        {
            return CombineCreatures(card1, card2);
        }
        
        // Creature + Spell = Enchanted creature
        if ((card1.Type == CardType.Creature && card2.Type == CardType.Spell) ||
            (card1.Type == CardType.Spell && card2.Type == CardType.Creature))
        {
            return EnchantCreature(
                card1.Type == CardType.Spell ? card1 : card2,
                card1.Type == CardType.Creature ? card1 : card2
            );
        }
        
        // Creature + Artifact = Equipped creature
        if ((card1.Type == CardType.Creature && card2.Type == CardType.Artifact) ||
            (card1.Type == CardType.Artifact && card2.Type == CardType.Creature))
        {
            return EquipCreature(
                card1.Type == CardType.Artifact ? card1 : card2,
                card1.Type == CardType.Creature ? card1 : card2
            );
        }
        
        // Spell + Spell = Augmented spell
        if (card1.Type == CardType.Spell && card2.Type == CardType.Spell)
        {
            return CombineSpells(card1, card2);
        }
        
        // Spell + Artifact = Magical item (can cast spell multiple times)
        if ((card1.Type == CardType.Spell && card2.Type == CardType.Artifact) ||
            (card1.Type == CardType.Artifact && card2.Type == CardType.Spell))
        {
            return CreateMagicalItem(
                card1.Type == CardType.Spell ? card1 : card2,
                card1.Type == CardType.Artifact ? card1 : card2
            );
        }
        
        // Artifact + Artifact = Mega artifact
        if (card1.Type == CardType.Artifact && card2.Type == CardType.Artifact)
        {
            return CombineArtifacts(card1, card2);
        }
        
        result.Success = false;
        result.Message = "These cards cannot be combined.";
        return result;
    }
    
    /// <summary>
    /// Combine two creatures into a bigger creature (hybrid)
    /// Uses combination efficiency - result is better than either but not 100% of both
    /// </summary>
    private CombinationResult CombineCreatures(Card card1, Card card2)
    {
        var result = new CombinationResult();
        
        // Note: Current implementation allows any elements to combine, but gives bonus for synergies
        
        var creature1 = (MagicalDeckbuilder.Cards.CreatureCard)card1;
        var creature2 = (MagicalDeckbuilder.Cards.CreatureCard)card2;
        
        // Calculate combined stats using efficiency formula
        // Not quite 100% of both - e.g., Goblin (2/2) + Goblin (2/2) ≠ 4/4, it's closer to 3/3
        int combinedPower = (int)((card1.Power + card2.Power) * CombinationEfficiency) + _random.Next(1, 3);
        int combinedHealth = (int)((card1.Health + card2.Health) * CombinationEfficiency) + _random.Next(1, 3);
        
        // Create hybrid creature - bigger than either parent
        var hybrid = new MagicalDeckbuilder.Cards.CreatureCard
        {
            Name = $"{card1.Name}/{card2.Name} Hybrid",
            Description = $"A powerful fusion of {card1.Name} and {card2.Name}. " +
                         $"Has the combined strengths of both creatures.",
            Element = GetSynergyElement(card1.Element, card2.Element),
            ManaCost = Math.Max(card1.ManaCost, card2.ManaCost) + 1,
            Power = Math.Max(combinedPower, Math.Max(card1.Power, card2.Power) + 1), // Bigger than either
            Health = Math.Max(combinedHealth, Math.Max(card1.Health, card2.Health) + 1), // Bigger than either
            Rarity = Math.Max(card1.Rarity, card2.Rarity) + 1,
            IsLegendary = card1.IsLegendary || card2.IsLegendary
        };
        
        // Combine effects - but scale values down a bit
        foreach (var effect in card1.Effects)
        {
            hybrid.Effects.Add(new CardEffect
            {
                Name = effect.Name,
                Description = effect.Description,
                Type = effect.Type,
                Value = (int)(effect.Value * CombinationEfficiency),
                Target = effect.Target,
                IsTemporary = effect.IsTemporary
            });
        }
        
        foreach (var effect in card2.Effects)
        {
            // Don't add duplicate effect names
            if (!hybrid.Effects.Any(e => e.Name == effect.Name))
            {
                hybrid.Effects.Add(new CardEffect
                {
                    Name = effect.Name,
                    Description = effect.Description,
                    Type = effect.Type,
                    Value = (int)(effect.Value * CombinationEfficiency),
                    Target = effect.Target,
                    IsTemporary = effect.IsTemporary
                });
            }
        }
        
        // Add a special hybrid ability
        hybrid.Effects.Add(new CardEffect
        {
            Name = "Hybrid Strength",
            Description = "The combined power of two creatures",
            Type = EffectType.Buff,
            Value = 1,
            Target = TargetType.Self,
            IsTemporary = false
        });
        
        result.Success = true;
        result.ResultCard = hybrid;
        result.Message = $"Created {hybrid.Name}! Power: {hybrid.Power}, Health: {hybrid.Health}";
        return result;
    }
    
    /// <summary>
    /// Enchant a creature with a spell - adds buff effects
    /// The creature keeps its stats but gains magical enhancements
    /// </summary>
    private CombinationResult EnchantCreature(Card spell, Card creature)
    {
        var result = new CombinationResult();
        
        // Calculate enhanced stats - spell power adds to creature
        // Using efficiency: spell contributes ~60% of its power
        int enhancedPower = creature.Power + (int)(spell.Power * CombinationEfficiency);
        
        var enchantedCreature = new MagicalDeckbuilder.Cards.CreatureCard
        {
            Name = $"Enchanted {creature.Name}",
            Description = $"{creature.Name} enhanced with magical enchantments from {spell.Name}. " +
                         $"{spell.Description}",
            Element = GetSynergyElement(creature.Element, spell.Element),
            ManaCost = creature.ManaCost + spell.ManaCost,
            Power = enhancedPower,
            Health = creature.Health, // Health stays the same unless buffed
            Rarity = Math.Max(creature.Rarity, spell.Rarity) + 1,
            IsLegendary = creature.IsLegendary || spell.IsLegendary
        };
        
        // Add creature's original effects
        foreach (var effect in creature.Effects)
        {
            enchantedCreature.Effects.Add(effect);
        }
        
        // Add spell effects as buffs/enchantments
        foreach (var effect in spell.Effects)
        {
            // Convert spell effects to buffs for the creature
            var buffType = effect.Type;
            
            // If it's a damage spell, make it a "on hit" effect instead
            if (effect.Type == EffectType.Damage)
            {
                enchantedCreature.Effects.Add(new CardEffect
                {
                    Name = $"Enchanted: {effect.Name}",
                    Description = $"On attack: {effect.Description}",
                    Type = EffectType.Buff,
                    Value = (int)(effect.Value * CombinationEfficiency), // Reduced for efficiency
                    Target = TargetType.Self,
                    IsTemporary = false
                });
            }
            else
            {
                // Convert other spell effects to buffs
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
        }
        
        // Add enchantment bonus effect
        enchantedCreature.Effects.Add(new CardEffect
        {
            Name = "Magic Enhancement",
            Description = "Powered by magical enchantments",
            Type = EffectType.Buff,
            Value = 1,
            Target = TargetType.Self,
            IsTemporary = false
        });
        
        result.Success = true;
        result.ResultCard = enchantedCreature;
        result.Message = $"Created {enchantedCreature.Name}! Power: {enchantedCreature.Power}";
        return result;
    }
    
    /// <summary>
    /// Equip a creature with an artifact - stats increased according to artifact
    /// </summary>
    private CombinationResult EquipCreature(Card artifact, Card creature)
    {
        var result = new CombinationResult();
        
        // Artifact provides bonus to creature stats
        int powerBonus = 0;
        int healthBonus = 0;
        
        foreach (var effect in artifact.Effects)
        {
            if (effect.Type == EffectType.Buff)
            {
                powerBonus += (int)(effect.Value * CombinationEfficiency);
            }
            else if (effect.Type == EffectType.Shield)
            {
                healthBonus += effect.Value;
            }
            else if (effect.Type == EffectType.Damage)
            {
                powerBonus += (int)(effect.Value * CombinationEfficiency);
            }
        }
        
        // Fallback if no effects - use artifact power directly
        if (powerBonus == 0 && artifact.Power > 0)
        {
            powerBonus = (int)(artifact.Power * CombinationEfficiency);
        }
        
        var equippedCreature = new MagicalDeckbuilder.Cards.CreatureCard
        {
            Name = $"{creature.Name} of {artifact.Name}",
            Description = $"{creature.Name} equipped with {artifact.Name}. {artifact.Description}",
            Element = GetSynergyElement(creature.Element, artifact.Element),
            ManaCost = creature.ManaCost + artifact.ManaCost,
            Power = creature.Power + powerBonus,
            Health = creature.Health + healthBonus,
            Rarity = Math.Max(creature.Rarity, artifact.Rarity) + 1,
            IsLegendary = creature.IsLegendary || artifact.IsLegendary
        };
        
        // Add creature's effects
        foreach (var effect in creature.Effects)
        {
            equippedCreature.Effects.Add(effect);
        }
        
        // Add artifact effects as equipment bonuses
        foreach (var effect in artifact.Effects)
        {
            equippedCreature.Effects.Add(new CardEffect
            {
                Name = $"Equipped: {effect.Name}",
                Description = effect.Description,
                Type = effect.Type,
                Value = effect.Value,
                Target = TargetType.Self,
                IsTemporary = false
            });
        }
        
        // Add equipment bonus
        equippedCreature.Effects.Add(new CardEffect
        {
            Name = "Equipment Bonus",
            Description = "Enhanced by equipped artifact",
            Type = EffectType.Buff,
            Value = 1,
            Target = TargetType.Self,
            IsTemporary = false
        });
        
        result.Success = true;
        result.ResultCard = equippedCreature;
        result.Message = $"Created {equippedCreature.Name}! Power: {equippedCreature.Power}, Health: {equippedCreature.Health}";
        return result;
    }
    
    /// <summary>
    /// Combine two spells into an augmented spell
    /// - Positive + Positive = Better buff
    /// - Positive + Negative = Double-edged sword
    /// - Negative + Negative = Extra strong damage/debuff
    /// </summary>
    private CombinationResult CombineSpells(Card card1, Card card2)
    {
        var result = new CombinationResult();
        
        // Categorize effects as positive or negative
        bool card1Positive = IsPositiveEffect(card1);
        bool card2Positive = IsPositiveEffect(card2);
        
        string spellName;
        string spellDescription;
        
        if (card1Positive && card2Positive)
        {
            // Positive + Positive = Better buff
            spellName = $"Greater {card1.Name}";
            spellDescription = $"A powerful combination of beneficial magic. " +
                             $"Combines the strengths of {card1.Name} and {card2.Name}.";
        }
        else if (!card1Positive && !card2Positive)
        {
            // Negative + Negative = Extra strong damage
            spellName = $"Devastating {card1.Name}";
            spellDescription = $"A devastating combination of destructive magic. " +
                             $"Far more powerful than either spell alone.";
        }
        else
        {
            // Positive + Negative = Double-edged sword
            spellName = $"Cursed {card1.Name}";
            spellDescription = $"A dangerous spell with both beneficial and harmful effects. " +
                             $"Use with caution!";
        }
        
        // Calculate combined power using efficiency
        int combinedPower = (int)((card1.Power + card2.Power) * CombinationEfficiency) + _random.Next(1, 4);
        
        var combinedSpell = new MagicalDeckbuilder.Cards.SpellCard
        {
            Name = spellName,
            Description = spellDescription,
            Element = GetSynergyElement(card1.Element, card2.Element),
            ManaCost = Math.Max(card1.ManaCost, card2.ManaCost) + 2,
            Power = combinedPower,
            Rarity = Math.Max(card1.Rarity, card2.Rarity) + 1,
            IsLegendary = card1.IsLegendary && card2.IsLegendary
        };
        
        // Combine effects with bonuses
        // For positive + positive: boost the values
        // For negative + negative: significantly boost damage
        // For mixed: keep both but scale down slightly
        
        double effectMultiplier = CombinationEfficiency;
        
        if (card1Positive && card2Positive)
        {
            effectMultiplier = CombinationEfficiency + 0.2; // Better bonuses
        }
        else if (!card1Positive && !card2Positive)
        {
            effectMultiplier = CombinationEfficiency + 0.3; // Stronger damage
        }
        
        foreach (var effect in card1.Effects)
        {
            combinedSpell.Effects.Add(new CardEffect
            {
                Name = effect.Name,
                Description = effect.Description,
                Type = effect.Type,
                Value = (int)(effect.Value * effectMultiplier),
                Target = effect.Target,
                IsTemporary = effect.IsTemporary
            });
        }
        
        foreach (var effect in card2.Effects)
        {
            // Don't add duplicate effect types
            if (!combinedSpell.Effects.Any(e => e.Type == effect.Type))
            {
                combinedSpell.Effects.Add(new CardEffect
                {
                    Name = effect.Name,
                    Description = effect.Description,
                    Type = effect.Type,
                    Value = (int)(effect.Value * effectMultiplier),
                    Target = effect.Target,
                    IsTemporary = effect.IsTemporary
                });
            }
        }
        
        result.Success = true;
        result.ResultCard = combinedSpell;
        result.Message = $"Created {combinedSpell.Name}! Power: {combinedSpell.Power}";
        return result;
    }
    
    /// <summary>
    /// Create a magical item from spell + artifact
    /// The resulting artifact can cast the spell multiple times
    /// </summary>
    private CombinationResult CreateMagicalItem(Card spell, Card artifact)
    {
        var result = new CombinationResult();
        
        // Calculate charges based on spell power and artifact (using efficiency)
        int charges = 2 + (int)(spell.Power * CombinationEfficiency / 3);
        
        var magicalItem = new MagicalDeckbuilder.Cards.ArtifactCard
        {
            Name = $"Enchanted {artifact.Name}",
            Description = $"A magical artifact containing {spell.Name}. " +
                         $"Can be used {charges} times. {spell.Description}",
            Element = GetSynergyElement(spell.Element, artifact.Element),
            ManaCost = Math.Max(spell.ManaCost, artifact.ManaCost),
            Power = (int)((spell.Power + artifact.Power) * CombinationEfficiency),
            Rarity = Math.Max(spell.Rarity, artifact.Rarity) + 1,
            IsLegendary = spell.IsLegendary || artifact.IsLegendary
        };
        
        // Add artifact's original effects
        foreach (var effect in artifact.Effects)
        {
            magicalItem.Effects.Add(effect);
        }
        
        // Add spell as a reusable effect (multiple charges)
        magicalItem.Effects.Add(new CardEffect
        {
            Name = $"Stored Spell: {spell.Name}",
            Description = $"Can cast {spell.Name} {charges} times. {spell.Description}",
            Type = spell.Effects.FirstOrDefault()?.Type ?? EffectType.Damage,
            Value = spell.Power,
            Target = TargetType.Any,
            IsTemporary = false
        });
        
        // Add bonus for having stored magic
        magicalItem.Effects.Add(new CardEffect
        {
            Name = "Arcane Storage",
            Description = $"Stores {charges} charges of magical energy",
            Type = EffectType.Buff,
            Value = charges,
            Target = TargetType.Self,
            IsTemporary = false
        });
        
        result.Success = true;
        result.ResultCard = magicalItem;
        result.Message = $"Created {magicalItem.Name}! Stores {charges} charges of {spell.Name}";
        return result;
    }
    
    /// <summary>
    /// Combine two artifacts into a mega artifact
    /// Combined effects create a more powerful item
    /// </summary>
    private CombinationResult CombineArtifacts(Card card1, Card card2)
    {
        var result = new CombinationResult();
        
        var combinedArtifact = new MagicalDeckbuilder.Cards.ArtifactCard
        {
            Name = $"Mega {card1.Name}",
            Description = $"A superior artifact combining the powers of {card1.Name} and {card2.Name}.",
            Element = GetSynergyElement(card1.Element, card2.Element),
            ManaCost = Math.Max(card1.ManaCost, card2.ManaCost),
            Power = (int)((card1.Power + card2.Power) * CombinationEfficiency) + _random.Next(1, 3),
            Rarity = Math.Max(card1.Rarity, card2.Rarity) + 1,
            IsLegendary = card1.IsLegendary || card2.IsLegendary
        };
        
        // Combine effects from both artifacts (scaled down)
        foreach (var effect in card1.Effects)
        {
            combinedArtifact.Effects.Add(new CardEffect
            {
                Name = effect.Name,
                Description = effect.Description,
                Type = effect.Type,
                Value = (int)(effect.Value * CombinationEfficiency),
                Target = effect.Target,
                IsTemporary = effect.IsTemporary
            });
        }
        
        foreach (var effect in card2.Effects)
        {
            // Combine similar effects if possible
            var existingEffect = combinedArtifact.Effects.FirstOrDefault(e => e.Type == effect.Type);
            if (existingEffect != null)
            {
                // Combine similar effects for greater power
                existingEffect.Value += (int)(effect.Value * CombinationEfficiency);
                existingEffect.Description = $"{existingEffect.Description} Also {effect.Description}";
            }
            else
            {
                combinedArtifact.Effects.Add(new CardEffect
                {
                    Name = effect.Name,
                    Description = effect.Description,
                    Type = effect.Type,
                    Value = (int)(effect.Value * CombinationEfficiency),
                    Target = effect.Target,
                    IsTemporary = effect.IsTemporary
                });
            }
        }
        
        // Add mega bonus
        combinedArtifact.Effects.Add(new CardEffect
        {
            Name = "Mega Power",
            Description = "Combined artifact power",
            Type = EffectType.Buff,
            Value = 2,
            Target = TargetType.Self,
            IsTemporary = false
        });
        
        result.Success = true;
        result.ResultCard = combinedArtifact;
        result.Message = $"Created {combinedArtifact.Name}! Power: {combinedArtifact.Power}";
        return result;
    }
    
    /// <summary>
    /// Combine a blank card with another card to enhance it
    /// The blank adds an extra effect to the other card
    /// </summary>
    private CombinationResult CombineWithBlank(Card normalCard, Card blankCard)
    {
        var result = new CombinationResult();
        
        // Create enhanced version of the normal card
        var enhancedCard = normalCard.Clone();
        enhancedCard.Name = $"Enhanced {normalCard.Name}";
        enhancedCard.Description = $"{normalCard.Description} Infused with extra power from blank essence!";
        
        // Add random bonus based on blank's element (using efficiency)
        var bonus = _random.Next(1, 3);
        
        if (enhancedCard.Type == CardType.Creature)
        {
            // Creature gets power and health boost
            enhancedCard.Power += bonus;
            enhancedCard.Health += bonus;
        }
        else if (enhancedCard.Type == CardType.Spell)
        {
            // Spell gets power boost
            enhancedCard.Power += bonus + 1;
        }
        else if (enhancedCard.Type == CardType.Artifact)
        {
            // Artifact gets power boost
            enhancedCard.Power += bonus + 1;
        }
        
        // Add blank's infusion effect as extra
        enhancedCard.Effects.Add(new CardEffect
        {
            Name = "Blank Infusion",
            Description = "Enhanced with raw magical essence",
            Type = EffectType.Buff,
            Value = bonus,
            Target = TargetType.Self,
            IsTemporary = false
        });
        
        enhancedCard.Rarity = Math.Min(enhancedCard.Rarity + 1, 4);
        
        result.Success = true;
        result.ResultCard = enhancedCard;
        result.Message = $"Created {enhancedCard.Name}! +{bonus} to stats";
        return result;
    }
    
    /// <summary>
    /// Check if a card's effects are primarily positive (beneficial)
    /// </summary>
    private bool IsPositiveEffect(Card card)
    {
        foreach (var effect in card.Effects)
        {
            // Consider healing, buffs, card draw, mana gain as positive
            // Consider damage, debuffs as negative
            if (effect.Type == EffectType.Heal ||
                effect.Type == EffectType.Buff ||
                effect.Type == EffectType.DrawCard ||
                effect.Type == EffectType.ManaGain ||
                effect.Type == EffectType.Shield)
            {
                return true;
            }
            if (effect.Type == EffectType.Damage ||
                effect.Type == EffectType.Debuff ||
                effect.Type == EffectType.Destroy)
            {
                return false;
            }
        }
        
        // Default to positive if no clear negative effects
        return card.Power >= 0;
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
        if (elem1 == elem2)
        {
            return elem1; // Same element stays same
        }
        
        if (ElementSynergies.TryGetValue((elem1, elem2), out var synergy))
        {
            return GetElementFromSynergy(synergy);
        }
        if (ElementSynergies.TryGetValue((elem2, elem1), out synergy))
        {
            return GetElementFromSynergy(synergy);
        }
        
        // Default to the higher mana cost card's element
        return elem1;
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
        // Event cards cannot be combined
        if (card1.Type == CardType.Event || card2.Type == CardType.Event)
        {
            return "";
        }
        
        // Blank + Blank = N/A
        if (card1.Type == CardType.Blank && card2.Type == CardType.Blank)
        {
            return "";
        }
        
        // Blank + Other
        if (card1.Type == CardType.Blank || card2.Type == CardType.Blank)
        {
            var normalCard = card1.Type == CardType.Blank ? card2 : card1;
            return $"Enhance {normalCard.Name} with Blank";
        }
        
        // Creature + Creature
        if (card1.Type == CardType.Creature && card2.Type == CardType.Creature)
        {
            return $"Fusion: {card1.Name} + {card2.Name}";
        }
        
        // Creature + Spell
        if ((card1.Type == CardType.Creature && card2.Type == CardType.Spell) ||
            (card1.Type == CardType.Spell && card2.Type == CardType.Creature))
        {
            var spell = card1.Type == CardType.Spell ? card1 : card2;
            var creature = card1.Type == CardType.Creature ? card1 : card2;
            return $"Enchant {creature.Name} with {spell.Name}";
        }
        
        // Creature + Artifact
        if ((card1.Type == CardType.Creature && card2.Type == CardType.Artifact) ||
            (card1.Type == CardType.Artifact && card2.Type == CardType.Creature))
        {
            var artifact = card1.Type == CardType.Artifact ? card1 : card2;
            var creature = card1.Type == CardType.Creature ? card1 : card2;
            return $"Equip {creature.Name} with {artifact.Name}";
        }
        
        // Spell + Spell
        if (card1.Type == CardType.Spell && card2.Type == CardType.Spell)
        {
            return $"Combine Spells: {card1.Name} + {card2.Name}";
        }
        
        // Spell + Artifact
        if ((card1.Type == CardType.Spell && card2.Type == CardType.Artifact) ||
            (card1.Type == CardType.Artifact && card2.Type == CardType.Spell))
        {
            var spell = card1.Type == CardType.Spell ? card1 : card2;
            var artifact = card1.Type == CardType.Artifact ? card1 : card2;
            return $"Create Magical Item: {artifact.Name} with {spell.Name}";
        }
        
        // Artifact + Artifact
        if (card1.Type == CardType.Artifact && card2.Type == CardType.Artifact)
        {
            return $"Mega Artifact: {card1.Name} + {card2.Name}";
        }
        
        return "";
    }
}