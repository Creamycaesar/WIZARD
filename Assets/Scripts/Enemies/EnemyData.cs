using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
//  EnemyAttack — Serializable struct for a single attack action.
//  Maps to the attack entries in a 5e stat block.
//  Used by CombatManager.ResolveAttack() and EnemyAI.DecideAction().
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Represents a single attack action an enemy can take.
/// Melee attacks have range 1 (adjacent tiles). Ranged attacks have range > 1.
/// The EnemyAI uses these to decide which attack to use based on distance.
/// </summary>
[System.Serializable]
public struct EnemyAttack
{
    [Tooltip("Display name shown in the action log (e.g. 'Shortsword', 'Claw', 'Fire Bolt')")]
    public string attackName;

    [Tooltip("Melee or Ranged — determines whether the enemy needs to be adjacent")]
    public AttackRange attackRange;

    [Tooltip("Range in tiles. Melee attacks should be 1. Ranged attacks use their actual range.")]
    public int rangeTiles;

    [Tooltip("Added to the d20 roll vs the target's AC")]
    public int attackBonus;

    [Tooltip("Number of damage dice (the N in NdX+B)")]
    public int damageDiceCount;

    [Tooltip("Sides per damage die (the X in NdX+B)")]
    public int damageDiceSides;

    [Tooltip("Flat bonus added to damage (the B in NdX+B)")]
    public int damageBonus;

    [Tooltip("Damage type for resistance/immunity checks")]
    public DamageType damageType;

    [Tooltip("Optional: condition applied on hit (e.g. Poisoned from a spider bite). Set to None if N/A.")]
    public Condition appliedCondition;

    [Tooltip("If a condition is applied, the DC for the saving throw to resist it")]
    public int conditionSaveDC;

    [Tooltip("Which ability score the target uses to save against the applied condition")]
    public AbilityScore conditionSaveAbility;

    [Tooltip("Duration in turns for the applied condition (0 = no condition)")]
    public int conditionDurationTurns;
}

/// <summary>
/// Melee vs Ranged. Used by EnemyAI to decide positioning behavior:
/// melee enemies close distance, ranged enemies try to maintain it.
/// </summary>
public enum AttackRange
{
    Melee,
    Ranged
}

// ═══════════════════════════════════════════════════════════════════════════
//  EnemySpellcasting — Serializable struct for innate/prepared spellcasting.
//  References existing SpellData ScriptableObjects so enemies and the player
//  share the same spell definitions and resolution pipeline.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Enemy spellcasting block. Modeled after 5e stat block spellcasting entries.
/// The EnemyAI inserts spell evaluation into its decision tree when this is present.
/// </summary>
[System.Serializable]
public struct EnemySpellcasting
{
    [Tooltip("The ability score this creature uses for spellcasting (usually Intelligence, Wisdom, or Charisma)")]
    public AbilityScore spellcastingAbility;

    [Tooltip("Spell save DC — targets must beat this on their saving throw")]
    public int spellSaveDC;

    [Tooltip("Spell attack bonus — added to d20 for spell attack rolls")]
    public int spellAttackBonus;

    [Tooltip("Spell slots available per spell level. Index 0 = 1st-level slots, index 8 = 9th-level slots.")]
    public int[] spellSlots;

    [Tooltip("Spells this creature knows/has prepared, referencing SpellData assets")]
    public SpellData[] knownSpells;
}

// ═══════════════════════════════════════════════════════════════════════════
//  EnemyData — ScriptableObject modeled after a 5e SRD stat block.
//  One asset per enemy type (e.g. Skeleton, Goblin, Lich).
//  Referenced by EnemyController at runtime; spawned by EnemySpawner.
//
//  File: Assets/Scripts/Enemies/EnemyData.cs
//  Asset location: Assets/Data/Enemies/
// ═══════════════════════════════════════════════════════════════════════════

[CreateAssetMenu(menuName = "WIZARD/Enemy")]
public class EnemyData : ScriptableObject
{
    // ── Identity ─────────────────────────────────────────────────────────

    [Header("Identity")]

    [Tooltip("Display name shown in the HUD and action log")]
    public string creatureName;

    [Tooltip("Challenge Rating as an int (0, 1, 2, 3...). CR 1/4 = 0, CR 1/2 = 0.")]
    public int challengeRating;

    [Tooltip("Creature size for future tile-occupancy rules")]
    public CreatureSize size;

    [Tooltip("Flavor text shown when the player examines this enemy")]
    [TextArea(2, 4)]
    public string description;

    // ── Core Stats ───────────────────────────────────────────────────────

    [Header("Core Stats")]

    public int armorClass;
    public int maxHP;

    [Tooltip("Movement speed in tiles per turn")]
    public int movementSpeed;

    [Tooltip("Proficiency bonus (derived from CR in 5e, but stored explicitly for clarity)")]
    public int proficiencyBonus;

    // ── Ability Scores ───────────────────────────────────────────────────

    [Header("Ability Scores")]

    public int strength;
    public int dexterity;
    public int constitution;
    public int intelligence;
    public int wisdom;
    public int charisma;

    // ── Saving Throw Proficiencies ───────────────────────────────────────

    [Header("Saving Throws")]

