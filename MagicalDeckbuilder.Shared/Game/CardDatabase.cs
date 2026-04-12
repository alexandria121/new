using System.Collections.Generic;

namespace MagicalDeckbuilder.Game;

/// <summary>
/// Static database of all cards in the game with their Template IDs.
/// IDs 1-200: Base/Combinable cards
/// IDs 1000-2000: Combo/Created cards
/// </summary>
public static class CardDatabase
{
    /// <summary>
    /// All base cards that can be combined (IDs 1-22)
    /// </summary>
    public static readonly Dictionary<int, CardInfo> BaseCards = new()
    {
        { 1, new CardInfo { Id = 1, Name = "Irradiated Blob", Element = "Radioactivity", Type = "Creature", ManaCost = 1, Health = 6, Power = 1, Rarity = "Common" } },
        { 2, new CardInfo { Id = 2, Name = "Confused Predator", Element = "Radioactivity", Type = "Creature", ManaCost = 1, Health = 3, Power = 3, Rarity = "Common" } },
        { 3, new CardInfo { Id = 3, Name = "Geiger Giant", Element = "Radioactivity", Type = "Creature", ManaCost = 5, Health = 10, Power = 4, Rarity = "Rare" } },
        { 4, new CardInfo { Id = 4, Name = "Fruiting Body", Element = "Fungus", Type = "Creature", ManaCost = 4, Health = 3, Power = 4, Rarity = "Common" } },
        { 5, new CardInfo { Id = 5, Name = "Mycelium Spore", Element = "Fungus", Type = "Creature", ManaCost = 2, Health = 1, Power = 1, Rarity = "Common" } },
        { 6, new CardInfo { Id = 6, Name = "Refuse Slinger", Element = "Toxin", Type = "Creature", ManaCost = 1, Health = 1, Power = 2, Rarity = "Common" } },
        { 7, new CardInfo { Id = 7, Name = "Pupae Patroller", Element = "Toxin", Type = "Creature", ManaCost = 3, Health = 2, Power = 3, Rarity = "Uncommon" } },
        { 8, new CardInfo { Id = 8, Name = "Brood Monarch", Element = "Toxin", Type = "Creature", ManaCost = 6, Health = 4, Power = 3, Rarity = "Rare" } },
        { 9, new CardInfo { Id = 9, Name = "Undead Stump", Element = "Fungus", Type = "Creature", ManaCost = 5, Health = 4, Power = 4, Rarity = "Uncommon" } },
        { 10, new CardInfo { Id = 10, Name = "Amalgamation", Element = "Flesh", Type = "Creature", ManaCost = 2, Health = 3, Power = 5, Rarity = "Common" } },
        { 11, new CardInfo { Id = 11, Name = "Heat Mote", Element = "Thermodynamics", Type = "Creature", ManaCost = 1, Health = 1, Power = 2, Rarity = "Common" } },
        { 12, new CardInfo { Id = 12, Name = "Cold Mote", Element = "Thermodynamics", Type = "Creature", ManaCost = 1, Health = 2, Power = 1, Rarity = "Common" } },
        { 13, new CardInfo { Id = 13, Name = "Anthropomorphized Meat", Element = "Food", Type = "Creature", ManaCost = 1, Health = 8, Power = 0, Rarity = "Common" } },
        { 14, new CardInfo { Id = 14, Name = "Jubilee Jester", Element = "Food", Type = "Creature", ManaCost = 5, Health = 6, Power = 6, Rarity = "Rare" } },
        { 15, new CardInfo { Id = 15, Name = "Questing Tendril", Element = "Eldritch", Type = "Creature", ManaCost = 1, Health = 2, Power = 1, Rarity = "Common" } },
        { 16, new CardInfo { Id = 16, Name = "Occult Dabbler", Element = "Eldritch", Type = "Creature", ManaCost = 2, Health = 2, Power = 2, Rarity = "Common" } },
        { 17, new CardInfo { Id = 17, Name = "Dwarf Star Spawn", Element = "Eldritch", Type = "Creature", ManaCost = 2, Health = 1, Power = 3, Rarity = "Common", IsQuestCard = true } },
        { 18, new CardInfo { Id = 18, Name = "Malformed Summon", Element = "Eldritch", Type = "Creature", ManaCost = 3, Health = 2, Power = 3, Rarity = "Common", IsQuestCard = true } },
        { 19, new CardInfo { Id = 19, Name = "Eater of Hope", Element = "Eldritch", Type = "Creature", ManaCost = 5, Health = 3, Power = 8, Rarity = "Uncommon", IsQuestCard = true } },
        { 20, new CardInfo { Id = 20, Name = "Greater Star Spawn", Element = "Eldritch", Type = "Creature", ManaCost = 5, Health = 3, Power = 4, Rarity = "Uncommon", IsQuestCard = true } },
        { 21, new CardInfo { Id = 21, Name = "Herald of Jaa'aird'thuun", Element = "Eldritch", Type = "Creature", ManaCost = 4, Health = 4, Power = 4, Rarity = "Rare", IsQuestCard = true } },
        { 22, new CardInfo { Id = 22, Name = "Jaa'aird'thuun", Element = "Eldritch", Type = "Creature", ManaCost = 7, Health = 8, Power = 8, Rarity = "Legendary", IsQuestCard = true } }
    };

