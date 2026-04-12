// ============ NEW COMBO CARDS (6 cards, using valid ID formula) ============

        // Combo 1: Irradiated Blob(1) + Heat Mote(10) = 1000 + (1*10) + 10 = 1010
        var radioactiveHeat = CreateCreature("Radioactive Heat", "A bubbling mass of radioactive heat.\n[ABILITY: Meltdown - 3 MC, 5 DTT and +2P self-buff]",
            ElementType.Radioactivity, 3, 4, 4, 2, false,
            new List<CardAbility>
            {
                new CardAbility { Name = "Meltdown", Description = "5 DTT and +2P self-buff", ManaCost = 3, EffectType = EffectType.Buff, EffectValue = 2, RequiresTarget = false }
            });
        radioactiveHeat.IsCombinable = true;
        radioactiveHeat.IsComboOnly = true;
        radioactiveHeat.TemplateId = 1010;
        deck.Add(radioactiveHeat);

        // Combo 2: Irradiated Blob(1) + Cold Mote(11) = 1000 + (1*10) + 11 = 1011
        var frozenBlob = CreateCreature("Frozen Blob", "A frozen irradiated blob.\n[ABILITY: Cold Radiation - 25% freeze attacker]",
            ElementType.Radioactivity, 2, 4, 5, 1, false,
            new List<CardAbility>
            {
                new CardAbility { Name = "Cold Radiation", Description = "25% freeze attacker", ManaCost = 0, EffectType = EffectType.Debuff, EffectValue = 1, IsPassive = true, RequiresTarget = false }
            });
        frozenBlob.IsCombinable = true;
        frozenBlob.IsComboOnly = true;
        frozenBlob.TemplateId = 1011;
        deck.Add(frozenBlob);

        // Combo 3: Heat Mote(10) + Cold Mote(11) = 1000 + (10*10) + 11 = 1111 (but 1111 is taken by Temp Mote - use 1012)
        var thermalBalance = CreateCreature("Thermal Balance", "Balanced thermal energy.\n[ABILITY: Thermal Equilibrium - +2P and +2H]",
            ElementType.Thermodynamics, 2, 4, 3, 1, false,
            new List<CardAbility>
            {
                new CardAbility { Name = "Thermal Equilibrium", Description = "+2P and +2H", ManaCost = 0, EffectType = EffectType.Buff, EffectValue = 2, IsPassive = true, RequiresTarget = false }
            });
        thermalBalance.IsCombinable = true;
        thermalBalance.IsComboOnly = true;
        thermalBalance.TemplateId = 1012;
        deck.Add(thermalBalance);

        // Combo 4: Heat Mote(10) + Heat Mote(10) = 1000 + (10*10) + 10 = 1010 (duplicate - use 1013)
        var doubleHeat = CreateCreature("Double Heat", "Double the heat energy.\n[ABILITY: Heat Amplification - 30% double stats]",
            ElementType.Thermodynamics, 2, 3, 2, 1, false,
            new List<CardAbility>
            {
                new CardAbility { Name = "Heat Amplification", Description = "30% double stats", ManaCost = 0, EffectType = EffectType.Buff, EffectValue = 2, IsPassive = true, RequiresTarget = false }
            });
        doubleHeat.IsCombinable = true;
        doubleHeat.IsComboOnly = true;
        doubleHeat.TemplateId = 1013;
        deck.Add(doubleHeat);

        // Combo 5: Cold Mote(11) + Cold Mote(11) = 1000 + (11*10) + 11 = 1121 (use 1014)
        var doubleCold = CreateCreature("Double Cold", "Double the cold energy.\n[ABILITY: Cold Amplification - 30% double stats]",
            ElementType.Thermodynamics, 2, 4, 2, 1, false,
            new List<CardAbility>
            {
                new CardAbility { Name = "Cold Amplification", Description = "30% double stats", ManaCost = 0, EffectType = EffectType.Buff, EffectValue = 2, IsPassive = true, RequiresTarget = false }
            });
        doubleCold.IsCombinable = true;
        doubleCold.IsComboOnly = true;
        doubleCold.TemplateId = 1014;
        deck.Add(doubleCold);

        // Combo 6: Irradiated Blob(1) + Anthropomorphized Meat(12) = 1000 + (1*10) + 12 = 1012? No wait - (1*10)+12 = 22, 1000+22=1022. But 1022 is Sponge Cakes. Use 1015.
        var radioactiveMeat = CreateCreature("Radioactive Meat", "Flesh infused with radiation.\n[ABILITY: Radioactive Flesh - passive, radiate all attackers]",
            ElementType.Radioactivity, 2, 6, 6, 1, false,
            new List<CardAbility>
            {
                new CardAbility { Name = "Radioactive Flesh", Description = "radiate all attackers", ManaCost = 0, EffectType = EffectType.Damage, EffectValue = 1, IsPassive = true, RequiresTarget = false }
            });
        radioactiveMeat.IsCombinable = true;
        radioactiveMeat.IsComboOnly = true;
        radioactiveMeat.TemplateId = 1015;
        deck.Add(radioactiveMeat);