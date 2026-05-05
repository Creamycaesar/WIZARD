using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ScriptableObject template for an item. One asset per item type.
/// Runtime instances are wrapped in ItemInstance (which adds identification state, charges, etc.).
///
/// All items use a single sprite (icon) for inventory, menus, and world placement.
/// Furniture items have additional fields gated behind isFurniture for Tower
/// functionality, placement rules, and multi-tile support.
///
/// File: Assets/Scripts/Items/ItemData.cs
/// Asset location: Assets/Data/Items/
/// </summary>
[CreateAssetMenu(menuName = "WIZARD/Item")]
public class ItemData : ScriptableObject
{
    // ── Identity ─────────────────────────────────────────────────────────

    [Header("Identity")]
    public string itemName;
    public string unidentifiedName;         // "blue potion", "rusted ring"
    [TextArea(2, 4)]
    public string description;
    [TextArea(2, 4)]
    public string unidentifiedDescription;
    public ItemCategory category;
    public EquipmentSlot validSlot;         // Which slot this equips to (None if not equippable)
    public float weight;                    // In pounds
    public int goldValue;
    public bool requiresIdentification;

    // ── Visuals ──────────────────────────────────────────────────────────

    [Header("Visuals")]

    [Tooltip("Item sprite used everywhere: inventory, menus, and placed in the world.")]
    public Sprite icon;

    [Tooltip("Sprite shown before the item is identified (e.g. a generic 'blue potion' icon)")]
    public Sprite unidentifiedIcon;

    // ── Equipment Stats ──────────────────────────────────────────────────

    [Header("Equipment Stats")]
    public int acBonus;
    public int attackBonus;
    public int damageDiceCount, damageDiceSides, damageBonus;
    public DamageType damageType;

    // ── Consumable Effects ───────────────────────────────────────────────

    [Header("Consumable Effects")]
    public SpellData scrollSpell;           // If this is a spell scroll
    public int healAmount;                  // If this is a healing potion

    // ── Furniture ────────────────────────────────────────────────────────
    //  All fields below are only relevant when isFurniture == true.
    //  Controls dungeon placement, pickup behavior, and Tower functionality.

    [Header("Furniture")]

    [Tooltip("Is this item a piece of furniture that can be placed in the world " +
             "and brought back to the Wizard Tower?")]
    public bool isFurniture = false;

    [Tooltip("Rule Tile for multi-tile furniture (large tables, long bookshelves). " +
             "If assigned, the furniture spans multiple tiles using neighbor-based auto-tiling. " +
             "If null, the item's icon sprite is used for single-tile placement.")]
    public RuleTile furnitureRuleTile;

    [Tooltip("Size in tiles for multi-tile furniture (1x1 for chairs/candles, " +
             "2x1 for dressers, 3x2 for large tables). Only used when furnitureRuleTile is assigned.")]
    public Vector2Int furnitureTileSize = Vector2Int.one;

    [Tooltip("Whether this furniture blocks movement when placed on the grid. " +
             "Tables, bookcases = true. Rugs, floor candles = false.")]
    public bool blocksMovement = true;

    [Tooltip("Whether this furniture blocks line of sight (fog of war and spell targeting). " +
             "Tall bookcases = true. Tables, chairs = false.")]
    public bool blocksLineOfSight = false;

    [Tooltip("Whether the player can pick this up and carry it. " +
             "Some dungeon fixtures (pillars, statues) might be non-portable.")]
    public bool isPickupable = true;

    [Tooltip("Whether this furniture has contents the player can look inside " +
             "(drawers, chests, bookshelves with loot). " +
             "Appears as an option in the right-click context menu.")]
    public bool isSearchable = false;

    [Tooltip("What this furniture does when placed in the Wizard Tower.")]
    public TowerFunction towerFunction = TowerFunction.Decorative;

    [Tooltip("Number of storage slots this provides when placed in the Tower. " +
             "Only relevant when towerFunction == Storage. " +
             "Small chest = 4, bookcase = 8, large dresser = 12.")]
    public int storageSlots = 0;

    [Tooltip("Which Tower room this furniture must be placed in to activate its function. " +
             "Any = can be placed anywhere.")]
    public TowerRoom requiredRoom = TowerRoom.Any;

    [Tooltip("If true, only one of this furniture can be functionally active in the Tower. " +
             "Extra copies placed are purely decorative. " +
             "e.g. you only need one Alchemy Table.")]
    public bool isUniqueUpgrade = false;

    [Tooltip("Description of the Tower function shown in the placement UI. " +
             "e.g. 'Provides 8 storage slots for potions and scrolls.'")]
    [TextArea(2, 3)]
    public string towerFunctionDescription;
}

// ═══════════════════════════════════════════════════════════════════════════
//  TowerFunction — what a piece of furniture does in the Wizard Tower.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Functional role of furniture when placed in the Wizard Tower.
/// </summary>
public enum TowerFunction
{
    /// <summary>No gameplay function. Purely cosmetic decoration.</summary>
    Decorative,

    /// <summary>Provides item storage slots (chests, bookcases, dressers).
    /// Number of slots defined by ItemData.storageSlots.</summary>
    Storage,

    /// <summary>Alchemy crafting station. Allows potion brewing from reagents.</summary>
    AlchemyLab,

    /// <summary>Spell research station. Allows copying spells into the spellbook
    /// and preparing spells between runs.</summary>
    Library,

    /// <summary>Scrying station. Previews dungeon bosses and floor layouts
    /// before entering a run.</summary>
    ScryingFont,

    /// <summary>Enchanting station. Allows upgrading or modifying equipment.</summary>
    Enchanting,

    /// <summary>Rest point. Provides benefits at the start of a run
    /// (e.g. bonus temp HP, inspiration).</summary>
    RestArea,

    /// <summary>Trophy/display. Shows collected boss trophies or rare items.
    /// May provide passive bonuses based on what's displayed.</summary>
    Display,

    /// <summary>Light source. Candles, braziers, magical lights.
    /// Certain Tower rooms may require light sources to function.</summary>
    LightSource
}

// ═══════════════════════════════════════════════════════════════════════════
//  TowerRoom — rooms in the Wizard Tower where furniture can be placed.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Rooms in the Wizard Tower. Used for furniture placement validation.
/// </summary>
public enum TowerRoom
{
    /// <summary>Can be placed in any room.</summary>
    Any,

    /// <summary>Main hall / foyer.</summary>
    MainHall,

    /// <summary>Alchemy laboratory. Required for AlchemyLab furniture.</summary>
    Laboratory,

    /// <summary>Library / study. Required for Library furniture.</summary>
    Study,

    /// <summary>Scrying chamber. Required for ScryingFont furniture.</summary>
    ScryingChamber,

    /// <summary>Storage room / vault.</summary>
    Vault,

    /// <summary>Bedroom / rest area. Required for RestArea furniture.</summary>
    Bedroom,

    /// <summary>Trophy room. Required for Display furniture.</summary>
    TrophyRoom,

    /// <summary>Workshop. Required for Enchanting furniture.</summary>
    Workshop
}