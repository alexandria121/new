# S02: Battle + Turn Structure — UAT

**Milestone:** M001
**Written:** 2026-05-06T18:03:39.144Z

## UAT: S02 Battle + Turn Structure

### UAT Type
**Automated unit tests** + manual integration walkthrough. The 12 BattleResolutionTests.cs tests prove combat resolution mechanics programmatically. Manual steps below verify the full player-facing flow.

### Preconditions
- BattleRoom is loaded with a player hand of creature cards
- Player has creatures in slots 6-11 (slots 6, 7, or 8 at minimum)
- Opponent has at least one creature in slot 0-5
- PlayerHealth > 0, OpponentHealth > 0, TurnCount = N

---

### Test Cases

**TC01 — Creature vs. creature combat**
1. Place a creature with Power=3 in player slot 6
2. Place an opposing creature with Health=3 in opponent slot 0
3. Click End Turn
4. **Expected:** Opponent slot 0 creature is removed (Health=0); player slot 6 creature remains (Health after opposing attack depends on opponent creature's Power)
5. **Tests:** `DamageRouting_CreatureVsCreature_BothDie`, `DamageRouting_CreatureVsCreature_AttackerSurvives`

**TC02 — Creature vs. face damage**
1. Place a creature with Power=4 in player slot 6
2. Ensure opponent slot 0 is empty
3. Note OpponentHealth before End Turn
4. Click End Turn
5. **Expected:** OpponentHealth decreases by 4
6. **Test:** `DamageRouting_CreatureVsFace_DamageToOpponent`

**TC03 — Dead creature removal**
1. Set up combat that kills a player creature (player creature Power=1, opposing creature Health=1 but opposing Power=5 vs player Health=4)
2. Click End Turn
3. **Expected:** Player creature in slot 6 is removed (null/empty) after combat
4. **Test:** `DeadCreatureRemoval_RemovesDeadCreatures`

**TC04 — Mana refills to PlayerMaxMana (not flat +2)**
1. Spend mana during turn (play cards, use abilities)
2. Note PlayerMaxMana value (turns played + 3, capped at 10)
3. Click End Turn
4. **Expected:** PlayerMana equals PlayerMaxMana immediately, not `PlayerMaxMana + 2` or flat `+2`
5. **Test:** `ManaRefill_ResetsToMaxOnNewTurn`

**TC05 — Game ends when OpponentHealth reaches 0**
1. Deal enough damage to reduce OpponentHealth to exactly 0 (e.g., via creature attack)
2. Click End Turn (or wait for opponent turn damage)
3. **Expected:** GameEndWindow appears immediately showing "Victory"
4. **Test:** `GameEndCondition_PlayerWinsWhenOpponentHealthZero`

**TC06 — Game ends when PlayerHealth reaches 0 (opponent attack)**
1. Reduce OpponentHealth to trigger AI counter-attack (or let opponent creatures attack)
2. Deal enough damage to reduce PlayerHealth to 0 via opponent combat or opponent weapon
3. **Expected:** GameEndWindow appears immediately showing "Defeat"
4. **Test:** `GameEndCondition_PlayerLosesWhenHealthZero`

**TC07 — TurnCount increments after complete round**
1. Note TurnCount = N
2. Click End Turn (player turn + opponent turn completes)
3. **Expected:** TurnCount = N+1
4. **Test:** `TurnCountIncrements_AfterPlayerAndOpponentTurn`

**TC08 — Opponent weapon damage to player**
1. Give opponent a weapon with Power=N
2. Note PlayerHealth before opponent's turn
3. Wait for opponent turn to complete
4. **Expected:** PlayerHealth decreases by N
5. **Test:** `OpponentWeaponDamage_DealtToPlayer`

**TC09 — Mid-combat game-end (creature kills opponent)**
1. Place a creature in player slot 6 with Power=100 (kills opponent in one hit)
2. Place no opponent creatures
3. Click End Turn — player creature attacks opponent face
4. **Expected:** GameEndWindow appears immediately *during* EndTurn execution (before ResolveOpponentWeaponDamage runs)
5. **Test:** `GameEndCondition_PlayerWinsWhenOpponentHealthZero`

**TC10 — Mana cap at 10 (PlayerMaxMana)**
1. Play through 10+ turns (PlayerMaxMana = 10)
2. Spend some mana
3. Click End Turn
4. **Expected:** PlayerMana = 10 (not 11, 12, etc.)
5. **Test:** `ManaRefill_ResetsToMaxOnNewTurn` (implicit cap at 10)

---

### Not Proven By This UAT
- Manual verification of GameEndWindow UI appearance and user interaction (handled by S04)
- AI opponent turn decision-making and card play (S04)
- Drag-combine card creation (S03)
- Actual visual card rendering and positioning in BattleRoom UI
- Real-time visual feedback during combat animation
- Performance under rapid repeated EndTurn calls

### Test Coverage
| Scenario | Test Method |
|---|---|
| EndTurn triggers combat | `EndTurn_TriggersCombatResolution` |
| Creature vs creature routing | `DamageRouting_CreatureVsCreature_BothDie`, `DamageRouting_CreatureVsCreature_AttackerSurvives` |
| Creature vs face damage | `DamageRouting_CreatureVsFace_DamageToOpponent` |
| Dead creature removal | `DeadCreatureRemoval_RemovesDeadCreatures` |
| Player wins at 0 opponent HP | `GameEndCondition_PlayerWinsWhenOpponentHealthZero` |
| Player loses at 0 player HP | `GameEndCondition_PlayerLosesWhenHealthZero` |
| Mana refill to max | `ManaRefill_ResetsToMaxOnNewTurn` |
| Turn count increment | `TurnCountIncrements_AfterPlayerAndOpponentTurn` |
| Opponent weapon damage | `OpponentWeaponDamage_DealtToPlayer` |
| No crash on game-end | `NoCrash_WhenGameEndWindowDisplayed` |

**12 total automated tests, all passing.**
