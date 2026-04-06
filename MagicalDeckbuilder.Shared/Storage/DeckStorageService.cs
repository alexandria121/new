using System.IO;
using System.Text.RegularExpressions;
using MagicalDeckbuilder.Cards;
using MagicalDeckbuilder.Game;
using MagicalDeckbuilder.Logging;
using SavedDeckType = MagicalDeckbuilder.Storage.DeckType;
using SavedDifficulty = MagicalDeckbuilder.Game.DifficultyLevel;

namespace MagicalDeckbuilder.Storage;

/// <summary>
/// Service for managing deck persistence - saves, loads, and manages decks stored alongside game install
/// </summary>
public class DeckStorageService
{
    private static DeckStorageService? _instance;
    private static readonly object _lock = new();
    
    private readonly string _decksDirectory;
    private readonly string _opponentDecksDirectory;
    private readonly string _indexFilePath;
    private DeckIndex _index;
    
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static DeckStorageService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new DeckStorageService();
                }
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Event raised when deck collection changes
    /// </summary>
    public event EventHandler? DecksChanged;
    
    /// <summary>
    /// Event raised when a specific deck is modified
    /// </summary>
    public event EventHandler<SavedDeck>? DeckModified;
    
    /// <summary>
    /// Private constructor for singleton
    /// </summary>
    private DeckStorageService()
    {
        // Store alongside game install directory
        var baseDir = AppContext.BaseDirectory;
        _decksDirectory = Path.Combine(baseDir, "Decks", "Player");
        _opponentDecksDirectory = Path.Combine(baseDir, "Decks", "Opponent");
        _indexFilePath = Path.Combine(baseDir, "Decks", "deck_index.json");
        
        _index = new DeckIndex();
        
        // Ensure directories exist
        Directory.CreateDirectory(_decksDirectory);
        Directory.CreateDirectory(_opponentDecksDirectory);
        
        ErrorLogger.Instance.Info("DeckStorageService", $"[Operation: Constructor] Decks directory: {_decksDirectory}");
    }
    
    /// <summary>
    /// Get the decks directory path
    /// </summary>
    public string DecksDirectory => _decksDirectory;
    
    /// <summary>
    /// Initialize the storage service - load index and ensure directories exist
    /// </summary>
    public async Task InitializeAsync()
    {
        ErrorLogger.Instance.Debug("DeckStorageService", "[Operation: Initialize] Starting initialization");
        
        try
        {
            // Load index if exists
            if (File.Exists(_indexFilePath))
            {
                var json = await File.ReadAllTextAsync(_indexFilePath);
                _index = DeckSerialization.IndexFromJson(json) ?? new DeckIndex();
                ErrorLogger.Instance.Info("DeckStorageService", $"[Operation: Initialize] Loaded index with {_index.Decks.Count} decks");
            }
            else
            {
                _index = new DeckIndex();
                ErrorLogger.Instance.Info("DeckStorageService", "[Operation: Initialize] Created new index");
            }
            
            // Ensure opponent decks directory exists
            Directory.CreateDirectory(_opponentDecksDirectory);
            
            // Generate opponent decks if needed
            await EnsureOpponentDecksExistAsync();
            
            DecksChanged?.Invoke(this, EventArgs.Empty);
            
            ErrorLogger.Instance.Info("DeckStorageService", "[Operation: Initialize] Initialization complete");
        }
        catch (Exception ex)
        {
            ErrorLogger.Instance.Error("DeckStorageService", "[Operation: Initialize] Failed to initialize", ex);
            throw;
        }
    }
    
    /// <summary>
    /// Validate deck name (max 20 chars, letters/numbers/symbols only, no spaces)
    /// </summary>
    public static bool IsValidDeckName(string name, out string? errorMessage)
    {
        errorMessage = null;
        
        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = "Deck name cannot be empty.";
            return false;
        }
        
        if (name.Length > 20)
        {
            errorMessage = "Deck name cannot exceed 20 characters.";
            return false;
        }
        
        if (name.Contains(' '))
        {
            errorMessage = "Deck name cannot contain spaces.";
            return false;
        }
        
        // Allow letters, numbers, and common symbols
        if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_\-\.]+$"))
        {
            errorMessage = "Deck name can only contain letters, numbers, underscores, hyphens, and periods.";
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Save a new deck or update existing
    /// </summary>
    public async Task<SavedDeck> SaveDeckAsync(string name, List<Card> cards, DeckType type = DeckType.Player, DifficultyLevel difficulty = DifficultyLevel.Journeyman)
    {
        ErrorLogger.Instance.Debug("DeckStorageService", $"[Operation: SaveDeckAsync] Saving deck: {name}");
        
        // Validate name
        if (!IsValidDeckName(name, out var errorMessage))
        {
            throw new ArgumentException(errorMessage);
        }
        
        // Check deck limit for player decks
        if (type == DeckType.Player)
        {
            var playerDeckCount = _index.Decks.Count(d => d.Type == DeckType.Player);
            if (playerDeckCount >= 9)
            {
                throw new InvalidOperationException("Maximum of 9 player decks reached. Please delete an existing deck first.");
            }
        }
        
        var deck = new SavedDeck
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Type = type,
            Difficulty = difficulty,
            Cards = cards.Select(c => new SavedCard 
            { 
                CardTemplateId = GetCardTemplateId(c),
                Quantity = 1 
            }).ToList()
        };
        deck.CardCount = deck.Cards.Count;
        
        // Save deck file
        var filePath = GetDeckFilePath(deck);
        var json = DeckSerialization.ToJson(deck);
        await File.WriteAllTextAsync(filePath, json);
        
        // Update index
        var indexEntry = new DeckIndexEntry
        {
            Id = deck.Id,
            Name = deck.Name,
            Type = deck.Type,
            Difficulty = deck.Difficulty,
            CardCount = deck.CardCount,
            ModifiedAt = deck.ModifiedAt
        };
        
        _index.Decks.RemoveAll(d => d.Id == deck.Id);
        _index.Decks.Add(indexEntry);
        _index.LastModified = DateTime.UtcNow;
        
        await SaveIndexAsync();
        
        DecksChanged?.Invoke(this, EventArgs.Empty);
        DeckModified?.Invoke(this, deck);
        
        ErrorLogger.Instance.Info("DeckStorageService", $"[Operation: SaveDeckAsync] Saved deck: {name} ({deck.Id})");
        
        return deck;
    }
    
    /// <summary>
    /// Update an existing deck
    /// </summary>
    public async Task<SavedDeck> UpdateDeckAsync(string deckId, List<Card> cards)
    {
        ErrorLogger.Instance.Debug("DeckStorageService", $"[Operation: UpdateDeckAsync] Updating deck: {deckId}");
        
        var existingDeck = await LoadDeckAsync(deckId);
        if (existingDeck == null)
        {
            throw new InvalidOperationException($"Deck not found: {deckId}");
        }
        
        existingDeck.Cards = cards.Select(c => new SavedCard 
        { 
            CardTemplateId = GetCardTemplateId(c),
            Quantity = 1 
        }).ToList();
        existingDeck.CardCount = existingDeck.Cards.Count;
        existingDeck.ModifiedAt = DateTime.UtcNow;
        
        // Save deck file
        var filePath = GetDeckFilePath(existingDeck);
        var json = DeckSerialization.ToJson(existingDeck);
        await File.WriteAllTextAsync(filePath, json);
        
        // Update index
        var indexEntry = _index.Decks.FirstOrDefault(d => d.Id == deckId);
        if (indexEntry != null)
        {
            indexEntry.CardCount = existingDeck.CardCount;
            indexEntry.ModifiedAt = existingDeck.ModifiedAt;
        }
        _index.LastModified = DateTime.UtcNow;
        
        await SaveIndexAsync();
        
        DecksChanged?.Invoke(this, EventArgs.Empty);
        DeckModified?.Invoke(this, existingDeck);
        
        ErrorLogger.Instance.Info("DeckStorageService", $"[Operation: UpdateDeckAsync] Updated deck: {deckId}");
        
        return existingDeck;
    }
    
    /// <summary>
    /// Load a specific deck by ID
    /// </summary>
    public async Task<SavedDeck?> LoadDeckAsync(string id)
    {
        var filePath = Path.Combine(_decksDirectory, $"{id}.json");
        
        if (!File.Exists(filePath))
        {
            // Try opponent directory
            filePath = Path.Combine(_opponentDecksDirectory, $"{id}.json");
        }
        
        if (!File.Exists(filePath))
        {
            ErrorLogger.Instance.Warning("DeckStorageService", $"[Operation: LoadDeckAsync] Deck file not found: {id}");
            return null;
        }
        
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var deck = DeckSerialization.FromJson(json);
            
            if (deck != null)
            {
                ErrorLogger.Instance.Debug("DeckStorageService", $"[Operation: LoadDeckAsync] Loaded deck: {deck.Name}");
            }
            
            return deck;
        }
        catch (Exception ex)
        {
            ErrorLogger.Instance.Error("DeckStorageService", $"[Operation: LoadDeckAsync] Failed to load deck: {id}", ex);
            return null;
        }
    }
    
    /// <summary>
    /// Get all saved decks
    /// </summary>
    public async Task<List<SavedDeck>> GetAllDecksAsync()
    {
        var decks = new List<SavedDeck>();
        
        foreach (var entry in _index.Decks)
        {
            var deck = await LoadDeckAsync(entry.Id);
            if (deck != null)
            {
                decks.Add(deck);
            }
        }
        
        return decks;
    }
    
    /// <summary>
    /// Get deck index entries (lightweight, for listing)
    /// </summary>
    public List<DeckIndexEntry> GetDeckIndex()
    {
        return _index.Decks.ToList();
    }
    
    /// <summary>
    /// Get all player decks
    /// </summary>
    public List<DeckIndexEntry> GetPlayerDecks()
    {
        return _index.Decks.Where(d => d.Type == DeckType.Player).ToList();
    }
    
    /// <summary>
    /// Get all opponent decks
    /// </summary>
    public List<DeckIndexEntry> GetOpponentDecks()
    {
        return _index.Decks.Where(d => d.Type == DeckType.Opponent).ToList();
    }
    
    /// <summary>
    /// Delete a deck
    /// </summary>
    public async Task DeleteDeckAsync(string id)
    {
        ErrorLogger.Instance.Debug("DeckStorageService", $"[Operation: DeleteDeckAsync] Deleting deck: {id}");
        
        var filePath = Path.Combine(_decksDirectory, $"{id}.json");
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        
        // Also check opponent directory
        var opponentPath = Path.Combine(_opponentDecksDirectory, $"{id}.json");
        if (File.Exists(opponentPath))
        {
            File.Delete(opponentPath);
        }
        
        // Remove from index
        _index.Decks.RemoveAll(d => d.Id == id);
        _index.LastModified = DateTime.UtcNow;
        
        await SaveIndexAsync();
        
        DecksChanged?.Invoke(this, EventArgs.Empty);
        
        ErrorLogger.Instance.Info("DeckStorageService", $"[Operation: DeleteDeckAsync] Deleted deck: {id}");
    }
    
    /// <summary>
    /// Duplicate a deck with a new name
    /// </summary>
    public async Task<SavedDeck?> DuplicateDeckAsync(string id, string newName)
    {
        var original = await LoadDeckAsync(id);
        if (original == null)
        {
            ErrorLogger.Instance.Warning("DeckStorageService", $"[Operation: DuplicateDeckAsync] Original deck not found: {id}");
            return null;
        }
        
        // Convert saved cards back to Card objects
        var cards = GetCardsFromSavedDeck(original);
        
        return await SaveDeckAsync(newName, cards, original.Type, original.Difficulty);
    }
    
    /// <summary>
    /// Rename a deck
    /// </summary>
    public async Task<SavedDeck?> RenameDeckAsync(string id, string newName)
    {
        var deck = await LoadDeckAsync(id);
        if (deck == null) return null;
        
        if (!IsValidDeckName(newName, out var errorMessage))
        {
            throw new ArgumentException(errorMessage);
        }
        
        deck.Name = newName;
        deck.ModifiedAt = DateTime.UtcNow;
        
        // Save deck file
        var filePath = GetDeckFilePath(deck);
        var json = DeckSerialization.ToJson(deck);
        await File.WriteAllTextAsync(filePath, json);
        
        // Update index
        var indexEntry = _index.Decks.FirstOrDefault(d => d.Id == id);
        if (indexEntry != null)
        {
            indexEntry.Name = newName;
            indexEntry.ModifiedAt = deck.ModifiedAt;
        }
        _index.LastModified = DateTime.UtcNow;
        
        await SaveIndexAsync();
        
        DecksChanged?.Invoke(this, EventArgs.Empty);
        DeckModified?.Invoke(this, deck);
        
        return deck;
    }
    
    /// <summary>
    /// Convert a SavedDeck back to Card objects for use in game
    /// </summary>
    public List<Card> GetCardsFromSavedDeck(SavedDeck savedDeck)
    {
        var cards = new List<Card>();
        
        foreach (var savedCard in savedDeck.Cards)
        {
            var cardTemplate = CardFactory.GetCardByTemplateId(savedCard.CardTemplateId);
            if (cardTemplate != null)
            {
                for (int i = 0; i < savedCard.Quantity; i++)
                {
                    cards.Add(cardTemplate.Clone());
                }
            }
            else
            {
                ErrorLogger.Instance.Warning("DeckStorageService", 
                    $"[Operation: GetCardsFromSavedDeck] Card template not found: {savedCard.CardTemplateId}");
            }
        }
        
        return cards;
    }
    
    /// <summary>
    /// Get the file path for a deck
    /// </summary>
    private string GetDeckFilePath(SavedDeck deck)
    {
        return deck.Type switch
        {
            DeckType.Player => Path.Combine(_decksDirectory, $"{deck.Id}.json"),
            DeckType.Opponent => Path.Combine(_opponentDecksDirectory, $"{deck.Id}.json"),
            _ => Path.Combine(_decksDirectory, $"{deck.Id}.json")
        };
    }
    
    /// <summary>
    /// Save the deck index to disk
    /// </summary>
    private async Task SaveIndexAsync()
    {
        var json = DeckSerialization.IndexToJson(_index);
        await File.WriteAllTextAsync(_indexFilePath, json);
    }
    
    /// <summary>
    /// Ensure opponent decks exist (generate if needed)
    /// </summary>
    private async Task EnsureOpponentDecksExistAsync()
    {
        // Check if we already have opponent decks
        var existingOpponentDecks = _index.Decks.Where(d => d.Type == DeckType.Opponent).ToList();
        var difficulties = new[] { DifficultyLevel.Apprentice, DifficultyLevel.Journeyman, DifficultyLevel.Expert, DifficultyLevel.Grandmaster };
        
        foreach (var difficulty in difficulties)
        {
            var existing = existingOpponentDecks.FirstOrDefault(d => d.Difficulty == difficulty);
            if (existing == null)
            {
                // Generate new opponent deck
                var deck = OpponentDeckGenerator.GenerateDeck(difficulty);
                await SaveDeckAsync($"OPP_{difficulty}", deck, DeckType.Opponent, difficulty);
                ErrorLogger.Instance.Info("DeckStorageService", $"[Operation: EnsureOpponentDecksExistAsync] Generated {difficulty} opponent deck");
            }
        }
    }
    
    /// <summary>
    /// Regenerate opponent deck for a difficulty
    /// </summary>
    public async Task RegenerateOpponentDeckAsync(DifficultyLevel difficulty)
    {
        // Find and delete existing opponent deck for this difficulty
        var existing = _index.Decks.FirstOrDefault(d => d.Type == DeckType.Opponent && d.Difficulty == difficulty);
        if (existing != null)
        {
            await DeleteDeckAsync(existing.Id);
        }
        
        // Generate new deck
        var deck = OpponentDeckGenerator.GenerateDeck(difficulty);
        await SaveDeckAsync($"OPP_{difficulty}", deck, DeckType.Opponent, difficulty);
        
        ErrorLogger.Instance.Info("DeckStorageService", $"[Operation: RegenerateOpponentDeckAsync] Regenerated {difficulty} opponent deck");
    }
    
    /// <summary>
    /// Get card template ID (for serialization)
    /// </summary>
    private string GetCardTemplateId(Card card)
    {
        // Use name + type + element as unique identifier for template lookup
        return $"{card.Name}_{card.Type}_{card.Element}";
    }
}

