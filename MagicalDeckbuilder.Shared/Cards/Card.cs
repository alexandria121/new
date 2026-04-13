using System.Collections.Generic;
using System.Linq;

namespace MagicalDeckbuilder.Cards;

/// <summary>
/// Represents the type of a card in the game
/// </summary>
public enum CardType
{
    Spell,
    Creature,
    Artifact,
    Enchantment,
    Weapon,
    Armor,
    Event,
    Blank
}

/// <summary>
/// Represents an ability that can be triggered by paying mana
/// </summary>
public class CardAbility
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int ManaCost { get; set; } // MC: X
    public EffectType EffectType { get; set; }
    public int EffectValue { get; set; } // The value (damage, buff amount, etc.)
    public TargetType Target { get; set; } = TargetType.Any;
    public bool IsTemporary { get; set; }
    public int Duration { get; set; } // D: X turns (0 if permanent)
    public bool IsPassive { get; set; } // passive abilities
    public bool RequiresTarget { get; set; } // true if needs a target (DTT, DBTT, etc.)
}

/// <summary>
/// Represents an effect that a card can have
/// </summary>
public class CardEffect
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public EffectType Type { get; set; }
    public int Value { get; set; }
    public TargetType Target { get; set; }
    public bool IsTemporary { get; set; }
}

/// <summary>
/// Types of effects cards can have
/// </summary>
public enum EffectType
{
    Damage,
    Heal,
    DrawCard,
    Buff,
    Debuff,
    Shield,
    ManaGain,
    Destroy,
    Duplicate,
    Transform,
    Search,           // Adds quest/search token
    BuffPower,        // Buff to power (BTS +P)
    BuffHealth,       // Buff to health (BTS +H)
    DebuffPower,     // Debuff to power (DBTT -P)
    DebuffHealth,    // Debuff to health (DBTT -H)
    DamageToAll,     // Damage to all (DTA)
    DamageToAllCreatures, // DTAC
    DamageToAllEnemyCreatures, // DTAEC
    DamageToSelf,    // DTS - damage to self
    BuffAllPlayerCreatures, // BTAPC - buff all player creatures
    HealSelf,        // HTS - heal to self
    ManaRegen,       // MRPT - mana regen per turn
    ShieldSelf,      // Shield self
    Biteback,        // Counterattack when attacked
    Infect,          // Add infection tokens
    DisableAttack,   // Disable creature from attacking (Astral Eviction)
    EldritchSummon,  // Summon from Eldritch table after X turns (Call of the Deep)
    Spawn,          // Spawn a creature (Mycelium Spore, etc.) in an empty slot
    
    // New armor effect types
    NegateDamage,    // Negates all damage (Suspicious Suit, Aegis of Jaa'aird'thuun)
    Temporary,       // Temporary effect that expires after X turns
    OnHitDebuff,      // Debuffs attacker when hit (Virulent Vambrace)
    RevealHand      // Makes opponent's hand visible for a turn (Jar of Eyes)
}

/// <summary>
/// Who or what an effect targets
/// </summary>
public enum TargetType
{
    Self,
    Enemy,
    Ally,
    Any,
    AllEnemies,
    AllAllies
}

/// <summary>
/// Element type for cards (for combining/ synergies)
/// </summary>
public enum ElementType
{
    Radioactivity,
    Flesh,
    Toxin,
    Fungus,
    Thermodynamics,
    Time,
    Food,
    Eldritch
}

/// <summary>
/// Target type for weapons - what the weapon damages
/// </summary>
public enum WeaponTargetType
{
    DamageToOpponent,
    DamageToCreatures
}

