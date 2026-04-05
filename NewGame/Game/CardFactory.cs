using Card = MagicalDeckbuilder.Cards.Card;
using CardType = MagicalDeckbuilder.Cards.CardType;
using ElementType = MagicalDeckbuilder.Cards.ElementType;
using EffectType = MagicalDeckbuilder.Cards.EffectType;
using TargetType = MagicalDeckbuilder.Cards.TargetType;
using MagicalDeckbuilder.Cards;

namespace MagicalDeckbuilder.Game;

/// <summary>
/// Factory for creating game cards
/// </summary>
public static class CardFactory
{
    /// <summary>
    /// Create a starter deck with a variety of cards
    /// </summary>
    public static List<Card> CreateStarterDeck()
    {
        var deck = new List<Card>();
        
        // Fire creatures
        deck.Add(CreateCreature("Ember Wolf", "A fierce wolf made of living fire", 
            ElementType.Fire, 3, 4, 3));
        deck.Add(CreateCreature("Flame Sprite", "A mischievous fire elemental", 
            ElementType.Fire, 2, 2, 2));
        deck.Add(CreateCreature("Phoenix Hatchling", "A young phoenix, not yet at full power", 
            ElementType.Fire, 4, 5, 4));
        
        // Water creatures
        deck.Add(CreateCreature("Frost Serpent", "A serpent made of glacial ice", 
            ElementType.Water, 3, 3, 4));
        deck.Add(CreateCreature("Tidal Guardian", "Protector of waterways", 
            ElementType.Water, 4, 4, 5));
        
        // Earth creatures
        deck.Add(CreateCreature("Stone Golem", "A massive golem of living rock", 
            ElementType.Earth, 4, 6, 6));
        deck.Add(CreateCreature("Moss Spirit", "A small nature spirit", 
            ElementType.Earth, 2, 2, 3));
        
        // Air creatures
        deck.Add(CreateCreature("Wind Hawk", "A swift bird of prey", 
            ElementType.Air, 3, 3, 2));
        deck.Add(CreateCreature("Storm Drake", "A drake that controls lightning", 
            ElementType.Air, 5, 5, 4));
        
        // Light creatures
        deck.Add(CreateCreature("Solar Angel", "A being of pure light", 
            ElementType.Light, 5, 4, 5));
        
        // Dark creatures
        deck.Add(CreateCreature("Shadow Wraith", "A幽灵 of darkness", 
            ElementType.Dark, 3, 2, 4));
        
        // Nature creatures
        deck.Add(CreateCreature("Forest Ent", "Ancient guardian of the woods", 
            ElementType.Nature, 4, 5, 6));
        deck.Add(CreateCreature("Vine Spider", "A spider with plant-like abilities", 
            ElementType.Nature, 2, 2, 3));
        
        // Fire spells
        deck.Add(CreateSpell("Fireball", "Deals damage to a target", 
            ElementType.Fire, 2, 5));
        deck.Add(CreateSpell("Inferno", "Deals massive fire damage", 
            ElementType.Fire, 4, 10));
        
        // Water spells
        deck.Add(CreateHealingSpell("Healing Wave", "Restores health", 
            ElementType.Water, 2, 5));
        deck.Add(CreateSpell("Frost Nova", "Freezes enemies", 
            ElementType.Water, 3, 4));
        
        // Arcane spells
        deck.Add(CreateSpell("Arcane Blast", "Pure magical energy", 
            ElementType.Arcane, 3, 6));
        deck.Add(CreateSpell("Mind Control", "Take control of an enemy", 
            ElementType.Arcane, 5, 0));
        
        // Nature spells
        deck.Add(CreateHealingSpell("Regrowth", "Restore health over time", 
            ElementType.Nature, 2, 3));
        deck.Add(CreateSpell("Entangle", "Immobilize enemies with vines", 
            ElementType.Nature, 3, 0));
        
        // Artifacts
        deck.Add(CreateArtifact("Flame Amulet", "Grants fire resistance", 
            ElementType.Fire, 2));
        deck.Add(CreateArtifact("Crystal Orb", "Amplifies magic", 
            ElementType.Arcane, 3));
        
        // Events
        deck.Add(CreateEvent("Mana Potion", "Recover mana", 
            ElementType.Arcane, 1, 2));
        deck.Add(CreateEvent("Quick Draw", "Draw extra cards", 
            ElementType.Air, 2, 1));
        
        // Blanks (for combining)
        deck.Add(CreateBlank("Unstable Fragment", "A raw fragment of magical energy", 
            ElementType.Arcane, 1));
        deck.Add(CreateBlank("Raw Essence", "Unrefined magical essence", 
            ElementType.Nature, 1));
        
        return deck;
    }
    
