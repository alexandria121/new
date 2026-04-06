using Card = MagicalDeckbuilder.Cards.Card;
using CardType = MagicalDeckbuilder.Cards.CardType;
using ElementType = MagicalDeckbuilder.Cards.ElementType;
using EffectType = MagicalDeckbuilder.Cards.EffectType;
using TargetType = MagicalDeckbuilder.Cards.TargetType;
using MagicalDeckbuilder.Cards;
using MagicalDeckbuilder.Logging;

namespace MagicalDeckbuilder.Game;

/// <summary>
/// Factory for creating game cards
/// </summary>
public static class CardFactory
{
    /// <summary>
    /// Create a starter deck with a variety of cards from all elements
    /// </summary>
    public static List<Card> CreateStarterDeck()
    {
        ErrorLogger.Instance.Debug("CardFactory", "[Operation: CreateStarterDeck] Starting starter deck creation");
        try
        {
            var deck = new List<Card>();
            
            deck.AddRange(CreateRadioactivityDeck());
            deck.AddRange(CreateFleshDeck());
            deck.AddRange(CreateToxinDeck());
            deck.AddRange(CreateFungusDeck());
            deck.AddRange(CreateThermodynamicsDeck());
            deck.AddRange(CreateTimeDeck());
            deck.AddRange(CreateFoodDeck());
            deck.AddRange(CreateEldritchDeck());
            
            ErrorLogger.Instance.Info("CardFactory", $"[Operation: CreateStarterDeck] Created deck with {deck.Count} cards");
            return deck;
        }
        catch (Exception ex)
        {
            ErrorLogger.Instance.Error("CardFactory", "[Operation: CreateStarterDeck] Failed to create starter deck", ex);
            throw;
        }
    }
    
    // ============ RADIOACTIVITY (15 cards) ============
    private static List<Card> CreateRadioactivityDeck()
    {
        var deck = new List<Card>();
        
        // 3 Creatures
        deck.Add(CreateCreature("Radiated Cockroach", "A mutated insect that thrives in radiation", 
            ElementType.Radioactivity, 1, 1, 2, 1));
        deck.Add(CreateCreature("Glowing Slime", "A luminescent puddle of radioactive ooze", 
            ElementType.Radioactivity, 2, 2, 3, 1));
        deck.Add(CreateCreature("Nuclear Hulk", "A massive creature fused with radioactive material", 
            ElementType.Radioactivity, 5, 6, 8, 3));
        
        // 2 Spells
        deck.Add(CreateSpell("Radiation Burst", "Unleash a wave of harmful radiation", 
            ElementType.Radioactivity, 3, 4, EffectType.Damage, TargetType.Enemy));
        deck.Add(CreateSpell("Contamination", "Spread radioactive toxins", 
            ElementType.Radioactivity, 4, 3, EffectType.Debuff, TargetType.Enemy));
        
        // 2 Artifacts
        deck.Add(CreateArtifact("Uranium Core", "A glowing chunk of enriched uranium", 
            ElementType.Radioactivity, 3));
        deck.Add(CreateArtifact("Radiation Shield", "Protection from harmful radiation", 
            ElementType.Radioactivity, 2));
        
        // 1 Event
        deck.Add(CreateEvent("Meltdown", "A catastrophic nuclear event", 
            ElementType.Radioactivity, 5, 8));
        
        // 1 Blank
        deck.Add(CreateBlank("Raw Isotope", "Unrefined radioactive material", 
            ElementType.Radioactivity, 1));
        
        // 3 Enchantments
        deck.Add(CreateEnchantment("Glowing Aura", "An eerie glow surrounds creatures", 
            ElementType.Radioactivity, 2));
        deck.Add(CreateEnchantment("Mutation Seed", "Plants absorb radiation and mutate", 
            ElementType.Radioactivity, 3));
        deck.Add(CreateEnchantment("Nuclear Winter", "Fallout spreads across the land", 
            ElementType.Radioactivity, 4));
        
        // 3 Weapons
        deck.Add(CreateWeapon("Plutonium Dagger", "A blade laced with radioactive material", 
            ElementType.Radioactivity, 2, 3));
        deck.Add(CreateWeapon("Gamma Ray Gun", "Fires concentrated gamma radiation", 
            ElementType.Radioactivity, 4, 5));
        deck.Add(CreateWeapon("Atomic Hammer", "Channels nuclear energy into devastating strikes", 
            ElementType.Radioactivity, 6, 7));
        
        return deck;
    }
    
