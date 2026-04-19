/// <summary>
/// Pure data: the complete 5e (2024) Wizard progression table, levels 1–20.
/// Nothing here modifies game state — it's a lookup table.
///
/// Usage:
///   var row = WizardProgressionTable.GetLevel(5);
///   int profBonus = row.ProficiencyBonus;            // 3
///   int[] slots   = row.SpellSlots;                  // {4,3,2,0,0,0,0,0,0}
///   bool hasFeature = row.HasFeature(WizardClassFeature.MemorizeSpell); // true
/// </summary>
public static class WizardProgressionTable
{
    // ══════════════════════════════════════════════════════════════════════
    //  Per-level data struct
    // ══════════════════════════════════════════════════════════════════════

    public readonly struct LevelData
    {
        public readonly int Level;
        public readonly int ProficiencyBonus;
        public readonly int MaxPreparedSpells;
        public readonly int CantripsKnown;
        public readonly int[] SpellSlots;               // index 0 = 1st level … index 8 = 9th level
        public readonly WizardClassFeature Features;    // features GAINED at this level
        public readonly int XPThreshold;                // total XP needed to REACH this level
        public readonly int HitDie;                     // always d6 for Wizard

        public LevelData(
            int level,
            int profBonus,
            int maxPrepared,
            int cantrips,
            int[] spellSlots,
            WizardClassFeature features,
            int xpThreshold)
        {
            Level = level;
            ProficiencyBonus = profBonus;
            MaxPreparedSpells = maxPrepared;
            CantripsKnown = cantrips;
            SpellSlots = spellSlots;
            Features = features;
            XPThreshold = xpThreshold;
            HitDie = 6;
        }

        /// <summary>True if this level grants the given feature.</summary>
        public bool HasFeature(WizardClassFeature f) => (Features & f) != 0;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  The Table  (index 0 = level 1, index 19 = level 20)
    // ══════════════════════════════════════════════════════════════════════

    private static readonly LevelData[] Table = new LevelData[]
    {
        // ── Level 1 ──────────────────────────────────────────────────────
        new LevelData( 1, 2,  4, 3, new[]{2,0,0,0,0,0,0,0,0},
            WizardClassFeature.Spellcasting |
            WizardClassFeature.RitualAdept  |
            WizardClassFeature.ArcaneRecovery,
            0),

        // ── Level 2 ──────────────────────────────────────────────────────
        new LevelData( 2, 2,  5, 3, new[]{3,0,0,0,0,0,0,0,0},
            WizardClassFeature.Scholar,
            300),

        // ── Level 3 ──────────────────────────────────────────────────────
        new LevelData( 3, 2,  6, 3, new[]{4,2,0,0,0,0,0,0,0},
            WizardClassFeature.None,
            900),

        // ── Level 4 ──────────────────────────────────────────────────────
        new LevelData( 4, 2,  7, 4, new[]{4,3,0,0,0,0,0,0,0},
            WizardClassFeature.AbilityScoreImprovement,
            2700),

        // ── Level 5 ──────────────────────────────────────────────────────
        new LevelData( 5, 3,  9, 4, new[]{4,3,2,0,0,0,0,0,0},
            WizardClassFeature.MemorizeSpell,
            6500),

        // ── Level 6 ──────────────────────────────────────────────────────
        new LevelData( 6, 3, 10, 4, new[]{4,3,3,0,0,0,0,0,0},
            WizardClassFeature.None,
            14000),

        // ── Level 7 ──────────────────────────────────────────────────────
        new LevelData( 7, 3, 11, 4, new[]{4,3,3,1,0,0,0,0,0},
            WizardClassFeature.None,
            23000),

        // ── Level 8 ──────────────────────────────────────────────────────
        new LevelData( 8, 3, 12, 4, new[]{4,3,3,2,0,0,0,0,0},
            WizardClassFeature.AbilityScoreImprovement,
            34000),

        // ── Level 9 ──────────────────────────────────────────────────────
        new LevelData( 9, 4, 14, 4, new[]{4,3,3,3,1,0,0,0,0},
            WizardClassFeature.None,
            48000),

        // ── Level 10 ─────────────────────────────────────────────────────
        new LevelData(10, 4, 15, 5, new[]{4,3,3,3,2,0,0,0,0},
            WizardClassFeature.None,
            64000),

        // ── Level 11 ─────────────────────────────────────────────────────
        new LevelData(11, 4, 16, 5, new[]{4,3,3,3,2,1,0,0,0},
            WizardClassFeature.None,
            85000),

        // ── Level 12 ─────────────────────────────────────────────────────
        new LevelData(12, 4, 16, 5, new[]{4,3,3,3,2,1,0,0,0},
            WizardClassFeature.AbilityScoreImprovement,
            100000),

        // ── Level 13 ─────────────────────────────────────────────────────
        new LevelData(13, 5, 17, 5, new[]{4,3,3,3,2,1,1,0,0},
            WizardClassFeature.None,
            120000),

        // ── Level 14 ─────────────────────────────────────────────────────
        new LevelData(14, 5, 18, 5, new[]{4,3,3,3,2,1,1,0,0},
            WizardClassFeature.None,
            140000),

        // ── Level 15 ─────────────────────────────────────────────────────
        new LevelData(15, 5, 19, 5, new[]{4,3,3,3,2,1,1,1,0},
            WizardClassFeature.None,
            165000),

        // ── Level 16 ─────────────────────────────────────────────────────
        new LevelData(16, 5, 21, 5, new[]{4,3,3,3,2,1,1,1,0},
            WizardClassFeature.AbilityScoreImprovement,
            195000),

        // ── Level 17 ─────────────────────────────────────────────────────
        new LevelData(17, 6, 22, 5, new[]{4,3,3,3,2,1,1,1,1},
            WizardClassFeature.None,
            225000),

        // ── Level 18 ─────────────────────────────────────────────────────
        new LevelData(18, 6, 23, 5, new[]{4,3,3,3,3,1,1,1,1},
            WizardClassFeature.SpellMastery,
            265000),

        // ── Level 19 ─────────────────────────────────────────────────────
        new LevelData(19, 6, 24, 5, new[]{4,3,3,3,3,2,1,1,1},
            WizardClassFeature.EpicBoon,
            305000),

        // ── Level 20 ─────────────────────────────────────────────────────
        new LevelData(20, 6, 25, 5, new[]{4,3,3,3,3,2,2,1,1},
            WizardClassFeature.SignatureSpells,
            355000),
    };

    // ══════════════════════════════════════════════════════════════════════
    //  Public API
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Max Wizard level.</summary>
    public const int MaxLevel = 20;

    /// <summary>
    /// Returns the progression data for the given Wizard level (1–20).
    /// </summary>
    public static LevelData GetLevel(int level)
    {
        int idx = UnityEngine.Mathf.Clamp(level, 1, MaxLevel) - 1;
        return Table[idx];
    }

    /// <summary>
    /// Returns the total XP required to reach a given level.
    /// Level 1 = 0 XP, Level 2 = 300, etc.
    /// </summary>
    public static int XPForLevel(int level) => GetLevel(level).XPThreshold;

    /// <summary>
    /// Given a total XP amount, returns what level the Wizard should be.
    /// </summary>
    public static int LevelForXP(int totalXP)
    {
        for (int i = MaxLevel - 1; i >= 0; i--)
        {
            if (totalXP >= Table[i].XPThreshold)
                return Table[i].Level;
        }
        return 1;
    }

    /// <summary>
    /// Returns all features gained across levels 1 through the given level
    /// (cumulative bitmask). Useful for checking "does this Wizard have
    /// Arcane Recovery?" without tracking it separately.
    /// </summary>
    public static WizardClassFeature CumulativeFeatures(int level)
    {
        WizardClassFeature result = WizardClassFeature.None;
        int cap = UnityEngine.Mathf.Clamp(level, 1, MaxLevel);
        for (int i = 0; i < cap; i++)
            result |= Table[i].Features;
        return result;
    }
}