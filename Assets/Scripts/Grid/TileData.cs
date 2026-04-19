using UnityEngine;

/// <summary>
/// All possible tile types in WIZARD.
/// Drives walkability, rendering, and interaction logic.
/// </summary>
public enum TileType
{
    Floor,
    Wall,
    DestructibleWall,   // Can be blasted open by certain spells
    Door,               // Opened via Interact (E)
    StairsDown,         // Descend to next floor
    StairsUp,           // Ascend (unused in v1 - no going back)
    Water,              // Interacts with cold/lightning spells
    Grease,             // Interacts with fire spells
    Trap,               // Pressure plates, glyphs, pit traps
    Void                // Out-of-bounds / ungenerated
}

/// <summary>
/// Logical data for a single tile on the grid.
/// This is the game's source of truth — separate from any visual Tilemap.
/// All systems (combat, AI, fog of war, spells) query TileData, not the renderer.
/// </summary>
public class TileData
{
    // ── Position ──────────────────────────────────────────────────────────────
    public Vector2Int GridPosition { get; private set; }

    // ── Tile identity ─────────────────────────────────────────────────────────
    public TileType TileType { get; set; }

    // ── Walkability ───────────────────────────────────────────────────────────
    /// <summary>
    /// Cached walkability. Call RefreshWalkability() after changing TileType or occupant.
    /// </summary>
    public bool IsWalkable { get; private set; }

    // ── Fog of war ────────────────────────────────────────────────────────────
    /// <summary>True when this tile is inside the player's current vision radius.</summary>
    public bool IsVisible { get; set; }

    /// <summary>True once the player has seen this tile at least once.</summary>
    public bool IsExplored { get; set; }

    // ── Entity occupancy ──────────────────────────────────────────────────────
    /// <summary>
    /// The entity currently standing on this tile (player, enemy, NPC).
    /// Null if unoccupied. Used for collision and targeting.
    /// </summary>
    public GameObject Occupant { get; set; }

    /// <summary>True if an entity is standing on this tile.</summary>
    public bool IsOccupied => Occupant != null;

    // ── Constructor ───────────────────────────────────────────────────────────
    public TileData(Vector2Int gridPosition, TileType tileType = TileType.Void)
    {
        GridPosition = gridPosition;
        TileType = tileType;
        IsVisible = false;
        IsExplored = false;
        Occupant = null;
        RefreshWalkability();
    }

    // ── Walkability ───────────────────────────────────────────────────────────
    /// <summary>
    /// Recalculates IsWalkable based on TileType and occupancy.
    /// Call this whenever TileType changes or an entity moves on/off this tile.
    /// </summary>
    public void RefreshWalkability()
    {
        // Determine base walkability from tile type
        bool typeWalkable = TileType switch
        {
            TileType.Floor => true,
            TileType.Door => true,   // Doors are walkable (opening handled separately)
            TileType.StairsDown => true,
            TileType.StairsUp => true,
            TileType.Water => true,   // Walkable but applies effects
            TileType.Grease => true,   // Walkable but applies effects
            TileType.Trap => true,   // Walkable but triggers on entry
            TileType.Wall => false,
            TileType.DestructibleWall => false,  // Becomes Floor after destruction
            TileType.Void => false,
            _ => false
        };

        // A tile is walkable only if the type allows it AND no entity occupies it
        IsWalkable = typeWalkable && !IsOccupied;
    }

    // ── Line of sight helper ──────────────────────────────────────────────────
    /// <summary>
    /// True if this tile blocks line of sight (walls stop vision).
    /// Distinct from IsWalkable — a door is walkable but doesn't block LoS.
    /// </summary>
    public bool BlocksLineOfSight => TileType == TileType.Wall
                                  || TileType == TileType.DestructibleWall
                                  || TileType == TileType.Void;

    public override string ToString() =>
        $"Tile({GridPosition.x},{GridPosition.y}) [{TileType}] " +
        $"Walk:{IsWalkable} Vis:{IsVisible} Exp:{IsExplored}";
}