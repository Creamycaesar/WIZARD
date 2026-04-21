/// <summary>
/// When during a turn a spell can be cast. Maps to the 5e action economy.
///
/// In WIZARD's turn-based system:
/// - Action: Standard cast, consumes the player's turn.
/// - BonusAction: Can be cast alongside another action (future feature).
/// - Reaction: Cast in response to a trigger during an enemy's turn (e.g. Shield).
/// </summary>
public enum CastTime
{
    Action,
    BonusAction,
    Reaction
}