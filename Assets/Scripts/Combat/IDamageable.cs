using System.Collections.Generic;

/// <summary>
/// Interface for anything that can take damage and be targeted in combat.
/// Implemented by PlayerController and EnemyController.
///
/// CombatManager operates entirely through this interface so it doesn't
/// need to know whether it's resolving an attack against the player or an enemy.
/// </summary>
public interface IDamageable
{
    int ArmorClass { get; }
    int CurrentHP { get; }
    bool IsAlive { get; }
    void TakeDamage(int amount, DamageType type);
    List<Condition> ActiveConditions { get; }
}