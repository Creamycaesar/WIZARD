/// <summary>
/// Result of a resolved attack roll via CombatManager.ResolveAttack().
/// Consumed by the action log, UI, and any system that needs to know
/// the outcome of a combat exchange.
/// </summary>
public struct AttackResult
{
    /// <summary>True if the attack hit (roll + bonus >= target AC).</summary>
    public bool Hit;

    /// <summary>True if the raw d20 roll was a natural 20.</summary>
    public bool CriticalHit;

    /// <summary>Total damage dealt after rolling damage dice. 0 on a miss.</summary>
    public int DamageDealt;

    /// <summary>The damage type of the attack (for resistance/immunity logging).</summary>
    public DamageType Type;

    /// <summary>The entity that made the attack.</summary>
    public IDamageable Attacker;

    /// <summary>The entity that was attacked.</summary>
    public IDamageable Target;

    /// <summary>The raw d20 roll (before modifiers). Useful for logging and UI.</summary>
    public int Roll;
}

/// <summary>
/// Result of a resolved saving throw via CombatManager.ResolveSavingThrow().
/// Used by SpellManager to determine spell effects (full damage vs half, etc.)
/// and by the action log for feedback.
/// </summary>
public struct SaveResult
{
    /// <summary>True if the saving throw succeeded (roll + modifier >= DC).</summary>
    public bool Success;

    /// <summary>The raw d20 roll (before modifiers).</summary>
    public int Roll;

    /// <summary>The total result after adding the ability modifier (and proficiency if applicable).</summary>
    public int Total;

    /// <summary>The DC that was being rolled against.</summary>
    public int DC;

    /// <summary>Which ability score was tested.</summary>
    public AbilityScore Ability;

    /// <summary>The entity that made the saving throw.</summary>
    public IDamageable Target;
}