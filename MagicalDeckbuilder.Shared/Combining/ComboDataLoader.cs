using System.Text.Json;
using System.Text.Json.Serialization;
using Card = MagicalDeckbuilder.Cards.Card;
using CreatureCard = MagicalDeckbuilder.Cards.CreatureCard;
using ElementType = MagicalDeckbuilder.Cards.ElementType;
using MagicalDeckbuilder.Logging;

namespace MagicalDeckbuilder.Combining;

/// <summary>
/// Data model for combo entries in combos.json
/// </summary>
public class ComboEntry
{
    [JsonPropertyName("card1Id")]
    public int Card1Id { get; set; }
    
    [JsonPropertyName("card2Id")]
    public int Card2Id { get; set; }
    
    [JsonPropertyName("resultName")]
    public string ResultName { get; set; } = "";
    
    [JsonPropertyName("element")]
    public string Element { get; set; } = "Radioactivity";
}

/// <summary>
/// Root object for combos.json
/// </summary>
public class CombosFile
{
    [JsonPropertyName("combos")]
    public List<ComboEntry> Combos { get; set; } = new();
}

/// <summary>
/// Loads combo cards from combos.json and creates Card objects.
/// Auto-calculates TemplateIds using formula: 10000 + (minId * 1000) + maxId
/// </summary>
public class ComboDataLoader
{
    private static ComboDataLoader? _instance;
    private static readonly object _lock = new();
    
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static ComboDataLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ComboDataLoader();
                }
            }
            return _instance;
        }
    }
    
    private List<Card>? _cachedCombos;
    
    /// <summary>
    /// Load all combo cards from combos.json
    /// </summary>
    public List<Card> LoadCombos()
    {
        if (_cachedCombos != null)
        {
            return _cachedCombos;
        }
        
        try
        {
            var jsonPath = GetCombosJsonPath();
            var jsonContent = File.ReadAllText(jsonPath);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var combosFile = JsonSerializer.Deserialize<CombosFile>(jsonContent, options);
            
            if (combosFile == null || combosFile.Combos == null)
            {
                ErrorLogger.Instance.Warning("ComboDataLoader", "combos.json is empty or malformed");
                return new List<Card>();
            }
            
            _cachedCombos = new List<Card>();
            
            foreach (var entry in combosFile.Combos)
            {
                var card = CreateComboCard(entry);
                _cachedCombos.Add(card);
                
                // Register the combo in the registry
                CardRecipeRegistry.Instance.RegisterRecipe(
                    entry.Card1Id, 
                    entry.Card2Id, 
                    CalculateTemplateId(entry.Card1Id, entry.Card2Id)
                );
            }
            
            ErrorLogger.Instance.Info("ComboDataLoader", $"Loaded {_cachedCombos.Count} combo cards from combos.json");
            
            return _cachedCombos;
        }
        catch (Exception ex)
        {
            ErrorLogger.Instance.Error("ComboDataLoader", "Failed to load combos.json", ex);
            return new List<Card>();
        }
    }
    
    /// <summary>
    /// Calculate the template ID for a combo card
    /// Formula: 10000 + (minId * 1000) + maxId
    /// </summary>
    public static int CalculateTemplateId(int card1Id, int card2Id)
    {
        var minId = Math.Min(card1Id, card2Id);
        var maxId = Math.Max(card1Id, card2Id);
        return 10000 + (minId * 1000) + maxId;
    }
    
    /// <summary>
    /// Create a combo card from a combo entry
    /// </summary>
    private Card CreateComboCard(ComboEntry entry)
    {
        var element = ParseElement(entry.Element);
        var templateId = CalculateTemplateId(entry.Card1Id, entry.Card2Id);
        
        // Log the combo recipe for debugging purposes
        ErrorLogger.Instance.Debug("ComboDataLoader", 
            $"Created combo: Card {entry.Card1Id} + Card {entry.Card2Id} → {entry.ResultName} (TemplateId: {templateId})");
        
        var card = new CreatureCard
        {
            Name = entry.ResultName,
            Description = "A powerful creature born from elemental fusion",
            Element = element,
            ManaCost = 3, // Default mana cost for combo cards
            Power = 3,    // Default power
            Health = 3,    // Default health
            Rarity = 2,    // Uncommon by default
            TemplateId = templateId,
            IsCombinable = false,
            IsComboOnly = true,
            Abilities = new List<MagicalDeckbuilder.Cards.CardAbility>()
        };
        
        return card;
    }
    
    /// <summary>
    /// Parse element string to ElementType enum
    /// </summary>
    private ElementType ParseElement(string element)
    {
        return element switch
        {
            "Radioactivity" => ElementType.Radioactivity,
            "Flesh" => ElementType.Flesh,
            "Toxin" => ElementType.Toxin,
            "Fungus" => ElementType.Fungus,
            "Thermodynamics" => ElementType.Thermodynamics,
            "Food" => ElementType.Food,
            "Eldritch" => ElementType.Eldritch,
            _ => ElementType.Radioactivity
        };
    }
    
    /// <summary>
    /// Get the path to combos.json
    /// </summary>
    private string GetCombosJsonPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var combosPath = Path.Combine(baseDir, "Combining", "combos.json");
        
        // Fallback to relative path if not found
        if (!File.Exists(combosPath))
        {
            combosPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Combining", "combos.json");
        }
        
        if (!File.Exists(combosPath))
        {
            // Try workspace relative path
            var currentDir = Directory.GetCurrentDirectory();
            combosPath = Path.Combine(currentDir, "MagicalDeckbuilder.Shared", "Combining", "combos.json");
        }
        
        ErrorLogger.Instance.Debug("ComboDataLoader", $"Loading combos from: {combosPath}");
        return combosPath;
    }
    
    /// <summary>
    /// Clear the cached combos (useful for reloading)
    /// </summary>
    public void ClearCache()
    {
        _cachedCombos = null;
    }
    
    /// <summary>
    /// Reload combos from disk
    /// </summary>
    public List<Card> Reload()
    {
        ClearCache();
        return LoadCombos();
    }
}