    // ============ FLESH (15 cards) ============
    private static List<Card> CreateFleshDeck()
    {
        var deck = new List<Card>();
        
        // 3 Creatures
        deck.Add(CreateCreature("Living Corpse", "A reanimated body without a soul", 
            ElementType.Flesh, 2, 2, 4, 1));
        deck.Add(CreateCreature("Horror Maw", "A massive mouth with countless teeth", 
            ElementType.Flesh, 4, 5, 5, 2));
        deck.Add(CreateCreature("Bone Titan", "A giant assembled from countless bones", 
            ElementType.Flesh, 6, 7, 7, 3));
        
        // 2 Spells
        deck.Add(CreateSpell("Cannibalize", "Consume flesh to restore health", 
            ElementType.Flesh, 2, 4, EffectType.Heal, TargetType.Self));
        deck.Add(CreateSpell("Flay", "Tear flesh from bone", 
            ElementType.Flesh, 3, 5, EffectType.Damage, TargetType.Enemy));
        
        // 2 Artifacts
        deck.Add(CreateArtifact("Preserved Heart", "A still-beating heart that never stops", 
            ElementType.Flesh, 3));
        deck.Add(CreateArtifact("Skin Cloak", "A cloak made from living skin", 
            ElementType.Flesh, 2));
        
        // 1 Event
        deck.Add(CreateEvent("Epidemic", "A plague spreads through the land", 
            ElementType.Flesh, 4, 6));
        
        // 1 Blank
        deck.Add(CreateBlank("Meat Chunk", "A lump of undifferentiated flesh", 
            ElementType.Flesh, 1));
        
        // 3 Enchantments
        deck.Add(CreateEnchantment("Regeneration", "Flesh knits itself back together", 
            ElementType.Flesh, 2));
        deck.Add(CreateEnchantment("Parasitic Bond", "A parasite feeds off the host", 
            ElementType.Flesh, 3));
        deck.Add(CreateEnchantment("Body Horror", "Transform into a nightmare form", 
            ElementType.Flesh, 4));
        
        // 3 Weapons
        deck.Add(CreateWeapon("Bone Blade", "A sword crafted from sharpened bones", 
            ElementType.Flesh, 2, 3));
        deck.Add(CreateWeapon("Flesh Ripper", "Hooks designed to tear flesh", 
            ElementType.Flesh, 3, 4));
        deck.Add(CreateWeapon("Skull Crusher", "A massive hammer topped with skulls", 
            ElementType.Flesh, 5, 6));
        
        return deck;
    }
    
