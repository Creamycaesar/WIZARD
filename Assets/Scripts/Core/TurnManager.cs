using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines whose turn it currently is.
/// </summary>
public enum TurnState
{
    PlayerTurn,
    EnemyTurn
}

/// <summary>
/// TurnManager is the heartbeat of WIZARD's combat loop.
/// It sequences turns in order: Player acts → Enemies act (in order) → Player again.
///
/// All systems end their turn by calling TurnManager.Instance.EndPlayerTurn()
/// or TurnManager.Instance.EndEnemyTurn(). Nothing self-sequences.
///
/// Setup in Unity:
///   1. Create an empty GameObject named "TurnManager".
///   2. Attach this script to it.
///   3. Assign the PlayerController reference in the Inspector.
///
/// To register an enemy: call TurnManager.Instance.RegisterEnemy(enemy)
/// To remove an enemy:   call TurnManager.Instance.UnregisterEnemy(enemy)
/// </summary>
public class TurnManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static TurnManager Instance { get; private set; }

    // ── Inspector fields ──────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("The PlayerController in the scene.")]
    [SerializeField] private PlayerController playerController;

    [Header("Debug")]
    [Tooltip("Log every turn transition to the Console.")]
    [SerializeField] private bool verboseLogging = true;

    // ── Public state ──────────────────────────────────────────────────────────
    public TurnState CurrentState { get; private set; } = TurnState.PlayerTurn;

    /// <summary>Increments each time a full round (player + all enemies) completes.</summary>
    public int RoundNumber { get; private set; } = 1;

    // ── Enemy registry ────────────────────────────────────────────────────────
    private readonly List<EnemyController> _enemies = new List<EnemyController>();

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TurnManager: duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        BeginPlayerTurn();
    }

    // ── Enemy registry ────────────────────────────────────────────────────────
    /// <summary>
    /// Registers an enemy to participate in the turn order.
    /// Call this when an enemy spawns.
    /// </summary>
    public void RegisterEnemy(EnemyController enemy)
    {
        if (!_enemies.Contains(enemy))
            _enemies.Add(enemy);
    }

    /// <summary>
    /// Removes an enemy from the turn order.
    /// Call this when an enemy dies or is removed from the floor.
    /// </summary>
    public void UnregisterEnemy(EnemyController enemy)
    {
        _enemies.Remove(enemy);
    }

    // ── Turn flow ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Starts the player's turn. Called at game start and after all enemies
    /// have taken their turns.
    /// </summary>
    private void BeginPlayerTurn()
    {
        CurrentState = TurnState.PlayerTurn;

        if (verboseLogging)
            Debug.Log($"[Round {RoundNumber}] Player turn begins.");

        playerController.BeginTurn();
    }

    /// <summary>
    /// Called by PlayerController when the player has taken their action.
    /// Kicks off the enemy turn sequence.
    /// </summary>
    public void EndPlayerTurn()
    {
        if (CurrentState != TurnState.PlayerTurn)
        {
            Debug.LogWarning("TurnManager.EndPlayerTurn() called outside of PlayerTurn state. Ignored.");
            return;
        }

        if (verboseLogging)
            Debug.Log($"[Round {RoundNumber}] Player turn ends.");

        CurrentState = TurnState.EnemyTurn;
        StartCoroutine(RunEnemyTurns());
    }

    /// <summary>
    /// Iterates through all registered enemies and gives each a turn.
    /// Enemies resolve instantly in v1 (no animation delay).
    /// The coroutine structure is in place for when we add per-enemy delays.
    /// </summary>
    private IEnumerator RunEnemyTurns()
    {
        if (verboseLogging)
            Debug.Log($"[Round {RoundNumber}] Enemy turns begin. ({_enemies.Count} enemies)");

        // Iterate over a copy in case an enemy dies mid-loop and
        // removes itself from _enemies during iteration.
        var enemiesThisTurn = new List<EnemyController>(_enemies);

        foreach (var enemy in enemiesThisTurn)
        {
            // Skip enemies that died this turn
            if (enemy == null || !enemy.IsAlive) continue;

            enemy.TakeTurn();

            // v1: enemies resolve instantly. Yield return null gives Unity
            // one frame to breathe between enemies — remove if too slow later.
            yield return null;
        }

        EndEnemyTurn();
    }

    /// <summary>
    /// Called internally after all enemies have acted.
    /// Increments the round counter and begins the next player turn.
    /// </summary>
    private void EndEnemyTurn()
    {
        if (verboseLogging)
            Debug.Log($"[Round {RoundNumber}] Enemy turns end.");

        RoundNumber++;
        BeginPlayerTurn();
    }
}