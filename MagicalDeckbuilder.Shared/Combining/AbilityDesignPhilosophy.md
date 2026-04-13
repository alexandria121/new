# Combo Card Ability Design Philosophy

## Overview
Combo cards are created by combining two base cards. Their abilities are designed based on the resulting element type.

## Element-Based Ability Themes

### Radioactivity
- **Theme**: High damage, radiation effects, nuclear power
- **Ability Types**: Damage, DamageToAll
- **Tone**: Destructive, powerful, contaminating
- **Example Abilities**: Gamma Burst, Radiation Poison, Nuclear Surge

### Flesh
- **Theme**: Regeneration, absorption, organic strength
- **Ability Types**: Heal, Shield, Buff
- **Tone**: Biological, self-sustaining, adaptive
- **Example Abilities**: Regrowth, Absorb, Flesh Armor

### Toxin
- **Theme**: Poison, corrosion, infection
- **Ability Types**: Damage, Debuff, DamageOverTime
- **Tone**: Venomous,腐蚀性, weakening
- **Example Abilities**: Corrosion, Venom Strike, Toxic Cloud

### Fungus
- **Theme**: Growth, defense, spore effects
- **Ability Types**: Heal, Shield, Buff
- **Tone**: Natural, defensive, regenerative
- **Example Abilities**: Spore Burst, Regenerate, Mycelium Shield

### Thermodynamics
- **Theme**: Temperature-based effects (heat/cold)
- **Ability Types**: Damage, Buff, Debuff
- **Tone**: Elemental, thermal, extreme
- **Heat Abilities**: Fire damage, power buffs
- **Cold Abilities**: Slow effects, health debuffs

### Food
- **Theme**: Nourishment, satisfaction, sustenance
- **Ability Types**: Heal, Shield, Buff
- **Tone**: Satisfying, restorative, comforting
- **Example Abilities**: Feast, Satisfied, Nourish

### Eldritch
- **Theme**: Unpredictable, powerful, otherworldly
- **Ability Types**: Damage, Debuff, DrawCard, Buff
- **Tone**: Mysterious, alien, reality-bending
- **Example Abilities**: Void Touch, Arcane Bolt, Cosmic Ray, Prophecy

## Ability Scaling Guidelines

### Mana Cost (MC) Guidelines
- MC 1: Minor effects, single target, small values (1-2)
- MC 2: Standard effects, moderate values (2-3)
- MC 3: Powerful effects, large values (3-4)
- MC 4+: Ultimate abilities, massive effects (4-5+)

### Value Scaling by Effect Type
- **Damage**: 1-5 based on mana cost and target scope
- **Heal**: 1-4 based on mana cost
- **Shield**: 1-3 based on mana cost
- **Buff/Debuff**: 1-3 based on mana cost
- **DamageToAll**: 1-3 (multi-target reduction)

## Target Types
- **Self**: Effects that benefit the card itself
- **Enemy**: Effects that target opponent's creatures
- **AllEnemies**: High mana cost AoE effects
- **AllAllies**: Team-wide buffs (rare)

## Naming Conventions
- Use thematic names related to the element
- Radioactivity: "Gamma", "Radiation", "Nuclear", "Contamination"
- Flesh: "Regrowth", "Absorb", "Organic", "Vital"
- Toxin: "Venom", "Corrosion", "Poison", "Toxic"
- Fungus: "Spore", "Mycelium", "Fruiting", "Growth"
- Thermodynamics: "Thermal", "Heat", "Cold", "Temperature"
- Food: "Feast", "Nourish", "Satisfy", "Consume"
- Eldritch: "Void", "Arcane", "Cosmic", "Madness", "Prophecy"

## Passive Abilities
Cards with strong thematic identity can have passive abilities marked with `isPassive: true`.
These activate automatically without mana cost.

## Progress Summary

### COMPLETE: All 233 combos have abilities (100%)

| Group | Combos | Description |
|-------|--------|-------------|
| 1-2 | 33 | Initial combos (Radioactivity, Flesh, Toxin, Fungus, Food, Thermodynamics, Eldritch blobs) |
| 3-4 | 22 | Heat/Cold Thermodynamics + Heat/Cold Predator/Giant + Meat combos |
| 5 | 17 | Jester combos |
| 6 | 17 | Flesh combos |
| 7 | 15 | Toxic combos |
| 8 | 27 | Patroller + Monarch combos |
| 9 | 25 | Fruiting + Spore + Stump combos |
| 10 | 25 | Stump + Questing + Dabbler combos |
| 11-12 | 52 | Star + Malformed + Eater + Greater + Herald + Jaa'aird + Predator combos |

**All 233 combo cards now have abilities!**

### Abilities Added Summary
- Radioactivity: 52 combos (high damage, radiation effects)
- Flesh: 32 combos (healing, regeneration, shield)
- Toxin: 36 combos (damage, debuffs, poison)
- Fungus: 46 combos (growth, defense, healing)
- Food: 12 combos (nourishment, sustain)
- Thermodynamics: 4 combos (heat/cold themed)
- Eldritch: 121 combos (arcane, void, cosmic effects)
- Mixed: 20 combos (combined element themes)

## Design Notes
- Combo cards combining two strong elements get 2 abilities
- Single strong element combos get 1 ability
- Thermodynamics cards are split into Heat vs Cold themed
- Food element focuses on sustain and recovery
- Eldritch is the most versatile element with varied effects