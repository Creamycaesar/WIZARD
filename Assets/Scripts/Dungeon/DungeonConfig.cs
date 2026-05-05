using UnityEngine;

/// <summary>
/// Gameplay configuration for a dungeon. Controls structure (floors, room sizes),
/// enemy roster, loot tables, trap frequency, level range, and generation algorithm.
/// References a DungeonTheme for all visual/rendering decisions.
///
/// One asset per dungeon (e.g. "The Sunken Crypts", "Goblin Warrens").
/// DungeonGenerator reads this to produce each floor.
///
/// File: Assets/Scripts/Dungeon/DungeonConfig.cs
/// Asset location: Assets/Data/Dungeons/
/// Layer: 0 (Pure data — no dependencies)
///
/// NOTE: This replaces the original DungeonConfig spec from the Tech Arch doc.
/// Visual fields (defaultFloorType, defaultWallType) have been moved to DungeonTheme.
/// </summary>
[CreateAssetMenu(menuName = "WIZARD/Dungeon Config")]
public class DungeonConfig : ScriptableObject
{
    // ── Identity ─────────────────────────────────────────────────────────

    [Header("Identity")]

    [Tooltip("Display name shown in the Tower's dungeon selection UI")]
    public string dungeonName;

    [Tooltip("Short flavor text describing the dungeon")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("Unique ID for save data and unlock tracking")]
    public string dungeonID;

    // ── Theme (Visuals) ──────────────────────────────────────────────────

    [Header("Theme")]

    [Tooltip("Visual theme for this dungeon. Controls tilesets, colors, and dressing. " +
             "Multiple dungeons can share a theme; one dungeon can have theme variants.")]
    public DungeonTheme theme;

    [Tooltip("Optional: additional theme variants that can be randomly selected per run " +
             "for visual variety (e.g. Blue vs Red color variants of the same dungeon). " +
             "If empty, only the primary theme is used.")]
    public DungeonTheme[] themeVariants;

    // ── Level Range ──────────────────────────────────────────────────────

    [Header("Level Range")]

    [Tooltip("Minimum recommended player level. Used for dungeon selection UI and gating.")]
    public int minPlayerLevel = 1;

    [Tooltip("Maximum recommended player level. Enemies and loot scale within this range.")]
    public int maxPlayerLevel = 3;

    // ── Structure ────────────────────────────────────────────────────────

    [Header("Structure")]

    [Tooltip("Number of floors in this dungeon (not counting the boss floor)")]
    public int totalFloors = 5;

    [Tooltip("Which generation algorithm to use for floor layouts")]
    public GenerationAlgorithm algorithm = GenerationAlgorithm.BSP;

    [Tooltip("Max grid width in tiles for generated floors")]
    public int gridWidth = 50;

    [Tooltip("Max grid height in tiles for generated floors")]
    public int gridHeight = 50;

    [Tooltip("Minimum room dimension (width or height) in tiles")]
    public int minRoomSize = 4;

    [Tooltip("Maximum room dimension (width or height) in tiles")]
    public int maxRoomSize = 10;

    [Tooltip("Minimum number of rooms per floor")]
    public int minRooms = 4;

    [Tooltip("Maximum number of rooms per floor")]
    public int maxRooms = 8;

    [Tooltip("Corridor width in tiles. 1 = classic tight corridors, 2 = wider feel.")]
    public int corridorWidth = 1;

    // ── Enemies ──────────────────────────────────────────────────────────

    [Header("Enemies")]

    [Tooltip("Enemy types that can spawn in this dungeon. DungeonGenerator picks from this roster. " +
             "Enemies are filtered by CR relative to the current floor and player level.")]
    public EnemyData[] enemyRoster;

    [Tooltip("Minimum enemies spawned per floor")]
    public int minEnemiesPerFloor = 3;

    [Tooltip("Maximum enemies spawned per floor")]
    public int maxEnemiesPerFloor = 8;

    [Tooltip("If true, enemy count and difficulty scale up on deeper floors")]
    public bool scaleWithFloor = true;

    // ── Bosses ───────────────────────────────────────────────────────────

    [Header("Bosses")]

    [Tooltip("Possible bosses for the final floor. One is chosen randomly per run. " +
             "GDD specifies 2 rotating bosses per dungeon for replayability.")]
    public EnemyData[] bosses;

    // ── Loot ─────────────────────────────────────────────────────────────

    [Header("Loot")]

    [Tooltip("Item pool for floor loot drops, chest contents, and enemy drops in this dungeon")]
    public LootEntry[] lootTable;

    [Tooltip("Minimum item spawns per floor (ground pickups, not enemy drops)")]
    public int minItemsPerFloor = 1;

    [Tooltip("Maximum item spawns per floor")]
    public int maxItemsPerFloor = 4;

    [Tooltip("Chance (0–1) that a room contains a treasure chest")]
    [Range(0f, 1f)]
    public float chestChance = 0.15f;