    // ============ TOXIN (15 cards) ============
    private static List<Card> CreateToxinDeck()
    {
        var deck = new List<Card>();
        
        // 3 Creatures
        deck.Add(CreateCreature("Poison Dart Frog", "A small amphibian with deadly toxins", 
            ElementType.Toxin, 1, 1, 1, 1));
        deck.Add(CreateCreature("Venomous Snake", "A serpent with potent venom", 
            ElementType.Toxin, 2, 3, 2, 1));
        deck.Add(CreateCreature("Toxic Elemental", "A being made entirely of poison", 
            ElementType.Toxin, 4, 4, 4, 2));
        
        // 2 Spells
        deck.Add(CreateSpell("Poison Cloud", "Release a deadly miasma", 
            ElementType.Toxin, 3, 3, EffectType.Damage, TargetType.AllEnemies));
        deck.Add(CreateSpell("Neurotoxin", "Attack the nervous system", 
            ElementType.Toxin, 4, 6, EffectType.Debuff, TargetType.Enemy));
        
        // 2 Artifacts
        deck.Add(CreateArtifact("Venom Vial", "A small bottle of concentrated toxin", 
            ElementType.Toxin, 2));
        deck.Add(CreateArtifact("Antidote Charm", "Protects against poison effects", 
            ElementType.Toxin, 2));
        
        // 1 Event
        deck.Add(CreateEvent("Plague Outbreak", "A devastating sickness spreads", 
            ElementType.Toxin, 5, 7));
        
        // 1 Blank
        deck.Add(CreateBlank("Toxic Slime", "An unrefined toxic substance", 
            ElementType.Toxin, 1));
        
        // 3 Enchantments
        deck.Add(CreateEnchantment("Toxic Aura", "Poison seeps from the skin", 
            ElementType.Toxin, 2));
        deck.Add(CreateEnchantment("Blighted Weapon", "Coat weapons in poison", 
            ElementType.Toxin, 3));
        deck.Add(CreateEnchantment("Corrosive Mist", "Acid-like vapors fill the air", 
            ElementType.Toxin, 4));
        
        // 3 Weapons
        deck.Add(CreateWeapon("Poisoned Dagger", "A blade coated with toxin", 
            ElementType.Toxin, 1, 2));
        deck.Add(CreateWeapon("Viper Fangs", "Fangs that inject venom", 
            ElementType.Toxin, 3, 3));
        deck.Add(CreateWeapon("Toxic Cannon", "Fires globules of acid", 
            ElementType.Toxin, 5, 6));
        
        return deck;
    }
    
    // ============ FUNGUS (15 cards) ============
    private static List<Card> CreateFungusDeck()
    {
        var deck = new List<Card>();
        
        // 3 Creatures
        deck.Add(CreateCreature("Spore Crawler", "A creature covered in fungus", 
            ElementType.Fungus, 2, 2, 3, 1));
        deck.Add(CreateCreature("Mushroom Giant", "A massive fungal growth given life", 
            ElementType.Fungus, 4, 3, 6, 2));
        deck.Add(CreateCreature("Mycotyx", "An ancient fungal horror", 
            ElementType.Fungus, 5, 4, 7, 3));
        
        // 2 Spells
        deck.Add(CreateSpell("Spore Burst", "Release a cloud of fungal spores", 
            ElementType.Fungus, 3, 4, EffectType.Damage, TargetType.Enemy));
        deck.Add(CreateSpell("Fungal Growth", "Accelerate fungal development", 
            ElementType.Fungus, 2, 3, EffectType.Heal, TargetType.Self));
        
        // 2 Artifacts
        deck.Add(CreateArtifact("Glowing Mushroom", "A bioluminescent fungus", 
            ElementType.Fungus, 2));
        deck.Add(CreateArtifact("Fungal Network", "Connects to a vast fungal mind", 
            ElementType.Fungus, 3));
        
        // 1 Event
        deck.Add(CreateEvent("Bloom", "Fungi spread rapidly", 
            ElementType.Fungus, 3, 5));
        
        // 1 Blank
        deck.Add(CreateBlank("Fungal Spore", "A single, undifferentiated spore", 
            ElementType.Fungus, 1));
        
        // 3 Enchantments
        deck.Add(CreateEnchantment("Mycological Bond", "Connected through the fungal network", 
            ElementType.Fungus, 2));
        deck.Add(CreateEnchantment("Hallucinogenic Aura", "Spores cause vivid hallucinations", 
            ElementType.Fungus, 3));
        deck.Add(CreateEnchantment("Decomposition", "Breaking down organic matter", 
            ElementType.Fungus, 4));
        
        // 3 Weapons
        deck.Add(CreateWeapon("Mushroom Staff", "A staff topped with fungal growth", 
            ElementType.Fungus, 2, 2));
        deck.Add(CreateWeapon("Spore Launcher", "Fires concentrated fungal projectiles", 
            ElementType.Fungus, 3, 4));
        deck.Add(CreateWeapon("Fungal Scythe", "A blade that spreads infection", 
            ElementType.Fungus, 4, 5));
        
        return deck;
    }
    
