using Card = MagicalDeckbuilder.Cards.Card;
using CardType = MagicalDeckbuilder.Cards.CardType;
using ElementType = MagicalDeckbuilder.Cards.ElementType;
using EffectType = MagicalDeckbuilder.Cards.EffectType;
using TargetType = MagicalDeckbuilder.Cards.TargetType;
using WeaponTargetType = MagicalDeckbuilder.Cards.WeaponTargetType;
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
            deck.AddRange(CreateFoodDeck());
            deck.AddRange(CreateEldritchDeck());
            deck.AddRange(CreateComboDeck());
            
            ErrorLogger.Instance.Info("CardFactory", $"[Operation: CreateStarterDeck] Created deck with {deck.Count} cards");
            return deck;
        }
        catch (Exception ex)
        {
            ErrorLogger.Instance.Error("CardFactory", "[Operation: CreateStarterDeck] Failed to create starter deck", ex);
            throw;
        }
    }
    
    // ============ RADIATION (5 cards) ============
    private static List<Card> CreateRadioactivityDeck()
    {
        var deck = new List<Card>();
        
        // Irradiated Blob - CREATURE, MC:1, H:6, P:1, COMMON
        deck.Add(CreateCreature("Irradiated Blob", "This was once a person... you think.\n[ABILITY: Mutation - 2 MC, BTS +2P, DBTS -2H]", 
            ElementType.Radioactivity, 1, 6, 1, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Mutation",
                    Description = "2 MC, BTS +2P, DBTS -2H",
                    ManaCost = 2,
                    EffectType = EffectType.BuffPower,
                    EffectValue = 2,
                    RequiresTarget = false
                }
            }));
        
        // Confused Predator - CREATURE, MC:1, H:3, P:3, COMMON
        deck.Add(CreateCreature("Confused Predator", "They're relatively easy to capture, but much more difficult to designate targets to.\n[ABILITY: Brain Fog - passive, 25% chance to deal damage to owner, 75% chance to do damage to original target]", 
            ElementType.Radioactivity, 1, 3, 3, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Brain Fog",
                    Description = "passive, 25% chance to deal damage to owner, 75% chance to do damage to original target",
                    ManaCost = 0,
                    EffectType = EffectType.Damage, // Simplified - deals damage to random target
                    EffectValue = 1,
                    IsPassive = true
                }
            }));
        
        // Geiger Giant - CREATURE, MC:5, H:10, P:4, RARE
        deck.Add(CreateCreature("Geiger Giant", "You're not quite positive who Geiger is, but he sure is tall.\n[ABILITY: Slam - 4 MC, 6 DTA]", 
            ElementType.Radioactivity, 5, 10, 4, 3, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Slam",
                    Description = "4 MC, 6 DTA",
                    ManaCost = 4,
                    EffectType = EffectType.DamageToAllEnemyCreatures,
                    EffectValue = 6,
                    RequiresTarget = false
                }
            }));
        
        // Mutually Assured Duplication - SPELL, MC:5, T:SE&PC, RARE
        deck.Add(CreateSpell("Mutually Assured Duplication", "Wait, this is supposed to deter an attack??\n[ABILITY: Budding - Choose one enemy and friendly creature; duplicate these creatures to the closest unoccupied slot]", 
            ElementType.Radioactivity, 5, 0, EffectType.Damage, TargetType.Any,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Budding",
                    Description = "Select player owned creature, then select opponent owned creature to duplicate",
                    ManaCost = 0, // Cost is the spell cost
                    EffectType = EffectType.Duplicate,
                    EffectValue = 0,
                    RequiresTarget = true
                }
            }));
        
        // ...Winter? - EVENT, MC:7, D:3T, RARE
        deck.Add(CreateEvent("...Winter?", "What's worse, eternal nuclear winter or literally anything else?\n[ABILITY: Whiteout - 3DTAC for duration, damage halved rounded down (1) to RAD, TOX & THE]", 
            ElementType.Radioactivity, 7, 3,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Whiteout",
                    Description = "3DTAC for duration, damage halved rounded down (1) to RAD, TOX & THE",
                    ManaCost = 0,
                    EffectType = EffectType.DamageToAllCreatures,
                    EffectValue = 3,
                    RequiresTarget = false,
                    IsTemporary = true,
                    Duration = 3
                }
            }));
        
        return deck;
    }
    
    // ============ FLESH (5 cards) ============
    private static List<Card> CreateFleshDeck()
    {
        var deck = new List<Card>();
        
        // Amalgamation - CREATURE, MC:2, H:3, P:5, COMMON
        deck.Add(CreateCreature("Amalgamation", "Some animals were clearly harmed in the making of this abomination.\n[ABILITY: Gutteral Roar - 3 MC, -2P DBTT for 2 turns]", 
            ElementType.Flesh, 2, 3, 5, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Gutteral Roar",
                    Description = "3 MC, -2P DBTT for 2 turns",
                    ManaCost = 3,
                    EffectType = EffectType.DebuffPower,
                    EffectValue = 2,
                    RequiresTarget = true,
                    IsTemporary = true,
                    Duration = 2
                }
            }));
        
        // Disembody - SPELL, MC:3, T:SEC, COMMON
        deck.Add(CreateSpell("Disembody", "Grummaz went from screaming and struggling to quiet and calm in about 2 seconds.\n[ABILITY: Astral Eviction - Creature may no longer attack, or use abilities]", 
            ElementType.Flesh, 3, 0, EffectType.Damage, TargetType.Any,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Astral Eviction",
                    Description = "Creature may no longer attack, or use abilities",
                    ManaCost = 0,
                    EffectType = EffectType.DisableAttack,
                    EffectValue = 0,
                    RequiresTarget = true,
                    IsTemporary = true,
                    Duration = 999 // Permanent until removed
                }
            }));
        
        // Evil Eyeball - SPELL, MC:4, T:SEC, UNCOMMON
        deck.Add(CreateSpell("Evil Eyeball", "Wow, I sure feel weird...\n[ABILITY: Weird Looking Stare - DBTT, debuff kills creature at the beginning of your next turn]", 
            ElementType.Flesh, 4, 0, EffectType.Damage, TargetType.Any,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Weird Looking Stare",
                    Description = "DBTT, debuff kills creature at the beginning of your next turn",
                    ManaCost = 0,
                    EffectType = EffectType.Debuff,
                    EffectValue = 999, // High damage to kill - actual kill happens on next turn
                    RequiresTarget = true,
                    IsTemporary = true,
                    Duration = 1
                }
            }));
        
        // Meat Rain - SPELL, MC:4, RARE
        deck.Add(CreateSpell("Meat Rain", "After the storm passed, the villagers emerged to discover a scene unlike any they'd ever seen.\n[ABILITY: Feast, My Minions! - HAPC 100% of normal stats, doesn't do anything for health value over 100% of base stats of card]", 
            ElementType.Flesh, 4, 0, EffectType.Heal, TargetType.AllAllies,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Feast, My Minions!",
                    Description = "HAPC 100% of normal stats, doesn't do anything for health value over 100%",
                    ManaCost = 0,
                    EffectType = EffectType.Heal,
                    EffectValue = 0, // Heals all player creatures to full
                    RequiresTarget = false
                }
            }));
        
        // Jar of Eyes - ARTIFACT, MC:3, RARE  
        deck.Add(CreateArtifact("Jar of Eyes", "Wherever you go, at least one of the eyes seems to follow.\n[ABILITY: Forbidden Knowledge - 50% chance of making opponents hand visible for a turn]", 
            ElementType.Flesh, 3));
        
        return deck;
    }
    
    // ============ TOXIN (4 cards) ============
    private static List<Card> CreateToxinDeck()
    {
        var deck = new List<Card>();
        
        // Refuse Slinger - CREATURE, MC:1, H:1, P:2, COMMON
        deck.Add(CreateCreature("Refuse Slinger", "Isn't that a biohazard?\n[ABILITY: Hurl - 2 MC, -1P&-1H DBTT&A for 3 turns]", 
            ElementType.Toxin, 1, 1, 2, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Hurl",
                    Description = "2 MC, -1P&-1H DBTT&A for 3 turns",
                    ManaCost = 2,
                    EffectType = EffectType.Debuff,
                    EffectValue = 1,
                    RequiresTarget = true,
                    IsTemporary = true,
                    Duration = 3
                }
            }));
        
        // Pupae Patroller - CREATURE, MC:3, H:2, P:3, UNCOMMON
        // SKIP - requires larval creature card/tag (need to add larval creatures first)
        deck.Add(CreateCreature("Pupae Patroller", "You're not sure how this guy fits in the insect heirarchy, but he sure seems happy. (this is a literal human being)\n[ABILITY: Prowl - 1 MC, +1P&+1H for a random larval creature]", 
            ElementType.Toxin, 3, 2, 3, 2));
        
        // Brood Monarch - CREATURE, MC:6, H:4, P:3, RARE
        deck.Add(CreateCreature("Brood Monarch", "Without a doubt, the biggest and grossest bug you've ever seen.\n[ABILITY: Larval Gestation - 4 MC, starts 3 turn timer, spawns 2 3H3P workers]", 
            ElementType.Toxin, 6, 4, 3, 3, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Larval Gestation",
                    Description = "4 MC, starts 3 turn timer, spawns 2 3H3P larval workers",
                    ManaCost = 4,
                    EffectType = EffectType.Duplicate, // Using duplicate as spawn mechanic
                    EffectValue = 2,
                    RequiresTarget = false,
                    IsTemporary = true,
                    Duration = 3
                }
            }));
        
        // Suspicious Mound - ARTIFACT, MC:4, UNCOMMON
        deck.Add(CreateArtifact("Suspicious Mound", "People have learned to give these a wide berth, and not just because of the smell.\n[ABILITY: Overrun - 1MC, spawns 1 2H1P larval naiad (max 2 per turn)]", 
            ElementType.Toxin, 4,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Overrun",
                    Description = "1MC, spawns 1 2H1P larval naiad (max 2 per turn)",
                    ManaCost = 1,
                    EffectType = EffectType.Duplicate,
                    EffectValue = 1,
                    RequiresTarget = false
                }
            }));
        
        return deck;
    }
    
    // ============ FUNGUS (5 cards) ============
    private static List<Card> CreateFungusDeck()
    {
        var deck = new List<Card>();
        
        // Mycelium Spore - CREATURE, MC:2, H:1, P:1, COMMON
        deck.Add(CreateCreature("Mycelium Spore", "Where a single spore is found, more are bound to follow.\n[ABILITY: Growth - 1 MC, +1H&+1P BTS (perm)]", 
            ElementType.Fungus, 2, 1, 1, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Growth",
                    Description = "1 MC, +1H&+1P BTS (perm)",
                    ManaCost = 1,
                    EffectType = EffectType.Buff,
                    EffectValue = 1,
                    RequiresTarget = false
                }
            }));
        
        // Fruiting Body - CREATURE, MC:4, H:3, P:4, COMMON
        deck.Add(CreateCreature("Fruiting Body", "Some poor animal ate one too many mushrooms, and ended up becoming a mushroom.\n[ABILITY: Troop Thickening - 2 MCPT, +1H&+1P PT to all spores]", 
            ElementType.Fungus, 4, 3, 4, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Troop Thickening",
                    Description = "2 MCPT, +1H&+1P PT to all spores",
                    ManaCost = 2,
                    EffectType = EffectType.Buff, // Buff to all friendly spores
                    EffectValue = 1,
                    IsPassive = true
                }
            }));
        
        // Undead Stump - CREATURE, MC:5, H:4, P:4, UNCOMMON
        deck.Add(CreateCreature("Undead Stump", "The tree that once made up this stump is now dead, but to say the stump is lifeless is entirely inaccurate.\n[ABILITY: Germinate - 2MC, spawn 1 spore in the closest unoccupied creature slot]", 
            ElementType.Fungus, 5, 4, 4, 2, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Germinate",
                    Description = "2MC, spawn 1 spore in the closest unoccupied creature slot",
                    ManaCost = 2,
                    EffectType = EffectType.Duplicate, // Spawn a copy/spawn
                    EffectValue = 1,
                    RequiresTarget = false
                }
            }));
        
        // Creeping Infection - SPELL, MC:3, T:SC, UNCOMMON
        deck.Add(CreateSpell("Creeping Infection", "It enters through the skin and only gets worse with time.\n[ABILITY: It Begins - 2 DTT for 4 turns, DTT increases by 1 for each turn after the first]", 
            ElementType.Fungus, 3, 0, EffectType.Damage, TargetType.Any,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "It Begins",
                    Description = "2 DTT for 4 turns, DTT increases by 1 each turn (T1-2, T2-3, T3-4, T4-5)",
                    ManaCost = 0,
                    EffectType = EffectType.Damage,
                    EffectValue = 2,
                    RequiresTarget = true,
                    IsTemporary = true,
                    Duration = 4
                }
            }));
        
        // Brain Altering Powder - ARTIFACT, MC:4, T:SC, RARE
        deck.Add(CreateArtifact("Brain Altering Powder", "It took me years to find the guy who sold this to me!\n[ABILITY: Yo, Try This! - 3MC, 1 Infection Counter]", 
            ElementType.Fungus, 4,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Yo, Try This!",
                    Description = "3MC, 1 Infection Counter",
                    ManaCost = 3,
                    EffectType = EffectType.Infect,
                    EffectValue = 1,
                    RequiresTarget = true
                }
            }));
        
        return deck;
    }
    
    // ============ THERMODYNAMICS (5 cards) ============
    private static List<Card> CreateThermodynamicsDeck()
    {
        var deck = new List<Card>();
        
        // Heat Mote - CREATURE, MC:1, H:1, P:2, COMMON
        deck.Add(CreateCreature("Heat Mote", "A single spark, ready to burst into existence.\n[ABILITY: Temperature Gradient(up) - passive, double the amount and length of +P buffs]", 
            ElementType.Thermodynamics, 1, 1, 2, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Temperature Gradient(up)",
                    Description = "passive, double the amount and length of +P buffs",
                    ManaCost = 0,
                    EffectType = EffectType.Buff,
                    EffectValue = 0,
                    IsPassive = true
                }
            }));
        
        // Cold Mote - CREATURE, MC:1, H:2, P:1, COMMON
        deck.Add(CreateCreature("Cold Mote", "A cold snap just waiting to happen.\n[ABILITY: Temperature Gradient(down) - passive, double the amount and length of +H buffs]", 
            ElementType.Thermodynamics, 1, 2, 1, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Temperature Gradient(down)",
                    Description = "passive, double the amount and length of +H buffs",
                    ManaCost = 0,
                    EffectType = EffectType.Buff,
                    EffectValue = 0,
                    IsPassive = true
                }
            }));
        
        // Thermal Shock - SPELL, MC:3, T:SC, COMMON
        deck.Add(CreateSpell("Thermal Shock", "You don't have to be an expert to know that going from 100 degrees to -100 in 2 seconds is bad.\n[ABILITY: Heat and Cool - 2 DTT, -2H DBTT for 2 turns]", 
            ElementType.Thermodynamics, 3, 0, EffectType.Damage, TargetType.Any,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Heat and Cool",
                    Description = "2 DTT, -2H DBTT for 2 turns",
                    ManaCost = 0,
                    EffectType = EffectType.Damage,
                    EffectValue = 2,
                    RequiresTarget = true,
                    IsTemporary = true,
                    Duration = 2
                }
            }));
        
        // Superheated Ice Spike - SPELL, MC:3, T:SC, UNCOMMON
        deck.Add(CreateSpell("Superheated Ice Spike", "Heat stroke sure feels different below freezing.\n[ABILITY: Frostbite Burn - 3 DTT, 2 DPT for 2 turns]", 
            ElementType.Thermodynamics, 3, 0, EffectType.Damage, TargetType.Any,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Frostbite Burn",
                    Description = "3 DTT, 2 DPT for 2 turns",
                    ManaCost = 0,
                    EffectType = EffectType.Damage,
                    EffectValue = 3,
                    RequiresTarget = true,
                    IsTemporary = true,
                    Duration = 2
                }
            }));
        
        // Silver Rod - ARTIFACT, MC:2, UNCOMMON
        deck.Add(CreateArtifact("Silver Rod", "\"Don't touch! It's a temperature!\" -average thermodynamics fan\n[ABILITY: Temperature Sensitive (all) - 2 MC, Absorb Excess (steal all buffs from a creature with any creature with Temperature Gradient (up, down and both) (+P&+H))]", 
            ElementType.Thermodynamics, 2,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Temperature Sensitive(all)",
                    Description = "2 MC, Absorb Excess (steal all buffs from a creature with Temperature Gradient)",
                    ManaCost = 2,
                    EffectType = EffectType.Buff,
                    EffectValue = 0,
                    RequiresTarget = true
                }
            }));
        
        return deck;
    }
    
    // ============ FOOD (6 cards) ============
    private static List<Card> CreateFoodDeck()
    {
        var deck = new List<Card>();
        
        // Anthropomorphized Meat - CREATURE, MC:1, H:8, P:0, COMMON
        deck.Add(CreateCreature("Anthropomorphized Meat", "\"There, right there! Did you see it move?\"\n[ABILITY: Attention Hog - 1 MC, Shield]", 
            ElementType.Food, 1, 0, 8, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Attention Hog",
                    Description = "1 MC, Shield",
                    ManaCost = 1,
                    EffectType = EffectType.ShieldSelf,
                    EffectValue = 0,
                    RequiresTarget = false
                }
            }));
        
        // Jubilee Jester - CREATURE, MC:5, H:6, P:6, RARE
        deck.Add(CreateCreature("Jubilee Jester", "\"Hee hee, you can eat me!\" -last words of the jester\n[ABILITY: Joviality - 3MC, +4H+4P BTAPC for one turn, 4 DTS]", 
            ElementType.Food, 5, 6, 6, 3, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Joviality",
                    Description = "3MC, +4H+4P BTAPC for one turn, 4 DTS",
                    ManaCost = 3,
                    EffectType = EffectType.Buff,
                    EffectValue = 4,
                    RequiresTarget = false
                }
            }));
        
        // Last Morsel - SPELL, MC:2, T:SC, UNCOMMON
        deck.Add(CreateSpell("Last Morsel", "Yum, crunchy!\n[ABILITY: Are you going to finish that? - 3 DTT] - If this spell kills a creature, regain 1.5 times that many health points (rounded down)", 
            ElementType.Food, 2, 0, EffectType.Damage, TargetType.Any,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Are you going to finish that?",
                    Description = "3 DTT, if kills creature regain 1.5x health",
                    ManaCost = 0,
                    EffectType = EffectType.Damage,
                    EffectValue = 3,
                    RequiresTarget = true
                }
            }));
        
        // Colonic Tonic - ARTIFACT, MC:3, T:SPC, UNCOMMON
        deck.Add(CreateArtifact("Colonic Tonic", "\"What did you say it does again..?\"\n[ABILITY: Cleanse - Remove all debuffs from target creature]", 
            ElementType.Food, 3,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Cleanse",
                    Description = "Remove all debuffs from target creature",
                    ManaCost = 0,
                    EffectType = EffectType.Heal, // Using heal as a proxy for removing debuffs
                    EffectValue = 0,
                    RequiresTarget = true
                }
            }));
        
        // Tin of Multicolored Tablets - ARTIFACT, MC:2, T:PO, RARE
        deck.Add(CreateArtifact("Tin of Multicolored Tablets", "Yech, that's a gross one.\n[ABILITY: Dry Swallow - Gain a random effect (5% of total health DTS, 15% of total health HTS, +2 MRPT for 2 turns, +2 MT, +1 card draw per turn for 2 turns)]", 
            ElementType.Food, 2));
        
        // Consuming Feast - EVENT, MC:4, H:15, RARE, D:4T
        deck.Add(CreateEvent("Consuming Feast", "\"It's strange all these guys starved to death right here with all this food. Oh well!\"\n[ABILITY: Gnawing Hunger - passive, DBTAEC halved P, Shield, Biteback 3]", 
            ElementType.Food, 4, 4,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Gnawing Hunger",
                    Description = "passive, DBTAEC halved P, Shield, Biteback 3",
                    ManaCost = 0,
                    EffectType = EffectType.Shield,
                    EffectValue = 0,
                    IsPassive = true
                },
                new CardAbility
                {
                    Name = "Biteback",
                    Description = "3 damage to attacker when attacked",
                    ManaCost = 0,
                    EffectType = EffectType.Damage,
                    EffectValue = 3,
                    IsPassive = true
                }
            }));
        
        return deck;
    }
    
    // ============ ELDRITCH (9 cards) ============
    private static List<Card> CreateEldritchDeck()
    {
        var deck = new List<Card>();
        
        // Questing Tendril - CREATURE, MC:1, H:2, P:1, COMMON
        deck.Add(CreateCreature("Questing Tendril", "\"I've seen enough apprentice illustrations to know where this is going...\" -Grandmaster Magus Zerris IV\n[ABILITY: Search - 2 MC, 1 quest/search token]", 
            ElementType.Eldritch, 1, 2, 1, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Search",
                    Description = "2 MC, 1 quest/search token",
                    ManaCost = 2,
                    EffectType = EffectType.Search,
                    EffectValue = 1,
                    RequiresTarget = false
                }
            }));
        
        // Occult Dabbler - CREATURE, MC:2, H:2, P:2, COMMON
        deck.Add(CreateCreature("Occult Dabbler", "\"You want me to do WHAT?\"\n[ABILITY: Fiddle With - 1 MC, double quest/search tokens generated this turn]", 
            ElementType.Eldritch, 2, 2, 2, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Fiddle With",
                    Description = "1 MC, double quest/search tokens generated this turn",
                    ManaCost = 1,
                    EffectType = EffectType.Search,
                    EffectValue = 2, // Double tokens
                    RequiresTarget = false
                }
            }));
        
        // Call of the Deep - EVENT, MC:5, RARE, D:3T after effect
        deck.Add(CreateEvent("Call of the Deep", "They all laughed, until the ritual worked.\n[ABILITY: Elaborate Rite - 5 MC, +2 MRPT for 2 turns] - Summons SOMETHING on the third turn", 
            ElementType.Eldritch, 5, 3,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Elaborate Rite",
                    Description = "5 MC, +2 MRPT for 2 turns, summons something on 3rd turn",
                    ManaCost = 5,
                    EffectType = EffectType.ManaRegen,
                    EffectValue = 2,
                    RequiresTarget = false,
                    IsTemporary = true,
                    Duration = 2
                }
            }));
        
        return deck;
    }
    
    // ============ ELDRITCH QUEST/SEARCH TABLE CARDS (6 cards - only via tokens) ============
    /// These cards are not in the deck builder but can be added to hand via quest tokens
    /// C: probability - chance when 3 quest tokens are accumulated
    public static List<Card> GetQuestTableCards()
    {
        var tableCards = new List<Card>();
        
        // Dwarf Star Spawn - CREATURE, MC:2, C:26%, H:1, P:3, COMMON
        tableCards.Add(CreateCreature("Dwarf Star Spawn", "They hide in the shadows, waiting for an unguarded ear to make a home.\n[ABILITY: Earwig - 1MC, 4DTT, next turn the card does nothing, the turn after it Transforms into a Greater Star Spawn]", 
            ElementType.Eldritch, 2, 1, 3, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Earwig",
                    Description = "1MC, 4DTT, next turn nothing, turn after transforms to Greater Star Spawn",
                    ManaCost = 1,
                    EffectType = EffectType.Damage,
                    EffectValue = 4,
                    RequiresTarget = true
                }
            }));
        
        // Malformed Summon - CREATURE, MC:3, C:25%, H:2, P:3, COMMON
        tableCards.Add(CreateCreature("Malformed Summon", "Whatever this was before, the journey was clearly not kind.\n[ABILITY: Wheeze - 2MC, -2P DBTT]", 
            ElementType.Eldritch, 3, 2, 3, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Wheeze",
                    Description = "2MC, -2P DBTT",
                    ManaCost = 2,
                    EffectType = EffectType.DebuffPower,
                    EffectValue = 2,
                    RequiresTarget = true
                }
            }));
        
        // Eater of Hope - CREATURE, MC:5, C:20%, H:3, P:8, UNCOMMON
        tableCards.Add(CreateCreature("Eater of Hope", "Geez, do they not feed you or something?\n[ABILITY: Otherworldly Hunger - 4MC, Consume]", 
            ElementType.Eldritch, 5, 3, 8, 2, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Otherworldly Hunger",
                    Description = "4MC, Consume (if target H lower, destroy and add 2H)",
                    ManaCost = 4,
                    EffectType = EffectType.Destroy,
                    EffectValue = 0,
                    RequiresTarget = true
                }
            }));
        
        // Greater Star Spawn - CREATURE, MC:5, C:17%, H:3, P:4, UNCOMMON
        tableCards.Add(CreateCreature("Greater Star Spawn", "After growing large in an ear, they crawl forth to aid in bringing about their Lord.\n[ABILITY: Thrash - 3MC, 10DTT]", 
            ElementType.Eldritch, 5, 3, 4, 2, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Thrash",
                    Description = "3MC, 10DTT",
                    ManaCost = 3,
                    EffectType = EffectType.Damage,
                    EffectValue = 10,
                    RequiresTarget = true
                }
            }));
        
        // Herald of Jaa'aird'thuun - CREATURE, MC:4, C:10%, H:4, P:4, RARE
        tableCards.Add(CreateCreature("Herald of Jaa'aird'thuun", "\"Do you have a moment to talk about our Lord and destroyer?\"\n[ABILITY: Proselytize - 3MC, add 2 Infection token/counters]", 
            ElementType.Eldritch, 4, 4, 4, 3, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Proselytize",
                    Description = "3MC, add 2 Infection token/counters",
                    ManaCost = 3,
                    EffectType = EffectType.Infect,
                    EffectValue = 2,
                    RequiresTarget = true
                }
            }));
        
        // Jaa'aird'thuun - CREATURE, MC:7, C:2%, H:8, P:8, LEGENDARY
        tableCards.Add(CreateCreature("Jaa'aird'thuun", "He's real!!! And really... hungry?\n[ABILITY: Gobble - 6 MC, 10 DTAEC]", 
            ElementType.Eldritch, 7, 8, 8, 4, true,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Gobble",
                    Description = "6 MC, 10 DTAEC",
                    ManaCost = 6,
                    EffectType = EffectType.DamageToAllEnemyCreatures,
                    EffectValue = 10,
                    RequiresTarget = false
                }
            }));
        
        return tableCards;
    }
    
    // ============ COMBO (2 cards - multi-element) ============
    private static List<Card> CreateComboDeck()
    {
        var deck = new List<Card>();
        
        // Slightly Glowing Sponge Cakes - CREATURE, MC:3, RADxFOO, H:7, P:1, COMMON
        // Combo of Anthropomorphized Meat + Irradiated Blob
        deck.Add(CreateCreature("Slightly Glowing Sponge Cakes", "See? I knew the only things that would be left were sponge cakes and giant bugs. (combo anthropomorphized meat + irradiated blob)\n[ABILITY: Temptation - 1 MC, BTS +4P, DBTS -3H]", 
            ElementType.Radioactivity, 3, 7, 1, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Temptation",
                    Description = "1 MC, BTS +4P, DBTS -3H",
                    ManaCost = 1,
                    EffectType = EffectType.Buff,
                    EffectValue = 4,
                    RequiresTarget = false
                }
            }));
        
        // Tempature Mote - CREATURE, MC:3, THExTHE, H:2, P:2, COMMON
        // Combo of Heat Mote + Cold Mote
        deck.Add(CreateCreature("Tempature Mote", "A cold snap just waiting to happen.\n[ABILITY: Tempature Gradient(both) - passive, doubles +P&+H buff values and durations]", 
            ElementType.Thermodynamics, 3, 2, 2, 1, false,
            new List<CardAbility>
            {
                new CardAbility
                {
                    Name = "Tempature Gradient(both)",
                    Description = "passive, doubles +P&+H buff values and durations",
                    ManaCost = 0,
                    EffectType = EffectType.Buff,
                    EffectValue = 0,
                    IsPassive = true
                }
            }));
        
        return deck;
    }
    
    // ============ CARD CREATION HELPERS ============
    
    private static Card CreateCreature(string name, string description, 
        ElementType element, int manaCost, int power, int health, int rarity = 1, bool isLegendary = false,
        List<CardAbility>? abilities = null)
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
            Abilities = abilities ?? new List<CardAbility>(),
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
        ElementType element, int manaCost, int power, EffectType effectType, TargetType target,
        List<CardAbility>? abilities = null)
    {
        var spell = new SpellCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Power = power,
            Rarity = 1,
            Abilities = abilities ?? new List<CardAbility>()
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
        ElementType element, int manaCost, List<CardAbility>? abilities = null)
    {
        return new ArtifactCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Rarity = 1,
            Abilities = abilities ?? new List<CardAbility>()
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
            TargetType = WeaponTargetType.DamageToOpponent,
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

    private static Card CreateWeapon(string name, string description,
        ElementType element, int manaCost, int power, WeaponTargetType targetType)
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
            TargetType = targetType,
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
        ElementType element, int manaCost, int effectValue, List<CardAbility>? abilities = null)
    {
        var eventCard = new EventCard
        {
            Name = name,
            Description = description,
            Element = element,
            ManaCost = manaCost,
            Rarity = 1,
            Power = effectValue,
            Abilities = abilities ?? new List<CardAbility>()
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
    
    /// <summary>
    /// Get pre-made combination cards.
    /// These cards should have names starting with "combine_" followed by the source card IDs.
    /// Format: "combine_{id1}_{id2}" where id1 and id2 are the card IDs being combined.
    /// Example: "combine_radiatedcockroach_glowingslime" for combining those two cards.
    /// </summary>
    public static List<Card> GetCustomCombinationCards()
    {
        // TODO: Add pre-made combination cards here
        // Example format:
        // var comboCard = CreateCreature("combine_abc123_def456", "A powerful combination", ElementType.Radioactivity, 3, 4, 5, 2);
        // return new List<Card> { comboCard };
        
        return new List<Card>();
    }
}
