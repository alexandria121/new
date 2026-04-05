using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CardType = MagicalDeckbuilder.Cards.CardType;
using ElementType = MagicalDeckbuilder.Cards.ElementType;
using EffectType = MagicalDeckbuilder.Cards.EffectType;

namespace NewGame.UI.Converters;

/// <summary>
/// Converts ElementType to a gradient brush for card borders
/// </summary>
public class ElementToGradientConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ElementType element)
        {
            return element switch
            {
                ElementType.Radioactivity => new LinearGradientBrush(Color.FromRgb(0, 255, 100), Color.FromRgb(0, 204, 80), 45),
                ElementType.Flesh => new LinearGradientBrush(Color.FromRgb(200, 100, 100), Color.FromRgb(160, 80, 80), 45),
                ElementType.Toxin => new LinearGradientBrush(Color.FromRgb(150, 200, 0), Color.FromRgb(120, 160, 0), 45),
                ElementType.Fungus => new LinearGradientBrush(Color.FromRgb(100, 150, 100), Color.FromRgb(80, 120, 80), 45),
                ElementType.Thermodynamics => new LinearGradientBrush(Color.FromRgb(255, 100, 50), Color.FromRgb(204, 80, 40), 45),
                ElementType.Time => new LinearGradientBrush(Color.FromRgb(150, 100, 200), Color.FromRgb(120, 80, 160), 45),
                ElementType.Food => new LinearGradientBrush(Color.FromRgb(200, 150, 100), Color.FromRgb(160, 120, 80), 45),
                ElementType.Eldritch => new LinearGradientBrush(Color.FromRgb(100, 0, 200), Color.FromRgb(80, 0, 160), 45),
                _ => new SolidColorBrush(Color.FromRgb(128, 128, 128))
            };
        }
        return new SolidColorBrush(Color.FromRgb(128, 128, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts ElementType to a glow drop shadow effect
/// </summary>
public class ElementToGlowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ElementType element)
        {
            return element switch
            {
                ElementType.Radioactivity => new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(0, 255, 100), BlurRadius = 15, ShadowDepth = 0, Opacity = 0.7 },
                ElementType.Flesh => new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(200, 100, 100), BlurRadius = 15, ShadowDepth = 0, Opacity = 0.7 },
                ElementType.Toxin => new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(150, 200, 0), BlurRadius = 15, ShadowDepth = 0, Opacity = 0.7 },
                ElementType.Fungus => new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(100, 150, 100), BlurRadius = 15, ShadowDepth = 0, Opacity = 0.7 },
                ElementType.Thermodynamics => new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(255, 100, 50), BlurRadius = 15, ShadowDepth = 0, Opacity = 0.7 },
                ElementType.Time => new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(150, 100, 200), BlurRadius = 15, ShadowDepth = 0, Opacity = 0.7 },
                ElementType.Food => new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(200, 150, 100), BlurRadius = 15, ShadowDepth = 0, Opacity = 0.7 },
                ElementType.Eldritch => new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(100, 0, 200), BlurRadius = 15, ShadowDepth = 0, Opacity = 0.7 },
                _ => new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Gray, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.5 }
            };
        }
        return new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Gray, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.5 };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts Rarity (int) to a gradient brush for legendary/rare cards
/// </summary>
public class RarityToGradientConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int rarity)
        {
            return rarity switch
            {
                4 => new LinearGradientBrush(Color.FromRgb(255, 215, 0), Color.FromRgb(200, 150, 50), 45), // Legendary - gold
                3 => new LinearGradientBrush(Color.FromRgb(80, 128, 255), Color.FromRgb(50, 100, 200), 45), // Rare - blue
                2 => new LinearGradientBrush(Color.FromRgb(128, 224, 128), Color.FromRgb(100, 200, 100), 45), // Uncommon - green
                _ => new LinearGradientBrush(Color.FromRgb(180, 180, 180), Color.FromRgb(150, 150, 150), 45) // Common - gray
            };
        }
        return new LinearGradientBrush(Color.FromRgb(128, 128, 128), Color.FromRgb(100, 100, 100), 45);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts Rarity to a glow effect for high-rarity cards
