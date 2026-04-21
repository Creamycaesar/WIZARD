/// <summary>
/// Interface for any entity that takes a turn during the World Turn phase.
/// Implemented by EnemyController. The player is NOT an ITurnActor —
/// the player's turn is implicit (they act, then ProcessPlayerAction fires).
///
/// File: Assets/Scripts/Turn/ITurnActor.cs
/// </summary>
public interface ITurnActor
{
    /// <summary>Dexterity score for turn ordering (higher = acts first).</summary>
    int Dexterity { get; }

    /// <summary>Spawn order for breaking Dexterity ties (lower = acts first).</summary>
    int SpawnOrder { get; }

    /// <summary>Called by TurnManager during the World Turn, in Dexterity order.</summary>
    void TakeTurn();
}