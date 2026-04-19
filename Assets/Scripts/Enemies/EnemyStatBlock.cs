using UnityEngine;

/// <summary>
/// Holds the D&D 5e stat block for one enemy type.
/// Create one asset per enemy type (Skeleton, Goblin, etc.) via:
///   Assets → Create → WIZARD → Enemy Stat Block
///
/// EnemyController reads from this at runtime — the stat block itself
/// is never modified, so it's safe to share across multiple enemy instances.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyStatBlock", menuName = "WIZARD/Enemy Stat Block")]
public class EnemyStatBlock : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown in the combat log and tooltips.")]
    public string enemyName = "Unknown Enemy";

    [Tooltip("Challenge Rating from the 5e SRD.")]
    public float challengeRating = 0.25f;

    [Tooltip("XP awarded to the player on kill.")]
    public int xpValue = 50;

    // ── Core stats ────────────────────────────────────────────────────────────
    [Header("Core Stats")]
    [Tooltip("Maximum hit points. Rolled fresh for each instance (see EnemyController).")]
    public int maxHP = 13;

    [Tooltip("Armor Class. Attacker must meet or beat this with their attack roll.")]
    public int armorClass = 13;

    [Header("Movement")]
    [Tooltip("Movement speed in tiles per turn. 30ft = 6 tiles (1 tile = 5ft per 5e).")]
    public int movementSpeedTiles = 6;

    // ── Attack ────────────────────────────────────────────────────────────────
    [Header("Melee Attack")]
    [Tooltip("Name of the attack shown in the combat log. e.g. 'Shortsword', 'Claws'.")]
    public string attackName = "Attack";

    [Tooltip("Attack bonus added to the d20 roll against the target's AC.")]
    public int attackBonus = 4;

    [Tooltip("Number of damage dice. e.g. 2 for '2d6'.")]
    public int damageDiceCount = 1;

    [Tooltip("Sides on each damage die. e.g. 6 for 'd6'.")]
    public int damageDiceSides = 6;

    [Tooltip("Flat bonus added to damage. e.g. 2 for '1d6+2'.")]
    public int damageBonus = 2;

    [Tooltip("Damage type for resistance/immunity checks later.")]
    public DamageType damageType = DamageType.Piercing;

    // ── Behaviour ─────────────────────────────────────────────────────────────
    [Header("AI Behaviour")]
    [Tooltip("How many tiles away this enemy will start pursuing the player.")]
    public int aggroRadius = 8;

    [Tooltip("HP percentage below which this enemy tries to retreat (0 = never retreats).")]
    [Range(0f, 1f)]
    public float retreatThreshold = 0f;

    // ── Helpers ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Rolls damage for one hit using this stat block's damage dice.
    /// e.g. a Skeleton with 1d6+2 calls Dice.Roll(1, 6, 2).
    /// </summary>
    public int RollDamage() => Dice.Roll(damageDiceCount, damageDiceSides, damageBonus);

    /// <summary>
    /// Returns a human-readable damage expression for the combat log.
    /// e.g. "1d6+2 piercing"
    /// </summary>
    public string DamageExpression =>
        $"{damageDiceCount}d{damageDiceSides}+{damageBonus} {damageType}";
}

/// <summary>
/// Damage types from the 5e SRD. Used for resistance and immunity checks.
/// Expand as needed when the spell system is built.
/// </summary>
public enum DamageType
{
    Slashing,
    Piercing,
    Bludgeoning,
    Fire,
    Cold,
    Lightning,
    Thunder,
    Acid,
    Poison,
    Necrotic,
    Radiant,
    Psychic,
    Force
}