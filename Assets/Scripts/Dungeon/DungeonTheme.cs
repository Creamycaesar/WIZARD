using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Pure visual theming for a dungeon. Controls which tilesets, sprites,
/// and colors are used when rendering a dungeon level. Multiple DungeonConfigs
/// can reference the same DungeonTheme (e.g. two dungeons sharing a "cave" look)
/// and the same DungeonConfig can swap themes for visual variety.
///
/// File: Assets/Scripts/Dungeon/DungeonTheme.cs
/// Asset location: Assets/Data/Dungeons/Themes/
/// Layer: 0 (Pure data — no dependencies)
/// </summary>
[CreateAssetMenu(menuName = "WIZARD/Dungeon Theme")]
public class DungeonTheme : ScriptableObject
{
    // ── Identity ─────────────────────────────────────────────────────────

    [Header("Identity")]

    [Tooltip("Display name for this theme (e.g. 'Stone Dungeon - Blue', 'Caverns - Red')")]
    public string themeName;

    [Tooltip("Short description for editor reference")]
    [TextArea(2, 4)]
    public string description;

    // ── Background ───────────────────────────────────────────────────────

    [Header("Background")]

    [Tooltip("Solid color rendered behind all tiles. Visible through alpha and in unexplored areas.")]
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);

    [Tooltip("Optional: a subtle background tile to fill behind floor/wall tiles instead of flat color. " +
             "If null, backgroundColor is used as a flat fill.")]
    public Sprite backgroundTile;

    // ── Wall Tiles ───────────────────────────────────────────────────────

    [Header("Walls")]

    [Tooltip("Rule Tile for walls. Automatically selects the correct sprite (corners, edges, T-junctions, etc.) " +
             "based on neighboring wall tiles. Assign a RuleTile asset with all 16+ wall variations.")]
    public RuleTile wallRuleTile;

    // ── Floor Tiles ──────────────────────────────────────────────────────

    [Header("Floors")]

    [Tooltip("Floor tile sprites. GridRenderer picks randomly from this array for visual variety. " +
             "All should be the same size (16x16) and visually interchangeable.")]
    public Sprite[] floorSprites;

    [Tooltip("If true, floor sprites are placed randomly from the array. " +
             "If false, only the first sprite is used (uniform floors).")]
    public bool randomizeFloors = true;

    // ── Doors ────────────────────────────────────────────────────────────

    [Header("Doors")]

    [Tooltip("Sprite for a closed door tile")]
    public Sprite closedDoorSprite;

    [Tooltip("Alternate closed door sprite for variety (nullable)")]
    public Sprite closedDoorSpriteAlt;

    [Tooltip("Sprite for an open doorway")]
    public Sprite openDoorSprite;

    // ── Stairs ───────────────────────────────────────────────────────────

    [Header("Stairs")]

    [Tooltip("Sprite for stairs leading down (player descends to next floor)")]
    public Sprite stairsDownSprite;

    [Tooltip("Sprite for stairs leading up (where the player entered this floor)")]
    public Sprite stairsUpSprite;

    // ── Water ────────────────────────────────────────────────────────────

    [Header("Water")]

    [Tooltip("Rule Tile for water. Handles edges, corners, and inner corners " +
             "so water pools/rivers can be any shape.")]
    public RuleTile waterRuleTile;

    // ── Dungeon Dressing (decorative objects placed in rooms) ─────────

    [Header("Dungeon Dressing")]

    [Tooltip("Decorative objects that can be randomly placed in rooms for atmosphere. " +
             "These are non-blocking visual props (rendered on a separate tilemap layer above floors).")]
    public DungeonDressing[] dressingObjects;

    // ── Trap Visuals ─────────────────────────────────────────────────────

    [Header("Traps")]

    [Tooltip("Sprite for a hidden/armed trap (shown after detection or triggering)")]
    public Sprite trapSprite;

    [Tooltip("Sprite for a triggered/disarmed trap")]
    public Sprite trapTriggeredSprite;

    // ── Special Tiles ────────────────────────────────────────────────────

    [Header("Special Tiles")]

    [Tooltip("Sprite for campfire rest points")]
    public Sprite campfireSprite;

    [Tooltip("Sprite for grease-covered floor (spell interaction surface)")]
    public Sprite greaseSprite;

    // ── Fog of War Tinting ───────────────────────────────────────────────

    [Header("Fog of War")]

    [Tooltip("Color tint applied to Explored (previously seen) tiles. " +
             "Typically a dark grey. Alpha controls how much the original tile shows through.")]
    public Color exploredTint = new Color(0.3f, 0.3f, 0.35f, 1f);

    [Tooltip("Color used for Hidden (never seen) tiles. Usually matches or is darker than backgroundColor.")]
    public Color hiddenColor = new Color(0.02f, 0.02f, 0.05f, 1f);

    // ── Ambient / Mood ───────────────────────────────────────────────────

    [Header("Ambient")]

    [Tooltip("Optional ambient light color tint applied to the entire level. " +
             "White = no tint. Slight blue = cold dungeon. Slight orange = warm cavern.")]
    public Color ambientTint = Color.white;
}

// ═══════════════════════════════════════════════════════════════════════════
//  DungeonDressing — a furnishing that spawns in dungeon rooms.
//  These are real items the player can pick up and carry back to the
//  Wizard Tower. DungeonGenerator places them as interactable world objects.
//  All visual and functional data lives on ItemData + FurnitureData;
//  this struct only controls spawn frequency per dungeon theme.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Spawn entry for furniture/dressing in a dungeon theme.
/// References an ItemData (which must have category == Furniture and a
/// FurnitureData reference) and adds spawn parameters for the generator.
/// </summary>
[System.Serializable]
public struct DungeonDressing
{
    [Tooltip("The furniture item. Must be an ItemData with category Furniture " +
             "and a valid FurnitureData reference. All visual, weight, and " +
             "Tower functionality data comes from here.")]
    public ItemData item;

    [Tooltip("Relative spawn weight. Higher = more common. " +
             "A candle might be 10, a bookcase might be 2, " +
             "an alchemy table might be 1.")]
    public int spawnWeight;

    [Tooltip("Max instances of this furniture per room. " +
             "0 = unlimited (use spawnWeight only). " +
             "Unique items like an alchemy table should be 1.")]
    public int maxPerRoom;

    [Tooltip("Minimum dungeon floor this furniture can appear on (1-indexed). " +
             "0 = can appear on any floor. Rarer/more valuable pieces " +
             "might only show up on deeper floors.")]
    public int minFloor;
}