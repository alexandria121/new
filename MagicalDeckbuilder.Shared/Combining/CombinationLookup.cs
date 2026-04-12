using Card = MagicalDeckbuilder.Cards.Card;

namespace MagicalDeckbuilder.Combining;

/// <summary>
/// Looks up pre-made combination cards based on two input cards.
/// Format: combine_{sorted_id1}_{sorted_id2}
/// </summary>
public class CombinationLookup
{
    private readonly Dictionary<string, Card> _combinationCards = new();

    /// <summary>
    /// Initialize with a collection of pre-made combination cards
    /// </summary>
    public void LoadCombinationCards(IEnumerable<Card> cards)
    {
        _combinationCards.Clear();
        
        foreach (var card in cards)
        {
            // Pre-made combo-only cards are stored in the deck
            // They have IsComboOnly = true and IsCombinable = true
            if (card.IsComboOnly && card.IsCombinable)
            {
                // Create a key from the card name in lowercase with underscores
                var key = card.Name.ToLowerInvariant().Replace(" ", "_").Replace("'", "");
                _combinationCards[key] = card;
            }
        }
    }

    /// <summary>
    /// Find a pre-made combination card for the given two cards.
    /// Returns null if no pre-made combination exists.
    /// </summary>
    public Card? FindCombination(Card card1, Card card2)
    {
        // Sort IDs so order doesn't matter (combine_a_b == combine_b_a)
        var ids = new[] { card1.Id, card2.Id }.OrderBy(x => x).ToArray();
        var key = $"combine_{ids[0]}_{ids[1]}";
        
        if (_combinationCards.TryGetValue(key, out var result))
        {
            return result;
        }

        // Try matching by checking combo card descriptions for the two base card names
        // The combo card description contains "(combo X + Y)" format
        var name1 = card1.Name.ToLowerInvariant();
        var name2 = card2.Name.ToLowerInvariant();
        
        foreach (var kvp in _combinationCards)
        {
            var desc = kvp.Value.Description.ToLowerInvariant();
            // Check if description contains both card names (with "combo" keyword)
            if (desc.Contains("combo") && 
                (desc.Contains(name1) || desc.Contains(name1.Replace(" ", ""))) &&
                (desc.Contains(name2) || desc.Contains(name2.Replace(" ", ""))))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Sanitize card name for use in lookup key
    /// </summary>
    private static string SanitizeName(string name)
    {
        // Remove spaces and special characters, keep only alphanumeric
        return new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    /// <summary>
    /// Check if two cards have a pre-made combination available
    /// </summary>
    public bool HasCombination(Card card1, Card card2)
    {
        return FindCombination(card1, card2) != null;
    }
}

/// <summary>
/// Result of attempting to combine cards
/// </summary>
public class CombinationResult
{
    public bool Success { get; set; }
    public Card? ResultCard { get; set; }
    public string Message { get; set; } = "";
    public bool IsPreMade => ResultCard != null;
}