    /// <summary>
    /// All combo cards created by combining base cards (IDs 1000+)
    /// </summary>
    public static readonly Dictionary<int, ComboCardInfo> ComboCards = new()
    {
        // Original 8 combo cards
        { 1010, new ComboCardInfo { Id = 1010, Name = "Radioactive Heat", Recipe = "Irradiated Blob (1) + Heat Mote (11)", Element = "Radioactivity" } },
        { 1011, new ComboCardInfo { Id = 1011, Name = "Frozen Blob", Recipe = "Irradiated Blob (1) + Cold Mote (12)", Element = "Radioactivity" } },
        { 1012, new ComboCardInfo { Id = 1012, Name = "Thermal Balance", Recipe = "Heat Mote (11) + Cold Mote (12)", Element = "Thermodynamics" } },
        { 1013, new ComboCardInfo { Id = 1013, Name = "Double Heat", Recipe = "Heat Mote (11) + Heat Mote (11)", Element = "Thermodynamics" } },
        { 1014, new ComboCardInfo { Id = 1014, Name = "Double Cold", Recipe = "Cold Mote (12) + Cold Mote (12)", Element = "Thermodynamics" } },
        { 1015, new ComboCardInfo { Id = 1015, Name = "Radioactive Meat", Recipe = "Irradiated Blob (1) + Anthropomorphized Meat (13)", Element = "Flesh" } },
        { 1022, new ComboCardInfo { Id = 1022, Name = "Slightly Glowing Sponge Cakes", Recipe = "Anthropomorphized Meat (13) + Irradiated Blob (1)", Element = "Food" } },
        { 1111, new ComboCardInfo { Id = 1111, Name = "Tempature Mote", Recipe = "Heat Mote (11) + Cold Mote (12)", Element = "Thermodynamics" } },
        // New combo cards from list
        { 1106, new ComboCardInfo { Id = 1106, Name = "Frozen Blob", Recipe = "Irradiated Blob (1) + Cold Mote (12)", Element = "Radioactivity" } },
        { 1107, new ComboCardInfo { Id = 1107, Name = "Fire Stump", Recipe = "Undead Stump (9) + Heat Mote (11)", Element = "Thermodynamics" } },
        { 1108, new ComboCardInfo { Id = 1108, Name = "Toxic Oracle", Recipe = "Occult Dabbler (16) + Refuse Slinger (6)", Element = "Toxin" } },
        { 1109, new ComboCardInfo { Id = 1109, Name = "Star Eater", Recipe = "Eater of Hope (19) + Dwarf Star Spawn (17)", Element = "Eldritch" } },
        { 1110, new ComboCardInfo { Id = 1110, Name = "Void Consumer", Recipe = "Eater of Hope (19) + Malformed Summon (18)", Element = "Eldritch" } },
        { 1112, new ComboCardInfo { Id = 1112, Name = "Royal Toxin", Recipe = "Brood Monarch (8) + Refuse Slinger (6)", Element = "Toxin" } },
        { 1113, new ComboCardInfo { Id = 1113, Name = "Cold Meat", Recipe = "Anthropomorphized Meat (13) + Cold Mote (12)", Element = "Flesh" } },
        { 1114, new ComboCardInfo { Id = 1114, Name = "Fire Meat", Recipe = "Anthropomorphized Meat (13) + Heat Mote (11)", Element = "Flesh" } },
        { 1115, new ComboCardInfo { Id = 1115, Name = "Eldritch Meat", Recipe = "Anthropomorphized Meat (13) + Dwarf Star Spawn (17)", Element = "Eldritch" } },
        { 1116, new ComboCardInfo { Id = 1116, Name = "Fire Mote", Recipe = "Heat Mote (11) + Heat Mote (11)", Element = "Thermodynamics" } },
        { 1117, new ComboCardInfo { Id = 1117, Name = "Cold Mote", Recipe = "Cold Mote (12) + Cold Mote (12)", Element = "Thermodynamics" } },
        { 1119, new ComboCardInfo { Id = 1119, Name = "Toxic Mote", Recipe = "Refuse Slinger (6) + Heat Mote (11)", Element = "Toxin" } },
        { 1120, new ComboCardInfo { Id = 1120, Name = "Radioactive Mote", Recipe = "Irradiated Blob (1) + Heat Mote (11)", Element = "Radioactivity" } },
        { 1121, new ComboCardInfo { Id = 1121, Name = "Fungal Mote", Recipe = "Mycelium Spore (5) + Heat Mote (11)", Element = "Fungus" } },
        { 1122, new ComboCardInfo { Id = 1122, Name = "Flesh Mote", Recipe = "Anthropomorphized Meat (13) + Heat Mote (11)", Element = "Flesh" } },
        { 1123, new ComboCardInfo { Id = 1123, Name = "Thermo Mote", Recipe = "Heat Mote (11) + Heat Mote (11)", Element = "Thermodynamics" } },
        { 1124, new ComboCardInfo { Id = 1124, Name = "Frost Jester", Recipe = "Jubilee Jester (14) + Cold Mote (12)", Element = "Food" } },
        { 1125, new ComboCardInfo { Id = 1125, Name = "Fire Jester", Recipe = "Jubilee Jester (14) + Heat Mote (11)", Element = "Food" } },
        { 1126, new ComboCardInfo { Id = 1126, Name = "Toxic Jester", Recipe = "Jubilee Jester (14) + Refuse Slinger (6)", Element = "Food" } },
        { 1127, new ComboCardInfo { Id = 1127, Name = "Radiant Jester", Recipe = "Jubilee Jester (14) + Irradiated Blob (1)", Element = "Food" } },
        { 1128, new ComboCardInfo { Id = 1128, Name = "Fire Tendril", Recipe = "Questing Tendril (15) + Heat Mote (11)", Element = "Eldritch" } },
        { 1129, new ComboCardInfo { Id = 1129, Name = "Cold Tendril", Recipe = "Questing Tendril (15) + Cold Mote (12)", Element = "Eldritch" } },
        { 1130, new ComboCardInfo { Id = 1130, Name = "Toxic Tendril", Recipe = "Questing Tendril (15) + Refuse Slinger (6)", Element = "Toxin" } },
        { 1131, new ComboCardInfo { Id = 1131, Name = "Star Tendril", Recipe = "Questing Tendril (15) + Dwarf Star Spawn (17)", Element = "Eldritch" } },
        { 1132, new ComboCardInfo { Id = 1132, Name = "Abyssal Tendril", Recipe = "Questing Tendril (15) + Jaaaird'thuun (22)", Element = "Eldritch" } },
        { 1133, new ComboCardInfo { Id = 1133, Name = "Fire Oracle", Recipe = "Occult Dabbler (16) + Heat Mote (11)", Element = "Eldritch" } },
        { 1135, new ComboCardInfo { Id = 1135, Name = "Star Mote", Recipe = "Heat Mote (11) + Dwarf Star Spawn (17)", Element = "Eldritch" } },
        { 1136, new ComboCardInfo { Id = 1136, Name = "Cold Star Spawn", Recipe = "Cold Mote (12) + Dwarf Star Spawn (17)", Element = "Eldritch" } },
        { 1137, new ComboCardInfo { Id = 1137, Name = "Fire Giant", Recipe = "Geiger Giant (3) + Heat Mote (11)", Element = "Thermodynamics" } },
        { 1139, new ComboCardInfo { Id = 1139, Name = "Toxic Blob", Recipe = "Irradiated Blob (1) + Refuse Slinger (6)", Element = "Toxin" } },
        { 1140, new ComboCardInfo { Id = 1140, Name = "Fungal Blob", Recipe = "Irradiated Blob (1) + Mycelium Spore (5)", Element = "Fungus" } },
        { 1141, new ComboCardInfo { Id = 1141, Name = "Flesh Blob", Recipe = "Irradiated Blob (1) + Anthropomorphized Meat (13)", Element = "Flesh" } },
        { 1142, new ComboCardInfo { Id = 1142, Name = "Fire Predator", Recipe = "Confused Predator (2) + Heat Mote (11)", Element = "Thermodynamics" } },
        { 1143, new ComboCardInfo { Id = 1143, Name = "Cold Predator", Recipe = "Confused Predator (2) + Cold Mote (12)", Element = "Flesh" } },
        { 1145, new ComboCardInfo { Id = 1145, Name = "Royal Stump", Recipe = "Undead Stump (9) + Brood Monarch (8)", Element = "Fungus" } },
        // Batch 2 combo cards (1080-1105)
        { 1080, new ComboCardInfo { Id = 1080, Name = "Glacial Beast", Recipe = "Geiger Giant (3) + Cold Mote (12)", Element = "Thermodynamics" } },
        { 1081, new ComboCardInfo { Id = 1081, Name = "Heat Hive", Recipe = "Brood Monarch (8) + Heat Mote (11)", Element = "Toxin" } },
        { 1082, new ComboCardInfo { Id = 1082, Name = "Void Mushroom", Recipe = "Mycelium Spore (5) + Dwarf Star Spawn (17)", Element = "Eldritch" } },
        { 1083, new ComboCardInfo { Id = 1083, Name = "Radiant Stump", Recipe = "Undead Stump (9) + Heat Mote (11)", Element = "Fungus" } },
        { 1084, new ComboCardInfo { Id = 1084, Name = "Toxic Tendril", Recipe = "Questing Tendril (15) + Refuse Slinger (6)", Element = "Toxin" } },
        { 1085, new ComboCardInfo { Id = 1085, Name = "Cold Tendril", Recipe = "Questing Tendril (15) + Cold Mote (12)", Element = "Eldritch" } },
        { 1086, new ComboCardInfo { Id = 1086, Name = "Star Monarch", Recipe = "Brood Monarch (8) + Malformed Summon (18)", Element = "Eldritch" } },
        { 1087, new ComboCardInfo { Id = 1087, Name = "Cold Hive", Recipe = "Pupae Patroller (7) + Cold Mote (12)", Element = "Toxin" } },
        { 1088, new ComboCardInfo { Id = 1088, Name = "Fungal Stoker", Recipe = "Mycelium Spore (5) + Heat Mote (11)", Element = "Fungus" } },
        { 1089, new ComboCardInfo { Id = 1089, Name = "Cold Refuse", Recipe = "Refuse Slinger (6) + Cold Mote (12)", Element = "Toxin" } },
        { 1090, new ComboCardInfo { Id = 1090, Name = "Abyssal Spawn", Recipe = "Dwarf Star Spawn (17) + Jaaaird'thuun (22)", Element = "Eldritch" } },
        { 1092, new ComboCardInfo { Id = 1092, Name = "Frost Jester", Recipe = "Jubilee Jester (14) + Geiger Giant (3)", Element = "Food" } },
        { 1093, new ComboCardInfo { Id = 1093, Name = "Plasma Jester", Recipe = "Jubilee Jester (14) + Heat Mote (11)", Element = "Food" } },
        { 1094, new ComboCardInfo { Id = 1094, Name = "Radiant Jester", Recipe = "Jubilee Jester (14) + Irradiated Blob (1)", Element = "Food" } },
        { 1095, new ComboCardInfo { Id = 1095, Name = "Void Geiger", Recipe = "Geiger Giant (3) + Malformed Summon (18)", Element = "Eldritch" } },
        { 1096, new ComboCardInfo { Id = 1096, Name = "Fungal Queen", Recipe = "Brood Monarch (8) + Fruiting Body (4)", Element = "Fungus" } },
        { 1097, new ComboCardInfo { Id = 1097, Name = "Cold Fruiting", Recipe = "Fruiting Body (4) + Cold Mote (12)", Element = "Fungus" } },
        { 1098, new ComboCardInfo { Id = 1098, Name = "Heat Fruiting", Recipe = "Fruiting Body (4) + Heat Mote (11)", Element = "Fungus" } },
        { 1099, new ComboCardInfo { Id = 1099, Name = "Cold Oracle", Recipe = "Occult Dabbler (16) + Cold Mote (12)", Element = "Eldritch" } },
        { 1100, new ComboCardInfo { Id = 1100, Name = "Dark Oracle", Recipe = "Occult Dabbler (16) + Jaaaird'thuun (22)", Element = "Eldritch" } },
        { 1101, new ComboCardInfo { Id = 1101, Name = "Cold Giant", Recipe = "Geiger Giant (3) + Cold Mote (12)", Element = "Thermodynamics" } },
        { 1102, new ComboCardInfo { Id = 1102, Name = "Toxic Giant", Recipe = "Geiger Giant (3) + Refuse Slinger (6)", Element = "Toxin" } },
        { 1103, new ComboCardInfo { Id = 1103, Name = "Flesh Giant", Recipe = "Geiger Giant (3) + Anthropomorphized Meat (13)", Element = "Flesh" } },
        { 1104, new ComboCardInfo { Id = 1104, Name = "Star Prophet", Recipe = "Herald of Jaaaird'thuun (21) + Cold Mote (12)", Element = "Eldritch" } },
        { 1105, new ComboCardInfo { Id = 1105, Name = "Void Prophet", Recipe = "Herald of Jaaaird'thuun (21) + Heat Mote (11)", Element = "Eldritch" } },
        // Batch 3 combo cards (1051-1079)
        { 1051, new ComboCardInfo { Id = 1051, Name = "Thermal Fruit", Recipe = "Fruiting Body (4) + Heat Mote (11)", Element = "Fungus" } },
        { 1052, new ComboCardInfo { Id = 1052, Name = "Frozen Fruit", Recipe = "Fruiting Body (4) + Cold Mote (12)", Element = "Fungus" } },
        { 1053, new ComboCardInfo { Id = 1053, Name = "Toxic Fruit", Recipe = "Fruiting Body (4) + Refuse Slinger (6)", Element = "Fungus" } },
        { 1054, new ComboCardInfo { Id = 1054, Name = "Royal Heat", Recipe = "Brood Monarch (8) + Heat Mote (11)", Element = "Toxin" } },
        { 1055, new ComboCardInfo { Id = 1055, Name = "Royal Cold", Recipe = "Brood Monarch (8) + Cold Mote (12)", Element = "Toxin" } },
        { 1056, new ComboCardInfo { Id = 1056, Name = "Spore Stump", Recipe = "Undead Stump (9) + Mycelium Spore (5)", Element = "Fungus" } },
        { 1057, new ComboCardInfo { Id = 1057, Name = "Eldritch Duo", Recipe = "Questing Tendril (15) + Occult Dabbler (16)", Element = "Eldritch" } },
        { 1058, new ComboCardInfo { Id = 1058, Name = "Star Oracle", Recipe = "Occult Dabbler (16) + Greater Star Spawn (20)", Element = "Eldritch" } },
        { 1059, new ComboCardInfo { Id = 1059, Name = "Void Spawn", Recipe = "Dwarf Star Spawn (17) + Malformed Summon (18)", Element = "Eldritch" } },
        { 1061, new ComboCardInfo { Id = 1061, Name = "Cold Meat", Recipe = "Anthropomorphized Meat (13) + Cold Mote (12)", Element = "Flesh" } },
        { 1062, new ComboCardInfo { Id = 1062, Name = "Rotting Meat", Recipe = "Anthropomorphized Meat (13) + Refuse Slinger (6)", Element = "Flesh" } },
        { 1063, new ComboCardInfo { Id = 1063, Name = "Flesh Patrol", Recipe = "Anthropomorphized Meat (13) + Pupae Patroller (7)", Element = "Flesh" } },
        { 1065, new ComboCardInfo { Id = 1065, Name = "Jester Tendril", Recipe = "Jubilee Jester (14) + Questing Tendril (15)", Element = "Eldritch" } },
        { 1066, new ComboCardInfo { Id = 1066, Name = "Jester Spore", Recipe = "Jubilee Jester (14) + Mycelium Spore (5)", Element = "Fungus" } },
        { 1067, new ComboCardInfo { Id = 1067, Name = "Giant Jester", Recipe = "Jubilee Jester (14) + Geiger Giant (3)", Element = "Food" } },
        { 1068, new ComboCardInfo { Id = 1068, Name = "Toxic Heap", Recipe = "Refuse Slinger (6) + Fruiting Body (4)", Element = "Toxin" } },
        { 1069, new ComboCardInfo { Id = 1069, Name = "Toxic Corpse", Recipe = "Refuse Slinger (6) + Undead Stump (9)", Element = "Toxin" } },
        { 1070, new ComboCardInfo { Id = 1070, Name = "Armored Insect", Recipe = "Pupae Patroller (7) + Geiger Giant (3)", Element = "Toxin" } },
        { 1072, new ComboCardInfo { Id = 1072, Name = "Fungal Guardian", Recipe = "Pupae Patroller (7) + Undead Stump (9)", Element = "Fungus" } },
        { 1074, new ComboCardInfo { Id = 1074, Name = "Warmongers", Recipe = "Pupae Patroller (7) + Heat Mote (11)", Element = "Toxin" } },
        { 1075, new ComboCardInfo { Id = 1075, Name = "Undead Giant", Recipe = "Undead Stump (9) + Geiger Giant (3)", Element = "Fungus" } },
        { 1076, new ComboCardInfo { Id = 1076, Name = "Heat Stump", Recipe = "Undead Stump (9) + Heat Mote (11)", Element = "Fungus" } },
        { 1077, new ComboCardInfo { Id = 1077, Name = "Void Jester", Recipe = "Jubilee Jester (14) + Malformed Summon (18)", Element = "Eldritch" } },
        { 1078, new ComboCardInfo { Id = 1078, Name = "Abomination", Recipe = "Amalgamation (10) + Pupae Patroller (7)", Element = "Flesh" } },
        { 1079, new ComboCardInfo { Id = 1079, Name = "Elder Spawn", Recipe = "Greater Star Spawn (20) + Herald of Jaaaird'thuun (21)", Element = "Eldritch" } },
        // Batch 4 combo cards (1014-1050)
        { 1014, new ComboCardInfo { Id = 1014, Name = "Radioactive Predator", Recipe = "Irradiated Blob (1) + Refuse Slinger (6)", Element = "Radioactivity" } },
        { 1015, new ComboCardInfo { Id = 1015, Name = "Toxic Mote", Recipe = "Heat Mote (11) + Refuse Slinger (6)", Element = "Toxin" } },
        { 1016, new ComboCardInfo { Id = 1016, Name = "Frost Amalgamation", Recipe = "Amalgamation (10) + Cold Mote (12)", Element = "Flesh" } },
        { 1017, new ComboCardInfo { Id = 1017, Name = "Fire Toxin", Recipe = "Confused Predator (2) + Heat Mote (11)", Element = "Toxin" } },
        { 1018, new ComboCardInfo { Id = 1018, Name = "Eldritch Flesh", Recipe = "Amalgamation (10) + Dwarf Star Spawn (17)", Element = "Eldritch" } },
        { 1020, new ComboCardInfo { Id = 1020, Name = "Fungal Predator", Recipe = "Mycelium Spore (5) + Refuse Slinger (6)", Element = "Fungus" } },
        { 1023, new ComboCardInfo { Id = 1023, Name = "Radiant Flesh", Recipe = "Anthropomorphized Meat (13) + Heat Mote (11)", Element = "Flesh" } },
        { 1024, new ComboCardInfo { Id = 1024, Name = "Frozen Jester", Recipe = "Jubilee Jester (14) + Cold Mote (12)", Element = "Food" } },
        { 1025, new ComboCardInfo { Id = 1025, Name = "Spore Tendril", Recipe = "Questing Tendril (15) + Mycelium Spore (5)", Element = "Eldritch" } },
        { 1026, new ComboCardInfo { Id = 1026, Name = "Star Geiger", Recipe = "Geiger Giant (3) + Dwarf Star Spawn (17)", Element = "Eldritch" } },
        { 1027, new ComboCardInfo { Id = 1027, Name = "Toxic Dabbler", Recipe = "Occult Dabbler (16) + Refuse Slinger (6)", Element = "Toxin" } },
        { 1030, new ComboCardInfo { Id = 1030, Name = "Cold Star Spawn", Recipe = "Cold Mote (12) + Dwarf Star Spawn (17)", Element = "Eldritch" } },
        { 1031, new ComboCardInfo { Id = 1031, Name = "Heat Star Spawn", Recipe = "Heat Mote (11) + Malformed Summon (18)", Element = "Eldritch" } },
        { 1035, new ComboCardInfo { Id = 1035, Name = "Biohazard", Recipe = "Refuse Slinger (6) + Pupae Patroller (7)", Element = "Toxin" } },
        { 1036, new ComboCardInfo { Id = 1036, Name = "Undead Hive", Recipe = "Undead Stump (9) + Brood Monarch (8)", Element = "Fungus" } },
        { 1037, new ComboCardInfo { Id = 1037, Name = "Giant Flesh", Recipe = "Amalgamation (10) + Geiger Giant (3)", Element = "Flesh" } },
        { 1042, new ComboCardInfo { Id = 1042, Name = "Spore Giant", Recipe = "Mycelium Spore (5) + Brood Monarch (8)", Element = "Fungus" } },
        { 1043, new ComboCardInfo { Id = 1043, Name = "Fire Blob", Recipe = "Irradiated Blob (1) + Heat Mote (11)", Element = "Radioactivity" } },
        { 1045, new ComboCardInfo { Id = 1045, Name = "Frozen Stump", Recipe = "Undead Stump (9) + Cold Mote (12)", Element = "Fungus" } },
        { 1046, new ComboCardInfo { Id = 1046, Name = "Star Brood", Recipe = "Brood Monarch (8) + Dwarf Star Spawn (17)", Element = "Eldritch" } },
        { 1047, new ComboCardInfo { Id = 1047, Name = "Toxic Mote", Recipe = "Cold Mote (12) + Refuse Slinger (6)", Element = "Toxin" } },
        { 1048, new ComboCardInfo { Id = 1048, Name = "Necrotic Giant", Recipe = "Geiger Giant (3) + Undead Stump (9)", Element = "Fungus" } },
        { 1049, new ComboCardInfo { Id = 1049, Name = "Flesh Tendril", Recipe = "Questing Tendril (15) + Anthropomorphized Meat (13)", Element = "Flesh" } },
        { 1050, new ComboCardInfo { Id = 1050, Name = "Void Consumer", Recipe = "Eater of Hope (19) + Malformed Summon (18)", Element = "Eldritch" } }
    };

