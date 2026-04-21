using UnityEngine;

/// <summary>
/// ScriptableObject template for an item. One asset per item type.
/// Runtime instances are wrapped in ItemInstance (which adds identification state, charges, etc.).
///
/// File: Assets/Scripts/Items/ItemData.cs
/// Asset location: Assets/Data/Items/
/// </summary>
[CreateAssetMenu(menuName = "WIZARD/Item")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName;
    public string unidentifiedName;         // "blue potion", "rusted ring"
    [TextArea(2, 4)]
    public string description;
    [TextArea(2, 4)]
    public string unidentifiedDescription;
    public ItemCategory category;
    public EquipmentSlot validSlot;         // Which slot this equips to
    public float weight;                    // In pounds
    public int goldValue;
    public bool requiresIdentification;

    [Header("Visuals")]
    public Sprite icon;
    public Sprite unidentifiedIcon;

    [Header("Equipment Stats")]
    public int acBonus;
    public int attackBonus;
    public int damageDiceCount, damageDiceSides, damageBonus;
    public DamageType damageType;

    [Header("Consumable Effects")]
    public SpellData scrollSpell;           // If this is a spell scroll
    public int healAmount;                  // If this is a healing potion

    [Header("Decoration")]
    public bool isDecoration;               // Can be placed in the Tower
    public Sprite decorationSprite;         // Tower placement visual
}