    private static Card CreateCreature(string name, string description, 
        ElementType element, int manaCost, int power, int health)
    {
        return new MagicalDeckbuilder.Cards.CreatureCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Power = power,
            Health = health,
            Rarity = 1,
            Effects = new List<CardEffect>
            {
                new CardEffect
                {
                    Name = "Attack",
                    Description = "Can attack each turn",
                    Type = EffectType.Damage,
                    Value = power,
                    Target = TargetType.Enemy
                }
            }
        };
    }
    
    private static Card CreateSpell(string name, string description,
        ElementType element, int manaCost, int power)
    {
        var spell = new MagicalDeckbuilder.Cards.SpellCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Power = power,
            Rarity = 1
        };
        
        if (power > 0)
        {
            spell.Effects.Add(new CardEffect
            {
                Name = "Effect",
                Description = description,
                Type = EffectType.Damage,
                Value = power,
                Target = TargetType.Enemy
            });
        }
        
        return spell;
    }
    
    private static Card CreateArtifact(string name, string description,
        ElementType element, int manaCost)
    {
        return new MagicalDeckbuilder.Cards.ArtifactCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Rarity = 1
        };
    }
    
    /// <summary>
    /// Create a simple starter deck for quick testing
    /// </summary>
    public static List<Card> CreateSimpleDeck()
    {
        var deck = new List<Card>();
        
        // 5 creatures
        deck.Add(CreateCreature("Ember Wolf", "A fierce wolf made of living fire", 
            ElementType.Fire, 3, 4, 3));
        deck.Add(CreateCreature("Frost Serpent", "A serpent made of glacial ice", 
            ElementType.Water, 3, 3, 4));
        deck.Add(CreateCreature("Stone Golem", "A massive golem of living rock", 
            ElementType.Earth, 4, 6, 6));
        deck.Add(CreateCreature("Wind Hawk", "A swift bird of prey", 
            ElementType.Air, 3, 3, 2));
        deck.Add(CreateCreature("Forest Ent", "Ancient guardian of the woods", 
            ElementType.Nature, 4, 5, 6));
        
        // 5 spells (with various effects)
        deck.Add(CreateSpell("Fireball", "Deals damage to a target", 
            ElementType.Fire, 2, 5));
        deck.Add(CreateHealingSpell("Healing Wave", "Restores health", 
            ElementType.Water, 2, 5));
        deck.Add(CreateSpell("Arcane Blast", "Pure magical energy", 
            ElementType.Arcane, 3, 6));
        deck.Add(CreateHealingSpell("Regrowth", "Restore health over time", 
            ElementType.Nature, 2, 3));
        deck.Add(CreateSpell("Lightning Bolt", "Strike with lightning", 
            ElementType.Air, 2, 4));
        
        // 2 artifacts
        deck.Add(CreateArtifact("Flame Amulet", "Grants fire resistance", 
            ElementType.Fire, 2));
        deck.Add(CreateArtifact("Crystal Orb", "Amplifies magic", 
            ElementType.Arcane, 3));
        
        // 2 events
        deck.Add(CreateEvent("Mana Potion", "Recover mana", 
            ElementType.Arcane, 1, 2));
        deck.Add(CreateEvent("Quick Draw", "Draw extra cards", 
            ElementType.Air, 2, 1));
        
        return deck;
    }
    
    private static Card CreateHealingSpell(string name, string description,
        ElementType element, int manaCost, int power)
    {
        var spell = new MagicalDeckbuilder.Cards.SpellCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Power = power,
            Rarity = 1
        };
        
        spell.Effects.Add(new CardEffect
        {
            Name = "Heal",
            Description = description,
            Type = EffectType.Heal,
            Value = power,
            Target = TargetType.Self
        });
        
        return spell;
    }
    
    private static Card CreateEvent(string name, string description,
        ElementType element, int manaCost, int effectValue)
    {
        var eventCard = new MagicalDeckbuilder.Cards.EventCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Power = effectValue,
            Rarity = 1
        };
        
        // Determine effect based on name
        if (name.Contains("Mana"))
        {
            eventCard.Effects.Add(new CardEffect
            {
                Name = "Mana Gain",
                Description = "Increase max mana",
                Type = EffectType.ManaGain,
                Value = effectValue,
                Target = TargetType.Self
            });
        }
        else if (name.Contains("Draw"))
        {
            eventCard.Effects.Add(new CardEffect
            {
                Name = "Draw",
                Description = "Draw additional cards",
                Type = EffectType.DrawCard,
                Value = effectValue,
                Target = TargetType.Self
            });
        }
        
        return eventCard;
    }
    
    private static Card CreateBlank(string name, string description,
        ElementType element, int manaCost)
    {
        var blank = new MagicalDeckbuilder.Cards.BlankCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Rarity = 1,
            Power = 1
        };
        
        // Blanks have a small random effect
        blank.Effects.Add(new CardEffect
        {
            Name = "Infusion",
            Description = "Can be used to enhance other cards",
            Type = EffectType.Buff,
            Value = 1,
            Target = TargetType.Self
        });
        
        return blank;
    }
}