    // ============ THERMODYNAMICS (15 cards) ============
    private static List<Card> CreateThermodynamicsDeck()
    {
        var deck = new List<Card>();
        
        // 3 Creatures
        deck.Add(CreateCreature("Flame Imp", "A small being of pure heat", 
            ElementType.Thermodynamics, 1, 2, 1, 1));
        deck.Add(CreateCreature("Ice Elemental", "A frozen construct of ice", 
            ElementType.Thermodynamics, 3, 3, 4, 1));
        deck.Add(CreateCreature("Heat Monster", "A creature that feeds on fire", 
            ElementType.Thermodynamics, 4, 5, 5, 2));
        
        // 2 Spells
        deck.Add(CreateSpell("Absolute Zero", "Freeze to near absolute zero", 
            ElementType.Thermodynamics, 4, 7, EffectType.Damage, TargetType.Enemy));
        deck.Add(CreateSpell("Heat Wave", "Unleash scorching temperatures", 
            ElementType.Thermodynamics, 3, 5, EffectType.Damage, TargetType.Enemy));
        
        // 2 Artifacts
        deck.Add(CreateArtifact("Thermal Core", "A reactor that generates heat", 
            ElementType.Thermodynamics, 3));
        deck.Add(CreateArtifact("Heat Sink", "Absorbs and dissipates thermal energy", 
            ElementType.Thermodynamics, 2));
        
        // 1 Event
        deck.Add(CreateEvent("Supernova", "A stellar explosion", 
            ElementType.Thermodynamics, 6, 10));
        
        // 1 Blank
        deck.Add(CreateBlank("Thermal Mass", "Raw thermal energy", 
            ElementType.Thermodynamics, 1));
        
        // 3 Enchantments
        deck.Add(CreateEnchantment("Temperature Control", "Manipulate ambient temperature", 
            ElementType.Thermodynamics, 2));
        deck.Add(CreateEnchantment("Heat Absorption", "Drawing heat from surroundings", 
            ElementType.Thermodynamics, 3));
        deck.Add(CreateEnchantment("Entropy Field", "Increased entropy in the area", 
            ElementType.Thermodynamics, 4));
        
        // 3 Weapons
        deck.Add(CreateWeapon("Plasma Torch", "A weapon that burns with plasma", 
            ElementType.Thermodynamics, 2, 4));
        deck.Add(CreateWeapon("Cryo Cannon", "Freezes targets with extreme cold", 
            ElementType.Thermodynamics, 4, 4));
        deck.Add(CreateWeapon("Thermal Lance", "Channels focused thermal energy", 
            ElementType.Thermodynamics, 5, 7));
        
        return deck;
    }
    