    /// <summary>
    /// Get a base card by ID
    /// </summary>
    public static CardInfo? GetBaseCard(int id)
    {
        return BaseCards.TryGetValue(id, out var card) ? card : null;
    }

    /// <summary>
    /// Get a combo card by ID
    /// </summary>
    public static ComboCardInfo? GetComboCard(int id)
    {
        return ComboCards.TryGetValue(id, out var card) ? card : null;
    }

    /// <summary>
    /// Get any card (base or combo) by ID
    /// </summary>
    public static object? GetCard(int id)
    {
        if (id < 1000)
            return GetBaseCard(id);
        return GetComboCard(id);
    }

    /// <summary>
    /// Check if an ID belongs to a base card
    /// </summary>
    public static bool IsBaseCard(int id) => id > 0 && id < 1000;

    /// <summary>
    /// Check if an ID belongs to a combo card
    /// </summary>
    public static bool IsComboCard(int id) => id >= 1000 && id < 2000;
}

/// <summary>
/// Information about a base/combinable card
/// </summary>
public class CardInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Element { get; set; } = "";
    public string Type { get; set; } = "";
    public int ManaCost { get; set; }
    public int Health { get; set; }
    public int Power { get; set; }
    public string Rarity { get; set; } = "";
    public bool IsCombinable { get; set; } = true;
    public bool IsQuestCard { get; set; } = false;
}

/// <summary>
/// Information about a combo/created card
/// </summary>
public class ComboCardInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Recipe { get; set; } = "";
    public string Element { get; set; } = "";
    public string Type { get; set; } = "Creature";
}