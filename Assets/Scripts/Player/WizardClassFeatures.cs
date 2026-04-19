/// <summary>
/// Every class feature a Wizard can gain through leveling.
/// Flags enum so a single int can store multiple features gained at a level.
///
/// Subclass features are NOT here — those are unlocked via books found
/// in dungeons and studied at the Tower Library.
/// </summary>
[System.Flags]
public enum WizardClassFeature
{
    None = 0,

    // ── Level 1 ──────────────────────────────────────────────────────────
    Spellcasting = 1 << 0,   // Core spellcasting ability
    RitualAdept = 1 << 1,   // Can cast ritual spells without preparing them
    ArcaneRecovery = 1 << 2,   // Recover spell slots on short rest (once per long rest)

    // ── Level 2 ──────────────────────────────────────────────────────────
    Scholar = 1 << 3,   // Expertise in one Int-based skill

    // ── Level 4, 8, 12, 16 ──────────────────────────────────────────────
    AbilityScoreImprovement = 1 << 4,   // +2 to one score or +1 to two (repeatable)

    // ── Level 5 ──────────────────────────────────────────────────────────
    MemorizeSpell = 1 << 5,   // Swap one prepared spell after a short rest

    // ── Level 18 ─────────────────────────────────────────────────────────
    SpellMastery = 1 << 6,   // Cast one 1st and one 2nd level spell at will

    // ── Level 19 ─────────────────────────────────────────────────────────
    EpicBoon = 1 << 7,   // Choose an Epic Boon feat

    // ── Level 20 ─────────────────────────────────────────────────────────
    SignatureSpells = 1 << 8,   // Two 3rd-level spells always prepared, cast once free
}