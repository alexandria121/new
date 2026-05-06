# M001: Card Object Hierarchy + Battle Foundation

**Gathered:** 2025-05-06
**Status:** Ready for planning

---

## Project Description

Magical Deckbuilder is a WPF card game built on .NET 9. Cards are combined from blank cards + elements (Radioactivity, Flesh, Toxin, Fungus, Thermodynamics, Time, Food, Eldritch) with types (Spell, Creature, Artifact, Enchantment, Weapon, Armor, Event, Blank) and rarities (Common → Legendary). Players build decks and battle AI opponents.

## Why This Milestone

The current codebase has functional but unorganized card representation (Card.cs, CardFactory, CardDatabase) and a working but undocumented battle system (Game.cs). M001 establishes the clean object hierarchy and battle foundation that all future features (combining, AI, UI, effects) build on.

## User-Visible Outcome

### When this milestone is complete, the user can:

- See a card displayed with its name, type, element, rarity, health, and attack — in a WPF View
- Play a card from hand to the field and watch it interact with the opponent
- Complete a full turn: draw → play cards → end turn → AI takes turn
- See health change when cards attack or effects trigger

### Entry point / environment

- Entry point: `dotnet run --project NewGame.UI` → Menu → Battle
- Environment: local WPF dev (Windows)
- Live dependencies: none (self-contained game logic)

---

## Completion Class

- **Contract complete** means: Battle logic resolves turns correctly (draw, play, attack, damage, win/loss) — verifiable via unit tests with mock data
- **Integration complete** means: Minimal battle board View correctly displays hand, field, and health bars, wired to the battle engine
- **Operational complete** means: Full turn cycle completes in-process without crashes or unhandled exceptions

---

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- A card can be created and displayed in the UI with all 6 fields (name, type, rarity, health, attack, element)
- A player can draw a card from deck and play it to the field
- Two-sided combat resolves: cards deal damage to each other and to player health
- Turn cycle completes: player turn → end turn → AI turn → back to player
- A player wins when opponent health reaches 0, or loses when their own health reaches 0

---

## Architectural Decisions

### Cards are plain data objects, not rich objects

**Decision:** Card.cs is a data container (struct or simple class with public fields/properties) — no behavior methods attached to card instances. Card data flows through service classes for manipulation.

**Rationale:** The existing Card.cs already acts as a data container. Separating data from behavior keeps the object hierarchy simple and testable. Rich object behavior can be added later in a separate milestone if needed.

**Alternatives Considered:**
- Rich objects with CalculateDamage(), CanPlay(), etc. — deferred to later milestone; adds complexity before scope is proven

---

### Battle is two-sided turn-based: Draw → Play → End Turn → AI

**Decision:** One player draws 1 card per turn, plays any card from hand (no mana cost yet), then ends turn. The AI opponent takes its turn. Cards on the field attack automatically or manually depending on state.

**Rationale:** Matches the existing Game.cs turn structure. No mana/resource system in this milestone — keeps the battle foundation bounded and testable.

**Alternatives Considered:**
- Mana/energy resource system — deferred to a future milestone (Resource System)
- Instant-only resolution (no turn phases) — too simple for a two-sided system

---

### UI is minimal battle board alongside logic, not full GameView

**Decision:** M001 builds a minimal battle board View showing hand, field, and health bars. The logic is complete; the UI is stripped-down but functional. Full GameView integration is a future slice.

**Rationale:** Keeps scope focused on the logic layer. Minimal UI enables visual testing of battle without building a complete game view. Wire-up to existing GameView can happen in a later milestone.

**Alternatives Considered:**
- Full GameView integration — too broad for a foundation milestone
- Logic-only with no UI — harder to validate user-visible behavior without visual output

---

## Error Handling Strategy

- **Card not found:** Throw `InvalidOperationException` with card ID; caught and logged by `ErrorLogger`
- **Invalid play (e.g. card not in hand):** Return a result object or boolean; do not throw — UX should handle gracefully with a message
- **Deck runs out of cards:** Draw fails; player cannot draw — game continues or ends by rules (fatigue/draw)
- **AI turn errors:** Caught and logged; AI forfeits turn to keep game from crashing

