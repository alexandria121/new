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

    /// <summary>
    /// Generate a list of new unique cards for the game
    /// </summary>
    public static List<Card> GenerateNewCards()
    {
        var newCards = new List<Card>();

        // Legendary Fire Dragon
        newCards.Add(new CreatureCard
        {
            Name = "Infernal Dragon",
            Description = "A mighty dragon wreathed in eternal flames",
            Element = ElementType.Fire,
            ManaCost = 8,
            Power = 10,
            Health = 10,
            Rarity = 4,
            IsLegendary = true,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Fire Breath", Description = "Deals 5 damage to all enemies", Type = EffectType.Damage, Value = 5, Target = TargetType.AllEnemies },
                new CardEffect { Name = "Regeneration", Description = "Heals 3 HP each turn", Type = EffectType.Heal, Value = 3, Target = TargetType.Self }
            }
        });

        // Rare Water Kraken
        newCards.Add(new CreatureCard
        {
            Name = "Abyssal Kraken",
            Description = "A terrifying creature from the ocean depths",
            Element = ElementType.Water,
            ManaCost = 6,
            Power = 7,
            Health = 8,
            Rarity = 3,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Tentacle Slam", Description = "Stuns enemy", Type = EffectType.Debuff, Value = 2, Target = TargetType.Enemy }
            }
        });

        // Epic Earth Titan
        newCards.Add(new CreatureCard
        {
            Name = "Mountain Titan",
            Description = "An ancient giant made of living stone",
            Element = ElementType.Earth,
            ManaCost = 7,
            Power = 8,
            Health = 12,
            Rarity = 3,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Earthquake", Description = "Shields self", Type = EffectType.Shield, Value = 5, Target = TargetType.Self }
            }
        });

        // Rare Air Phoenix
        newCards.Add(new CreatureCard
        {
            Name = "Storm Phoenix",
            Description = "A phoenix that rides the winds of storms",
            Element = ElementType.Air,
            ManaCost = 5,
            Power = 6,
            Health = 4,
            Rarity = 3,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Wind Slash", Description = "Quick attack", Type = EffectType.Damage, Value = 6, Target = TargetType.Enemy },
                new CardEffect { Name = "Fly", Description = "Cannot be blocked", Type = EffectType.Buff, Value = 1, Target = TargetType.Self }
            }
        });

        // Legendary Light Seraph
        newCards.Add(new CreatureCard
        {
            Name = "Solar Seraph",
            Description = "A divine angel of pure sunlight",
            Element = ElementType.Light,
            ManaCost = 7,
            Power = 6,
            Health = 7,
            Rarity = 4,
            IsLegendary = true,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Divine Light", Description = "Heal all allies", Type = EffectType.Heal, Value = 4, Target = TargetType.AllAllies },
                new CardEffect { Name = "Banish Evil", Description = "Destroy darkness", Type = EffectType.Destroy, Value = 1, Target = TargetType.Enemy }
            }
        });

        // Rare Dark Vampire
        newCards.Add(new CreatureCard
        {
            Name = "Night Vampire",
            Description = "A blood-drinking lord of the night",
            Element = ElementType.Dark,
            ManaCost = 4,
            Power = 5,
            Health = 4,
            Rarity = 3,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Life Drain", Description = "Steal health", Type = EffectType.Heal, Value = 3, Target = TargetType.Enemy }
            }
        });

        // Epic Arcane Wizard
        newCards.Add(new CreatureCard
        {
            Name = "Arcane Archmage",
            Description = "A master of all magical arts",
            Element = ElementType.Arcane,
            ManaCost = 6,
            Power = 4,
            Health = 5,
            Rarity = 3,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Spell Boost", Description = "Double next spell", Type = EffectType.Buff, Value = 2, Target = TargetType.Self },
                new CardEffect { Name = "Mana Surge", Description = "Gain extra mana", Type = EffectType.ManaGain, Value = 2, Target = TargetType.Self }
            }
        });

        // Rare Nature Treant
        newCards.Add(new CreatureCard
        {
            Name = "Ancient Treant",
            Description = "A wise tree spirit protecting the forest",
            Element = ElementType.Nature,
            ManaCost = 5,
            Power = 4,
            Health = 8,
            Rarity = 3,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Root Growth", Description = "Shield allies", Type = EffectType.Shield, Value = 3, Target = TargetType.AllAllies },
                new CardEffect { Name = "Regenerate", Description = "Heal self", Type = EffectType.Heal, Value = 2, Target = TargetType.Self }
            }
        });

        // Spell: Lightning Storm
        newCards.Add(new SpellCard
        {
            Name = "Lightning Storm",
            Description = "A devastating thunderstorm strikes all enemies",
            Element = ElementType.Air,
            ManaCost = 5,
            Power = 8,
            Rarity = 2,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Chain Lightning", Description = "Hits all enemies", Type = EffectType.Damage, Value = 8, Target = TargetType.AllEnemies }
            }
        });

        // Spell: Void Blast
        newCards.Add(new SpellCard
        {
            Name = "Void Blast",
            Description = "Unleash the power of the void",
            Element = ElementType.Dark,
            ManaCost = 6,
            Power = 12,
            Rarity = 3,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Destroy", Description = "Destroys target", Type = EffectType.Destroy, Value = 1, Target = TargetType.Enemy }
            }
        });

        // Spell: Holy Light
        newCards.Add(new SpellCard
        {
            Name = "Holy Light",
            Description = "Divine healing energy",
            Element = ElementType.Light,
            ManaCost = 3,
            Power = 8,
            Rarity = 2,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Heal", Description = "Restore health", Type = EffectType.Heal, Value = 8, Target = TargetType.Self }
            }
        });

        // Artifact: Shadow Cloak
        newCards.Add(new ArtifactCard
        {
            Name = "Shadow Cloak",
            Description = "Grants invisibility in darkness",
            Element = ElementType.Dark,
            ManaCost = 4,
            Rarity = 2,
            Power = 0,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Stealth", Description = "Cannot be targeted", Type = EffectType.Buff, Value = 1, Target = TargetType.Self, IsTemporary = true }
            }
        });

        // Artifact: Staff of Wisdom
        newCards.Add(new ArtifactCard
        {
            Name = "Staff of Wisdom",
            Description = "Amplifies magical power",
            Element = ElementType.Arcane,
            ManaCost = 5,
            Rarity = 3,
            Power = 3,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Mana Boost", Description = "+2 mana per turn", Type = EffectType.ManaGain, Value = 2, Target = TargetType.Self }
            }
        });

        // Event: Blessing of the Gods
        newCards.Add(new EventCard
        {
            Name = "Divine Blessing",
            Description = "The gods grant you favor",
            Element = ElementType.Light,
            ManaCost = 1,
            Power = 3,
            Rarity = 2,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Buff", Description = "Gain power", Type = EffectType.Buff, Value = 3, Target = TargetType.Self }
            }
        });

        // Event: Card Draw
        newCards.Add(new EventCard
        {
            Name = "Scholar's Insight",
            Description = "Gain knowledge through study",
            Element = ElementType.Arcane,
            ManaCost = 2,
            Power = 2,
            Rarity = 1,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Draw", Description = "Draw cards", Type = EffectType.DrawCard, Value = 2, Target = TargetType.Self }
            }
        });

        // Enchantment: Fire Aura
        new Cards.Card
        {
            Name = "Blazing Aura",
            Description = "Surrounds creatures with flames",
            Type = CardType.Enchantment,
            Element = ElementType.Fire,
            ManaCost = 3,
            Power = 2,
            Rarity = 2,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Burn", Description = "Damage attackers", Type = EffectType.Damage, Value = 2, Target = TargetType.Enemy }
            }
        };

        // Enchantment: Ice Shield
        new Cards.Card
        {
            Name = "Frost Barrier",
            Description = "Protects with icy defense",
            Type = CardType.Enchantment,
            Element = ElementType.Water,
            ManaCost = 2,
            Power = 0,
            Rarity = 2,
            Effects = new List<CardEffect>
            {
                new CardEffect { Name = "Shield", Description = "Block damage", Type = EffectType.Shield, Value = 4, Target = TargetType.Self }
            }
        };

        return newCards;
    }

    /// <summary>
    /// Generate a random deck of 11-19 cards for testing
    /// </summary>
    public static List<Card> GenerateRandomDeck()
    {
        var random = new Random();
        var deck = new List<Card>();
        
        // Random deck size between 11 and 19
        int deckSize = random.Next(11, 20);
        
        // Get all available card templates
        var allCards = CreateStarterDeck();
        
        // Randomly select cards for the deck
        for (int i = 0; i < deckSize; i++)
        {
            int cardIndex = random.Next(allCards.Count);
            deck.Add(allCards[cardIndex].Clone());
        }
        
        return deck;
    }
}