# Magical Deckbuilder

A WPF card game where you combine cards, build decks, and battle opponents.

## Features

- **Card Combining**: Merge blank cards with elements to create unique cards
- **Deck Building**: Build custom decks from your card collection
- **Battle System**: Turn-based combat against AI opponents
- **8 Card Types**: Spell, Creature, Artifact, Enchantment, Weapon, Armor, Event, Blank
- **8 Elements**: Radioactivity, Flesh, Toxin, Fungus, Thermodynamics, Time, Food, Eldritch
- **Rarity System**: Common, Uncommon, Rare, and Legendary cards

## Project Structure

```
MagicalDeckbuilder.Shared/    # Core game library
  Cards/          - Card classes and types
  Decks/          - Deck management
  Game/           - Game logic, CardFactory, OpponentAI
  Combining/      - Card combining system
  Logging/        - Error logging

NewGame.UI/                  # WPF application
  Views/          - MenuView, GameView, BattleSelectView, etc.
  ViewModels/     - MVVM ViewModels
  Controls/       - Custom UI controls
  Converters/     - Value converters
  Styles/         - XAML styles and themes
```

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run --project NewGame.UI
```

## Requirements

- .NET 9.0
- Windows