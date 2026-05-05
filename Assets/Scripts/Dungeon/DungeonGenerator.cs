using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally generates dungeon floors using Binary Space Partition (BSP).
/// Takes a DungeonConfig as input, produces a Tile[,] array, and feeds it
/// to GridManager.InitializeGrid(). Returns FloorData with room metadata
/// for use by other systems (EnemySpawner, item placement, etc.).
///
/// File: Assets/Scripts/Dungeon/DungeonGenerator.cs
/// Layer: 4 (Depends on GridManager, DungeonConfig)
///
/// Step 8 implementation — generates layout, player spawn, and stairs.
/// Enemy, item, trap, and dressing placement are stubbed for later steps.
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════
    //  Singleton
    // ═════════════════════════════════════════════════════════════════════

    public static DungeonGenerator Instance { get; private set; }

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
    //  Configuration
    // ═════════════════════════════════════════════════════════════════════

    [Header("Debug")]

    [Tooltip("If set, uses this seed for generation. 0 = random seed each time.")]
    public int debugSeed = 0;

    [Tooltip("Log generation details to the console")]
    public bool debugLogging = false;

    // ═════════════════════════════════════════════════════════════════════
    //  Generation State
    // ═════════════════════════════════════════════════════════════════════

    private Tile[,] tiles;
    private int gridWidth;
    private int gridHeight;
    private List<RoomData> rooms;
    private List<Vector2Int> doorPositions;
    private FloorData currentFloorData;

    /// <summary>The most recently generated floor's metadata.</summary>
    public FloorData CurrentFloor => currentFloorData;

    // ═════════════════════════════════════════════════════════════════════
    //  Events
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Fired after a floor is fully generated and GridManager is initialized.</summary>
    public static event System.Action<FloorData> OnFloorGenerated;

    // ═════════════════════════════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates a complete dungeon floor. Creates the tile grid, carves rooms
    /// and corridors, places stairs, and initializes GridManager.
    /// </summary>
    /// <param name="config">The dungeon configuration to generate from.</param>
    /// <param name="floorNumber">Current floor (1-indexed).</param>
    /// <returns>FloorData with room positions, spawn point, and stair locations.</returns>
    public FloorData GenerateFloor(DungeonConfig config, int floorNumber)
    {
        // Seed the random number generator for reproducibility
        int seed = debugSeed != 0 ? debugSeed : System.Environment.TickCount;
        Random.InitState(seed);
        if (debugLogging) Debug.Log($"[DungeonGenerator] Generating floor {floorNumber} with seed {seed}");

        // Select theme (primary or random variant)
        DungeonTheme theme = SelectTheme(config);

        // Initialize grid dimensions and tile array
        gridWidth = config.gridWidth;
        gridHeight = config.gridHeight;
        tiles = new Tile[gridWidth, gridHeight];
        rooms = new List<RoomData>();
        doorPositions = new List<Vector2Int>();

        // Fill entire grid with walls
        FillWithWalls();

        // Generate layout based on the configured algorithm
        switch (config.algorithm)
        {
            case GenerationAlgorithm.BSP:
                GenerateBSP(config);
                break;
            case GenerationAlgorithm.CellularAutomata:
                GenerateCellularAutomata(config);
                break;
            case GenerationAlgorithm.DrunkardWalk:
                GenerateDrunkardWalk(config);
                break;
            default:
                GenerateBSP(config);
                break;
        }

        // Ensure we have enough rooms (regenerate if BSP produced too few)
        int attempts = 0;
        while (rooms.Count < config.minRooms && attempts < 10)
        {
            if (debugLogging) Debug.Log($"[DungeonGenerator] Only {rooms.Count} rooms, need {config.minRooms}. Regenerating...");
            FillWithWalls();
            rooms.Clear();
            doorPositions.Clear();
            Random.InitState(seed + attempts + 1);
            GenerateBSP(config);
            attempts++;
        }

        // Place stairs
        Vector2Int stairsUp = PlaceStairsUp();
        Vector2Int stairsDown = PlaceStairsDown(stairsUp, config, floorNumber);

        // Build FloorData
        currentFloorData = new FloorData
        {
            Config = config,
            Theme = theme,
            FloorNumber = floorNumber,
            GridWidth = gridWidth,
            GridHeight = gridHeight,
            Rooms = new List<RoomData>(rooms),
            PlayerSpawn = stairsUp,
            StairsUp = stairsUp,
            StairsDown = stairsDown,
            IsBossFloor = floorNumber >= config.totalFloors,
            DoorPositions = new List<Vector2Int>(doorPositions)
        };

        // ── Future placement passes (stubs for later steps) ──────────
        // Step 12: PlaceEnemies(config, floorNumber);
        // Step 16: PlaceItems(config, floorNumber);
        // Step 16: PlaceDressing(config, theme);
        // Step 33: PlaceTraps(config, floorNumber);
        // Step ??: PlaceWater(config, theme, floorNumber);
        // Step ??: PlaceCampfire(config, floorNumber);

        // Initialize GridManager with the generated tiles
        GridManager.Instance.InitializeGrid(tiles);

        if (debugLogging)
        {
            Debug.Log($"[DungeonGenerator] Floor {floorNumber} complete: " +
                      $"{rooms.Count} rooms, {doorPositions.Count} doors, " +
                      $"spawn at {stairsUp}, stairs down at {stairsDown}");
        }

        // Notify listeners
        OnFloorGenerated?.Invoke(currentFloorData);

        return currentFloorData;
    }

    /// <summary>
    /// Special generation for the final floor — a boss arena.
    /// Creates a large central room with the boss spawn point.
    /// </summary>
    public FloorData GenerateBossFloor(DungeonConfig config)
    {
        int seed = debugSeed != 0 ? debugSeed : System.Environment.TickCount;
        Random.InitState(seed);

        DungeonTheme theme = SelectTheme(config);
        gridWidth = config.gridWidth;
        gridHeight = config.gridHeight;
        tiles = new Tile[gridWidth, gridHeight];
        rooms = new List<RoomData>();
        doorPositions = new List<Vector2Int>();

        FillWithWalls();

        // Create a large arena in the center
        int arenaWidth = Mathf.Min(config.maxRoomSize * 2, gridWidth - 4);
        int arenaHeight = Mathf.Min(config.maxRoomSize * 2, gridHeight - 4);
        int arenaX = (gridWidth - arenaWidth) / 2;
        int arenaY = (gridHeight - arenaHeight) / 2;
        CarveRoom(arenaX, arenaY, arenaWidth, arenaHeight, 0);

        // Optionally add 1-2 smaller side rooms connected to the arena
        if (gridWidth > arenaWidth + config.minRoomSize + 6)
        {
            // Left antechamber
            int sideW = Random.Range(config.minRoomSize, config.maxRoomSize);
            int sideH = Random.Range(config.minRoomSize, config.maxRoomSize);
            int sideX = arenaX - sideW - 2;
            int sideY = arenaY + (arenaHeight - sideH) / 2;
            if (sideX >= 1 && sideY >= 1)
            {
                CarveRoom(sideX, sideY, sideW, sideH, 1);
                CarveCorridor(rooms[1].Center, rooms[0].Center, config.corridorWidth);
            }
        }

        // Stairs up in the first side room (or bottom of arena if no side room)
        Vector2Int stairsUp;
        if (rooms.Count > 1)
            stairsUp = rooms[1].Center;
        else
            stairsUp = new Vector2Int(arenaX + 1, arenaY + 1);

        SetTileType(stairsUp, TileType.Stairs);

        currentFloorData = new FloorData
        {
            Config = config,
            Theme = theme,
            FloorNumber = config.totalFloors,
            GridWidth = gridWidth,
            GridHeight = gridHeight,
            Rooms = new List<RoomData>(rooms),
            PlayerSpawn = stairsUp,
            StairsUp = stairsUp,
            StairsDown = stairsUp, // No stairs down on boss floor
            IsBossFloor = true,
            DoorPositions = new List<Vector2Int>(doorPositions)
        };

        GridManager.Instance.InitializeGrid(tiles);
        OnFloorGenerated?.Invoke(currentFloorData);

        return currentFloorData;
    }

    /// <summary>Returns the player spawn point for the most recently generated floor.</summary>
    public Vector2Int GetPlayerSpawnPoint()
    {
        return currentFloorData?.PlayerSpawn ?? Vector2Int.zero;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BSP Generation
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Binary Space Partition generation. Recursively subdivides the grid,
    /// places rooms in leaf nodes, and connects siblings with corridors.
    /// </summary>
    private void GenerateBSP(DungeonConfig config)
    {
        // Create root node covering the usable grid area (1-tile border of walls)
        BSPNode root = new BSPNode(1, 1, gridWidth - 2, gridHeight - 2);

        // Recursively split
        SplitNode(root, config);

        // Place rooms in leaf nodes
        int roomIndex = 0;
        PlaceRoomsInLeaves(root, config, ref roomIndex);

        // Connect sibling rooms with corridors
        ConnectTree(root, config);

        // Place doors at corridor-room junctions
        PlaceDoors();

        if (debugLogging) Debug.Log($"[DungeonGenerator] BSP: {rooms.Count} rooms generated");
    }

    /// <summary>
    /// Recursively splits a BSP node into two children.
    /// Stops when the node is too small to split further or we have enough potential rooms.
    /// </summary>
    private void SplitNode(BSPNode node, DungeonConfig config)
    {
        // Don't split if the node is too small to contain two rooms
        int minNodeSize = config.minRoomSize + 3; // Room + padding

        if (node.Width < minNodeSize * 2 && node.Height < minNodeSize * 2)
            return;

        // Stop splitting randomly once nodes are moderately sized
        // This creates variation in room sizes
        if (node.Width < config.maxRoomSize * 2 && node.Height < config.maxRoomSize * 2)
        {
            if (Random.value < 0.25f)
                return;
        }

        // Decide split direction
        bool splitHorizontal;
        if (node.Width > node.Height * 1.25f)
            splitHorizontal = false; // Too wide, split vertically
        else if (node.Height > node.Width * 1.25f)
            splitHorizontal = true;  // Too tall, split horizontally
        else
            splitHorizontal = Random.value < 0.5f;

        if (splitHorizontal)
        {
            // Can we split horizontally?
            if (node.Height < minNodeSize * 2) return;

            // Pick a split point with some randomness (40%-60% of height)
            int splitMin = node.Y + minNodeSize;
            int splitMax = node.Y + node.Height - minNodeSize;
            if (splitMin >= splitMax) return;

            int splitY = Random.Range(splitMin, splitMax);

            node.Left = new BSPNode(node.X, node.Y, node.Width, splitY - node.Y);
            node.Right = new BSPNode(node.X, splitY, node.Width, node.Y + node.Height - splitY);
        }
        else
        {
            // Vertical split
            if (node.Width < minNodeSize * 2) return;

            int splitMin = node.X + minNodeSize;
            int splitMax = node.X + node.Width - minNodeSize;
            if (splitMin >= splitMax) return;

            int splitX = Random.Range(splitMin, splitMax);

            node.Left = new BSPNode(node.X, node.Y, splitX - node.X, node.Height);
            node.Right = new BSPNode(splitX, node.Y, node.X + node.Width - splitX, node.Height);
        }

        // Recurse into children
        SplitNode(node.Left, config);
        SplitNode(node.Right, config);
    }

    /// <summary>
    /// Places a room inside each leaf node of the BSP tree.
    /// Room size is randomized within config bounds but must fit inside the leaf.
    /// </summary>
    private void PlaceRoomsInLeaves(BSPNode node, DungeonConfig config, ref int roomIndex)
    {
        // If this node has children, recurse
        if (node.Left != null || node.Right != null)
        {
            if (node.Left != null) PlaceRoomsInLeaves(node.Left, config, ref roomIndex);
            if (node.Right != null) PlaceRoomsInLeaves(node.Right, config, ref roomIndex);
            return;
        }

        // Leaf node — place a room
        int maxW = Mathf.Min(config.maxRoomSize, node.Width - 2);
        int maxH = Mathf.Min(config.maxRoomSize, node.Height - 2);
        int minW = Mathf.Min(config.minRoomSize, maxW);
        int minH = Mathf.Min(config.minRoomSize, maxH);

        if (maxW < minW || maxH < minH) return; // Node too small

        int roomW = Random.Range(minW, maxW + 1);
        int roomH = Random.Range(minH, maxH + 1);

        // Position the room randomly within the leaf (with at least 1-tile padding from edges)
        int roomX = Random.Range(node.X + 1, node.X + node.Width - roomW);
        int roomY = Random.Range(node.Y + 1, node.Y + node.Height - roomH);

        // Carve the room
        CarveRoom(roomX, roomY, roomW, roomH, roomIndex);
        node.Room = rooms[rooms.Count - 1];
        roomIndex++;
    }

    /// <summary>
    /// Connects rooms across the BSP tree by linking sibling nodes with corridors.
    /// For each internal node, picks a room from the left subtree and one from
    /// the right subtree and carves a corridor between their centers.
    /// </summary>
    private void ConnectTree(BSPNode node, DungeonConfig config)
    {
        if (node.Left == null || node.Right == null) return;

        // Recurse first so all subtree rooms are connected internally
        ConnectTree(node.Left, config);
        ConnectTree(node.Right, config);

        // Get a room from each subtree to connect
        RoomData leftRoom = GetRoomFromNode(node.Left);
        RoomData rightRoom = GetRoomFromNode(node.Right);

        if (leftRoom != null && rightRoom != null)
        {
            CarveCorridor(leftRoom.Center, rightRoom.Center, config.corridorWidth);
        }
    }

    /// <summary>
    /// Returns a room from a BSP node — either its own room (if leaf) or
    /// a random room from its subtree (if internal node).
    /// </summary>
    private RoomData GetRoomFromNode(BSPNode node)
    {
        if (node.Room != null) return node.Room;

        // Pick from subtree
        RoomData leftRoom = node.Left != null ? GetRoomFromNode(node.Left) : null;
        RoomData rightRoom = node.Right != null ? GetRoomFromNode(node.Right) : null;

        if (leftRoom == null) return rightRoom;
        if (rightRoom == null) return leftRoom;
        return Random.value < 0.5f ? leftRoom : rightRoom;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Cellular Automata Generation (stub — future implementation)
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cellular Automata generation for organic cave-like layouts.
    /// TODO: Implement in a future step when the Cave dungeon is built.
    /// Currently falls back to BSP.
    /// </summary>
    private void GenerateCellularAutomata(DungeonConfig config)
    {
        Debug.LogWarning("[DungeonGenerator] CellularAutomata not yet implemented. Falling back to BSP.");
        GenerateBSP(config);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Drunkard's Walk Generation (stub — future implementation)
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Drunkard's Walk generation for winding tunnel layouts.
    /// TODO: Implement in a future step.
    /// Currently falls back to BSP.
    /// </summary>
    private void GenerateDrunkardWalk(DungeonConfig config)
    {
        Debug.LogWarning("[DungeonGenerator] DrunkardWalk not yet implemented. Falling back to BSP.");
        GenerateBSP(config);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Room & Corridor Carving
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Fills the entire grid with Wall tiles.</summary>
    private void FillWithWalls()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                tiles[x, y] = new Tile(new Vector2Int(x, y), TileType.Wall);
            }
        }
    }

    /// <summary>
    /// Carves a rectangular room into the grid by setting tiles to Floor.
    /// Adds the room to the rooms list.
    /// </summary>
    private void CarveRoom(int x, int y, int width, int height, int roomIndex)
    {
        // Clamp to grid bounds (leave a 1-tile wall border around the entire grid)
        int startX = Mathf.Max(x, 1);
        int startY = Mathf.Max(y, 1);
        int endX = Mathf.Min(x + width, gridWidth - 1);
        int endY = Mathf.Min(y + height, gridHeight - 1);

        for (int tx = startX; tx < endX; tx++)
        {
            for (int ty = startY; ty < endY; ty++)
            {
                SetTileType(new Vector2Int(tx, ty), TileType.Floor);
            }
        }

        RoomData room = new RoomData(
            new Vector2Int(startX, startY),
            new Vector2Int(endX - startX, endY - startY),
            roomIndex
        );
        rooms.Add(room);
    }

    /// <summary>
    /// Carves an L-shaped corridor between two points.
    /// Randomly chooses horizontal-first or vertical-first for variety.
    /// </summary>
    private void CarveCorridor(Vector2Int from, Vector2Int to, int width)
    {
        // Randomly choose whether to go horizontal-first or vertical-first
        if (Random.value < 0.5f)
        {
            // Horizontal then vertical
            CarveHorizontalTunnel(from.x, to.x, from.y, width);
            CarveVerticalTunnel(from.y, to.y, to.x, width);
        }
        else
        {
            // Vertical then horizontal
            CarveVerticalTunnel(from.y, to.y, from.x, width);
            CarveHorizontalTunnel(from.x, to.x, to.y, width);
        }
    }

    /// <summary>Carves a horizontal tunnel between two X coordinates at a given Y.</summary>
    private void CarveHorizontalTunnel(int x1, int x2, int y, int width)
    {
        int minX = Mathf.Min(x1, x2);
        int maxX = Mathf.Max(x1, x2);
        int halfWidth = width / 2;

        for (int x = minX; x <= maxX; x++)
        {
            for (int w = -halfWidth; w <= halfWidth; w++)
            {
                int ty = y + w;
                if (ty >= 1 && ty < gridHeight - 1 && x >= 1 && x < gridWidth - 1)
                {
                    SetTileType(new Vector2Int(x, ty), TileType.Floor);
                }
            }
        }
    }

    /// <summary>Carves a vertical tunnel between two Y coordinates at a given X.</summary>
    private void CarveVerticalTunnel(int y1, int y2, int x, int width)
    {
        int minY = Mathf.Min(y1, y2);
        int maxY = Mathf.Max(y1, y2);
        int halfWidth = width / 2;

        for (int y = minY; y <= maxY; y++)
        {
            for (int w = -halfWidth; w <= halfWidth; w++)
            {
                int tx = x + w;
                if (tx >= 1 && tx < gridWidth - 1 && y >= 1 && y < gridHeight - 1)
                {
                    SetTileType(new Vector2Int(tx, y), TileType.Floor);
                }
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Door Placement
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Scans the grid for corridor-room junctions and places doors.
    /// A valid door position is a floor tile that has walls on two opposite
    /// sides (forming a chokepoint) — the classic doorway pattern.
    /// </summary>
    private void PlaceDoors()
    {
        for (int x = 2; x < gridWidth - 2; x++)
        {
            for (int y = 2; y < gridHeight - 2; y++)
            {
                if (tiles[x, y].Type != TileType.Floor) continue;

                // Check for horizontal chokepoint: walls above and below, floor left and right
                bool horizontalChoke = IsWall(x, y + 1) && IsWall(x, y - 1)
                                    && IsFloorLike(x - 1, y) && IsFloorLike(x + 1, y);

                // Check for vertical chokepoint: walls left and right, floor above and below
                bool verticalChoke = IsWall(x - 1, y) && IsWall(x + 1, y)
                                  && IsFloorLike(x, y - 1) && IsFloorLike(x, y + 1);

                if (horizontalChoke || verticalChoke)
                {
                    // Only place a door if this tile is at a room boundary
                    // (has at least one adjacent tile that's inside a room and one that's in a corridor)
                    if (IsRoomBoundary(x, y))
                    {
                        SetTileType(new Vector2Int(x, y), TileType.Door);
                        doorPositions.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a floor tile is at a room boundary — meaning it's the transition
    /// point between a room and a corridor. Used for door placement.
    /// </summary>
    private bool IsRoomBoundary(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);

        // Check if this tile is at the edge of any room
        foreach (var room in rooms)
        {
            // Is this tile just outside or at the edge of the room?
            bool insideRoom = room.Contains(pos);

            // Check if any neighbor is inside the room while this might be at the border
            if (!insideRoom)
            {
                // Tile is outside the room — check if an adjacent tile is inside
                Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (var dir in neighbors)
                {
                    if (room.Contains(pos + dir))
                        return true;
                }
            }
            else
            {
                // Tile is inside the room — check if it's at the room's edge
                // (position matches room boundary)
                if (pos.x == room.Position.x || pos.x == room.Position.x + room.Size.x - 1
                    || pos.y == room.Position.y || pos.y == room.Position.y + room.Size.y - 1)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Stair Placement
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Places stairs up (player entrance) in the first room.
    /// Returns the stair position.
    /// </summary>
    private Vector2Int PlaceStairsUp()
    {
        if (rooms.Count == 0)
        {
            Debug.LogError("[DungeonGenerator] No rooms generated! Cannot place stairs.");
            return new Vector2Int(gridWidth / 2, gridHeight / 2);
        }

        // Place stairs up in the first room (not on the exact center, offset slightly)
        RoomData spawnRoom = rooms[0];
        Vector2Int stairsPos = new Vector2Int(
            spawnRoom.Center.x + Random.Range(-1, 2),
            spawnRoom.Center.y + Random.Range(-1, 2)
        );

        // Clamp to room bounds
        stairsPos.x = Mathf.Clamp(stairsPos.x, spawnRoom.Position.x, spawnRoom.Position.x + spawnRoom.Size.x - 1);
        stairsPos.y = Mathf.Clamp(stairsPos.y, spawnRoom.Position.y, spawnRoom.Position.y + spawnRoom.Size.y - 1);

        SetTileType(stairsPos, TileType.Stairs);
        return stairsPos;
    }

    /// <summary>
    /// Places stairs down (exit to next floor) in the room farthest from the player spawn.
    /// On the final floor, no stairs down are placed.
    /// Returns the stair position.
    /// </summary>
    private Vector2Int PlaceStairsDown(Vector2Int stairsUp, DungeonConfig config, int floorNumber)
    {
        // No stairs down on the final floor (boss floor)
        if (floorNumber >= config.totalFloors)
        {
            return stairsUp; // Return spawn as fallback (no exit)
        }

        // Find the room farthest from the spawn room
        RoomData farthestRoom = rooms[rooms.Count - 1]; // Default to last room
        float maxDist = 0f;

        foreach (var room in rooms)
        {
            float dist = Vector2Int.Distance(room.Center, stairsUp);
            if (dist > maxDist)
            {
                maxDist = dist;
                farthestRoom = room;
            }
        }

        // Place stairs down near the center of the farthest room
        Vector2Int stairsDown = new Vector2Int(
            farthestRoom.Center.x + Random.Range(-1, 2),
            farthestRoom.Center.y + Random.Range(-1, 2)
        );

        stairsDown.x = Mathf.Clamp(stairsDown.x, farthestRoom.Position.x, farthestRoom.Position.x + farthestRoom.Size.x - 1);
        stairsDown.y = Mathf.Clamp(stairsDown.y, farthestRoom.Position.y, farthestRoom.Position.y + farthestRoom.Size.y - 1);

        // Don't overlap with stairs up
        if (stairsDown == stairsUp)
        {
            stairsDown.x = Mathf.Clamp(stairsDown.x + 1, farthestRoom.Position.x, farthestRoom.Position.x + farthestRoom.Size.x - 1);
        }

        SetTileType(stairsDown, TileType.Stairs);
        return stairsDown;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Theme Selection
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Selects a theme for this floor. Uses the primary theme, or randomly
    /// picks from themeVariants if any exist.
    /// </summary>
    private DungeonTheme SelectTheme(DungeonConfig config)
    {
        if (config.themeVariants != null && config.themeVariants.Length > 0)
        {
            // Include primary theme in the selection pool
            int totalOptions = config.themeVariants.Length + 1;
            int selection = Random.Range(0, totalOptions);

            if (selection == 0)
                return config.theme;
            else
                return config.themeVariants[selection - 1];
        }

        return config.theme;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Tile Helpers
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Sets a tile's type and updates its walkability.</summary>
    private void SetTileType(Vector2Int pos, TileType type)
    {
        if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight) return;

        tiles[pos.x, pos.y].Type = type;
        tiles[pos.x, pos.y].IsWalkable = type != TileType.Wall;
    }

    /// <summary>Returns true if the tile at (x,y) is a wall.</summary>
    private bool IsWall(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return true;
        return tiles[x, y].Type == TileType.Wall;
    }

    /// <summary>Returns true if the tile at (x,y) is floor, door, or stairs (any walkable type).</summary>
    private bool IsFloorLike(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return false;
        TileType t = tiles[x, y].Type;
        return t == TileType.Floor || t == TileType.Door || t == TileType.DoorOpen || t == TileType.Stairs;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BSP Node (inner class)
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A node in the Binary Space Partition tree.
    /// Leaf nodes contain rooms. Internal nodes split space for their children.
    /// </summary>
    private class BSPNode
    {
        public int X, Y, Width, Height;
        public BSPNode Left, Right;
        public RoomData Room;

        public BSPNode(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Left = null;
            Right = null;
            Room = null;
        }
    }
}