/// <summary>
/// Base class representing a card in the game
/// </summary>
public class Card
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Template ID for lookups: 1-200 for base cards, 1000-2000 for combo cards
    /// </summary>
    public int TemplateId { get; set; }
    
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public CardType Type { get; set; }
    public ElementType Element { get; set; }
    public int ManaCost { get; set; }
    public int Power { get; set; }
    public int Health { get; set; }
    public List<CardEffect> Effects { get; set; } = new();
    
    /// <summary>
    /// Active abilities that can be triggered (stored as effects for simplicity)
    /// Format: each ability is a CardEffect with mana cost in a special field
    /// </summary>
    public List<CardAbility> Abilities { get; set; } = new();
    
    public bool IsLegendary { get; set; }
    public int Rarity { get; set; } // 1 = common, 2 = uncommon, 3 = rare, 4 = legendary
    public bool IsCombinable { get; set; } = true; // true if this card can be used in the combine system
    public bool IsComboOnly { get; set; } // true if this card can only be obtained via combining, not added to decks
    
    /// <summary>
    /// Temperature Gradient passive: "up" doubles Power buff amounts/duration, "down" doubles Health buff amounts/duration
    /// </summary>
    public string TemperatureGradient { get; set; } // "" = none, "up" = power, "down" = health, "both" = both

    public virtual Card Clone()
    {
        return new Card
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = this.TemplateId,
            Name = this.Name,
            Description = this.Description,
            Type = this.Type,
            Element = this.Element,
            ManaCost = this.ManaCost,
            Power = this.Power,
            Health = this.Health,
            Effects = this.Effects.Select(e => new CardEffect
            {
                Name = e.Name,
                Description = e.Description,
                Type = e.Type,
                Value = e.Value,
                Target = e.Target,
                IsTemporary = e.IsTemporary
            }).ToList(),
            Abilities = this.Abilities.Select(a => new CardAbility
            {
                Name = a.Name,
                Description = a.Description,
                ManaCost = a.ManaCost,
                EffectType = a.EffectType,
                EffectValue = a.EffectValue,
                Target = a.Target,
                IsTemporary = a.IsTemporary,
                Duration = a.Duration,
                IsPassive = a.IsPassive,
                RequiresTarget = a.RequiresTarget
            }).ToList(),
            IsLegendary = this.IsLegendary,
            Rarity = this.Rarity,
            IsCombinable = this.IsCombinable,
            IsComboOnly = this.IsComboOnly
        };
    }

    public virtual string GetDisplayInfo()
    {
        return $"[{Element}] {Name} - {ManaCost} Mana | {Type} | Power: {Power} | Health: {Health}\n  {Description}";
    }
}

/// <summary>
/// Represents a creature card that can attack and defend
/// </summary>
public class CreatureCard : Card
{
    public bool CanAttack { get; set; } = true;
    public bool HasAttacked { get; set; }

    public CreatureCard()
    {
        Type = CardType.Creature;
    }

    public override Card Clone()
    {
        var clone = new CreatureCard
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = this.TemplateId,
            Name = this.Name,
            Description = this.Description,
            Type = this.Type,
            Element = this.Element,
            ManaCost = this.ManaCost,
            Power = this.Power,
            Health = this.Health,
            Effects = this.Effects.Select(e => new CardEffect
            {
                Name = e.Name,
                Description = e.Description,
                Type = e.Type,
                Value = e.Value,
                Target = e.Target,
                IsTemporary = e.IsTemporary
            }).ToList(),
            Abilities = this.Abilities.Select(a => new CardAbility
            {
                Name = a.Name,
                Description = a.Description,
                ManaCost = a.ManaCost,
                EffectType = a.EffectType,
                EffectValue = a.EffectValue,
                Target = a.Target,
                IsTemporary = a.IsTemporary,
                Duration = a.Duration,
                IsPassive = a.IsPassive,
                RequiresTarget = a.RequiresTarget
            }).ToList(),
            IsLegendary = this.IsLegendary,
            Rarity = this.Rarity,
            CanAttack = this.CanAttack,
            HasAttacked = this.HasAttacked
        };
        return clone;
    }
    
    public void Reset() 
    {
        CanAttack = false;
    }
}

/// <summary>
/// Represents a spell card that has一次性 effects
/// </summary>
public class SpellCard : Card
{
    public SpellCard()
    {
        Type = CardType.Spell;
    }

    public override Card Clone()
    {
        return new SpellCard
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = this.TemplateId,
            Name = this.Name,
            Description = this.Description,
            Type = this.Type,
            Element = this.Element,
            ManaCost = this.ManaCost,
            Power = this.Power,
            Health = this.Health,
            Effects = this.Effects.Select(e => new CardEffect
            {
                Name = e.Name,
                Description = e.Description,
                Type = e.Type,
                Value = e.Value,
                Target = e.Target,
                IsTemporary = e.IsTemporary
            }).ToList(),
            Abilities = this.Abilities.Select(a => new CardAbility
            {
                Name = a.Name,
                Description = a.Description,
                ManaCost = a.ManaCost,
                EffectType = a.EffectType,
                EffectValue = a.EffectValue,
                RequiresTarget = a.RequiresTarget,
                IsPassive = a.IsPassive,
                Duration = a.Duration,
                Target = a.Target
            }).ToList(),
            IsLegendary = this.IsLegendary,
            Rarity = this.Rarity,
            IsCombinable = this.IsCombinable,
            IsComboOnly = this.IsComboOnly
        };
    }
}

/// <summary>
/// Represents an artifact that provides persistent effects
/// </summary>
public class ArtifactCard : Card
{
    public ArtifactCard()
    {
        Type = CardType.Artifact;
    }

