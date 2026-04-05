using Card = MagicalDeckbuilder.Cards.Card;

namespace MagicalDeckbuilder.Decks;

/// <summary>
/// Represents the state of a pile of cards
/// </summary>
public enum PileType
{
    DrawPile,
    Hand,
    DiscardPile,
    InPlay,
    Removed
}

/// <summary>
/// Manages a player's deck and various card piles
/// </summary>
public class DeckManager
{
    private readonly Random _random = new();
    
    public List<Card> DrawPile { get; private set; } = new();
    public List<Card> Hand { get; private set; } = new();
    public List<Card> DiscardPile { get; private set; } = new();
    public List<Card> InPlay { get; private set; } = new();
    
    public int MaxHandSize { get; set; } = 7;
    public int StartingHandSize { get; set; } = 4;
    
    /// <summary>
    /// Initialize a deck from a list of card templates
    /// </summary>
    public void InitializeDeck(List<Card> cardTemplates)
    {
        DrawPile.Clear();
        Hand.Clear();
        DiscardPile.Clear();
        InPlay.Clear();
        
        // Create copies of all cards in the deck
        foreach (var card in cardTemplates)
        {
            DrawPile.Add(card.Clone());
        }
        
        ShuffleDrawPile();
    }
    
    /// <summary>
    /// Shuffle the draw pile using Fisher-Yates algorithm
    /// </summary>
    public void ShuffleDrawPile()
    {
        for (int i = DrawPile.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (DrawPile[i], DrawPile[j]) = (DrawPile[j], DrawPile[i]);
        }
    }
    
    /// <summary>
    /// Draw cards from the draw pile
    /// </summary>
    public List<Card> DrawCards(int count)
    {
        var drawnCards = new List<Card>();
        
        for (int i = 0; i < count; i++)
        {
            if (DrawPile.Count == 0)
            {
                // Reshuffle discard pile into draw pile
                if (DiscardPile.Count > 0)
                {
                    DrawPile = new List<Card>(DiscardPile);
                    DiscardPile.Clear();
                    ShuffleDrawPile();
                }
                else
                {
                    break; // No cards left
                }
            }
            
            if (DrawPile.Count > 0 && Hand.Count < MaxHandSize)
            {
                var card = DrawPile[DrawPile.Count - 1];
                DrawPile.RemoveAt(DrawPile.Count - 1);
                Hand.Add(card);
                drawnCards.Add(card);
            }
        }
        
        return drawnCards;
    }
    
    /// <summary>
    /// Play a card from hand to play area
    /// </summary>
    public Card? PlayCard(string cardId)
    {
        var card = Hand.FirstOrDefault(c => c.Id == cardId);
        if (card != null)
        {
            Hand.Remove(card);
            InPlay.Add(card);
            return card;
        }
        return null;
    }
    
    /// <summary>
    /// Move a card from play to discard pile
    /// </summary>
    public void DiscardCard(string cardId)
    {
        var card = InPlay.FirstOrDefault(c => c.Id == cardId);
        if (card != null)
        {
            InPlay.Remove(card);
            DiscardPile.Add(card);
        }
    }
    
    /// <summary>
    /// Discard a card from hand
    /// </summary>
    public void DiscardFromHand(string cardId)
    {
        var card = Hand.FirstOrDefault(c => c.Id == cardId);
        if (card != null)
        {
            Hand.Remove(card);
            DiscardPile.Add(card);
        }
    }
    
    /// <summary>
    /// Get all cards in a specific pile
    /// </summary>
    public List<Card> GetPile(PileType pileType)
    {
        return pileType switch
        {
            PileType.DrawPile => DrawPile,
            PileType.Hand => Hand,
            PileType.DiscardPile => DiscardPile,
            PileType.InPlay => InPlay,
            _ => new List<Card>()
        };
    }
    
    /// <summary>
    /// Get the count of cards in a pile
    /// </summary>
    public int GetPileCount(PileType pileType)
    {
        return pileType switch
        {
            PileType.DrawPile => DrawPile.Count,
            PileType.Hand => Hand.Count,
            PileType.DiscardPile => DiscardPile.Count,
            PileType.InPlay => InPlay.Count,
            _ => 0
        };
    }
    
    /// <summary>
    /// Display current hand
    /// </summary>
    public string DisplayHand()
    {
        if (Hand.Count == 0)
            return "  (Empty Hand)";
        
        var display = new System.Text.StringBuilder();
        for (int i = 0; i < Hand.Count; i++)
        {
            display.AppendLine($"  {i + 1}. {Hand[i].Name} - {Hand[i].ManaCost} Mana | {Hand[i].Type}");
        }
        return display.ToString();
    }
    
    /// <summary>
    /// Display cards in play
    /// </summary>
    public string DisplayInPlay()
    {
        if (InPlay.Count == 0)
            return "  (Nothing in play)";
        
        var display = new System.Text.StringBuilder();
        foreach (var card in InPlay)
        {
            display.AppendLine($"  - {card.Name} ({card.Type}) | PWR:{card.Power} HP:{card.Health}");
        }
        return display.ToString();
    }
    
    /// <summary>
    /// Clear all piles (for new game)
    /// </summary>
    public void ClearAll()
    {
        DrawPile.Clear();
        Hand.Clear();
        DiscardPile.Clear();
        InPlay.Clear();
    }
}