/// </summary>
public class RarityToGlowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int rarity)
        {
            return rarity switch
            {
                4 => new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(255, 215, 0), BlurRadius = 20, ShadowDepth = 0, Opacity = 0.9 }, // Legendary - gold glow
                3 => new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(80, 128, 255), BlurRadius = 15, ShadowDepth = 0, Opacity = 0.7 }, // Rare - blue glow
                _ => null // Common/Uncommon - no glow
            };
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CardType type)
        {
            return type switch
            {
                CardType.Spell => new SolidColorBrush(Color.FromRgb(255, 100, 100)),
                CardType.Creature => new SolidColorBrush(Color.FromRgb(100, 255, 100)),
                CardType.Artifact => new SolidColorBrush(Color.FromRgb(200, 200, 100)),
                CardType.Enchantment => new SolidColorBrush(Color.FromRgb(150, 100, 200)),
                CardType.Weapon => new SolidColorBrush(Color.FromRgb(255, 150, 50)),
                CardType.Armor => new SolidColorBrush(Color.FromRgb(100, 150, 200)),
                CardType.Event => new SolidColorBrush(Color.FromRgb(255, 200, 100)),
                CardType.Blank => new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                _ => new SolidColorBrush(Color.FromRgb(128, 128, 128))
            };
        }
        return new SolidColorBrush(Color.FromRgb(128, 128, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ElementToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ElementType element)
        {
            return element switch
            {
                ElementType.Radioactivity => new SolidColorBrush(Color.FromRgb(0, 255, 100)),
                ElementType.Flesh => new SolidColorBrush(Color.FromRgb(200, 100, 100)),
                ElementType.Toxin => new SolidColorBrush(Color.FromRgb(150, 200, 0)),
                ElementType.Fungus => new SolidColorBrush(Color.FromRgb(100, 150, 100)),
                ElementType.Thermodynamics => new SolidColorBrush(Color.FromRgb(255, 100, 50)),
                ElementType.Time => new SolidColorBrush(Color.FromRgb(150, 100, 200)),
                ElementType.Food => new SolidColorBrush(Color.FromRgb(200, 150, 100)),
                ElementType.Eldritch => new SolidColorBrush(Color.FromRgb(100, 0, 150)),
                _ => new SolidColorBrush(Color.FromRgb(128, 128, 128))
            };
        }
        return new SolidColorBrush(Color.FromRgb(128, 128, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RarityToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int rarity)
        {
            return rarity switch
            {
                1 => new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                2 => new SolidColorBrush(Color.FromRgb(100, 200, 100)),
                3 => new SolidColorBrush(Color.FromRgb(50, 100, 200)),
                4 => new SolidColorBrush(Color.FromRgb(200, 150, 50)),
                _ => new SolidColorBrush(Color.FromRgb(128, 128, 128))
            };
        }
        return new SolidColorBrush(Color.FromRgb(128, 128, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IsCreatureConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CardType type)
        {
            return type == CardType.Creature ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToTurnTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isPlayerTurn)
        {
            return isPlayerTurn ? "Your Turn" : "Enemy Turn";
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToTurnColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isPlayerTurn)
        {
            return isPlayerTurn
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                : new SolidColorBrush(Color.FromRgb(244, 67, 54));
        }
        return new SolidColorBrush(Color.FromRgb(128, 128, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NotMenuToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string view)
        {
            return view == "Menu" ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringEqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && parameter is string param)
        {
            return str == param ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a card's mana cost compared to available mana to determine if it's playable
/// Returns a brush that highlights playable cards in green, unplayable in red overlay
/// </summary>
public class CanAffordToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // value is the card's ManaCost, parameter should be the available mana
        if (value is int manaCost && parameter is int availableMana)
        {
            return manaCost <= availableMana
                ? new SolidColorBrush(Color.FromRgb(70, 110, 70)) // Playable - subtle green tint
                : new SolidColorBrush(Color.FromRgb(110, 50, 50)); // Not affordable - subtle red tint
        }
        return new SolidColorBrush(Color.FromRgb(70, 70, 70));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean to an opacity value for card visual feedback
/// </summary>
public class CanAffordToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int manaCost && parameter is int availableMana)
        {
            return manaCost <= availableMana ? 1.0 : 0.5;
        }
        return 1.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts EffectType to an icon character
/// </summary>
public class EffectTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is EffectType effectType)
        {
            return effectType switch
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
        }
        return "✨";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts int to visibility (Visible if > 0)
/// </summary>
public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
