using MagicalDeckbuilder.Cards;
using CardType = MagicalDeckbuilder.Cards.CardType;
using ElementType = MagicalDeckbuilder.Cards.ElementType;
using EffectType = MagicalDeckbuilder.Cards.EffectType;
using TargetType = MagicalDeckbuilder.Cards.TargetType;

namespace NewGame.UI.ViewModels;

public class CardViewModel : ViewModelBase
{
    private readonly Card _card;
    private bool _isSelected;
    private bool _isDragging;

    public CardViewModel(Card card)
    {
        _card = card ?? throw new ArgumentNullException(nameof(card));
    }

    public Card Card => _card;
    public string Id => _card.Id;
    public string Name => _card.Name;
    public string Description => _card.Description;
    public CardType Type => _card.Type;
    public ElementType Element => _card.Element;
    public int ManaCost => _card.ManaCost;
    public int Power => _card.Power;
    public int Health => _card.Health;
    public bool IsLegendary => _card.IsLegendary;
    public int Rarity => _card.Rarity;
    public IReadOnlyList<CardEffect> Effects => _card.Effects;

    public string EffectsText => Effects.Count > 0
        ? string.Join(", ", Effects.Select(e => e.Name))
        : "";

    public bool HasEffects => Effects.Count > 0;

    /// <summary>
    /// Detailed effects text showing effect name, value, and target
    /// </summary>
    public string EffectsDetailText => Effects.Count > 0
        ? string.Join("\n", Effects.Select(e => $"• {GetEffectIcon(e.Type)} {e.Name} ({e.Value}) → {FormatTarget(e.Target)}"))
        : "";

    /// <summary>
    /// Formatted string for tooltip showing all effect descriptions
    /// </summary>
    public string EffectsTooltipText => Effects.Count > 0
        ? string.Join("\n\n", Effects.Select(e => $"{GetEffectIcon(e.Type)} {e.Name}\n{e.Description}\nValue: {e.Value} | Target: {FormatTarget(e.Target)}"))
        : "No effects";

    /// <summary>
    /// Number of effects on the card
    /// </summary>
    public int EffectCount => Effects.Count;

    /// <summary>
    /// Gets the icon for an effect type
    /// </summary>
    public static string GetEffectIcon(EffectType type) => type switch
    {
        EffectType.Damage => "⚔",
        EffectType.Heal => "❤",
        EffectType.DrawCard => "🃏",
        EffectType.Buff => "⬆",
        EffectType.Debuff => "⬇",
        EffectType.Shield => "🛡",
        EffectType.ManaGain => "💧",
        EffectType.Destroy => "💀",
        EffectType.Duplicate => "👥",
        EffectType.Transform => "🔄",
        _ => "✨"
    };

    /// <summary>
    /// Gets the element icon
    /// </summary>
    public string ElementIcon => Element switch
    {
        ElementType.Radioactivity => "☢",
        ElementType.Flesh => "💀",
        ElementType.Toxin => "☠",
        ElementType.Fungus => "🍄",
        ElementType.Thermodynamics => "🔥",
        ElementType.Time => "⏳",
        ElementType.Food => "🍖",
        ElementType.Eldritch => "👁",
        _ => "❓"
    };

    /// <summary>
    /// Gets the element emoji for larger displays
    /// </summary>
    public string ElementEmoji => Element switch
    {
        ElementType.Radioactivity => "☢️",
        ElementType.Flesh => "🫀",
        ElementType.Toxin => "☠️",
        ElementType.Fungus => "🍄",
        ElementType.Thermodynamics => "🔥",
        ElementType.Time => "⏳",
        ElementType.Food => "🍖",
        ElementType.Eldritch => "👁️",
        _ => "❓"
    };

    /// <summary>
    /// Gets the rarity gemstone symbol
    /// </summary>
    public string RarityGem => Rarity switch
    {
        1 => "◆",
        2 => "◆",
        3 => "◆",
        4 => "★",
        _ => "◇"
    };

    private static string FormatTarget(TargetType target) => target switch
    {
        TargetType.Self => "Self",
        TargetType.Enemy => "Enemy",
        TargetType.Ally => "Ally",
        TargetType.Any => "Any",
        TargetType.AllEnemies => "All Enemies",
        TargetType.AllAllies => "All Allies",
        _ => "Unknown"
    };

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsDragging
    {
        get => _isDragging;
        set => SetProperty(ref _isDragging, value);
    }

    public string TypeColor => Type switch
    {
        CardType.Spell => "#FF6464",
        CardType.Creature => "#64FF64",
        CardType.Artifact => "#C8C864",
        CardType.Enchantment => "#9664C8",
        CardType.Weapon => "#FF9632",
        CardType.Armor => "#6496C8",
        CardType.Event => "#FFC864",
        CardType.Blank => "#C8C8C8",
        _ => "#808080"
    };

    public string ElementColor => Element switch
    {
        ElementType.Radioactivity => "#00FF64",
        ElementType.Flesh => "#C86464",
        ElementType.Toxin => "#96C800",
        ElementType.Fungus => "#649664",
        ElementType.Thermodynamics => "#FF6432",
        ElementType.Time => "#9664C8",
        ElementType.Food => "#C89664",
        ElementType.Eldritch => "#6400C8",
        _ => "#808080"
    };

    public string RarityText => Rarity switch
    {
        1 => "Common",
        2 => "Uncommon",
        3 => "Rare",
        4 => "Legendary",
        _ => "Unknown"
    };

    public string RarityColor => Rarity switch
    {
        1 => "#B4B4B4",
        2 => "#64C864",
        3 => "#3264C8",
        4 => "#C89632",
        _ => "#808080"
    };

    /// <summary>
    /// Returns the badge symbol for the card type
    /// Sword for creatures, wand for spells, tent for events, etc.
    /// </summary>
    public string TypeBadgeSymbol => Type switch
    {
        CardType.Creature => "⚔",      // Sword
        CardType.Spell => "🪄",          // Wand/magic
        CardType.Event => "⛺",          // Tent
        CardType.Artifact => "🏺",       // Amphora/artifact
        CardType.Weapon => "⚔",         // Sword
        CardType.Armor => "🛡",          // Shield
        CardType.Enchantment => "✨",   // Sparkles
        CardType.Blank => "⬜",          // Blank square
        _ => "📄"                        // Default document
    };

    /// <summary>
    /// Returns the badge tooltip text for the card type
    /// </summary>
    public string TypeBadgeTooltip => Type switch
    {
        CardType.Creature => "Creature - Can attack and defend",
        CardType.Spell => "Spell - One-time effect",
        CardType.Event => "Event - World-altering effect",
        CardType.Artifact => "Artifact - Permanent equipment",
        CardType.Weapon => "Weapon - Equippable for damage",
        CardType.Armor => "Armor - Equippable for defense",
        CardType.Enchantment => "Enchantment - Permanent enhancement",
        CardType.Blank => "Blank - Can be infused",
        _ => "Card"
    };

    public CardViewModel Clone() => new(_card.Clone());
    
    /// <summary>
    /// Check if this card can be afforded with the given available mana
    /// </summary>
    public bool CanAffordWith(int availableMana) => ManaCost <= availableMana;
}
