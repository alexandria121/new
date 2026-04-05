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

    public CardViewModel Clone() => new(_card.Clone());
}
