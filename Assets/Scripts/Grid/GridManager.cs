using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// GridManager owns the logical tile grid for the current floor.
/// It is the single source of truth for tile state, occupancy, and fog of war.
///
/// All game systems (PlayerController, EnemyAI, SpellSystem, FogOfWar)
/// query GridManager — never the Unity Tilemap directly.
///
/// Setup in Unity:
///   1. Create an empty GameObject named "GridManager" in your scene.
///   2. Attach this script to it.
///   3. In the Inspector, assign the WallTilemap and FloorTilemap references.
///   4. Assign TileBase assets for each TileType you want rendered.
/// </summary>
public class GridManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static GridManager Instance { get; private set; }

    // ── Inspector fields ──────────────────────────────────────────────────────
    [Header("Grid Settings")]
    [Tooltip("Width of the grid in tiles.")]
    [SerializeField] private int gridWidth = 20;

    [Tooltip("Height of the grid in tiles.")]
    [SerializeField] private int gridHeight = 20;

    [Header("Tilemaps")]
    [Tooltip("The Unity Grid component that owns the Tilemaps. Drag the Grid GameObject here.")]
    [SerializeField] private Grid grid;

    [Tooltip("Tilemap used for rendering floor tiles.")]
    [SerializeField] private Tilemap floorTilemap;

    [Tooltip("Tilemap used for rendering wall tiles.")]
    [SerializeField] private Tilemap wallTilemap;

    [Header("Tile Assets")]
    [Tooltip("TileBase asset painted for TileType.Floor.")]
    [SerializeField] private TileBase floorTile;

    [Tooltip("TileBase asset painted for TileType.Wall.")]
    [SerializeField] private TileBase wallTile;

    [Tooltip("TileBase asset painted for TileType.DestructibleWall.")]
    [SerializeField] private TileBase destructibleWallTile;

    [Tooltip("TileBase asset painted for TileType.StairsDown.")]
    [SerializeField] private TileBase stairsDownTile;

    [Tooltip("TileBase asset painted for TileType.Door (closed).")]
    [SerializeField] private TileBase doorTile;

    [Tooltip("TileBase asset painted for TileType.Water.")]
    [SerializeField] private TileBase waterTile;

    // ── Private state ─────────────────────────────────────────────────────────
    private TileData[,] _grid;

    // ── Properties ────────────────────────────────────────────────────────────
    public int Width => gridWidth;
    public int Height => gridHeight;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GridManager: duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize the grid in Awake so it exists before any other
        // script's Start() tries to query it (e.g. PlayerController.Start).
        GenerateTestRoom();
    }

    // ── Grid initialisation ───────────────────────────────────────────────────
    /// <summary>
    /// Initialises a blank grid of the given dimensions.
    /// Called by the dungeon generator before it populates tiles.
    /// </summary>
    public void InitialiseGrid(int width, int height)
    {
        gridWidth = width;
        gridHeight = height;
        _grid = new TileData[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _grid[x, y] = new TileData(new Vector2Int(x, y), TileType.Void);

        // Clear both tilemaps before painting new tiles
        floorTilemap?.ClearAllTiles();
        wallTilemap?.ClearAllTiles();
    }

    // ── Tile setters ──────────────────────────────────────────────────────────
    /// <summary>
    /// Sets the tile type at a grid position, refreshes walkability,
    /// and paints the appropriate visual tile on the correct Tilemap.
    /// </summary>
    public void SetTile(int x, int y, TileType type)
    {
        if (!InBounds(x, y)) return;

        _grid[x, y].TileType = type;
        _grid[x, y].RefreshWalkability();
        PaintTile(x, y, type);
    }

    public void SetTile(Vector2Int pos, TileType type) => SetTile(pos.x, pos.y, type);

    // ── Tile getters ──────────────────────────────────────────────────────────
    /// <summary>Returns the TileData at a grid position, or null if out of bounds.</summary>
    public TileData GetTile(int x, int y)
    {
        if (!InBounds(x, y)) return null;
        return _grid[x, y];
    }

    public TileData GetTile(Vector2Int pos) => GetTile(pos.x, pos.y);

    /// <summary>True if the tile exists, is walkable, and has no occupant.</summary>
    public bool IsWalkable(int x, int y)
    {
        TileData tile = GetTile(x, y);
        return tile != null && tile.IsWalkable;
    }

    public bool IsWalkable(Vector2Int pos) => IsWalkable(pos.x, pos.y);

    /// <summary>True if the tile is currently in the player's vision radius.</summary>
    public bool IsVisible(Vector2Int pos)
    {
        TileData tile = GetTile(pos);
        return tile != null && tile.IsVisible;
    }

    /// <summary>True if the player has ever seen this tile.</summary>
    public bool IsExplored(Vector2Int pos)
    {
        TileData tile = GetTile(pos);
        return tile != null && tile.IsExplored;
    }

    // ── Occupancy ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Places an entity on a tile and refreshes walkability.
    /// Pass null to clear a tile's occupant.
    /// </summary>
    public void SetOccupant(Vector2Int pos, GameObject occupant)
    {
        TileData tile = GetTile(pos);
        if (tile == null) return;

        tile.Occupant = occupant;
        tile.RefreshWalkability();
    }

    /// <summary>Moves an entity from one tile to another, updating occupancy on both.</summary>
    public void MoveOccupant(Vector2Int from, Vector2Int to)
    {
        TileData fromTile = GetTile(from);
        TileData toTile = GetTile(to);
        if (fromTile == null || toTile == null) return;

        toTile.Occupant = fromTile.Occupant;
        fromTile.Occupant = null;

        fromTile.RefreshWalkability();
        toTile.RefreshWalkability();
    }

    // ── Neighbours ────────────────────────────────────────────────────────────
    /// <summary>
    /// Returns the four cardinal neighbours of a tile (no diagonals).
    /// Only returns tiles that are in bounds. Does not filter by walkability —
    /// callers decide what to do with walls (e.g. LoS raycast vs. movement check).
    /// </summary>
    public List<TileData> GetCardinalNeighbours(Vector2Int pos)
    {
        var neighbours = new List<TileData>(4);

        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var dir in directions)
        {
            TileData tile = GetTile(pos + dir);
            if (tile != null) neighbours.Add(tile);
        }

        return neighbours;
    }

    /// <summary>
    /// Returns all 8 neighbours (cardinal + diagonal).
    /// Used by LoS and AoE spell calculations.
    /// </summary>
    public List<TileData> GetAllNeighbours(Vector2Int pos)
    {
        var neighbours = new List<TileData>(8);

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                TileData tile = GetTile(pos.x + dx, pos.y + dy);
                if (tile != null) neighbours.Add(tile);
            }

        return neighbours;
    }

    // ── Coordinate conversion ─────────────────────────────────────────────────
    /// <summary>
    /// Converts a grid coordinate to the exact world-space centre of that tile.
    /// Delegates to Unity's Grid component so the result always matches what
    /// the Tilemap renders, regardless of cell size or Grid offset settings.
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return grid.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
    }

    /// <summary>
    /// Converts a world position to the grid coordinate of the tile it falls on.
    /// Use this to map mouse clicks or world positions back to tile coordinates.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3Int cell = grid.WorldToCell(worldPos);
        return new Vector2Int(cell.x, cell.y);
    }

    // ── Fog of war ────────────────────────────────────────────────────────────
    /// <summary>
    /// Updates visibility for all tiles based on the player's grid position
    /// and vision radius. Uses a simple circle check for v1 — a proper
    /// shadowcasting LoS algorithm can replace this later without changing
    /// any of the callers.
    /// </summary>
    public void UpdateFogOfWar(Vector2Int playerPos, int visionRadius)
    {
        // First: mark all currently visible tiles as no longer visible
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                _grid[x, y].IsVisible = false;

        // Then: mark tiles within vision radius as visible (and explored)
        int radiusSq = visionRadius * visionRadius;

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                int dx = x - playerPos.x;
                int dy = y - playerPos.y;

                if (dx * dx + dy * dy <= radiusSq)
                {
                    _grid[x, y].IsVisible = true;
                    _grid[x, y].IsExplored = true;
                }
            }
    }

    // ── Bounds check ─────────────────────────────────────────────────────────
    public bool InBounds(int x, int y) =>
        x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;

    public bool InBounds(Vector2Int pos) => InBounds(pos.x, pos.y);

    // ── Visual painting ───────────────────────────────────────────────────────
    /// <summary>
    /// Paints the correct TileBase onto the correct Unity Tilemap layer.
    /// Floor tiles go on floorTilemap; walls and features on wallTilemap.
    /// </summary>
    private void PaintTile(int x, int y, TileType type)
    {
        // Tilemap uses its own coordinate system — we use the grid position directly
        var tilemapPos = new Vector3Int(x, y, 0);

        switch (type)
        {
            case TileType.Floor:
                floorTilemap?.SetTile(tilemapPos, floorTile);
                wallTilemap?.SetTile(tilemapPos, null);
                break;

            case TileType.Wall:
                wallTilemap?.SetTile(tilemapPos, wallTile);
                floorTilemap?.SetTile(tilemapPos, null);
                break;

            case TileType.DestructibleWall:
                wallTilemap?.SetTile(tilemapPos, destructibleWallTile);
                floorTilemap?.SetTile(tilemapPos, null);
                break;

            case TileType.StairsDown:
                floorTilemap?.SetTile(tilemapPos, stairsDownTile);
                break;

            case TileType.Door:
                wallTilemap?.SetTile(tilemapPos, doorTile);
                break;

            case TileType.Water:
                floorTilemap?.SetTile(tilemapPos, waterTile);
                break;

            case TileType.Void:
                floorTilemap?.SetTile(tilemapPos, null);
                wallTilemap?.SetTile(tilemapPos, null);
                break;
        }
    }

    // ── Test room ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Generates a simple walled room for testing before procedural gen exists.
    /// Creates a 20x20 grid with:
    ///   - Wall border
    ///   - Floor interior
    ///   - StairsDown at (10, 2)
    ///   - A door on the east wall at mid-height
    ///
    /// Delete or replace this once DungeonGenerator exists.
    /// </summary>
    public void GenerateTestRoom()
    {
        InitialiseGrid(gridWidth, gridHeight);

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                bool isBorder = x == 0 || x == gridWidth - 1
                             || y == 0 || y == gridHeight - 1;

                SetTile(x, y, isBorder ? TileType.Wall : TileType.Floor);
            }

        // Stairs in the south-centre
        SetTile(gridWidth / 2, 1, TileType.StairsDown);

        // Door on the east wall at mid-height
        SetTile(gridWidth - 1, gridHeight / 2, TileType.Door);

        Debug.Log("GridManager: Test room generated.");
    }
}