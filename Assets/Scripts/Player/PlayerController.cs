using UnityEngine;

/// <summary>
/// PlayerController reads from WIZARD_InputActions and moves the player
/// one tile at a time on the GridManager's logical grid.
///
/// Setup in Unity:
///   1. Create a GameObject named "Player" in your scene.
///   2. Attach this script to it.
///   3. Set the starting grid position in the Inspector (default: 10, 10).
///   4. The player will snap to that tile on Start.
///
/// Dependencies:
///   - GridManager must exist in the scene.
///   - TurnManager must exist in the scene (stubbed for now).
///   - WIZARD_InputActions.cs must be generated from the .inputactions asset.
/// </summary>
public class PlayerController : MonoBehaviour
{
    // ── Inspector fields ──────────────────────────────────────────────────────
    [Header("Starting Position")]
    [Tooltip("Where the player spawns on the grid at the start of a floor.")]
    [SerializeField] private Vector2Int startGridPosition = new Vector2Int(10, 10);

    [Header("Fog of War")]
    [Tooltip("How many tiles away the player can see. 5 is a good starting value.")]
    [SerializeField] private int visionRadius = 5;

    // ── Public state ──────────────────────────────────────────────────────────
    /// <summary>The player's current position on the logical grid.</summary>
    public Vector2Int GridPosition { get; private set; }

    // ── Private ───────────────────────────────────────────────────────────────
    private WIZARD_InputActions _inputActions;
    private bool _myTurn = true;  // Will be driven by TurnManager once built

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _inputActions = new WIZARD_InputActions();
    }

    private void OnEnable()
    {
        // Enable the Gameplay action map
        _inputActions.Gameplay.Enable();

        // ── Movement ──────────────────────────────────────────────────────────
        _inputActions.Gameplay.MoveNorth.performed += _ => TryMove(Vector2Int.up);
        _inputActions.Gameplay.MoveSouth.performed += _ => TryMove(Vector2Int.down);
        _inputActions.Gameplay.MoveWest.performed += _ => TryMove(Vector2Int.left);
        _inputActions.Gameplay.MoveEast.performed += _ => TryMove(Vector2Int.right);

        // ── Wait / skip turn ──────────────────────────────────────────────────
        _inputActions.Gameplay.Wait.performed += _ => TryWait();

        // ── Interact ──────────────────────────────────────────────────────────
        _inputActions.Gameplay.Interact.performed += _ => TryInteract();
    }

    private void OnDisable()
    {
        _inputActions.Gameplay.MoveNorth.performed -= _ => TryMove(Vector2Int.up);
        _inputActions.Gameplay.MoveSouth.performed -= _ => TryMove(Vector2Int.down);
        _inputActions.Gameplay.MoveWest.performed -= _ => TryMove(Vector2Int.left);
        _inputActions.Gameplay.MoveEast.performed -= _ => TryMove(Vector2Int.right);
        _inputActions.Gameplay.Wait.performed -= _ => TryWait();
        _inputActions.Gameplay.Interact.performed -= _ => TryInteract();

        _inputActions.Gameplay.Disable();
    }

    private void Start()
    {
        // Snap player to starting tile
        GridPosition = startGridPosition;
        SnapToGrid();

        // Register as occupant on the grid
        GridManager.Instance.SetOccupant(GridPosition, gameObject);

        // Reveal initial fog of war
        GridManager.Instance.UpdateFogOfWar(GridPosition, visionRadius);

        Debug.Log($"Player spawned at grid position {GridPosition}.");
    }

    // ── Movement ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Attempts to move the player one tile in the given direction.
    /// Handles: walkability check, bump interaction, occupancy update,
    /// world position snap, fog of war update, and turn end.
    /// </summary>
    private void TryMove(Vector2Int direction)
    {
        if (!_myTurn) return;

        Vector2Int targetPos = GridPosition + direction;
        var targetTile = GridManager.Instance.GetTile(targetPos);

        if (targetTile == null)
            return; // Out of bounds

        // ── Bump into an occupant (enemy) ──────────────────────────────────
        if (targetTile.IsOccupied)
        {
            // TODO: When CombatManager exists, trigger a melee attack here.
            // For now, just log so we can verify bump detection is working.
            Debug.Log($"Bump! Occupant on tile {targetPos}: {targetTile.Occupant.name}");
            EndTurn();
            return;
        }

        // ── Bump into a door — open it ─────────────────────────────────────
        if (targetTile.TileType == TileType.Door)
        {
            OpenDoor(targetPos);
            EndTurn();
            return;
        }

        // ── Blocked by wall or non-walkable tile ───────────────────────────
        if (!targetTile.IsWalkable)
        {
            // Silently blocked — no turn cost for bumping a wall
            return;
        }

        // ── Valid move ─────────────────────────────────────────────────────
        GridManager.Instance.MoveOccupant(GridPosition, targetPos);
        GridPosition = targetPos;
        SnapToGrid();

        GridManager.Instance.UpdateFogOfWar(GridPosition, visionRadius);

        Debug.Log($"Player moved to {GridPosition}.");
        EndTurn();
    }

    /// <summary>
    /// Player passes their turn without acting.
    /// Still counts as a turn — enemies will take theirs.
    /// </summary>
    private void TryWait()
    {
        if (!_myTurn) return;

        Debug.Log("Player waited.");
        EndTurn();
    }

    // ── Interaction ───────────────────────────────────────────────────────────
    /// <summary>
    /// Checks the four cardinal neighbours for interactable tiles and acts
    /// on the first one found. Priority: Stairs > Chest/Item > NPC > Door.
    /// TODO: Expand once InteractableObject components exist.
    /// </summary>
    private void TryInteract()
    {
        if (!_myTurn) return;

        var neighbours = GridManager.Instance.GetCardinalNeighbours(GridPosition);

        foreach (var tile in neighbours)
        {
            switch (tile.TileType)
            {
                case TileType.StairsDown:
                    Debug.Log("Stairs found! TODO: descend to next floor.");
                    EndTurn();
                    return;

                case TileType.Door:
                    OpenDoor(tile.GridPosition);
                    EndTurn();
                    return;
            }
        }

        Debug.Log("Nothing to interact with.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Opens a door tile: converts it to Floor, refreshes walkability,
    /// and repaints the visual tile.
    /// </summary>
    private void OpenDoor(Vector2Int doorPos)
    {
        GridManager.Instance.SetTile(doorPos, TileType.Floor);
        Debug.Log($"Door opened at {doorPos}.");
    }

    /// <summary>
    /// Snaps the player's world position to the centre of their current grid tile.
    /// </summary>
    private void SnapToGrid()
    {
        transform.position = GridManager.Instance.GridToWorld(GridPosition);
    }

    /// <summary>
    /// Signals that the player has used their turn.
    /// Hands control to TurnManager, which will run enemy turns
    /// and call BeginTurn() again when the player can act.
    /// </summary>
    private void EndTurn()
    {
        _myTurn = false;
        TurnManager.Instance.EndPlayerTurn();
    }

    // ── Public API ────────────────────────────────────────────────────────────
    /// <summary>Called by TurnManager to give the player their turn.</summary>
    public void BeginTurn()
    {
        _myTurn = true;
    }
}