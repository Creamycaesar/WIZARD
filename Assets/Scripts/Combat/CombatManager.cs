using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure 5e rules engine. Resolves all combat actions: attack rolls, damage rolls,
/// saving throws, condition application, and concentration checks.
///
/// CombatManager does not own any entities and has no awareness of the turn system
/// or game state. It is called by other systems (PlayerController, EnemyController,
/// SpellManager) and produces outcomes via return values and events.
///
/// File: Assets/Scripts/Combat/CombatManager.cs
/// Layer: 0 (Foundation — no manager dependencies)
/// Dependencies: Dice.cs (for all rolls)
/// </summary>
public class CombatManager : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════
    //  Singleton
    // ═════════════════════════════════════════════════════════════════════

    public static CombatManager Instance { get; private set; }

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

    /// <summary>Fired after any attack roll is resolved.</summary>
    public static event Action<AttackResult> OnAttackResolved;

    /// <summary>Fired after any saving throw.</summary>
    public static event Action<SaveResult> OnSavingThrowResolved;

    /// <summary>Fired when a condition is added to a target.</summary>
    public static event Action<IDamageable, Condition> OnConditionApplied;

    /// <summary>Fired when a condition expires or is removed.</summary>
    public static event Action<IDamageable, Condition> OnConditionRemoved;

    /// <summary>Fired when a concentration check fails.</summary>
    public static event Action<PlayerController> OnConcentrationBroken;

    // ═════════════════════════════════════════════════════════════════════
    //  Condition Tracking
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tracks active conditions with their remaining duration.
    /// Key: the affected entity. Value: list of (condition, remaining turns).
    /// </summary>
    private readonly Dictionary<IDamageable, List<ActiveCondition>> _activeConditions = new();

    /// <summary>Internal tracking struct for conditions with duration.</summary>
    private struct ActiveCondition
    {
        public Condition Condition;
        public int RemainingTurns; // -1 = permanent (until removed explicitly)
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Public API — Attack Resolution
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves a full attack roll using 5e rules.
    /// Rolls d20 + attackBonus vs target.ArmorClass. On hit, rolls damage.
    /// Applies damage via target.TakeDamage(). Natural 20 = critical hit (double dice).
    /// Natural 1 = automatic miss regardless of modifiers.
    /// </summary>
    /// <param name="attacker">The entity making the attack.</param>
    /// <param name="target">The entity being attacked.</param>
    /// <param name="attackBonus">Added to the d20 roll (ability mod + proficiency).</param>
    /// <param name="damageDice">Number of damage dice (the N in NdX+B).</param>
    /// <param name="damageSides">Sides per die (the X in NdX+B).</param>
    /// <param name="damageBonus">Flat bonus added to damage (the B in NdX+B).</param>
    /// <param name="type">Damage type for resistance/immunity checks.</param>
    /// <returns>AttackResult with hit/miss, damage dealt, crit status, and raw roll.</returns>
    public AttackResult ResolveAttack(
        IDamageable attacker,
        IDamageable target,
        int attackBonus,
        int damageDice,
        int damageSides,
        int damageBonus,
        DamageType type)
    {
        int roll = Dice.D20();
        bool isCrit = roll == 20;
        bool isNat1 = roll == 1;

        // 5e: Natural 1 always misses, natural 20 always hits
        bool hit = isNat1 ? false : (isCrit || (roll + attackBonus >= target.ArmorClass));

        int damage = 0;

        if (hit)
        {
            // Roll damage — crits double the number of dice (5e rule)
            int diceToRoll = isCrit ? damageDice * 2 : damageDice;
            damage = Dice.Roll(diceToRoll, damageSides, damageBonus);

            // Ensure at least 1 damage on a hit
            if (damage < 1) damage = 1;

            target.TakeDamage(damage, type);
        }

        var result = new AttackResult
        {
            Hit = hit,
            CriticalHit = isCrit,
            DamageDealt = damage,
            Type = type,
            Attacker = attacker,
            Target = target,
            Roll = roll
        };

        OnAttackResolved?.Invoke(result);
        return result;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Public API — Saving Throws
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves a saving throw using 5e rules.
    /// Target rolls d20 + ability modifier vs DC.
    ///
    /// Note: The caller is responsible for providing the correct modifier
    /// (including proficiency if applicable). CombatManager is a pure rules
    /// engine and does not look up stats itself.
    /// </summary>
    /// <param name="target">The entity making the save.</param>
    /// <param name="ability">Which ability score is being tested.</param>
    /// <param name="dc">The Difficulty Class to beat.</param>
    /// <param name="modifier">The target's total save modifier (ability mod + proficiency if proficient).</param>
    /// <returns>SaveResult with success/failure, raw roll, total, and DC.</returns>
    public SaveResult ResolveSavingThrow(
        IDamageable target,
        AbilityScore ability,
        int dc,
        int modifier)
    {
        int roll = Dice.D20();
        int total = roll + modifier;
        bool success = total >= dc;

        var result = new SaveResult
        {
            Success = success,
            Roll = roll,
            Total = total,
            DC = dc,
            Ability = ability,
            Target = target
        };

        OnSavingThrowResolved?.Invoke(result);
        return result;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Public API — Conditions
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Applies a condition to the target for a given duration.
    /// Conditions tick down each World Turn via TickConditions().
    /// If the target already has this condition, the duration is refreshed
    /// (not stacked) per 5e rules.
    /// </summary>
    /// <param name="target">The entity to apply the condition to.</param>
    /// <param name="condition">Which condition to apply.</param>
    /// <param name="durationTurns">How many world turns it lasts. Use -1 for permanent.</param>
    public void ApplyCondition(IDamageable target, Condition condition, int durationTurns)
    {
        if (!_activeConditions.ContainsKey(target))
            _activeConditions[target] = new List<ActiveCondition>();

        var list = _activeConditions[target];

        // Check if already present — refresh duration instead of stacking
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Condition == condition)
            {
                list[i] = new ActiveCondition
                {
                    Condition = condition,
                    RemainingTurns = durationTurns
                };
                return; // Don't fire event again for a refresh
            }
        }

        list.Add(new ActiveCondition
        {
            Condition = condition,
            RemainingTurns = durationTurns
        });

        // Also add to the target's own ActiveConditions list so IDamageable
        // consumers (like CombatManager itself) can query conditions directly
        if (!target.ActiveConditions.Contains(condition))
            target.ActiveConditions.Add(condition);

        OnConditionApplied?.Invoke(target, condition);
    }

    /// <summary>
    /// Removes a condition from the target early (e.g. via Lesser Restoration).
    /// </summary>
    public void RemoveCondition(IDamageable target, Condition condition)
    {
        if (_activeConditions.TryGetValue(target, out var list))
        {
            list.RemoveAll(ac => ac.Condition == condition);

            if (list.Count == 0)
                _activeConditions.Remove(target);
        }

        target.ActiveConditions.Remove(condition);
        OnConditionRemoved?.Invoke(target, condition);
    }

    /// <summary>
    /// Ticks down all active condition durations by 1 turn.
    /// Called at the end of each World Turn (subscribe to TurnManager.OnWorldTurnEnded).
    /// Expired conditions are automatically removed.
    /// </summary>
    public void TickConditions()
    {
        // Collect removals to avoid modifying collections during iteration
        var removals = new List<(IDamageable target, Condition condition)>();

        foreach (var kvp in _activeConditions)
        {
            var target = kvp.Key;
            var list = kvp.Value;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var ac = list[i];

                // -1 = permanent, skip ticking
                if (ac.RemainingTurns < 0) continue;

                ac.RemainingTurns--;
                list[i] = ac;

                if (ac.RemainingTurns <= 0)
                {
                    removals.Add((target, ac.Condition));
                    list.RemoveAt(i);
                }
            }
        }

        // Fire removal events after iteration is complete
        foreach (var (target, condition) in removals)
        {
            target.ActiveConditions.Remove(condition);
            OnConditionRemoved?.Invoke(target, condition);
        }

        // Clean up empty entries
        var emptyKeys = new List<IDamageable>();
        foreach (var kvp in _activeConditions)
        {
            if (kvp.Value.Count == 0)
                emptyKeys.Add(kvp.Key);
        }
        foreach (var key in emptyKeys)
            _activeConditions.Remove(key);
    }

    /// <summary>
    /// Removes all tracked conditions for an entity (e.g. on death).
    /// Does NOT fire individual OnConditionRemoved events.
    /// </summary>
    public void ClearAllConditions(IDamageable target)
    {
        if (_activeConditions.ContainsKey(target))
            _activeConditions.Remove(target);

        target.ActiveConditions.Clear();
    }

    /// <summary>
    /// Checks if a target currently has a specific condition.
    /// </summary>
    public bool HasCondition(IDamageable target, Condition condition)
    {
        return target.ActiveConditions.Contains(condition);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Public API — Concentration
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Rolls a concentration check per 5e rules.
    /// DC = max(10, damageTaken / 2). Rolls CON save.
    /// Returns true if concentration holds, false if broken.
    ///
    /// Note: PlayerController is referenced here because only the player
    /// can concentrate on spells in WIZARD. If enemy spellcasters gain
    /// concentration in the future, this should be refactored to accept
    /// IDamageable + conModifier instead.
    /// </summary>
    /// <param name="caster">The player (concentration caster).</param>
    /// <param name="damageTaken">How much damage triggered this check.</param>
    /// <returns>True if concentration holds, false if broken.</returns>
    public bool CheckConcentration(PlayerController caster, int damageTaken)
    {
        int dc = Mathf.Max(10, damageTaken / 2);

        // TODO: Get CON modifier from caster's WizardStats.
        // For now, use the IDamageable interface — the caller should ensure
        // the caster implements it. The actual CON modifier lookup will go
        // through WizardStats once PlayerController is implemented.
        int conModifier = 0; // Placeholder — wire to WizardStats.GetModifier(AbilityScore.Constitution)

        int roll = Dice.D20();
        int total = roll + conModifier;
        bool holds = total >= dc;

        if (!holds)
        {
            OnConcentrationBroken?.Invoke(caster);
        }

        return holds;
    }
}