---

## Risks and Unknowns

- How field slots work — can any number of cards be on the field, or is there a max? — matters for card placement and combat targeting
- How combat resolves on the field — do cards attack each other, or only the opponent directly? — core to the battle math
- Whether existing CardFactory / CardDatabase load correctly into the new Card hierarchy — potential data migration risk

---

## Existing Codebase / Prior Art

- `MagicalDeckbuilder.Shared/Cards/Card.cs` — current card data class (name, type, rarity, health, attack, element) — will be refined to match the object hierarchy
- `MagicalDeckbuilder.Shared/Game/Game.cs` — existing battle logic and turn management — will be examined and extended
- `MagicalDeckbuilder.Shared/Game/CardFactory.cs` — card instantiation — must work with the new hierarchy
- `MagicalDeckbuilder.Shared/Game/CardDatabase.cs` — card data storage (JSON) — must load into the new card objects
- `NewGame.UI/Views/GameView.xaml.cs` — existing UI for battle — wire-up target for the minimal battle board
- `MagicalDeckbuilder.Shared/Combining/CardCombiner.cs` — card combining system — future milestone that depends on clean card objects

---

## Relevant Requirements

- R001 — Card object hierarchy with all required fields — M001 is the foundation that satisfies this
- R002 — Turn-based battle resolution — M001 provides the battle engine
- R003 — AI opponent behavior — depends on M001's battle foundation; deferred to future milestone

---

## Scope

### In Scope

- Card data object with fields: Name, CardType, Rarity, Health, Attack, Element
- Minimal battle board UI: hand display, field display, health bars, turn indicator
- Turn cycle: draw → play → end turn → AI turn → repeat
- Two-sided combat: field cards attack, damage applies to health
- Win/loss detection when health reaches 0
- Unit tests for battle logic (turn resolution, damage, win/loss)

### Out of Scope / Non-Goals

- Mana/resource system
- Card effects/abilities (Keyword system)
- AI opponent intelligence beyond basic play
- Full GameView / animation system
- Card combining system
- Deck saving/loading in battle context

---

## Technical Constraints

- .NET 9 / C# target
- WPF (NewGame.UI) for the UI layer
- MVVM pattern in NewGame.UI (ViewModels already exist: MainViewModel, CardViewModel)
- Shared library (MagicalDeckbuilder.Shared) for core logic
- ErrorLogger singleton for error reporting

---

## Integration Points

- **CardFactory** — must instantiate new Card objects correctly
- **CardDatabase** — must load JSON card data into the new Card hierarchy
- **GameView.xaml.cs** — the wire-up target for the minimal battle board View
- **ErrorLogger** — error propagation and logging throughout battle logic

---

## Testing Requirements

- Unit tests for turn resolution (draw, play, end turn sequence)
- Unit tests for damage calculation and health changes
- Unit tests for win/loss conditions
- Integration test: minimal battle board displays cards and health correctly
- Integration test: turn cycle completes without exception

---

## Acceptance Criteria

1. Card object has Name, CardType, Rarity, Health, Attack, Element — all correctly typed and settable
2. Minimal battle board shows hand cards, field cards, and both players' health
3. Player can play a card from hand to the field
4. Combat resolves: field cards deal damage, health updates correctly
5. Turn cycle completes: player turn → end turn → AI turn → next player turn
6. Game ends when either player reaches 0 health, with a win/loss result
7. Unit tests pass for: turn resolution, damage, win/loss detection

---

## Open Questions

- **Field slot count** — is there a maximum number of cards on the field? Or unlimited? — current thinking: no limit in M001, just a list
- **Combat targeting** — do field cards attack opponent's field cards first, or the opponent directly? — current thinking: field cards attack opponent's field cards if present, otherwise attack opponent health
- **Card ID system** — are cards identified by integer ID from CardDatabase, or by name, or by a generated GUID? — current thinking: stick with existing integer ID system from CardDatabase