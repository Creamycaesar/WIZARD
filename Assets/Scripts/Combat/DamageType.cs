/// <summary>
/// All damage types from the 5e SRD.
/// Used by CombatManager for resistance/immunity checks,
/// EnemyAttack for attack damage typing, and SpellData for spell damage.
/// </summary>
public enum DamageType
{
    Bludgeoning,
    Piercing,
    Slashing,
    Fire,
    Cold,
    Lightning,
    Acid,
    Poison,
    Necrotic,
    Radiant,
    Force,
    Psychic,
    Thunder
}