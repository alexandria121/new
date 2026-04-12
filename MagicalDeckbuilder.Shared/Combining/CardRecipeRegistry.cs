using Card = MagicalDeckbuilder.Cards.Card;
using MagicalDeckbuilder.Logging;

namespace MagicalDeckbuilder.Combining;

/// <summary>
/// Central registry for card combination recipes.
/// Defines all valid combinations between base cards.
/// Formula: combo_template_id = 10000 + (min_id * 1000) + max_id
/// </summary>
public class CardRecipeRegistry
{
    private static CardRecipeRegistry? _instance;
    private static readonly object _lock = new();
    
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static CardRecipeRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new CardRecipeRegistry();
                }
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Maps combo template ID to the source card template IDs
    /// Key: comboTemplateId (int), Value: Tuple of (card1TemplateId, card2TemplateId)
    /// </summary>
    private readonly Dictionary<int, (int Card1Id, int Card2Id)> _recipes = new();
    
    /// <summary>
    /// Reverse lookup: maps source card pair to combo template ID
    /// </summary>
    private readonly Dictionary<(int, int), int> _reverseLookup = new();
    
    private CardRecipeRegistry()
    {
        InitializeDefaultRecipes();
    }
    
    /// <summary>
    /// Calculate the combo template ID for two card template IDs
    /// Formula: 10000 + (minId * 1000) + maxId
    /// </summary>
    public static int CalculateComboTemplateId(int templateId1, int templateId2)
    {
        var minId = Math.Min(templateId1, templateId2);
        var maxId = Math.Max(templateId1, templateId2);
        return 10000 + (minId * 1000) + maxId;
    }
    
    /// <summary>
    /// Calculate the combo template ID for two cards
    /// </summary>
    public static int CalculateComboTemplateId(Card card1, Card card2)
    {
        return CalculateComboTemplateId(card1.TemplateId, card2.TemplateId);
    }
    
    /// <summary>
    /// Check if a combo recipe exists for two cards
    /// </summary>
    public bool HasRecipe(int templateId1, int templateId2)
    {
        var key = (Math.Min(templateId1, templateId2), Math.Max(templateId1, templateId2));
        return _reverseLookup.ContainsKey(key);
    }
    
    /// <summary>
    /// Check if a combo recipe exists for two cards
    /// </summary>
    public bool HasRecipe(Card card1, Card card2)
    {
        return HasRecipe(card1.TemplateId, card2.TemplateId);
    }
    
    /// <summary>
    /// Get the combo template ID for two cards, if a recipe exists
    /// </summary>
    public int? GetComboTemplateId(int templateId1, int templateId2)
    {
        var key = (Math.Min(templateId1, templateId2), Math.Max(templateId1, templateId2));
        return _reverseLookup.TryGetValue(key, out var comboId) ? comboId : null;
    }
    
    /// <summary>
    /// Get the source card IDs for a combo template ID
    /// </summary>
    public (int Card1Id, int Card2Id)? GetSourceCards(int comboTemplateId)
    {
        return _recipes.TryGetValue(comboTemplateId, out var recipe) ? recipe : null;
    }
    
    /// <summary>
    /// Get all registered combo template IDs
    /// </summary>
    public IEnumerable<int> GetAllComboTemplateIds()
    {
        return _recipes.Keys;
    }
    
    /// <summary>
    /// Register a new combination recipe
    /// </summary>
    public void RegisterRecipe(int card1TemplateId, int card2TemplateId, int comboTemplateId)
    {
        var sortedCards = (Math.Min(card1TemplateId, card2TemplateId), Math.Max(card1TemplateId, card2TemplateId));
        
        _recipes[comboTemplateId] = (sortedCards.Item1, sortedCards.Item2);
        _reverseLookup[sortedCards] = comboTemplateId;
        
        ErrorLogger.Instance.Debug("CardRecipeRegistry", 
            $"Registered recipe: {card1TemplateId} + {card2TemplateId} = {comboTemplateId}");
    }
    
    /// <summary>
    /// Validate that a combo card's template ID matches its recipe
    /// </summary>
    public bool ValidateComboCard(int card1TemplateId, int card2TemplateId, int comboTemplateId)
    {
        var expectedComboId = CalculateComboTemplateId(card1TemplateId, card2TemplateId);
        return expectedComboId == comboTemplateId;
    }
    
    /// <summary>
    /// Initialize default combination recipes
    /// Only creature and blank cards can be combined.
    /// Formula: comboId = 10000 + (minId * 1000) + maxId
    /// </summary>
    private void InitializeDefaultRecipes()
    {
        // Radioactive combinations
        RegisterRecipe(1, 11, CalculateComboTemplateId(1, 11));   // Irradiated Blob + Heat Mote = 11011
        RegisterRecipe(1, 12, CalculateComboTemplateId(1, 12));   // Irradiated Blob + Cold Mote = 11012
        RegisterRecipe(1, 13, CalculateComboTemplateId(1, 13));   // Irradiated Blob + Anthropomorphized Meat = 11013
        
        // Thermodynamics combinations
        RegisterRecipe(11, 12, CalculateComboTemplateId(11, 12)); // Heat Mote + Cold Mote = 12012
        RegisterRecipe(11, 11, CalculateComboTemplateId(11, 11)); // Heat Mote + Heat Mote = 12011
        RegisterRecipe(12, 12, CalculateComboTemplateId(12, 12)); // Cold Mote + Cold Mote = 12012 (same as above, but different source)
        
        // Food combinations
        RegisterRecipe(13, 1, CalculateComboTemplateId(1, 13));   // Anthropomorphized Meat + Irradiated Blob = 11013 (same as radioactive meat)
        
        // Flesh combinations
        RegisterRecipe(10, 13, CalculateComboTemplateId(10, 13)); // Amalgamation + Anthropomorphized Meat = 13010
        
        // Fungus combinations
        RegisterRecipe(4, 5, CalculateComboTemplateId(4, 5));     // Fruiting Body + Mycelium Spore = 5004
        RegisterRecipe(5, 9, CalculateComboTemplateId(5, 9));     // Mycelium Spore + Undead Stump = 9005
        
        // Toxin combinations
        RegisterRecipe(6, 8, CalculateComboTemplateId(6, 8));     // Refuse Slinger + Brood Monarch = 8006
        
        // Eldritch combinations
        RegisterRecipe(15, 16, CalculateComboTemplateId(15, 16)); // Questing Tendril + Occult Dabbler = 16015
        RegisterRecipe(17, 18, CalculateComboTemplateId(17, 18)); // Dwarf Star Spawn + Malformed Summon = 18017
        RegisterRecipe(19, 20, CalculateComboTemplateId(19, 20)); // Eater of Hope + Greater Star Spawn = 20019
        RegisterRecipe(21, 22, CalculateComboTemplateId(21, 22)); // Herald + Jaa'aird'thuun = 22021
        
        ErrorLogger.Instance.Info("CardRecipeRegistry", $"Initialized {_recipes.Count} default combination recipes");
    }
    
    /// <summary>
    /// Debug: Print all registered recipes
    /// </summary>
    public void DebugPrintRecipes()
    {
        ErrorLogger.Instance.Info("CardRecipeRegistry", "=== All Combination Recipes ===");
        foreach (var kvp in _recipes.OrderBy(x => x.Key))
        {
            ErrorLogger.Instance.Info("CardRecipeRegistry", 
                $"  {kvp.Value.Card1Id} + {kvp.Value.Card2Id} = {kvp.Key}");
        }
    }
}