    public override Card Clone()
    {
        var clone = new ArtifactCard
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = this.TemplateId,
            Name = this.Name,
            Description = this.Description,
            Type = this.Type,
            Element = this.Element,
            ManaCost = this.ManaCost,
            Power = this.Power,
            Health = this.Health,
            Effects = this.Effects.Select(e => new CardEffect
            {
                Name = e.Name,
                Description = e.Description,
                Type = e.Type,
                Value = e.Value,
                Target = e.Target,
                IsTemporary = e.IsTemporary
            }).ToList(),
            Abilities = this.Abilities.Select(a => new CardAbility
            {
                Name = a.Name,
                Description = a.Description,
                ManaCost = a.ManaCost,
                EffectType = a.EffectType,
                EffectValue = a.EffectValue,
                RequiresTarget = a.RequiresTarget,
                IsPassive = a.IsPassive,
                Duration = a.Duration,
                Target = a.Target
            }).ToList(),
            IsLegendary = this.IsLegendary,
            Rarity = this.Rarity,
            IsCombinable = this.IsCombinable,
            IsComboOnly = this.IsComboOnly
        };
        return clone;
    }
}

/// <summary>
/// Represents an event card that effects the player character
/// </summary>
public class EventCard : Card
{
    public EventCard()
    {
        Type = CardType.Event;
    }

    public override Card Clone()
    {
        return new EventCard
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = this.TemplateId,
            Name = this.Name,
            Description = this.Description,
            Type = this.Type,
            Element = this.Element,
            ManaCost = this.ManaCost,
            Power = this.Power,
            Health = this.Health,
            Effects = this.Effects.Select(e => new CardEffect
            {
                Name = e.Name,
                Description = e.Description,
                Type = e.Type,
                Value = e.Value,
                Target = e.Target,
                IsTemporary = e.IsTemporary
            }).ToList(),
            Abilities = this.Abilities.Select(a => new CardAbility
            {
                Name = a.Name,
                Description = a.Description,
                ManaCost = a.ManaCost,
                EffectType = a.EffectType,
                EffectValue = a.EffectValue,
                RequiresTarget = a.RequiresTarget,
                IsPassive = a.IsPassive,
                Duration = a.Duration,
                Target = a.Target
            }).ToList(),
            IsLegendary = this.IsLegendary,
            Rarity = this.Rarity,
            IsCombinable = this.IsCombinable,
            IsComboOnly = this.IsComboOnly
        };
    }
}

/// <summary>
/// Represents a blank card - unfinished cards useful only for combining
/// </summary>
public class BlankCard : Card
{
    public BlankCard()
    {
        Type = CardType.Blank;
    }

    public override Card Clone()
    {
        return new BlankCard
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = this.TemplateId,
            Name = this.Name,
            Description = this.Description,
            Type = this.Type,
            Element = this.Element,
            ManaCost = this.ManaCost,
            Power = this.Power,
            Health = this.Health,
            Effects = this.Effects.Select(e => new CardEffect
            {
                Name = e.Name,
                Description = e.Description,
                Type = e.Type,
                Value = e.Value,
                Target = e.Target,
                IsTemporary = e.IsTemporary
            }).ToList(),
            IsLegendary = this.IsLegendary,
            Rarity = this.Rarity,
            IsCombinable = this.IsCombinable,
            IsComboOnly = this.IsComboOnly
        };
    }
}

/// <summary>
/// Represents a weapon card that can be equipped to deal damage
/// </summary>
public class WeaponCard : Card
{
    public WeaponTargetType TargetType { get; set; } = WeaponTargetType.DamageToOpponent;
    
    /// <summary>
    /// Power growth per hit - for weapons like Disgusting! that gain power each time they deal damage
    /// </summary>
    public int PowerGrowth { get; set; }
    
    /// <summary>
    /// Number of times this weapon has dealt damage (for tracking power growth)
    /// </summary>
    public int HitCount { get; set; }

    public WeaponCard()
    {
        Type = CardType.Weapon;
    }

    public override Card Clone()
    {
        return new WeaponCard
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = this.TemplateId,
            Name = this.Name,
            Description = this.Description,
            Type = this.Type,
            Element = this.Element,
            ManaCost = this.ManaCost,
            Power = this.Power,
            Health = this.Health,
            Effects = this.Effects.Select(e => new CardEffect
            {
                Name = e.Name,
                Description = e.Description,
                Type = e.Type,
                Value = e.Value,
                Target = e.Target,
                IsTemporary = e.IsTemporary
            }).ToList(),
            Abilities = this.Abilities.Select(a => new CardAbility
            {
                Name = a.Name,
                Description = a.Description,
                ManaCost = a.ManaCost,
                EffectType = a.EffectType,
                EffectValue = a.EffectValue,
                RequiresTarget = a.RequiresTarget,
                IsPassive = a.IsPassive,
                Duration = a.Duration,
                Target = a.Target
            }).ToList(),
            IsLegendary = this.IsLegendary,
            Rarity = this.Rarity,
            TargetType = this.TargetType,
            PowerGrowth = this.PowerGrowth,
            HitCount = this.HitCount
        };
    }
}

