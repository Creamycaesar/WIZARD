using System;
using UnityEngine;

/// <summary>
/// Root of the dependency graph. Owns the game state machine and level context.
/// Manages level transitions (Tower ↔ Dungeon), holds cross-scene references
/// (WizardStats, TowerSaveData), and coordinates game-wide events.
///
/// File: Assets/Scripts/Core/GameManager.cs
/// Layer: 1 (Depends only on Layer 0 — no manager dependencies)
/// Persistence: DontDestroyOnLoad
/// </summary>
public class GameManager : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════
    //  Singleton (DontDestroyOnLoad)
    // ═════════════════════════════════════════════════════════════════════

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Events
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Whenever ChangeState() is called.</summary>
    public static event Action<GameState, GameState> OnStateChanged;

    /// <summary>When the loaded level changes (Tower ↔ Dungeon, floor transitions).</summary>
    public static event Action<LevelContext, int> OnLevelChanged;

    /// <summary>When TriggerPermadeath() is called.</summary>
    public static event Action OnPlayerDied;

    /// <summary>When LoadDungeonFloor() is first called for a new run.</summary>
    public static event Action<DungeonConfig> OnRunStarted;

    /// <summary>When the player beats or exits a dungeon.</summary>
    public static event Action<bool> OnRunCompleted;

    // ═════════════════════════════════════════════════════════════════════
    //  State
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>The current game state.</summary>
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    /// <summary>Whether the player is in the Tower or a Dungeon.</summary>
    public LevelContext CurrentLevel { get; private set; } = LevelContext.Tower;

    /// <summary>0 = Tower, 1+ = dungeon floor number.</summary>
    public int CurrentFloor { get; private set; }

    /// <summary>The active Wizard's stats for this run.</summary>
    public WizardStats CurrentWizard { get; private set; }

    /// <summary>Persistent Tower save data reference.</summary>
    public TowerSaveData TowerData { get; private set; } = new TowerSaveData();

    /// <summary>The dungeon config for the active or selected dungeon.</summary>
    public DungeonConfig CurrentDungeon { get; private set; }

    /// <summary>Whether a dungeon run is currently in progress.</summary>
    private bool runInProgress;

    // ═════════════════════════════════════════════════════════════════════
    //  Public API — State Machine
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Transitions to a new game state. Validates the transition and fires OnStateChanged.
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (newState == CurrentState) return;

        GameState oldState = CurrentState;

        // Validate transition legality
        if (!IsValidTransition(oldState, newState))
        {
            Debug.LogWarning($"GameManager: Invalid state transition {oldState} → {newState}");
            return;
        }

        CurrentState = newState;
        OnStateChanged?.Invoke(oldState, newState);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Public API — Level Management
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads the Tower level. Regenerates the grid with Tower layout data.
    /// Sets CurrentLevel to Tower, CurrentFloor to 0.
    /// </summary>
    public void LoadTowerLevel()
    {
        CurrentLevel = LevelContext.Tower;
        CurrentFloor = 0;

        TurnManager.Instance.ClearAllActors();

        // TODO: TowerManager.Instance.GenerateTowerLayout() → GridManager.InitializeGrid()

        if (CurrentState != GameState.Gameplay)
            ChangeState(GameState.Gameplay);

        OnLevelChanged?.Invoke(CurrentLevel, CurrentFloor);
    }

    /// <summary>
    /// Generates a dungeon floor. Sets CurrentLevel to Dungeon, updates CurrentFloor.
    /// </summary>
    public void LoadDungeonFloor(DungeonConfig dungeon, int floor)
    {
        CurrentDungeon = dungeon;
        CurrentLevel = LevelContext.Dungeon;
        CurrentFloor = floor;

        TurnManager.Instance.ClearAllActors();

        // Fire OnRunStarted on the first floor of a new run
        if (!runInProgress)
        {
            runInProgress = true;
            OnRunStarted?.Invoke(dungeon);
        }

        // TODO: DungeonGenerator.Instance.GenerateFloor(dungeon, floor)

        OnLevelChanged?.Invoke(CurrentLevel, CurrentFloor);
    }

    /// <summary>
    /// Advances to the next dungeon floor. If on the final floor, returns to Tower.
    /// </summary>
    public void AdvanceFloor()
    {
        if (CurrentDungeon == null) return;

        if (CurrentFloor >= CurrentDungeon.totalFloors)
        {
            ReturnToTower();
        }
        else
        {
            LoadDungeonFloor(CurrentDungeon, CurrentFloor + 1);
        }
    }

    /// <summary>
    /// Saves run results and loads the Tower level.
    /// </summary>
    public void ReturnToTower()
    {
        bool success = CurrentDungeon != null
                       && CurrentFloor >= CurrentDungeon.totalFloors;

        runInProgress = false;
        OnRunCompleted?.Invoke(success);

        // TODO: SaveManager.Instance.SaveTower(TowerData);

        LoadTowerLevel();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Public API — Permadeath
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fires OnPlayerDied, saves Tower data, clears current Wizard, sets state to GameOver.
    /// </summary>
    public void TriggerPermadeath()
    {
        OnPlayerDied?.Invoke();

        // TODO: SaveManager.Instance.SaveTower(TowerData);
        // TODO: SaveManager.Instance.DeleteRunSave();

        runInProgress = false;
        CurrentWizard = null;
        ChangeState(GameState.GameOver);
    }

    /// <summary>
    /// Creates a new WizardStats asset for a new run.
    /// Called after permadeath or at game start.
    /// </summary>
    public WizardStats GenerateNewWizard()
    {
        // TODO: Create a runtime WizardStats instance with randomized starting values.
        // For now, use the default asset assigned in the editor.
        CurrentWizard = ScriptableObject.CreateInstance<WizardStats>();

        // TODO: Initialize with default level-1 Wizard stats
        // (ability scores, HP, spell slots, proficiencies)

        return CurrentWizard;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Internal — State Transition Validation
    // ═════════════════════════════════════════════════════════════════════

    private bool IsValidTransition(GameState from, GameState to)
    {
        return (from, to) switch
        {
            (GameState.MainMenu, GameState.Gameplay) => true,
            (GameState.Gameplay, GameState.SpellTargeting) => true,
            (GameState.SpellTargeting, GameState.Gameplay) => true,
            (GameState.Gameplay, GameState.GameOver) => true,
            (GameState.GameOver, GameState.Gameplay) => true,
            (GameState.Gameplay, GameState.Paused) => true,
            (GameState.Paused, GameState.Gameplay) => true,
            _ => false
        };
    }
}