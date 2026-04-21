using UnityEngine;

/// <summary>
/// Configuration for a dungeon. One asset per dungeon type.
/// Consumed by DungeonGenerator to produce floors.
///
/// File: Assets/Scripts/Dungeon/DungeonConfig.cs
/// Asset location: Assets/Data/Dungeons/
/// </summary>
[CreateAssetMenu(menuName = "WIZARD/Dungeon Config")]
public class DungeonConfig : ScriptableObject
{
    public string dungeonName;
    public int totalFloors;
    public int minRoomSize, maxRoomSize;
    public int minRooms, maxRooms;

    [Tooltip("1 for classic narrow corridors, 2 for wider")]
    public int corridorWidth;

    [Header("Enemies")]
    public EnemyData[] enemyRoster;                 // Which enemies can appear
    public int minEnemiesPerFloor, maxEnemiesPerFloor;

    [Header("Loot")]
    public ItemData[] lootPool;                     // Possible item drops

    [Header("Bosses")]
    [Tooltip("2 possible bosses — one chosen per run")]
    public EnemyData[] bosses;

    [Header("Grid")]
    public int gridWidth, gridHeight;               // Max grid dimensions
    public TileType defaultFloorType;
    public TileType defaultWallType;
}