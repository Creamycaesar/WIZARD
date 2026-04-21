/// <summary>
/// Targeting shapes for spells. Determines how TargetingSystem highlights
/// valid tiles and how SpellManager resolves area-of-effect coverage.
///
/// Self/Touch don't enter SpellTargeting state — they resolve immediately.
/// All others trigger TargetingSystem.EnterTargeting().
/// </summary>
public enum TargetType
{
    /// <summary>Spell affects only the caster. No targeting needed.</summary>
    Self,

    /// <summary>Spell affects an adjacent tile (range 1). No targeting UI needed.</summary>
    Touch,

    /// <summary>Spell affects a single tile at range (e.g. Fire Bolt).</summary>
    SingleTile,

    /// <summary>Spell creates a line from caster in a direction (e.g. Lightning Bolt).</summary>
    Line,

    /// <summary>Spell creates a cone originating from the caster (e.g. Burning Hands).</summary>
    Cone,

    /// <summary>Spell affects a circular area centered on a target tile (e.g. Fireball).</summary>
    Sphere,

    /// <summary>Spell affects a square area centered on a target tile (e.g. Thunderwave).</summary>
    Cube
}