/// <summary>
/// Represents an armor card that provides defensive capabilities
/// </summary>
public class ArmorCard : Card
{
    /// <summary>
    /// Passive damage reduction - reduces all incoming damage by this amount
    /// </summary>
    public int DamageReduction { get; set; }
    
    /// <summary>
    /// Has Biteback effect - deals damage back to attacker
    /// </summary>
    public bool HasBiteback { get; set; }
    
    /// <summary>
    /// Biteback damage amount
    /// </summary>
    public int BitebackDamage { get; set; }
    
    /// <summary>
    /// Negates all incoming damage (for Suspicious Suit, Aegis of Jaa'aird'thuun)
    /// </summary>
    public bool NegatesAllDamage { get; set; }
    
    /// <summary>
    /// Number of turns until armor expires (for temporary armor)
    /// </summary>
    public int TurnsRemaining { get; set; }
    
    /// <summary>
    /// Has OnHitDebuff - debuffs attacker when hit
    /// </summary>
    public bool HasOnHitDebuff { get; set; }
    
    /// <summary>
    /// OnHitDebuff power reduction amount
    /// </summary>
    public int OnHitDebuffPower { get; set; }
    
    /// <summary>
    /// OnHitDebuff health reduction amount
    /// </summary>
    public int OnHitDebuffHealth { get; set; }
    
    /// <summary>
    /// Has OnHitSpawn - spawns a creature when hit (50% chance for Disintegrating Regalia)
    /// </summary>
    public bool HasOnHitSpawn { get; set; }
    
    /// <summary>
    /// Card ID to spawn when hit
    /// </summary>
    public string? SpawnCardId { get; set; }
    
    /// <summary>
    /// Chance to spawn (0-100)
    /// </summary>
    public int SpawnChance { get; set; }
    
    /// <summary>
    /// Has Intimidation effect - weakens attacking creatures
    /// </summary>
    public bool HasIntimidation { get; set; }
    
    /// <summary>
    /// Intimidation power threshold (creatures with power <= this cannot attack)
    /// </summary>
    public int IntimidationPowerThreshold { get; set; }
    
    /// <summary>
    /// Intimidation health threshold (creatures with health <= this cannot attack)
    /// </summary>
    public int IntimidationHealthThreshold { get; set; }

    public ArmorCard()
    {
        Type = CardType.Armor;
    }

    public override Card Clone()
    {
        return new ArmorCard
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = this.TemplateId,
            Name = this.Name,
            Description = this.Description,
            Type = this.Type,
            Element = this.Element,
            ManaCost = this.ManaCost,
            Power = this.Power,
            Health = this.Health,
            DamageReduction = this.DamageReduction,
            HasBiteback = this.HasBiteback,
            BitebackDamage = this.BitebackDamage,
            NegatesAllDamage = this.NegatesAllDamage,
            TurnsRemaining = this.TurnsRemaining,
            HasOnHitDebuff = this.HasOnHitDebuff,
            OnHitDebuffPower = this.OnHitDebuffPower,
            OnHitDebuffHealth = this.OnHitDebuffHealth,
            HasOnHitSpawn = this.HasOnHitSpawn,
            SpawnCardId = this.SpawnCardId,
            SpawnChance = this.SpawnChance,
            HasIntimidation = this.HasIntimidation,
            IntimidationPowerThreshold = this.IntimidationPowerThreshold,
            IntimidationHealthThreshold = this.IntimidationHealthThreshold,
            Effects = this.Effects.Select(e => new CardEffect
            {
                Name = e.Name,
                Description = e.Description,
                Type = e.Type,
                Value = e.Value,
                Target = e.Target,
                IsTemporary = e.IsTemporary
            }).ToList(),
            Abilities = this.Abilities.Select(a => new CardAbility
            {
                Name = a.Name,
                Description = a.Description,
                ManaCost = a.ManaCost,
                EffectType = a.EffectType,
                EffectValue = a.EffectValue,
                RequiresTarget = a.RequiresTarget,
                IsPassive = a.IsPassive,
                Duration = a.Duration,
                Target = a.Target
            }).ToList(),
            IsLegendary = this.IsLegendary,
            Rarity = this.Rarity,
            IsCombinable = this.IsCombinable,
            IsComboOnly = this.IsComboOnly
        };
    }
}
