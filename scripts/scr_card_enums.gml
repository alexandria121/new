/// Card type constants matching MagicalDeckbuilder.Cards.CardType C# enum
enum CardType {
    Spell = 0,
    Creature = 1,
    Artifact = 2,
    Enchantment = 3,
    Weapon = 4,
    Armor = 5,
    Event = 6,
    Blank = 7
}

/// Effect type constants matching MagicalDeckbuilder.Cards.EffectType C# enum
enum EffectType {
    Damage = 0,
    Heal = 1,
    DrawCard = 2,
    Buff = 3,
    Debuff = 4,
    Shield = 5,
    ManaGain = 6,
    Destroy = 7,
    Duplicate = 8,
    Transform = 9,
    Search = 10,
    BuffPower = 11,
    BuffHealth = 12,
    DebuffPower = 13,
    DebuffHealth = 14,
    DamageToAll = 15,
    DamageToAllCreatures = 16,
    DamageToAllEnemyCreatures = 17,
    DamageToSelf = 18,
    BuffAllPlayerCreatures = 19,
    HealSelf = 20,
    ManaRegen = 21,
    ShieldSelf = 22,
    Biteback = 23,
    Infect = 24,
    DisableAttack = 25,
    EldritchSummon = 26,
    Spawn = 27,
    NegateDamage = 28,
    Temporary = 29,
    OnHitDebuff = 30,
    RevealHand = 31
}

/// Target type constants matching MagicalDeckbuilder.Cards.TargetType C# enum
enum TargetType {
    Self = 0,
    Enemy = 1,
    Ally = 2,
    Any = 3,
    AllEnemies = 4,
    AllAllies = 5
}

/// Element type constants matching MagicalDeckbuilder.Cards.ElementType C# enum
enum ElementType {
    Radioactivity = 0,
    Flesh = 1,
    Toxin = 2,
    Fungus = 3,
    Thermodynamics = 4,
    Time = 5,
    Food = 6,
    Eldritch = 7
}

/// Weapon target type constants matching MagicalDeckbuilder.Cards.WeaponTargetType C# enum
enum WeaponTargetType {
    DamageToOpponent = 0,
    DamageToCreatures = 1
}