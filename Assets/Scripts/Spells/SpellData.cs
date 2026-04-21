using UnityEngine;

/// <summary>
/// ScriptableObject representing a single spell from the 5e SRD.
/// One asset per spell (e.g. Fire Bolt, Magic Missile, Shield).
/// Shared by both the player (via SpellManager) and enemy spellcasters
/// (via EnemyData.spellcasting.knownSpells).
///
/// File: Assets/Scripts/Spells/SpellData.cs
/// Asset location: Assets/Data/Spells/
/// </summary>
[CreateAssetMenu(menuName = "WIZARD/Spell")]
public class SpellData : ScriptableObject
{
    // ── Identity ─────────────────────────────────────────────────────────

    [Header("Identity")]

    public string spellName;

    [Tooltip("0 = cantrip, 1-9 = spell levels")]
    public int spellLevel;

    public SpellSchool school;

    [TextArea(2, 4)]
    public string description;

    // ── Targeting ────────────────────────────────────────────────────────

    [Header("Targeting")]

    [Tooltip("How the spell selects its target(s)")]
    public TargetType targetType;

    [Tooltip("Range in tiles (0 = self/touch)")]
    public int range;

    [Tooltip("Radius for Sphere AoE, in tiles (0 if single target)")]
    public int aoeRadius;

    [Tooltip("Length for Cone spells, in tiles (0 if not a cone)")]
    public int coneLength;

    // ── Casting ──────────────────────────────────────────────────────────

    [Header("Casting")]

    [Tooltip("Action, BonusAction, or Reaction")]
    public CastTime castTime;

    public bool requiresConcentration;

    public bool isRitual;

    // ── Damage ───────────────────────────────────────────────────────────

    [Header("Damage")]

    [Tooltip("Damage type. Ignored if the spell doesn't deal damage.")]
    public DamageType damageType;

    [Tooltip("Number of damage dice (the N in NdX+B)")]
    public int damageDiceCount;

    [Tooltip("Sides per damage die (the X in NdX+B)")]
    public int damageDiceSides;

    [Tooltip("Flat bonus added to damage (the B in NdX+B)")]
    public int damageBonus;

    // ── Resolution ───────────────────────────────────────────────────────

    [Header("Resolution")]

    [Tooltip("True = spell attack roll vs AC (e.g. Fire Bolt). False = saving throw.")]
    public bool requiresAttackRoll;

    [Tooltip("If not an attack roll, which ability the target saves with")]
    public AbilityScore savingThrowAbility;

    // ── Effects ──────────────────────────────────────────────────────────

    [Header("Effects")]

    [Tooltip("All mechanical effects this spell applies when cast")]
    public SpellEffect[] effects;

    // ── Upcasting ────────────────────────────────────────────────────────

    [Header("Upcasting")]

    [Tooltip("Whether this spell can be cast at a higher level for increased effect")]
    public bool canUpcast;

    [Tooltip("Additional damage dice per spell level above the base level")]
    public int upcastDicePerLevel;

    // ── Visuals ──────────────────────────────────────────────────────────

    [Header("Visuals")]

    [Tooltip("Icon shown in the spellbook and hotbar")]
    public Sprite icon;
}