    // ── Traps ────────────────────────────────────────────────────────────

    [Header("Traps")]

    [Tooltip("Trap types that can appear in this dungeon")]
    public TrapConfig[] trapTypes;

    [Tooltip("Minimum traps per floor (0 = traps possible but not guaranteed)")]
    public int minTrapsPerFloor = 0;

    [Tooltip("Maximum traps per floor")]
    public int maxTrapsPerFloor = 3;

    // ── Special Features ─────────────────────────────────────────────────

    [Header("Special Features")]

    [Tooltip("Chance (0–1) that a floor contains a campfire room (short rest opportunity)")]
    [Range(0f, 1f)]
    public float campfireChance = 0.2f;

    [Tooltip("Chance (0–1) that a floor contains a water feature (pools, rivers)")]
    [Range(0f, 1f)]
    public float waterChance = 0.3f;

    [Tooltip("If true, rooms can contain dungeon dressing objects from the theme")]
    public bool spawnDressing = true;

    [Tooltip("Average number of dressing objects per room (actual count is randomized around this)")]
    public int dressingDensity = 3;

    // ── Difficulty Scaling ───────────────────────────────────────────────

    [Header("Difficulty Scaling")]

    [Tooltip("Multiplier applied to enemy count on the deepest floors. " +
             "1.0 = no scaling, 1.5 = 50% more enemies on the last floor vs the first.")]
    public float enemyScaleMultiplier = 1.3f;

    [Tooltip("Multiplier applied to trap count on deeper floors")]
    public float trapScaleMultiplier = 1.2f;
}

// ═══════════════════════════════════════════════════════════════════════════
//  GenerationAlgorithm — which procedural generation method to use.
//  BSP is the default classic roguelike algorithm.
//  Others can be added as needed for different dungeon feels.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Procedural generation algorithm for dungeon floor layouts.
/// DungeonGenerator switches behavior based on this value.
/// </summary>
public enum GenerationAlgorithm
{
    /// <summary>Binary Space Partition — classic rooms and corridors. 
    /// Subdivides the grid recursively, places rooms in leaf nodes, 
    /// connects siblings with corridors.</summary>
    BSP,

    /// <summary>Cellular Automata — organic cave-like layouts. 
    /// Starts with random noise, iterates smoothing rules to form 
    /// natural-looking cavern shapes.</summary>
    CellularAutomata,

    /// <summary>Drunkard's Walk — carves winding, organic tunnels. 
    /// A random walker carves floor tiles as it moves, creating 
    /// irregular connected spaces.</summary>
    DrunkardWalk
}

// ═══════════════════════════════════════════════════════════════════════════
//  LootEntry — weighted item entry for dungeon loot tables.
//  DungeonGenerator and chest logic use these to determine drops.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// A weighted entry in a dungeon's loot table.
/// Higher weight = more likely to be selected when generating loot.
/// </summary>
[System.Serializable]
public struct LootEntry
{
    [Tooltip("The item that can drop")]
    public ItemData item;

    [Tooltip("Relative drop weight. Higher = more common. " +
             "A health potion might be 10, a rare scroll might be 1.")]
    public int weight;

    [Tooltip("Minimum floor this item can appear on (1-indexed). " +
             "0 = can appear on any floor.")]
    public int minFloor;

    [Tooltip("Maximum quantity that can drop at once (for stackable items like gold/reagents)")]
    public int maxQuantity;
}

// ═══════════════════════════════════════════════════════════════════════════
//  TrapConfig — definition of a trap type that can appear in a dungeon.
//  DungeonGenerator places these during the trap placement pass.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Configuration for a trap type in a dungeon.
/// Traps are hidden tile hazards that trigger when stepped on.
/// </summary>
[System.Serializable]
public struct TrapConfig
{
    [Tooltip("Display name for the action log (e.g. 'Spike Trap', 'Poison Dart Trap')")]
    public string trapName;

    [Tooltip("Damage dealt when triggered")]
    public int damage;

    [Tooltip("Damage type (for resistance/immunity checks)")]
    public DamageType damageType;

    [Tooltip("DC for the saving throw to avoid or reduce damage")]
    public int saveDC;

    [Tooltip("Which ability score the player uses to save")]
    public AbilityScore saveAbility;

    [Tooltip("Optional: condition applied on failed save (e.g. Poisoned). " +
             "Set to a default/None value if no condition is applied.")]
    public Condition appliedCondition;

    [Tooltip("Duration in turns for the applied condition (0 = no condition)")]
    public int conditionDuration;

    [Tooltip("DC for the Perception/Investigation check to detect the trap before triggering")]
    public int detectionDC;

    [Tooltip("DC for the Thieves' Tools or Arcana check to disarm the trap")]
    public int disarmDC;

    [Tooltip("Relative spawn weight vs other trap types in this dungeon")]
    public int spawnWeight;
}