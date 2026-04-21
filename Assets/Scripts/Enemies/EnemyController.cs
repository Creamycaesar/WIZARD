using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime representation of a dungeon enemy. Configured by EnemyData ScriptableObject.
/// Implements ITurnActor (for turn ordering) and IDamageable (for combat).
/// TakeTurn() delegates to EnemyAI.DecideAction().
///
/// File: Assets/Scripts/Enemies/EnemyController.cs
/// Layer: 3 (Depends on EnemyData, GridManager, TurnManager, CombatManager)
/// </summary>
public class EnemyController : MonoBehaviour, ITurnActor, IDamageable
{
    // ═════════════════════════════════════════════════════════════════════
    //  Events
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>After taking damage.</summary>
    public static event Action<EnemyController, int, int> OnEnemyDamaged;

    /// <summary>When HP reaches 0.</summary>
    public static event Action<EnemyController, int> OnEnemyDied;

    /// <summary>After moving on the grid.</summary>
    public static event Action<EnemyController, Vector2Int, Vector2Int> OnEnemyMoved;

    // ═════════════════════════════════════════════════════════════════════
    //  Data & State
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Reference to this enemy's ScriptableObject stats.</summary>
    public EnemyData Data { get; private set; }

    /// <summary>Current grid position.</summary>
    public Vector2Int GridPosition { get; private set; }

    /// <summary>Current hit points.</summary>
    public int CurrentHP { get; private set; }

    /// <summary>True if CurrentHP > 0.</summary>
    public bool IsAlive => CurrentHP > 0;

    private EnemyAI ai;

    // ═════════════════════════════════════════════════════════════════════
    //  ITurnActor
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Returns Data.dexterity for turn ordering.</summary>
    public int Dexterity => Data != null ? Data.dexterity : 10;

    /// <summary>Assigned by EnemySpawner. Used as Dexterity tiebreaker.</summary>
    public int SpawnOrder { get; private set; }

    /// <summary>Triggers EnemyAI.DecideAction() then executes the result.</summary>
    public void TakeTurn()
    {
        if (!IsAlive) return;
        ai?.DecideAction(this);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  IDamageable
    // ═════════════════════════════════════════════════════════════════════

    public int ArmorClass => Data != null ? Data.armorClass : 10;
    public List<Condition> ActiveConditions { get; } = new();

    /// <summary>
    /// Applies resistances/immunities, reduces HP. Fires OnEnemyDamaged.
    /// On death: fires OnEnemyDied, drops loot, grants XP, unregisters from TurnManager.
    /// </summary>
    public void TakeDamage(int amount, DamageType type)
    {
        if (!IsAlive || Data == null) return;

        // Check immunities
        if (Data.immunities != null)
        {
            foreach (var immunity in Data.immunities)
            {
                if (immunity == type)
                {
                    // Immune — no damage
                    OnEnemyDamaged?.Invoke(this, 0, CurrentHP);
                    return;
                }
            }
        }

        // Check resistances (half damage)
        if (Data.resistances != null)
        {
            foreach (var resistance in Data.resistances)
            {
                if (resistance == type)
                {
                    amount = Mathf.Max(1, amount / 2);
                    break;
                }
            }
        }

        CurrentHP -= amount;
        if (CurrentHP < 0) CurrentHP = 0;

        OnEnemyDamaged?.Invoke(this, amount, CurrentHP);

        if (!IsAlive)
        {
            Die();
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by EnemySpawner. Sets up stats from data, places on grid,
    /// registers with TurnManager.
    /// </summary>
    public void Initialize(EnemyData data, Vector2Int spawnPos, int spawnOrder)
    {
        Data = data;
        CurrentHP = data.maxHP;
        SpawnOrder = spawnOrder;
        GridPosition = spawnPos;

        ai = new EnemyAI();

        GridManager.Instance.SetOccupant(spawnPos, gameObject);
        transform.position = GridManager.Instance.GridToWorld(spawnPos);

        TurnManager.Instance.RegisterActor(this);
    }

    /// <summary>
    /// Moves this enemy to a new grid position.
    /// Called by EnemyAI during the enemy's turn.
    /// </summary>
    public void MoveTo(Vector2Int newPos)
    {
        if (!GridManager.Instance.IsWalkable(newPos)) return;

        Vector2Int fromPos = GridPosition;
        GridPosition = newPos;
        GridManager.Instance.SetOccupant(newPos, gameObject);
        transform.position = GridManager.Instance.GridToWorld(newPos);

        OnEnemyMoved?.Invoke(this, fromPos, newPos);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Internal
    // ═════════════════════════════════════════════════════════════════════

    private void Die()
    {
        OnEnemyDied?.Invoke(this, Data.xpValue);

        // TODO: Drop loot from Data.lootTable
        // TODO: Grant XP to player via WizardStats

        GridManager.Instance.ClearOccupant(GridPosition);
        TurnManager.Instance.UnregisterActor(this);
        CombatManager.Instance.ClearAllConditions(this);

        gameObject.SetActive(false);
        // TODO: Object pooling instead of SetActive(false)
    }
}