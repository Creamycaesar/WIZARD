/// <summary>
/// The game state machine. Governs input routing and UI behavior.
/// The turn system runs identically in all gameplay states — states exist
/// primarily to control what the player's input does, not to change game logic.
///
/// File: Assets/Scripts/Core/GameState.cs
/// </summary>
public enum GameState
{
    /// <summary>Title screen. No gameplay systems active.</summary>
    MainMenu,

    /// <summary>
    /// Primary state. Covers both Tower and Dungeon.
    /// Full gameplay input: movement, interact, cast spells, open panels.
    /// </summary>
    Gameplay,

    /// <summary>
    /// Sub-state of Gameplay. Player is choosing a spell target.
    /// Movement disabled. Mouse selects tiles. Escape/right-click cancels.
    /// </summary>
    SpellTargeting,

    /// <summary>
    /// Player has died. Permadeath screen shown. Tower data saved.
    /// After dismissal, new Wizard generated and player returns to Tower.
    /// </summary>
    GameOver,

    /// <summary>
    /// Pause menu open. Turn loop frozen. Save &amp; Quit available.
    /// </summary>
    Paused
}

/// <summary>
/// Whether the player is in the Tower or a Dungeon.
/// This is NOT a GameState — it's a level context. Systems that behave
/// differently (e.g. EnemySpawner, TowerManager) check this instead of GameState.
/// </summary>
public enum LevelContext
{
    Tower,
    Dungeon
}