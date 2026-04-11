using Card = MagicalDeckbuilder.Cards.Card;
using MagicalDeckbuilder.Logging;

namespace MagicalDeckbuilder.Combining;

/// <summary>
/// Handles card combining operations with flag validation.
/// Only cards marked as IsCombinable can be used in the combine system.
/// </summary>
public class CardCombiner
{
    private readonly CombinationLookup _lookup;

    public CardCombiner()
    {
        _lookup = new CombinationLookup();
    }

    /// <summary>
    /// Load pre-made combination cards into the combiner.
    /// </summary>
    public void LoadCombinationCards(IEnumerable<Card> cards)
    {
        ErrorLogger.Instance.Debug("CardCombiner", "Loading combination cards");
        _lookup.LoadCombinationCards(cards);
    }

    /// <summary>
    /// Check if a card can be used in the combine system.
    /// Requires the IsCombinable flag to be true.
    /// </summary>
    public bool CanCombine(Card card)
    {
        if (card == null)
        {
            ErrorLogger.Instance.Warning("CardCombiner", "Attempted to check null card for combinability");
            return false;
        }

        return card.IsCombinable;
    }

    /// <summary>
    /// Validate if two cards can be combined.
    /// Both cards must have IsCombinable = true.
    /// </summary>
    public bool CanCombine(Card card1, Card card2)
    {
        if (card1 == null || card2 == null)
        {
            ErrorLogger.Instance.Warning("CardCombiner", "Attempted to combine null cards");
            return false;
        }

        if (!card1.IsCombinable)
        {
            ErrorLogger.Instance.Debug("CardCombiner", $"Card '{card1.Name}' is not combinable (IsCombinable = false)");
            return false;
        }

        if (!card2.IsCombinable)
        {
            ErrorLogger.Instance.Debug("CardCombiner", $"Card '{card2.Name}' is not combinable (IsCombinable = false)");
            return false;
        }

        // Check if a pre-made combination exists
        if (!_lookup.HasCombination(card1, card2))
        {
            ErrorLogger.Instance.Debug("CardCombiner", $"No combination exists between '{card1.Name}' and '{card2.Name}'");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attempt to combine two cards.
    /// Returns a CombinationResult indicating success or failure.
    /// </summary>
    public CombinationResult TryCombine(Card card1, Card card2)
    {
        ErrorLogger.Instance.Info("CardCombiner", $"Attempting to combine '{card1.Name}' and '{card2.Name}'");

        // Validate both cards exist
        if (card1 == null || card2 == null)
        {
            return new CombinationResult
            {
                Success = false,
                Message = "One or both cards are null"
            };
        }

        // Check if first card is combinable
        if (!card1.IsCombinable)
        {
            return new CombinationResult
            {
                Success = false,
                Message = $"'{card1.Name}' cannot be combined (not marked as combinable)"
            };
        }

        // Check if second card is combinable
        if (!card2.IsCombinable)
        {
            return new CombinationResult
            {
                Success = false,
                Message = $"'{card2.Name}' cannot be combined (not marked as combinable)"
            };
        }

        // Try to find a pre-made combination
        var resultCard = _lookup.FindCombination(card1, card2);

        if (resultCard != null)
        {
            ErrorLogger.Instance.Info("CardCombiner", $"Successfully combined '{card1.Name}' and '{card2.Name}' into '{resultCard.Name}'");
            return new CombinationResult
            {
                Success = true,
                ResultCard = resultCard,
                Message = $"Combined {card1.Name} and {card2.Name} into {resultCard.Name}"
            };
        }

        // No combination found
        return new CombinationResult
        {
            Success = false,
            Message = $"No combination recipe exists for '{card1.Name}' and '{card2.Name}'"
        };
    }

    /// <summary>
    /// Get a list of valid combine targets for a given card.
    /// Returns only cards that are IsCombinable and have a combination recipe.
    /// </summary>
    public IEnumerable<Card> GetValidTargets(Card sourceCard, IEnumerable<Card> availableCards)
    {
        if (sourceCard == null || availableCards == null)
        {
            yield break;
        }

        // Source card must be combinable
        if (!sourceCard.IsCombinable)
        {
            yield break;
        }

        foreach (var targetCard in availableCards)
        {
            // Target must be combinable
            if (!targetCard.IsCombinable)
            {
                continue;
            }

            // Skip same card
            if (targetCard.Id == sourceCard.Id)
            {
                continue;
            }

            // Check if combination exists
            if (_lookup.HasCombination(sourceCard, targetCard))
            {
                yield return targetCard;
            }
        }
    }

    /// <summary>
    /// Check if two cards have any valid combination.
    /// </summary>
    public bool HasValidCombination(Card card1, Card card2)
    {
        if (card1 == null || card2 == null)
        {
            return false;
        }

        // Both must be combinable
        if (!card1.IsCombinable || !card2.IsCombinable)
        {
            return false;
        }

        return _lookup.HasCombination(card1, card2);
    }
}