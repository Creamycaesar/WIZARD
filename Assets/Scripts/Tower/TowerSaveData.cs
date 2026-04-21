using System.Collections.Generic;

/// <summary>
/// Persistent Tower data that survives permadeath.
/// Serialized to tower_save.json by SaveManager.
///
/// File: Assets/Scripts/Tower/TowerSaveData.cs
///
/// TODO: Flesh out during Milestone 2 (Tower implementation).
/// </summary>
[System.Serializable]
public class TowerSaveData
{
    /// <summary>Items stored in Tower chests.</summary>
    public List<string> storedItemIDs = new();

    /// <summary>Decoration placements (item ID + grid position).</summary>
    public List<DecorationPlacement> decorations = new();

    /// <summary>Which Tower upgrades have been installed.</summary>
    public bool hasLibrary;
    public bool hasAlchemyLab;
    public bool hasScryingFont;

    /// <summary>Spell names copied into the permanent spellbook.</summary>
    public List<string> spellbookEntries = new();

    /// <summary>Which dungeons have been unlocked.</summary>
    public List<string> unlockedDungeons = new();
}

/// <summary>
/// A decoration placed in the Tower (item + position).
/// </summary>
[System.Serializable]
public struct DecorationPlacement
{
    public string itemID;
    public int gridX;
    public int gridY;
}