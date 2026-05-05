using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Metadata for a generated dungeon floor. Returned by DungeonGenerator.GenerateFloor().
/// Contains room positions, spawn points, and stair locations for use by other systems
/// (EnemySpawner, item placement, fog of war, minimap, etc.).
///
/// File: Assets/Scripts/Dungeon/FloorData.cs
/// </summary>
public class FloorData
{
    /// <summary>The DungeonConfig that was used to generate this floor.</summary>
    public DungeonConfig Config;

    /// <summary>The DungeonTheme selected for this floor (may be a random variant).</summary>
    public DungeonTheme Theme;

    /// <summary>Floor number (1-indexed). 0 = Tower.</summary>
    public int FloorNumber;

    /// <summary>Grid dimensions used for this floor.</summary>
    public int GridWidth;
    public int GridHeight;

    /// <summary>All rooms generated on this floor, including corridors metadata.</summary>
    public List<RoomData> Rooms;

    /// <summary>Tile position where the player spawns (stairs up / entrance).</summary>
    public Vector2Int PlayerSpawn;

    /// <summary>Tile position of the stairs leading down to the next floor.</summary>
    public Vector2Int StairsDown;

    /// <summary>Tile position of the stairs leading up (where the player entered).</summary>
    public Vector2Int StairsUp;

    /// <summary>True if this is the final floor (boss floor).</summary>
    public bool IsBossFloor;

    /// <summary>All door positions on this floor.</summary>
    public List<Vector2Int> DoorPositions;

    /// <summary>
    /// Returns a random floor tile position within a random room.
    /// Useful for placing items, enemies, or other features.
    /// Excludes the player spawn and stairs tiles.
    /// </summary>
    public Vector2Int GetRandomRoomTile()
    {
        if (Rooms == null || Rooms.Count == 0) return PlayerSpawn;

        // Try up to 50 times to find a valid position
        for (int i = 0; i < 50; i++)
        {
            RoomData room = Rooms[Random.Range(0, Rooms.Count)];
            Vector2Int pos = room.GetRandomTile();
            if (pos != PlayerSpawn && pos != StairsDown && pos != StairsUp)
                return pos;
        }

        // Fallback: just return center of a random room
        return Rooms[Random.Range(0, Rooms.Count)].Center;
    }
}

/// <summary>
/// Metadata for a single room on a generated floor.
/// Stores position, size, and center for use by placement systems.
///
/// File: Assets/Scripts/Dungeon/FloorData.cs (same file as FloorData)
/// </summary>
public class RoomData
{
    /// <summary>Bottom-left corner of the room in grid coordinates.</summary>
    public Vector2Int Position;

    /// <summary>Room dimensions in tiles.</summary>
    public Vector2Int Size;

    /// <summary>Center tile of the room (used for corridor connections and as a reference point).</summary>
    public Vector2Int Center;

    /// <summary>Index of this room in the floor's room list. Used for spawn ordering.</summary>
    public int RoomIndex;

    public RoomData(Vector2Int position, Vector2Int size, int index)
    {
        Position = position;
        Size = size;
        Center = new Vector2Int(position.x + size.x / 2, position.y + size.y / 2);
        RoomIndex = index;
    }

    /// <summary>Returns a random tile position inside this room (floor tiles only, not walls).</summary>
    public Vector2Int GetRandomTile()
    {
        return new Vector2Int(
            Random.Range(Position.x, Position.x + Size.x),
            Random.Range(Position.y, Position.y + Size.y)
        );
    }

    /// <summary>True if the given position is inside this room's floor area.</summary>
    public bool Contains(Vector2Int pos)
    {
        return pos.x >= Position.x && pos.x < Position.x + Size.x
            && pos.y >= Position.y && pos.y < Position.y + Size.y;
    }
}