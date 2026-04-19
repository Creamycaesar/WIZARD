using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyController lives on every enemy GameObject in the scene.
/// It reads from an EnemyStatBlock for its stats, registers itself
/// with TurnManager on spawn, and executes its AI when TakeTurn() is called.
///
/// Setup in Unity:
///   1. Create a GameObject for your enemy (e.g. "Skeleton").
///   2. Attach this script to it.
///   3. Assign a StatBlock ScriptableObject in the Inspector.
///   4. Set the starting grid position in the Inspector.
///   5. Optionally attach a SpriteRenderer for visuals.
///
/// AI behaviour (v1 — simple but functional):
///   - Within aggro radius: pursue the player.
///   - Adjacent to player: melee attack.
///   - Below retreat threshold HP: move away from player.
/// </summary>
public class EnemyController : MonoBehaviour
{
    // ── Inspector fields ──────────────────────────────────────────────────────
    [Header("Stat Block")]
    [Tooltip("The 5e stat block ScriptableObject for this enemy type.")]
    [SerializeField] private EnemyStatBlock statBlock;

    [Header("Starting Position")]
    [SerializeField] private Vector2Int startGridPosition = new Vector2Int(15, 15);

    // ── Public state ──────────────────────────────────────────────────────────
    public Vector2Int GridPosition { get; private set; }
    public int CurrentHP { get; private set; }
    public bool IsAlive => CurrentHP > 0;
    public EnemyStatBlock StatBlock => statBlock;

    // ── Private ───────────────────────────────────────────────────────────────
    private PlayerController _player;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        if (statBlock == null)
        {
            Debug.LogError($"{name}: No EnemyStatBlock assigned! Destroying enemy.");
            Destroy(gameObject);
            return;
        }

        // Initialise HP (use stat block max for now;
        // TODO: roll HP from hit dice when WizardStats system exists)
        CurrentHP = statBlock.maxHP;

        // Cache player reference
        _player = FindAnyObjectByType<PlayerController>();
        if (_player == null)
            Debug.LogWarning($"{name}: No PlayerController found in scene.");

        // Place on grid
        GridPosition = startGridPosition;
        GridManager.Instance.SetOccupant(GridPosition, gameObject);
        SnapToGrid();

        // Register with TurnManager
        TurnManager.Instance.RegisterEnemy(this);