/// <summary>
/// Generates opponent decks based on difficulty
/// </summary>
public static class OpponentDeckGenerator
{
    /// <summary>
    /// Generate a deck for the specified difficulty
    /// </summary>
    public static List<Card> GenerateDeck(DifficultyLevel difficulty)
    {
        ErrorLogger.Instance.Debug("OpponentDeckGenerator", $"[Operation: GenerateDeck] Generating {difficulty} deck");
        
        var random = new Random();
        var allCards = CardFactory.CreateStarterDeck();
        
        // Filter cards based on difficulty
        var filteredCards = difficulty switch
        {
            DifficultyLevel.Apprentice => FilterByManaCost(allCards, 1, 3),
            DifficultyLevel.Journeyman => FilterByManaCost(allCards, 1, 5),
            DifficultyLevel.Expert => allCards.Where(c => c.ManaCost <= 6).ToList(),
            DifficultyLevel.Grandmaster => allCards,
            _ => allCards
        };
        
        // Determine deck size based on difficulty
        var deckSize = difficulty switch
        {
            DifficultyLevel.Apprentice => 12,
            DifficultyLevel.Journeyman => 15,
            DifficultyLevel.Expert => 18,
            DifficultyLevel.Grandmaster => 20,
            _ => 15
        };
        
        // Generate deck with balanced card types
        var deck = new List<Card>();
        
        // Add creatures (40% of deck)
        var creatures = filteredCards.Where(c => c.Type == CardType.Creature).ToList();
        int creatureCount = (int)(deckSize * 0.4);
        for (int i = 0; i < creatureCount && creatures.Count > 0; i++)
        {
            var card = creatures[random.Next(creatures.Count)];
            deck.Add(card.Clone());
        }
        
        // Add spells (30% of deck)
        var spells = filteredCards.Where(c => c.Type == CardType.Spell).ToList();
        int spellCount = (int)(deckSize * 0.3);
        for (int i = 0; i < spellCount && spells.Count > 0; i++)
        {
            var card = spells[random.Next(spells.Count)];
            deck.Add(card.Clone());
        }
        
        // Add artifacts (20% of deck)
        var artifacts = filteredCards.Where(c => c.Type == CardType.Artifact).ToList();
        int artifactCount = (int)(deckSize * 0.2);
        for (int i = 0; i < artifactCount && artifacts.Count > 0; i++)
        {
            var card = artifacts[random.Next(artifacts.Count)];
            deck.Add(card.Clone());
        }
        
        // Fill remaining with random cards
        while (deck.Count < deckSize && filteredCards.Count > 0)
        {
            var card = filteredCards[random.Next(filteredCards.Count)];
            deck.Add(card.Clone());
        }
        
        // Shuffle the deck
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        
        ErrorLogger.Instance.Info("OpponentDeckGenerator", $"[Operation: GenerateDeck] Generated {difficulty} deck with {deck.Count} cards");
        
        return deck.Take(deckSize).ToList();
    }
    
    /// <summary>
    /// Filter cards by mana cost range
    /// </summary>
    private static List<Card> FilterByManaCost(List<Card> cards, int minCost, int maxCost)
    {
        return cards.Where(c => c.ManaCost >= minCost && c.ManaCost <= maxCost).ToList();
    }
}
