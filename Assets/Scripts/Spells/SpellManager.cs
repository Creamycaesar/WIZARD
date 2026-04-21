using System;
using UnityEngine;

/// <summary>
/// Manages the player's prepared spells, spell slot expenditure,
/// concentration tracking, and spell execution.
///
/// File: Assets/Scripts/Spells/SpellManager.cs
/// Layer: 3 (Depends on Layers 0-2)
/// Dependencies: WizardStats, CombatManager, GridManager, TurnManager,
///               GameManager, TargetingSystem
/// </summary>
public class SpellManager : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════
    //  Singleton
    // ═════════════════════════════════════════════════════════════════════

    public static SpellManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Events
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Fired after a spell is fully resolved.</summary>
    public static event Action<SpellData, Vector2Int, int> OnSpellCast;

    /// <summary>Fired when a concentration spell begins.</summary>
    public static event Action<SpellData> OnConcentrationStarted;

    /// <summary>Fired when concentration ends (willingly or broken).</summary>
    public static event Action<SpellData> OnConcentrationEnded;

    /// <summary>Fired after a spell slot is used.</summary>
    public static event Action<int, int> OnSpellSlotExpended;

    // ═════════════════════════════════════════════════════════════════════
    //  Serialized References
    // ═════════════════════════════════════════════════════════════════════

    [SerializeField] private WizardStats wizardStats;

    // ═════════════════════════════════════════════════════════════════════
    //  Runtime State
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The 5 currently prepared spells, indexed 0-4 matching hotbar slots 1-5.
    /// </summary>
    public SpellData[] PreparedSpells { get; private set; } = new SpellData[5];

    /// <summary>
    /// The spell currently being concentrated on, or null if none.
    /// </summary>
    public SpellData ActiveConcentrationSpell { get; private set; }

    // ═════════════════════════════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets a prepared spell in a hotbar slot. Can be done anytime in the Tower.
    /// Consumes a turn if done in a dungeon (optional design decision).
    /// </summary>
    /// <param name="slot">Hotbar slot index (0-4).</param>
    /// <param name="spell">The SpellData to prepare in that slot.</param>
    public void PrepareSpell(int slot, SpellData spell)
    {
        if (slot < 0 || slot >= PreparedSpells.Length)
        {
            Debug.LogWarning($"SpellManager.PrepareSpell: invalid slot {slot}");
            return;
        }

        PreparedSpells[slot] = spell;
    }

    /// <summary>
    /// Begins casting a spell from the hotbar. Checks spell slot availability.
    /// If the spell needs a target, enters SpellTargeting state.
    /// If self-targeted, casts immediately.
    /// </summary>
    /// <param name="slotIndex">Hotbar slot index (0-4).</param>
    public void ActivateSpell(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= PreparedSpells.Length)
        {
            Debug.LogWarning($"SpellManager.ActivateSpell: invalid slot {slotIndex}");
            return;
        }

        SpellData spell = PreparedSpells[slotIndex];
        if (spell == null)
        {
            Debug.Log("SpellManager: No spell prepared in that slot.");
            return;
        }

        // Cantrips don't cost slots
        if (spell.spellLevel > 0 && !HasSpellSlot(spell.spellLevel))
        {
            Debug.Log($"SpellManager: No spell slots available for level {spell.spellLevel}.");
            return;
        }

        // Self-targeted spells cast immediately without entering targeting mode
        if (spell.targetType == TargetType.Self)
        {
            // TODO: Get player position from PlayerController.Instance.GridPosition
            CastSpell(spell, Vector2Int.zero, spell.spellLevel);
            return;
        }

        // All other spells enter targeting mode
        // TODO: GameManager.Instance.ChangeState(GameState.SpellTargeting);
        TargetingSystem.Instance.EnterTargeting(spell);
    }

    /// <summary>
    /// Executes a spell: expends spell slot, rolls attack or forces save
    /// via CombatManager, applies effects, starts concentration if needed.
    /// </summary>
    /// <param name="spell">The spell being cast.</param>
    /// <param name="targetTile">The grid tile targeted.</param>
    /// <param name="castLevel">The level at which it's being cast (for upcasting).</param>
    public void CastSpell(SpellData spell, Vector2Int targetTile, int castLevel)
    {
        // Expend spell slot (cantrips are free)
        if (spell.spellLevel > 0)
        {
            ExpendSpellSlot(castLevel);
        }

        // Handle concentration — break existing concentration if casting a new one
        if (spell.requiresConcentration)
        {
            if (ActiveConcentrationSpell != null)
            {
                BreakConcentration();
            }
            ActiveConcentrationSpell = spell;
            OnConcentrationStarted?.Invoke(spell);
        }

        // TODO: Resolve spell effects based on spell.effects[]
        // - If requiresAttackRoll: CombatManager.Instance.ResolveAttack(...)
        // - If saving throw: CombatManager.Instance.ResolveSavingThrow(...)
        // - Apply each SpellEffect in spell.effects[]
        // - Handle upcasting: add (castLevel - spell.spellLevel) * upcastDicePerLevel

        OnSpellCast?.Invoke(spell, targetTile, castLevel);

        // TODO: TurnManager.Instance.ProcessPlayerAction();
    }

    /// <summary>
    /// Ends the active concentration spell. Called by
    /// CombatManager.OnConcentrationBroken or when the player casts
    /// a new concentration spell.
    /// </summary>
    public void BreakConcentration()
    {
        if (ActiveConcentrationSpell == null) return;

        SpellData ending = ActiveConcentrationSpell;
        ActiveConcentrationSpell = null;

        // TODO: Remove ongoing effects of the concentration spell
        // (e.g. remove conditions applied by Hold Person)

        OnConcentrationEnded?.Invoke(ending);
    }

    /// <summary>
    /// Checks if the player has an available spell slot at the given level.
    /// </summary>
    public bool HasSpellSlot(int level)
    {
        if (wizardStats == null)
        {
            Debug.LogWarning("SpellManager: WizardStats not assigned.");
            return false;
        }

        if (level < 1 || level > 9) return false;

        return wizardStats.currentSpellSlots[level - 1] > 0;
    }

    /// <summary>
    /// Decrements the spell slot count in WizardStats at the given level.
    /// </summary>
    public void ExpendSpellSlot(int level)
    {
        if (wizardStats == null)
        {
            Debug.LogWarning("SpellManager: WizardStats not assigned.");
            return;
        }

        if (level < 1 || level > 9) return;

        int index = level - 1;
        if (wizardStats.currentSpellSlots[index] > 0)
        {
            wizardStats.currentSpellSlots[index]--;
            OnSpellSlotExpended?.Invoke(level, wizardStats.currentSpellSlots[index]);
        }
        else
        {
            Debug.LogWarning($"SpellManager: Tried to expend level {level} slot but none available.");
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Event Subscriptions
    // ═════════════════════════════════════════════════════════════════════

    private void OnEnable()
    {
        // Subscribe to concentration break events from CombatManager
        CombatManager.OnConcentrationBroken += HandleConcentrationBroken;
    }

    private void OnDisable()
    {
        CombatManager.OnConcentrationBroken -= HandleConcentrationBroken;
    }

    private void HandleConcentrationBroken(PlayerController caster)
    {
        BreakConcentration();
    }
}