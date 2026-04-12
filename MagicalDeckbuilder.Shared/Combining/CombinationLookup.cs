using Card = MagicalDeckbuilder.Cards.Card;

namespace MagicalDeckbuilder.Combining;

/// <summary>
/// Looks up pre-made combination cards based on two input cards' TemplateIds.
/// Base cards use IDs 1-200, combo cards use IDs 10000+.
/// Formula: combo_template_id = 10000 + (min_id * 1000) + max_id
/// This ensures unique, collision-free IDs for all combinations.
/// Example: Card 1 + Card 2 = 10000 + 1000 + 2 = 11002
/// Example: Card 10 + Card 20 = 10000 + 10000 + 20 = 20020
/// </summary>
public class CombinationLookup
{
    private readonly Dictionary<int, Card> _comboCardsByTemplateId = new();
    
    /// <summary>
    /// Initialize with a collection of pre-made combination cards
    /// </summary>
    public void LoadCombinationCards(IEnumerable<Card> cards)
    {
        _comboCardsByTemplateId.Clear();
        
        foreach (var card in cards)
        {
            // Pre-made combo-only cards are stored in the deck
            // Combo cards from ComboDataLoader have:
            // - IsComboOnly = true
            // - IsCombinable = false (they are the RESULT, not ingredients)
            // - TemplateId in range 10000+ (calculated as 10000 + minId * 1000 + maxId)
            if (card.IsComboOnly && card.TemplateId >= 10000)
            {
                _comboCardsByTemplateId[card.TemplateId] = card;
            }
        }
    }

    /// <summary>
    /// Find a pre-made combination card for the given two cards.
    /// Returns null if no pre-made combination exists.
    /// </summary>
    public Card? FindCombination(Card card1, Card card2)
    {
        if (card1.TemplateId <= 0 || card2.TemplateId <= 0)
        {
            // Fallback to name-based lookup if template IDs not set
            return FindCombinationByName(card1, card2);
        }
        
        // Calculate combo template ID: 10000 + (minId * 1000) + maxId
        // Example: Card 1 + Card 2 = 10000 + 1000 + 2 = 11002
        // Example: Card 10 + Card 20 = 10000 + 10000 + 20 = 20020
        var ids = new[] { card1.TemplateId, card2.TemplateId }.OrderBy(x => x).ToArray();
        var comboTemplateId = 10000 + (ids[0] * 1000) + ids[1];
        
        if (_comboCardsByTemplateId.TryGetValue(comboTemplateId, out var result))
        {
            return result;
        }
        
        // Fallback to name-based lookup
        return FindCombinationByName(card1, card2);
    }
    
    /// <summary>
    /// Fallback: find combination by matching card names in combo card names
    /// </summary>
    private Card? FindCombinationByName(Card card1, Card card2)
    {
        if (card1.Name == card2.Name)
        {
            var cardName = card1.Name.ToLowerInvariant();
            foreach (var kvp in _comboCardsByTemplateId.Values)
            {
                var comboName = kvp.Name.ToLowerInvariant();
                if (comboName.Contains(cardName) && comboName != cardName)
                {
                    return kvp;
                }
            }
            return null;
        }
        
        var name1 = card1.Name.ToLowerInvariant();
        var name2 = card2.Name.ToLowerInvariant();
        
        foreach (var kvp in _comboCardsByTemplateId.Values)
        {
            var comboName = kvp.Name.ToLowerInvariant();
            var desc = kvp.Description.ToLowerInvariant();
            
            if (comboName == name1 || comboName == name2)
                continue;
            
            bool name1InCombo = comboName.Contains(name1);
            bool name2InCombo = comboName.Contains(name2);
            
            if (name1InCombo && name2InCombo)
            {
                return kvp;
            }
            
            if (desc.Contains("combo") && desc.Contains(name1) && desc.Contains(name2))
            {
                return kvp;
            }
        }
        
        return null;
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