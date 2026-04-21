using UnityEngine;

/// <summary>
/// Defines what items an enemy can drop on death, with weighted probabilities.
/// Referenced by EnemyData.lootTable.
///
/// TODO: Full implementation during Milestone 2 (items/inventory system).
/// For now this is a compilable placeholder so EnemyData can reference it.
/// Will need ItemData references once that ScriptableObject is built.
/// </summary>
[CreateAssetMenu(menuName = "WIZARD/Loot Table")]
public class LootTable : ScriptableObject
{
    [Tooltip("Chance (0-1) that this enemy drops anything at all")]
    [Range(0f, 1f)]
    public float dropChance = 0.5f;

    [Tooltip("Minimum gold dropped (rolled between min and max)")]
    public int minGold;

    [Tooltip("Maximum gold dropped")]
    public int maxGold;

    // TODO: Add ItemData[] itemDrops with weights once ItemData is implemented.
    // TODO: Add RollLoot() method that uses Dice.cs for weighted random selection.
}