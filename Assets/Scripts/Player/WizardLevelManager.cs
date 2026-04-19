using UnityEngine;

/// <summary>
/// Manages the Wizard's XP, level-up checks, and applying progression
/// data from the WizardProgressionTable to the live WizardStats asset.
///
/// Attach this to the Player GameObject alongside PlayerController.
/// Assign the WizardStats ScriptableObject in the Inspector.
///
/// How it works:
///   1. Something calls GainXP(amount) — a kill, a skill check, etc.
///   2. This component checks if total XP crosses the next level threshold.
///   3. If so, it calls LevelUp(), which:
///        - Increments the level on WizardStats
///        - Rolls HP (d6 + CON mod), adds to maxHP and currentHP
///        - Updates proficiency bonus, spell slots, cantrips, max prepared
///        - Fires OnLevelUp so UI / VFX can react
///        - Logs every new class feature gained
///
/// Design notes:
///   - Subclass features are NOT handled here. Those come from books
///     found in dungeons and studied at the Tower Library.
///   - ASI (Ability Score Improvement) is flagged but not auto-applied.
///     A separate UI flow will let the player choose +2/+1 distribution.
///   - ArcaneRecovery, SpellMastery, etc. are flagged here; their runtime
///     behavior will live in the relevant gameplay systems (SpellManager, etc).
/// </summary>
public class WizardLevelManager : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════════
    //  Inspector
    // ══════════════════════════════════════════════════════════════════════

    [Header("References")]
    [Tooltip("The current Wizard's stat sheet. Assign the same asset as PlayerController.")]
    [SerializeField] private WizardStats stats;

    // ══════════════════════════════════════════════════════════════════════
    //  Runtime state
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cumulative bitmask of all class features this Wizard has gained.
    /// Persists through the run (serialized on WizardStats would also work,
    /// but we rebuild it from level on load for now).
    /// </summary>
    private WizardClassFeature unlockedFeatures;

    /// <summary>How many ASIs the player still needs to allocate.</summary>
    [HideInInspector] public int pendingASIs = 0;

    // ══════════════════════════════════════════════════════════════════════
    //  Events
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fired after a level-up is fully applied. Int = the new level.
    /// Subscribe from UI, sound, VFX, etc.
    /// </summary>
    public event System.Action<int> OnLevelUp;

    /// <summary>
    /// Fired when an ASI level is reached and the player needs to
    /// choose how to allocate their points. Int = number of pending ASIs.
    /// </summary>
    public event System.Action<int> OnASIPending;

    /// <summary>
    /// Fired when a new class feature is unlocked.
    /// Useful for tutorial popups, log messages, etc.
    /// </summary>
    public event System.Action<WizardClassFeature> OnFeatureUnlocked;

    // ══════════════════════════════════════════════════════════════════════
    //  Initialization
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogError("WizardLevelManager: No WizardStats assigned!");
            return;
        }

        // Rebuild cumulative features from current level (handles save/load)
        unlockedFeatures = WizardProgressionTable.CumulativeFeatures(stats.level);

        // Sync the xpToNextLevel field so the UI reads correctly
        SyncXPToNextLevel();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Public API
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Awards XP and triggers level-ups if thresholds are crossed.
    /// Can chain multiple level-ups if a massive XP dump occurs.
    /// </summary>
    public void GainXP(int amount)
    {
        if (amount <= 0) return;
        if (stats.level >= WizardProgressionTable.MaxLevel) return;

        stats.currentXP += amount;
        Debug.Log($"{stats.wizardName} gains {amount} XP. Total: {stats.currentXP}");

        // Check for one or more level-ups
        while (stats.level < WizardProgressionTable.MaxLevel)
        {
            int nextLevel = stats.level + 1;
            int xpNeeded = WizardProgressionTable.XPForLevel(nextLevel);

            if (stats.currentXP >= xpNeeded)
                LevelUp(nextLevel);
            else
                break;
        }

        SyncXPToNextLevel();
    }

    /// <summary>
    /// Returns true if the Wizard has unlocked a specific class feature.
    /// Other systems call this to check capabilities:
    ///   if (levelManager.HasFeature(WizardClassFeature.ArcaneRecovery)) ...
    /// </summary>
    public bool HasFeature(WizardClassFeature feature)
    {
        return (unlockedFeatures & feature) != 0;
    }

    /// <summary>
    /// Returns the current level's data from the progression table.
    /// Convenience for UI, tooltips, etc.
    /// </summary>
    public WizardProgressionTable.LevelData CurrentLevelData =>
        WizardProgressionTable.GetLevel(stats.level);

    // ══════════════════════════════════════════════════════════════════════
    //  Level-up logic
    // ══════════════════════════════════════════════════════════════════════

    private void LevelUp(int newLevel)
    {
        stats.level = newLevel;
        var data = WizardProgressionTable.GetLevel(newLevel);

        Debug.Log($"═══ {stats.wizardName} is now level {newLevel}! ═══");

        // ── HP ────────────────────────────────────────────────────────────
        // 5e Wizard: 1d6 + CON mod per level (level 1 is max die = 6)
        int hpGain;
        if (newLevel == 1)
        {
            // Should not hit this in practice (we start at 1), but safe default
            hpGain = data.HitDie + stats.ConMod;
        }
        else
        {
            // Roll hit die + CON mod, minimum 1 HP gained
            int roll = Dice.Roll(data.HitDie);
            hpGain = Mathf.Max(1, roll + stats.ConMod);
        }

        stats.maxHP += hpGain;
        stats.currentHP += hpGain;  // Heal the gained amount on level-up
        Debug.Log($"  HP: +{hpGain} (max {stats.maxHP})");

        // ── Proficiency bonus ─────────────────────────────────────────────
        if (stats.proficiencyBonus != data.ProficiencyBonus)
        {
            stats.proficiencyBonus = data.ProficiencyBonus;
            Debug.Log($"  Proficiency bonus → +{data.ProficiencyBonus}");
        }

        // ── Spell slots ───────────────────────────────────────────────────
        // Update max slots. Current slots get the DIFFERENCE added
        // (so you gain the new slots immediately, existing usage preserved).
        for (int i = 0; i < 9; i++)
        {
            int diff = data.SpellSlots[i] - stats.maxSpellSlots[i];
            if (diff > 0)
            {
                stats.maxSpellSlots[i] = data.SpellSlots[i];
                stats.currentSpellSlots[i] += diff;  // Grant new slots
                Debug.Log($"  Spell slot level {i + 1}: {data.SpellSlots[i]} (gained {diff})");
            }
        }

        // ── Class features ────────────────────────────────────────────────
        WizardClassFeature newFeatures = data.Features & ~unlockedFeatures;
        if (newFeatures != WizardClassFeature.None)
        {
            unlockedFeatures |= newFeatures;
            LogNewFeatures(newFeatures);

            // Special handling: ASI — queue it for player choice
            if ((newFeatures & WizardClassFeature.AbilityScoreImprovement) != 0)
            {
                pendingASIs++;
                Debug.Log($"  → ASI pending! ({pendingASIs} total to allocate)");
                OnASIPending?.Invoke(pendingASIs);
            }

            OnFeatureUnlocked?.Invoke(newFeatures);
        }

        // ── Fire level-up event ───────────────────────────────────────────
        OnLevelUp?.Invoke(newLevel);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ASI allocation (called by UI)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Applies an Ability Score Improvement. Call this from the ASI UI
    /// when the player has chosen their allocation.
    ///
    /// Examples:
    ///   ApplyASI("intelligence", 2);        // +2 INT
    ///   ApplyASI("intelligence", 1);        // +1 INT (call twice for +1/+1 split)
    ///   ApplyASI("dexterity", 1);
    /// </summary>
    /// <returns>True if applied successfully.</returns>
    public bool ApplyASI(string abilityName, int amount)
    {
        if (pendingASIs <= 0)
        {
            Debug.LogWarning("No pending ASIs to spend.");
            return false;
        }

        switch (abilityName.ToLower())
        {
            case "strength": stats.strength = Mathf.Min(20, stats.strength + amount); break;
            case "dexterity": stats.dexterity = Mathf.Min(20, stats.dexterity + amount); break;
            case "constitution":
                int oldConMod = stats.ConMod;
                stats.constitution = Mathf.Min(20, stats.constitution + amount);
                // If CON mod increased, retroactively add HP per level (5e rule)
                int conModDiff = stats.ConMod - oldConMod;
                if (conModDiff > 0)
                {
                    int bonusHP = conModDiff * stats.level;
                    stats.maxHP += bonusHP;
                    stats.currentHP += bonusHP;
                    Debug.Log($"  CON mod increased! +{bonusHP} retroactive HP.");
                }
                break;
            case "intelligence": stats.intelligence = Mathf.Min(20, stats.intelligence + amount); break;
            case "wisdom": stats.wisdom = Mathf.Min(20, stats.wisdom + amount); break;
            case "charisma": stats.charisma = Mathf.Min(20, stats.charisma + amount); break;
            default:
                Debug.LogError($"Unknown ability: {abilityName}");
                return false;
        }

        pendingASIs--;
        Debug.Log($"  ASI applied: {abilityName} +{amount}. Remaining: {pendingASIs}");
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Syncs the xpToNextLevel field on WizardStats for UI display.
    /// Shows how much MORE XP is needed, not the total threshold.
    /// </summary>
    private void SyncXPToNextLevel()
    {
        if (stats.level >= WizardProgressionTable.MaxLevel)
        {
            stats.xpToNextLevel = 0; // Max level
            return;
        }

        int nextThreshold = WizardProgressionTable.XPForLevel(stats.level + 1);
        stats.xpToNextLevel = nextThreshold - stats.currentXP;
    }

    /// <summary>Logs each individual feature in the bitmask.</summary>
    private void LogNewFeatures(WizardClassFeature features)
    {
        // Iterate each flag bit
        foreach (WizardClassFeature f in System.Enum.GetValues(typeof(WizardClassFeature)))
        {
            if (f == WizardClassFeature.None) continue;
            if ((features & f) != 0)
            {
                Debug.Log($"  ★ New feature: {f}");
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Debug (remove or gate behind #if UNITY_EDITOR later)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dumps the full Wizard state to the console. Call from a debug menu.
    /// </summary>
    [ContextMenu("Debug: Print Wizard Status")]
    public void DebugPrintStatus()
    {
        var d = CurrentLevelData;
        Debug.Log($"── {stats.wizardName} ── Level {stats.level} ──\n" +
                  $"  HP: {stats.currentHP}/{stats.maxHP}\n" +
                  $"  Prof: +{d.ProficiencyBonus}\n" +
                  $"  Cantrips known: {d.CantripsKnown}\n" +
                  $"  Max prepared: {d.MaxPreparedSpells}\n" +
                  $"  XP: {stats.currentXP} (need {stats.xpToNextLevel} more)\n" +
                  $"  Spell slots: [{string.Join(",", stats.maxSpellSlots)}]\n" +
                  $"  Features: {unlockedFeatures}\n" +
                  $"  Pending ASIs: {pendingASIs}");
    }
}