        Debug.Log($"{statBlock.enemyName} spawned at {GridPosition} with {CurrentHP} HP.");
    }

    private void OnDestroy()
    {
        // Always clean up — even if destroyed outside of normal death flow
        if (TurnManager.Instance != null)
            TurnManager.Instance.UnregisterEnemy(this);

        if (GridManager.Instance != null)
            GridManager.Instance.SetOccupant(GridPosition, null);
    }

    // ── Turn execution ────────────────────────────────────────────────────────
    /// <summary>
    /// Called by TurnManager during the enemy phase.
    /// Executes one full turn of AI: move and/or attack.
    /// </summary>
    public void TakeTurn()
    {
        if (!IsAlive || _player == null) return;

        Vector2Int playerPos = _player.GridPosition;
        int distToPlayer = ManhattanDistance(GridPosition, playerPos);

        // ── Out of aggro range — idle ──────────────────────────────────────
        if (distToPlayer > statBlock.aggroRadius)
        {
            Debug.Log($"{statBlock.enemyName} is idle (player out of aggro range).");
            return;
        }

        // ── Should retreat? ────────────────────────────────────────────────
        bool shouldRetreat = statBlock.retreatThreshold > 0f
                          && (float)CurrentHP / statBlock.maxHP <= statBlock.retreatThreshold;

        if (shouldRetreat)
        {
            MoveAwayFromPlayer(playerPos);
            return;
        }

        // ── Adjacent to player — attack ────────────────────────────────────
        if (distToPlayer == 1)
        {
            AttackPlayer();
            return;
        }

        // ── Not adjacent — move toward player, then attack if now adjacent ─
        MoveTowardPlayer(playerPos);

        // After moving, check if we're now adjacent
        if (ManhattanDistance(GridPosition, playerPos) == 1)
            AttackPlayer();
    }

    // ── AI movement ───────────────────────────────────────────────────────────
    /// <summary>
    /// Greedy step toward the player: picks the walkable cardinal neighbour
    /// that minimises Manhattan distance to the player.
    /// Good enough for v1; replace with A* pathfinding later.
    /// </summary>
    private void MoveTowardPlayer(Vector2Int playerPos)
    {
        Vector2Int bestPos = GridPosition;
        int bestDistance = int.MaxValue;

        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Vector2Int candidate = GridPosition + dir;

            // Don't walk into the player's tile (that's an attack, handled separately)
            if (candidate == playerPos) continue;

            if (!GridManager.Instance.IsWalkable(candidate)) continue;

            int dist = ManhattanDistance(candidate, playerPos);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestPos = candidate;
            }
        }

        if (bestPos != GridPosition)
            MoveTo(bestPos);
        else
            Debug.Log($"{statBlock.enemyName} is blocked — cannot move toward player.");
    }

    /// <summary>
    /// Greedy step away from the player: picks the walkable cardinal neighbour
    /// that maximises Manhattan distance from the player.
    /// </summary>
    private void MoveAwayFromPlayer(Vector2Int playerPos)
    {
        Vector2Int bestPos = GridPosition;
        int bestDistance = -1;

        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Vector2Int candidate = GridPosition + dir;
            if (!GridManager.Instance.IsWalkable(candidate)) continue;

            int dist = ManhattanDistance(candidate, playerPos);
            if (dist > bestDistance)
            {
                bestDistance = dist;
                bestPos = candidate;
            }
        }

        if (bestPos != GridPosition)
        {
            Debug.Log($"{statBlock.enemyName} retreats!");
            MoveTo(bestPos);
        }
    }

    /// <summary>
    /// Moves this enemy to a new grid position, updating occupancy and world position.
    /// </summary>
    private void MoveTo(Vector2Int newPos)
    {
        GridManager.Instance.MoveOccupant(GridPosition, newPos);
        GridPosition = newPos;
        SnapToGrid();

        Debug.Log($"{statBlock.enemyName} moves to {GridPosition}.");
    }

    // ── Combat ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Performs a melee attack against the player using 5e rules:
    /// roll d20 + attack bonus vs player AC.
    /// TODO: Read player AC from WizardStats once that system exists.
    /// </summary>
    private void AttackPlayer()
    {
        // TODO: Replace hardcoded player AC with _player.Stats.ArmorClass
        int playerAC = 13;

        int attackRoll = Dice.D20() + statBlock.attackBonus;
        bool hit = attackRoll >= playerAC;

        if (hit)
        {
            int damage = statBlock.RollDamage();
            Debug.Log($"{statBlock.enemyName} attacks! Roll: {attackRoll} vs AC {playerAC} — HIT! " +
                      $"{damage} {statBlock.damageType} damage. (TODO: apply to WizardStats)");

            // TODO: _player.Stats.TakeDamage(damage, statBlock.damageType);
        }
        else
        {
            Debug.Log($"{statBlock.enemyName} attacks! Roll: {attackRoll} vs AC {playerAC} — MISS!");
        }
    }

    // ── Taking damage ─────────────────────────────────────────────────────────
    /// <summary>
    /// Applies damage to this enemy. Called by the combat system (player attacks,
    /// spells, etc.) once CombatManager exists.
    /// </summary>
    public void TakeDamage(int amount, DamageType type = DamageType.Bludgeoning)
    {
        // TODO: Check for damage resistances/immunities from stat block

        CurrentHP -= amount;
        CurrentHP = Mathf.Max(CurrentHP, 0);

        Debug.Log($"{statBlock.enemyName} takes {amount} {type} damage. " +
                  $"HP: {CurrentHP}/{statBlock.maxHP}");

        if (CurrentHP <= 0)
            Die();
    }

    /// <summary>
    /// Handles enemy death: awards XP (stubbed), clears the grid tile,
    /// unregisters from TurnManager, and destroys the GameObject.
    /// </summary>
    private void Die()
    {
        Debug.Log($"{statBlock.enemyName} has been defeated! " +
                  $"(+{statBlock.xpValue} XP — TODO: award to WizardStats)");

        // TODO: Trigger loot drop
        // TODO: Award XP via WizardStats.Instance.AwardXP(statBlock.xpValue)

        // Unregister and clear grid before destroying
        TurnManager.Instance.UnregisterEnemy(this);
        GridManager.Instance.SetOccupant(GridPosition, null);

        Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void SnapToGrid()
    {
        transform.position = GridManager.Instance.GridToWorld(GridPosition);
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}