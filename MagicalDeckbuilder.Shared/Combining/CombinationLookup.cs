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
            // Pre-made combination cards should follow naming convention: 
            // "combine_{id1}_{id2}" or use a prefix to identify them
            if (card.Name.StartsWith("combine_", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the key from the name (e.g., "combine_abc123_def456")
                var key = card.Name.ToLowerInvariant();
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

        // Try alternative naming patterns
        // Pattern 2: combine_{name1}_{name2} (using sanitized card names)
        var nameKey = $"combine_{SanitizeName(card1.Name)}_{SanitizeName(card2.Name)}".ToLowerInvariant();
        
        // Check if we have any partial matches (names might not match exactly)
        foreach (var kvp in _combinationCards)
        {
            if (kvp.Key.Contains(ids[0]) || kvp.Key.Contains(ids[1]))
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