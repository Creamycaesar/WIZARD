/// <summary>
/// Status conditions from the 5e SRD.
/// Applied and tracked by CombatManager. Conditions tick down
/// each World Turn via TurnManager.OnWorldTurnEnded.
///
/// Gameplay effects (e.g. Paralyzed = auto-crit on melee hits,
/// Blinded = disadvantage on attacks) are handled in CombatManager
/// when resolving attacks and saving throws.
/// </summary>
public enum Condition
{
    Blinded,
    Charmed,
    Deafened,
    Frightened,
    Grappled,
    Incapacitated,
    Invisible,
    Paralyzed,
    Petrified,
    Poisoned,
    Prone,
    Restrained,
    Stunned,
    Unconscious,
    None
}