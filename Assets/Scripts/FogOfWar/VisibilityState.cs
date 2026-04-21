/// <summary>
/// Fog of war visibility states for each tile.
/// Managed by FogOfWarManager, stored on each Tile.
/// File: Assets/Scripts/FogOfWar/VisibilityState.cs
/// </summary>
public enum VisibilityState
{
    /// <summary>Never seen. Rendered as black/invisible.</summary>
    Hidden,

    /// <summary>Previously seen but not currently visible. Rendered dimmed.</summary>
    Explored,

    /// <summary>Currently within the player's vision radius. Fully lit.</summary>
    Visible
}