    // ============ TIME (15 cards) ============
    private static List<Card> CreateTimeDeck()
    {
        var deck = new List<Card>();
        
        // 3 Creatures
        deck.Add(CreateCreature("Temporal Wisp", "A ghostly being out of time", 
            ElementType.Time, 2, 2, 2, 1));
        deck.Add(CreateCreature("Elder Chronos", "An ancient being of time", 
            ElementType.Time, 5, 4, 5, 3));
        deck.Add(CreateCreature("Time Loop", "A creature trapped in recursion", 
            ElementType.Time, 3, 3, 4, 2));
        
        // 2 Spells
        deck.Add(CreateSpell("Time Stop", "Freeze time for all but the caster", 
            ElementType.Time, 5, 0, EffectType.Debuff, TargetType.AllEnemies));
        deck.Add(CreateSpell("Accelerate", "Speed up the target", 
            ElementType.Time, 2, 3, EffectType.Buff, TargetType.Self));
        
        // 2 Artifacts
        deck.Add(CreateArtifact("Hourglass of Eternity", "An artifact that controls time", 
            ElementType.Time, 4));
        deck.Add(CreateArtifact("Temporal Anchor", "Keeps the bearer in the present", 
            ElementType.Time, 3));
        
        // 1 Event
        deck.Add(CreateEvent("Temporal Rift", "A rupture in the time stream", 
            ElementType.Time, 5, 8));
        
        // 1 Blank
        deck.Add(CreateBlank("Temporal Shard", "A fragment of frozen time", 
            ElementType.Time, 1));
        
        // 3 Enchantments
        deck.Add(CreateEnchantment("Time Dilation", "Time moves slower for the enchanted", 
            ElementType.Time, 2));
        deck.Add(CreateEnchantment("Precognition", "See slightly into the future", 
            ElementType.Time, 3));
        deck.Add(CreateEnchantment("Age Acceleration", "Rapidly age the target", 
            ElementType.Time, 4));
        
        // 3 Weapons
        deck.Add(CreateWeapon("Chrono Blade", "A sword that cuts through time", 
            ElementType.Time, 3, 4));
        deck.Add(CreateWeapon("Temporal Bow", "Arrows that skip through time", 
            ElementType.Time, 3, 5));
        deck.Add(CreateWeapon("Minute Glass", "A weapon that ages enemies", 
            ElementType.Time, 4, 4));
        
        return deck;
    }
    
    // ============ FOOD (15 cards) ============
    private static List<Card> CreateFoodDeck()
    {
        var deck = new List<Card>();
        
        // 3 Creatures
        deck.Add(CreateCreature("Gorging Goblin", "A gluttonous creature", 
            ElementType.Food, 2, 3, 3, 1));
        deck.Add(CreateCreature("Living Kitchen", "A sentient cooking apparatus", 
            ElementType.Food, 3, 2, 5, 1));
        deck.Add(CreateCreature("Famine Beast", "A horror that devours all", 
            ElementType.Food, 5, 6, 6, 3));
        
        // 2 Spells
        deck.Add(CreateSpell("Feast", "A bountiful meal restores health", 
            ElementType.Food, 2, 5, EffectType.Heal, TargetType.Self));
        deck.Add(CreateSpell("Starve", "Deprive the target of food", 
            ElementType.Food, 3, 4, EffectType.Debuff, TargetType.Enemy));
        
        // 2 Artifacts
        deck.Add(CreateArtifact("Endless Pantry", "Never runs out of food", 
            ElementType.Food, 3));
        deck.Add(CreateArtifact("Cursed Cauldron", "A pot that cooks up misfortune", 
            ElementType.Food, 2));
        
        // 1 Event
        deck.Add(CreateEvent("Banquet", "A grand feast for all", 
            ElementType.Food, 3, 6));
        
        // 1 Blank
        deck.Add(CreateBlank("Raw Ingredient", "An unprocessed food item", 
            ElementType.Food, 1));
        
        // 3 Enchantments
        deck.Add(CreateEnchantment("Rotting Touch", "Food spoils at your command", 
            ElementType.Food, 2));
        deck.Add(CreateEnchantment("Sugar Rush", "Temporary energy from sweets", 
            ElementType.Food, 2));
        deck.Add(CreateEnchantment("Satisfaction", "Well-fed creatures fight harder", 
            ElementType.Food, 3));
        
        // 3 Weapons
        deck.Add(CreateWeapon("Cooking Knife", "Sharp enough to slice anything", 
            ElementType.Food, 1, 3));
        deck.Add(CreateWeapon("Cleaver", "A massive blade for chopping", 
            ElementType.Food, 3, 4));
        deck.Add(CreateWeapon("Spear of Sustenance", "A weapon that nourishes the wielder", 
            ElementType.Food, 4, 5));
        
        return deck;
    }
    
