/// <summary>
/// Runtime instance of an item. Wraps an ItemData ScriptableObject with
/// per-instance state (identification, charges, unique ID for save/load).
///
/// File: Assets/Scripts/Inventory/ItemInstance.cs
/// </summary>
public class ItemInstance
{
    /// <summary>Reference to the ScriptableObject template.</summary>
    public ItemData Data;

    /// <summary>Whether the player has identified this item.</summary>
    public bool IsIdentified;

    /// <summary>Current charges for wands. -1 if not applicable.</summary>
    public int CurrentCharges;

    /// <summary>GUID for save/load serialization.</summary>
    public string UniqueID;

    public ItemInstance(ItemData data)
    {
        Data = data;
        IsIdentified = !data.requiresIdentification;
        CurrentCharges = -1;
        UniqueID = System.Guid.NewGuid().ToString();
    }
}