    [Tooltip("Which saving throws this creature is proficient in (adds proficiency bonus)")]
    public AbilityScore[] savingThrowProficiencies;

    // ── Attacks ──────────────────────────────────────────────────────────

    [Header("Attacks")]

    [Tooltip("All attack actions this creature can take")]
    public EnemyAttack[] attacks;

    [Tooltip("Whether this creature can make multiple attacks per turn")]
    public bool hasMultiattack;

    [Tooltip("How many attacks per turn when using Multiattack")]
    public int multiattackCount;

    // ── Spellcasting ─────────────────────────────────────────────────────

    [Header("Spellcasting")]

    [Tooltip("Whether this creature can cast spells")]
    public bool canCastSpells;

    [Tooltip("Spellcasting details — only used if canCastSpells is true")]
    public EnemySpellcasting spellcasting;

    // ── Defenses ─────────────────────────────────────────────────────────

    [Header("Defenses")]

    public DamageType[] resistances;
    public DamageType[] immunities;
    public Condition[] conditionImmunities;

    // ── Vulnerabilities ──────────────────────────────────────────────

    [Header("Vulnerabilities")]

    public DamageType[] vulnerabilities;

    // ── Senses & Perception ──────────────────────────────────────────────

    [Header("Senses")]

    [Tooltip("Darkvision range in tiles (0 = no darkvision)")]
    public int darkvisionRange;

    [Tooltip("Passive Perception score — used for stealth/detection checks")]
    public int passivePerception;

    // ── AI Behavior Hints ────────────────────────────────────────────────

    [Header("AI Behavior")]

    [Tooltip("How far away the enemy detects the player (in tiles)")]
    public int aggroRadius;

    [Tooltip("HP percentage (0-1) at which the enemy tries to flee")]
    [Range(0f, 1f)]
    public float retreatThreshold;

    // ── Loot ─────────────────────────────────────────────────────────────

    [Header("Loot")]

    public int xpValue;

    [Tooltip("Loot table reference — leave null for enemies that drop nothing")]
    public LootTable lootTable;

    // ── Visuals ──────────────────────────────────────────────────────────

    [Header("Visuals")]

    public Sprite sprite;

    // ═════════════════════════════════════════════════════════════════════
    //  Helpers — keep MonoBehaviour code clean by centralizing lookups here
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the 5e ability modifier for the given score: (score - 10) / 2, rounded down.
    /// </summary>
    public int GetAbilityModifier(AbilityScore ability)
    {
        int score = ability switch
        {
            AbilityScore.Strength => strength,
            AbilityScore.Dexterity => dexterity,
            AbilityScore.Constitution => constitution,
            AbilityScore.Intelligence => intelligence,
            AbilityScore.Wisdom => wisdom,
            AbilityScore.Charisma => charisma,
            _ => 10
        };
        return Mathf.FloorToInt((score - 10) / 2f);
    }

    /// <summary>
    /// Returns the saving throw modifier for a given ability.
    /// If the creature is proficient in that save, adds proficiencyBonus.
    /// </summary>
    public int GetSavingThrowModifier(AbilityScore ability)
    {
        int modifier = GetAbilityModifier(ability);

        if (savingThrowProficiencies != null)
        {
            foreach (var prof in savingThrowProficiencies)
            {
                if (prof == ability)
                    return modifier + proficiencyBonus;
            }
        }

        return modifier;
    }

    /// <summary>
    /// Returns true if this creature has any attack that can reach the given distance.
    /// Used by EnemyAI to decide whether to move closer or attack.
    /// </summary>
    public bool HasAttackInRange(int distanceInTiles)
    {
        if (attacks == null) return false;

        foreach (var attack in attacks)
        {
            if (distanceInTiles <= attack.rangeTiles)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the strongest attack usable at the given distance
    /// (highest average damage). Used by EnemyAI to pick the best action.
    /// </summary>
    public EnemyAttack? GetBestAttackAtRange(int distanceInTiles)
    {
        EnemyAttack? best = null;
        float bestAvg = float.MinValue;

        if (attacks == null) return null;

        foreach (var attack in attacks)
        {
            if (distanceInTiles > attack.rangeTiles) continue;

            float avg = (attack.damageDiceCount * (attack.damageDiceSides + 1) / 2f)
                        + attack.damageBonus;

            if (avg > bestAvg)
            {
                bestAvg = avg;
                best = attack;
            }
        }

        return best;
    }

    /// <summary>
    /// Returns true if this creature has at least one spell slot remaining
    /// at any level. Used by EnemyAI to decide whether to consider casting.
    /// Note: Slot tracking at runtime is handled by EnemyController, not here.
    /// This checks the base data to see if spellcasting is even possible.
    /// </summary>
    public bool HasAnySpellSlots()
    {
        if (!canCastSpells || spellcasting.spellSlots == null) return false;

        foreach (int slots in spellcasting.spellSlots)
        {
            if (slots > 0) return true;
        }

        return false;
    }
}

/// <summary>
/// Creature size categories from 5e. Stored on EnemyData for future use
/// (e.g. Large creatures occupying 2x2 tiles, squeeze rules, grapple checks).
/// </summary>
public enum CreatureSize
{
    Tiny,
    Small,
    Medium,
    Large,
    Huge,
    Gargantuan
}