    // ============ ELDRITCH (15 cards) ============
    private static List<Card> CreateEldritchDeck()
    {
        var deck = new List<Card>();
        
        // 3 Creatures
        deck.Add(CreateCreature("Cultist", "A follower of ancient horrors", 
            ElementType.Eldritch, 1, 1, 2, 1));
        deck.Add(CreateCreature("Void Spawn", "A creature from beyond the veil", 
            ElementType.Eldritch, 3, 4, 3, 2));
        deck.Add(CreateCreature("Eldritch Horror", "A being of incomprehensible power", 
            ElementType.Eldritch, 6, 8, 8, 4, true));
        
        // 2 Spells
        deck.Add(CreateSpell("Unspeakable Pact", "A contract with entities unknown", 
            ElementType.Eldritch, 4, 5, EffectType.Damage, TargetType.Enemy));
        deck.Add(CreateSpell("Sanity Erosion", "Slowly break down mental defenses", 
            ElementType.Eldritch, 3, 4, EffectType.Debuff, TargetType.Enemy));
        
        // 2 Artifacts
        deck.Add(CreateArtifact("Madness Tome", "A book of forbidden knowledge", 
            ElementType.Eldritch, 3));
        deck.Add(CreateArtifact("Eldritch Sigil", "A symbol of cosmic horror", 
            ElementType.Eldritch, 2));
        
        // 1 Event
        deck.Add(CreateEvent("The Stars Align", "Cosmic alignment triggers apocalypse", 
            ElementType.Eldritch, 6, 9));
        
        // 1 Blank
        deck.Add(CreateBlank("Chaos Shard", "A fragment of pure chaos", 
            ElementType.Eldritch, 1));
        
        // 3 Enchantments
        deck.Add(CreateEnchantment("Dark Whispers", "Voices from beyond", 
            ElementType.Eldritch, 2));
        deck.Add(CreateEnchantment("Eye of Madness", "See through the veil of reality", 
            ElementType.Eldritch, 3));
        deck.Add(CreateEnchantment("Void Touched", "Marked by the void", 
            ElementType.Eldritch, 4));
        
        // 3 Weapons
        deck.Add(CreateWeapon("Tentacle Whip", "A writhing appendage of flesh", 
            ElementType.Eldritch, 2, 3));
        deck.Add(CreateWeapon("Starfire Staff", "Channels eldritch energies", 
            ElementType.Eldritch, 4, 5));
        deck.Add(CreateWeapon("Cosmic Blade", "A sword of stellar matter", 
            ElementType.Eldritch, 5, 7));
        
        return deck;
    }
    
    // ============ CARD CREATION HELPERS ============
    
