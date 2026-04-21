using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the 2D tile grid. Single source of truth for every grid position:
/// tile type, occupant, walkability, world-position conversion.
/// Same instance used for Tower and Dungeon — InitializeGrid() resets with new data.
///
/// File: Assets/Scripts/Grid/GridManager.cs
/// Layer: 0 (Foundation — no manager dependencies)
/// </summary>
public class GridManager : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════
    //  Singleton
    // ═════════════════════════════════════════════════════════════════════

    public static GridManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Constants
    // ═════════════════════════════════════════════════════════════════════

    public static readonly float TILE_SIZE = 1f;

    // ═════════════════════════════════════════════════════════════════════
    //  Events
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Fired when any occupant moves or is placed on a tile.</summary>
    public static event Action<Vector2Int, Vector2Int, GameObject> OnOccupantMoved;

    // ═════════════════════════════════════════════════════════════════════
    //  Grid Data
    // ═════════════════════════════════════════════════════════════════════

    private Tile[,] grid;

    /// <summary>Grid width for the current level.</summary>
    public int Width { get; private set; }

    /// <summary>Grid height for the current level.</summary>
    public int Height { get; private set; }

    // ═════════════════════════════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes the grid with a pre-built 2D Tile array.
    /// Called by DungeonGenerator or TowerManager when a level loads.
    /// </summary>
    public void InitializeGrid(Tile[,] tiles)
    {
        grid = tiles;
        Width = tiles.GetLength(0);
        Height = tiles.GetLength(1);
    }

    /// <summary>Returns the Tile at the given grid position, or null if out of bounds.</summary>
    public Tile GetTile(Vector2Int pos)
    {
        if (!IsInBounds(pos)) return null;
        return grid[pos.x, pos.y];
    }

    /// <summary>Overload for convenience.</summary>
    public Tile GetTile(int x, int y)
    {
        return GetTile(new Vector2Int(x, y));
    }

    /// <summary>True if the tile exists, is walkable, and has no occupant.</summary>
    public bool IsWalkable(Vector2Int pos)
    {
        Tile tile = GetTile(pos);
        return tile != null && tile.IsWalkable && tile.Occupant == null;
    }

    /// <summary>True if position is within grid dimensions.</summary>
    public bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
    }

    /// <summary>
    /// Places an occupant on a tile. Clears occupant from their previous tile.
    /// </summary>
    public void SetOccupant(Vector2Int pos, GameObject occupant)
    {
        // Find and clear the occupant's previous tile
        Vector2Int previousPos = pos;
        if (grid != null)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (grid[x, y].Occupant == occupant)
                    {
                        previousPos = new Vector2Int(x, y);
                        grid[x, y].Occupant = null;
                        break;
                    }
                }
            }
        }

        Tile tile = GetTile(pos);
        if (tile != null)
        {
            tile.Occupant = occupant;
            OnOccupantMoved?.Invoke(previousPos, pos, occupant);
        }
    }

    /// <summary>Removes the occupant from a tile (e.g. on death).</summary>
    public void ClearOccupant(Vector2Int pos)
    {
        Tile tile = GetTile(pos);
        if (tile != null)
            tile.Occupant = null;
    }

    /// <summary>Returns the occupant at a position, or null.</summary>
    public GameObject GetOccupant(Vector2Int pos)
    {
        Tile tile = GetTile(pos);
        return tile?.Occupant;
    }

    /// <summary>Converts a grid coordinate to a Unity world position (center of tile).</summary>
    public Vector3 GridToWorld(Vector2Int pos)
    {
        return new Vector3(pos.x * TILE_SIZE, pos.y * TILE_SIZE, 0f);
    }

    /// <summary>Converts a world position to the nearest grid coordinate.</summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / TILE_SIZE),
            Mathf.RoundToInt(worldPos.y / TILE_SIZE)
        );
    }

    /// <summary>Returns walkable adjacent tiles (4-directional, optionally 8-directional).</summary>
    public List<Tile> GetNeighbors(Vector2Int pos, bool includeDiagonals = false)
    {
        var neighbors = new List<Tile>();
        Vector2Int[] cardinals = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };
        Vector2Int[] diagonals = {
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        foreach (var dir in cardinals)
        {
            Tile tile = GetTile(pos + dir);
            if (tile != null && tile.IsWalkable)
                neighbors.Add(tile);
        }

        if (includeDiagonals)
        {
            foreach (var dir in diagonals)
            {
                Tile tile = GetTile(pos + dir);
                if (tile != null && tile.IsWalkable)
                    neighbors.Add(tile);
            }
        }

        return neighbors;
    }
}