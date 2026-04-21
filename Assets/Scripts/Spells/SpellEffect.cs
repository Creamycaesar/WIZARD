using UnityEngine;

/// <summary>
/// What category of effect a spell applies. A single spell can have
/// multiple SpellEffects (e.g. Chromatic Orb deals damage, Burning Hands
/// deals damage in a cone, Hold Person applies the Paralyzed condition).
///
/// SpellManager reads these during CastSpell() to determine what to resolve.
/// </summary>
public enum SpellEffectType
{
    /// <summary>Deals damage (uses SpellData's damage fields).</summary>
    Damage,

    /// <summary>Heals the target.</summary>
    Healing,

    /// <summary>Applies a condition to the target (e.g. Paralyzed, Blinded).</summary>
    ApplyCondition,

    /// <summary>Removes a condition from the target (e.g. Lesser Restoration).</summary>
    RemoveCondition,

    /// <summary>Grants a temporary AC bonus (e.g. Shield, Mage Armor).</summary>
    ACBonus,

    /// <summary>Pushes the target away from the caster by N tiles.</summary>
    ForcePush,

    /// <summary>Creates or modifies terrain tiles (e.g. Grease, Web).</summary>
    CreateTerrain,

    /// <summary>Summons a light source, extending vision radius.</summary>
    Light,

    /// <summary>Grants temporary hit points.</summary>
    TemporaryHP,

    /// <summary>Automatic hit damage with no attack roll (e.g. Magic Missile).</summary>
    AutoDamage,

    /// <summary>Half damage on a successful save (e.g. Burning Hands).</summary>
    HalfDamageOnSave
}

/// <summary>
/// A single effect that a spell applies when cast. SpellData holds an array
/// of these to support spells with multiple mechanical outcomes.
///
/// Not all fields are used by every effect type — the SpellManager checks
/// effectType and reads only the relevant fields.
///
/// NOTE: This type is not defined in the tech arch document. It was created
/// to support the SpellData.effects[] field which the doc references but
/// does not specify. This may need revision as spell implementation progresses.
/// </summary>
[System.Serializable]
public struct SpellEffect
{
    [Tooltip("What this effect does")]
    public SpellEffectType effectType;

    [Tooltip("Condition to apply or remove (used by ApplyCondition / RemoveCondition)")]
    public Condition condition;

    [Tooltip("Duration in turns for applied conditions (-1 = concentration duration)")]
    public int durationTurns;

    [Tooltip("Flat value used by ACBonus, ForcePush distance, Healing amount, TemporaryHP, etc.")]
    public int value;

    [Tooltip("Dice count for effects that roll (e.g. healing 2d8)")]
    public int diceCount;

    [Tooltip("Dice sides for effects that roll")]
    public int diceSides;

    [Tooltip("Tile type to create (used by CreateTerrain). Maps to TileType enum value.")]
    public int terrainTileType;

    [Tooltip("Saving throw ability for this specific effect (if different from SpellData's default)")]
    public AbilityScore saveAbility;

    [Tooltip("Whether this effect uses the spell's save DC for its own save")]
    public bool useSpellSaveDC;
}