    private static Card CreateCreature(string name, string description, 
        ElementType element, int manaCost, int power, int health, int rarity = 1, bool isLegendary = false)
    {
        return new CreatureCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Power = power,
            Health = health,
            Rarity = rarity,
            IsLegendary = isLegendary,
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
        ElementType element, int manaCost, int power, EffectType effectType, TargetType target)
    {
        var spell = new SpellCard
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
                Name = effectType.ToString(),
                Description = description,
                Type = effectType,
                Value = power,
                Target = target
            });
        }
        else
        {
            spell.Effects.Add(new CardEffect
            {
                Name = "Effect",
                Description = description,
                Type = effectType,
                Value = 1,
                Target = target
            });
        }
        
        return spell;
    }
    
    private static Card CreateArtifact(string name, string description,
        ElementType element, int manaCost)
    {
        return new ArtifactCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Rarity = 1
        };
    }
    
    private static Card CreateEnchantment(string name, string description,
        ElementType element, int manaCost)
    {
        return new Card
        {
            Name = name,
            Description = description,
            Type = CardType.Enchantment,
            Element = element,
            ManaCost = manaCost,
            Rarity = 1,
            Power = 0,
            Effects = new List<CardEffect>
            {
                new CardEffect
                {
                    Name = "Enchant",
                    Description = description,
                    Type = EffectType.Buff,
                    Value = 1,
                    Target = TargetType.Self
                }
            }
        };
    }
    
    private static Card CreateWeapon(string name, string description,
        ElementType element, int manaCost, int power)
    {
        return new WeaponCard
        {
            Name = name,
            Description = description,
            Type = CardType.Weapon,
            Element = element,
            ManaCost = manaCost,
            Power = power,
            Rarity = 1,
            Effects = new List<CardEffect>
            {
                new CardEffect
                {
                    Name = "Strike",
                    Description = description,
                    Type = EffectType.Damage,
                    Value = power,
                    Target = TargetType.Enemy
                }
            }
        };
    }

    private static Card CreateArmor(string name, string description,
        ElementType element, int manaCost, int defense)
    {
        return new ArmorCard
        {
            Name = name,
            Description = description,
            Type = CardType.Armor,
            Element = element,
            ManaCost = manaCost,
            Health = defense,
            Rarity = 1,
            Effects = new List<CardEffect>
            {
                new CardEffect
                {
                    Name = "Protect",
                    Description = description,
                    Type = EffectType.Shield,
                    Value = defense,
                    Target = TargetType.Self
                }
            }
        };
    }
    
    private static Card CreateEvent(string name, string description,
        ElementType element, int manaCost, int effectValue)
    {
        var eventCard = new EventCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Rarity = 1,
            Power = effectValue
        };
        
        if (name.Contains("Meltdown") || name.Contains("Supernova"))
        {
            eventCard.Effects.Add(new CardEffect
            {
                Name = "Destruction",
                Description = "Massive damage to all",
                Type = EffectType.Damage,
                Value = effectValue,
                Target = TargetType.AllEnemies
            });
        }
        else
        {
            eventCard.Effects.Add(new CardEffect
            {
                Name = "Event",
                Description = description,
                Type = EffectType.Damage,
                Value = effectValue,
                Target = TargetType.Any
            });
        }
        
        return eventCard;
    }
    
    private static Card CreateBlank(string name, string description,
        ElementType element, int manaCost)
    {
        var blank = new BlankCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Rarity = 1,
            Power = 1
        };
        
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
    /// Generate a random deck of cards
    /// </summary>
    public static List<Card> GenerateRandomDeck()
    {
        ErrorLogger.Instance.Debug("CardFactory", "[Operation: GenerateRandomDeck] Starting random deck generation");
        try
        {
            var random = new Random();
            var deck = new List<Card>();
            
            int deckSize = random.Next(11, 20);
            
            var allCards = CreateStarterDeck();
            
            for (int i = 0; i < deckSize; i++)
            {
                int cardIndex = random.Next(allCards.Count);
                deck.Add(allCards[cardIndex].Clone());
            }
            
            ErrorLogger.Instance.Info("CardFactory", $"[Operation: GenerateRandomDeck] Generated random deck with {deck.Count} cards");
            return deck;
        }
        catch (Exception ex)
        {
            ErrorLogger.Instance.Error("CardFactory", "[Operation: GenerateRandomDeck] Failed to generate random deck", ex);
            throw;
        }
    }
    
    /// <summary>
    /// Get a card template by its template ID (name_type_element format)
    /// </summary>
    public static Card? GetCardByTemplateId(string templateId)
    {
        var allCards = CreateStarterDeck();
        return allCards.FirstOrDefault(c => $"{c.Name}_{c.Type}_{c.Element}" == templateId);
    }
    
    /// <summary>
    /// Get all card templates for lookup purposes
    /// </summary>
    public static IReadOnlyList<Card> GetAllTemplates()
    {
        return CreateStarterDeck();
    }
}
