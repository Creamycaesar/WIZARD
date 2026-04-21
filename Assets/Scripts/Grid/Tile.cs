using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single cell in the grid. Plain C# class (not a MonoBehaviour).
/// Stores type, position, walkability, occupant, ground items, and fog of war state.
///
/// File: Assets/Scripts/Grid/Tile.cs
/// </summary>
public class Tile
{
    public Vector2Int GridPos;
    public TileType Type;
    public bool IsWalkable;
    public GameObject Occupant;             // Player or Enemy currently on this tile (null if empty)
    public List<ItemInstance> Items;         // Items lying on the ground here
    public VisibilityState Visibility;      // Fog of war state

    public Tile(Vector2Int pos, TileType type)
    {
        GridPos = pos;
        Type = type;
        IsWalkable = type != TileType.Wall;
        Occupant = null;
        Items = new List<ItemInstance>();
        Visibility = VisibilityState.Hidden;
    }
}