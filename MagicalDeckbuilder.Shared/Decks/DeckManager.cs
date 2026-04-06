using Card = MagicalDeckbuilder.Cards.Card;
using CardType = MagicalDeckbuilder.Cards.CardType;
using MagicalDeckbuilder.Logging;

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
/// Represents a creature slot on the battlefield
/// </summary>
public class CreatureSlot
{
    public int SlotIndex { get; set; }
    public Card? Creature { get; set; }
    public bool IsEmpty => Creature == null;
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
    
    // Creature slots (0-5 for opponent, 6-11 for player = 12 total)
    public List<CreatureSlot> CreatureSlots { get; private set; } = new();
    public int MaxCreatureSlots { get; set; } = 6;
    
    public int MaxHandSize { get; set; } = 7;
    public int StartingHandSize { get; set; } = 4;
    
    /// <summary>
    /// Initialize a deck from a list of card templates
    /// </summary>
    public void InitializeDeck(List<Card> cardTemplates)
    {
        ErrorLogger.Instance.Debug("DeckManager", "[Operation: InitializeDeck] Starting deck initialization");
        try
        {
            DrawPile.Clear();
            Hand.Clear();
            DiscardPile.Clear();
            InPlay.Clear();
            
            CreatureSlots.Clear();
            for (int i = 0; i < MaxCreatureSlots; i++)
            {
                CreatureSlots.Add(new CreatureSlot { SlotIndex = i });
            }
            
            foreach (var card in cardTemplates)
            {
                DrawPile.Add(card.Clone());
            }
            
            ShuffleDrawPile();
            ErrorLogger.Instance.Info("DeckManager", $"[Operation: InitializeDeck] Deck initialized with {cardTemplates.Count} cards");
        }
        catch (Exception ex)
        {
            ErrorLogger.Instance.Error("DeckManager", "[Operation: InitializeDeck] Failed to initialize deck", ex);
            throw;
        }
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
        ErrorLogger.Instance.Debug("DeckManager", $"[Operation: DrawCards] Drawing {count} cards");
        var drawnCards = new List<Card>();
        
        for (int i = 0; i < count; i++)
        {
            if (DrawPile.Count == 0)
            {
                if (DiscardPile.Count > 0)
                {
                    ErrorLogger.Instance.Info("DeckManager", "[Operation: DrawCards] Draw pile empty, reshuffling discard pile");
                    DrawPile = new List<Card>(DiscardPile);
                    DiscardPile.Clear();
                    ShuffleDrawPile();
                }
                else
                {
                    ErrorLogger.Instance.Warning("DeckManager", "[Operation: DrawCards] No cards left to draw - both draw pile and discard pile are empty");
                    break;
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
        
        if (drawnCards.Count > 0)
            ErrorLogger.Instance.Debug("DeckManager", $"[Operation: DrawCards] Drew {drawnCards.Count} cards");
        
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
            ErrorLogger.Instance.Debug("DeckManager", $"[Operation: PlayCard] Played card: {card.Name}");
            return card;
        }
        ErrorLogger.Instance.Warning("DeckManager", $"[Operation: PlayCard] Card not found in hand: {cardId}");
        return null;
    }
    
    /// <summary>
    /// Play a creature card into a specific slot
    /// </summary>
    public Card? PlayCreatureToSlot(string cardId, int slotIndex)
    {
        var card = Hand.FirstOrDefault(c => c.Id == cardId);
        if (card == null || card.Type != CardType.Creature)
        {
            ErrorLogger.Instance.Warning("DeckManager", $"[Operation: PlayCreatureToSlot] Card not found or not a creature: {cardId}");
            return null;
        }
            
        if (slotIndex < 0 || slotIndex >= CreatureSlots.Count)
        {
            ErrorLogger.Instance.Warning("DeckManager", $"[Operation: PlayCreatureToSlot] Invalid slot index: {slotIndex}");
            return null;
        }
            
        if (!CreatureSlots[slotIndex].IsEmpty)
        {
            ErrorLogger.Instance.Warning("DeckManager", $"[Operation: PlayCreatureToSlot] Slot already occupied: {slotIndex}");
            return null;
        }
            
        Hand.Remove(card);
        CreatureSlots[slotIndex].Creature = card;
        InPlay.Add(card);
        ErrorLogger.Instance.Debug("DeckManager", $"[Operation: PlayCreatureToSlot] Played {card.Name} to slot {slotIndex}");
        return card;
    }
    
    /// <summary>
    /// Get creature in a specific slot
    /// </summary>
    public Card? GetCreatureInSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < CreatureSlots.Count)
            return CreatureSlots[slotIndex].Creature;
        return null;
    }
    
    /// <summary>
    /// Remove creature from slot (to discard)
    /// </summary>
    public void RemoveCreatureFromSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < CreatureSlots.Count)
        {
            var creature = CreatureSlots[slotIndex].Creature;
            if (creature != null)
            {
                InPlay.Remove(creature);
                DiscardPile.Add(creature);
                CreatureSlots[slotIndex].Creature = null;
            }
        }
    }
    
    /// <summary>
    /// Get all creatures currently in slots
    /// </summary>
    public List<Card> GetCreaturesInSlots()
    {
        return CreatureSlots
            .Where(s => s.Creature != null)
            .Select(s => s.Creature!)
            .ToList();
    }
    
    /// <summary>
    /// Clear all slots (for new game)
    /// </summary>
    public void ClearSlots()
    {
        foreach (var slot in CreatureSlots)
        {
            slot.Creature = null;
        }
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
    /// Display cards in play (including creature slots)
    /// </summary>
    public string DisplayInPlay()
    {
        var display = new System.Text.StringBuilder();
        
        // Display creature slots
        bool hasCreatures = false;
        for (int i = 0; i < CreatureSlots.Count; i++)
        {
            var slot = CreatureSlots[i];
            if (slot.Creature != null)
            {
                display.AppendLine($"  [{i + 1}] {slot.Creature.Name} | PWR:{slot.Creature.Power} HP:{slot.Creature.Health}");
                hasCreatures = true;
            }
            else
            {
                display.AppendLine($"  [{i + 1}] (Empty)");
            }
        }
        
        // Display other cards in play (non-creatures)
        var otherCards = InPlay.Where(c => c.Type != CardType.Creature).ToList();
        if (otherCards.Count > 0)
        {
            if (hasCreatures) display.AppendLine("  ─ Other Cards ─");
            foreach (var card in otherCards)
            {
                display.AppendLine($"  • {card.Name} ({card.Type})");
            }
        }
        
        if (!hasCreatures && otherCards.Count == 0)
            return "  (Nothing in play)";
        
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
        ClearSlots();
    }
}