using UnityEngine;

/// <summary>
/// WizardStats is the ScriptableObject "character sheet" for the player.
/// It holds all 5e-derived stats for the current Wizard incarnation.
///
/// On permadeath, a new WizardStats asset is generated (or randomized)
/// for the next run — this object represents one mortal Wizard's life.
///
/// Setup in Unity:
///   1. Right-click in Project → Create → WIZARD → Wizard Stats
///   2. Name it something like "WizardStats_Default"
///   3. Assign it to PlayerController in the Inspector
///
/// Design note:
///   Movement speed is in TILES PER TURN, not 5e feet.
///   1 tile = standard. 2 tiles = haste/special. Effects modify
///   the runtime value, never this asset directly.
/// </summary>
[CreateAssetMenu(fileName = "WizardStats", menuName = "WIZARD/Wizard Stats")]
public class WizardStats : ScriptableObject
{
    // ── Identity ──────────────────────────────────────────────────────────────
    [Header("Identity")]
    [Tooltip("The Wizard's name. Randomized on each new run.")]
    public string wizardName = "Aldric the Unwise";

    [Tooltip("Wizard level. Starts at 1, max 20.")]
    [Range(1, 20)]
    public int level = 1;

    // ── Ability Scores ────────────────────────────────────────────────────────
    [Header("Ability Scores (5e)")]
    [Range(1, 20)] public int strength = 8;
    [Range(1, 20)] public int dexterity = 14;
    [Range(1, 20)] public int constitution = 13;
    [Range(1, 20)] public int intelligence = 16;
    [Range(1, 20)] public int wisdom = 12;
    [Range(1, 20)] public int charisma = 10;

    // ── Ability Score Modifiers (read-only helpers) ───────────────────────────
    public int StrMod => (strength - 10) / 2;
    public int DexMod => (dexterity - 10) / 2;
    public int ConMod => (constitution - 10) / 2;
    public int IntMod => (intelligence - 10) / 2;
    public int WisMod => (wisdom - 10) / 2;
    public int ChaMod => (charisma - 10) / 2;

    /// <summary>
    /// Returns the ability modifier for the given AbilityScore enum value.
    /// Bridges the existing per-ability properties (StrMod, DexMod, etc.)
    /// with the AbilityScore enum used by CombatManager and EnemyData.
    /// </summary>
    public int GetModifier(AbilityScore ability)
    {
        return ability switch
        {
            AbilityScore.Strength => StrMod,
            AbilityScore.Dexterity => DexMod,
            AbilityScore.Constitution => ConMod,
            AbilityScore.Intelligence => IntMod,
            AbilityScore.Wisdom => WisMod,
            AbilityScore.Charisma => ChaMod,
            _ => 0
        };
    }

    /// <summary>
    /// Returns the saving throw modifier for a given ability.
    /// TODO: Add saving throw proficiency tracking. For now, returns
    /// the raw ability modifier. Wizards are proficient in INT and WIS saves.
    /// </summary>
    public int GetSavingThrowModifier(AbilityScore ability)
    {
        int modifier = GetModifier(ability);

        // Wizards are proficient in Intelligence and Wisdom saving throws
        if (ability == AbilityScore.Intelligence || ability == AbilityScore.Wisdom)
            modifier += proficiencyBonus;

        return modifier;
    }

    // ── Proficiency ───────────────────────────────────────────────────────────
    [Header("Proficiency")]
    [Tooltip("Proficiency bonus. In 5e: +2 at level 1, scaling up to +6 at level 17+.")]
    public int proficiencyBonus = 2;

    // ── Hit Points ────────────────────────────────────────────────────────────
    [Header("Hit Points")]
    [Tooltip("Maximum HP. Calculated from d6 hit die + CON modifier per level.")]
    public int maxHP = 9;   // 6 (avg d6) + 3 (CON 16, mod +3) at level 1

    [Tooltip("Current HP at runtime. Do not set this manually — use TakeDamage/Heal.")]
    public int currentHP = 9;

    // ── Armor Class ───────────────────────────────────────────────────────────
    [Header("Armor Class")]
    [Tooltip("Base AC. Mage Armor (13 + DEX mod) is the standard Wizard baseline.")]
    public int armorClass = 13;   // Mage Armor default; no actual armor proficiency

    // ── Movement ──────────────────────────────────────────────────────────────
    [Header("Movement")]
    [Tooltip("Base movement in TILES PER TURN. 1 is standard for all creatures. " +
             "Effects (Haste, Slow, terrain) modify the runtime value — never this directly.")]
    public int baseMovementSpeed = 1;

    // ── Spell Slots ───────────────────────────────────────────────────────────
    [Header("Spell Slots")]
    [Tooltip("Maximum spell slots per level. Index 0 = 1st level, index 8 = 9th level.")]
    public int[] maxSpellSlots = new int[9] { 2, 0, 0, 0, 0, 0, 0, 0, 0 };

    [Tooltip("Current remaining spell slots. Restored on long rest.")]
    public int[] currentSpellSlots = new int[9] { 2, 0, 0, 0, 0, 0, 0, 0, 0 };

    // ── Experience ───────────────────────────────────────────────────────────
    [Header("Experience")]
    public int currentXP = 0;
    public int xpToNextLevel = 300;   // 5e level 1 → 2 threshold

    // ── Carrying Capacity ─────────────────────────────────────────────────────
    [Header("Inventory")]
    [Tooltip("Max carry weight in pounds. Per 5e: Strength × 15.")]
    public float carryCapacity => strength * 15f;   // Auto-derived, no Inspector field needed

    // ── Runtime helpers ───────────────────────────────────────────────────────
    /// <summary>True if the Wizard is still alive.</summary>
    public bool IsAlive => currentHP > 0;

    /// <summary>
    /// Applies damage to the Wizard, clamped to 0.
    /// TODO: Hook this up to PlayerController once CombatManager exists.
    /// </summary>
    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        Debug.Log($"{wizardName} takes {amount} damage. HP: {currentHP}/{maxHP}");
    }

    /// <summary>Heals the Wizard, clamped to maxHP.</summary>
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        Debug.Log($"{wizardName} heals {amount} HP. HP: {currentHP}/{maxHP}");
    }

    /// <summary>
    /// Restores all spell slots (Long Rest).
    /// TODO: Call this when the player returns to the Wizard Tower.
    /// </summary>
    public void LongRest()
    {
        currentHP = maxHP;
        for (int i = 0; i < maxSpellSlots.Length; i++)
            currentSpellSlots[i] = maxSpellSlots[i];

        Debug.Log($"{wizardName} takes a long rest. HP and spell slots restored.");
    }
}