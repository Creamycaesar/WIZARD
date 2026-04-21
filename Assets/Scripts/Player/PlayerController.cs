using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The player's in-game representation. Owns a reference to WizardStats,
/// handles movement on the grid, executes melee attacks, receives damage,
/// manages conditions, and signals turn completion.
///
/// The player is NOT an ITurnActor — the player's turn is implicit.
/// The player acts, then calls TurnManager.ProcessPlayerAction().
///
/// Spellcasting is handled entirely by SpellManager. This class just
/// routes the OnCastSpellInput event to SpellManager.ActivateSpell().
///
/// File: Assets/Scripts/Player/PlayerController.cs
/// Layer: 3 (Depends on WizardStats, GridManager, TurnManager, InputHandler,
///           CombatManager, GameManager)
/// </summary>
public class PlayerController : MonoBehaviour, IDamageable
{
    // ═════════════════════════════════════════════════════════════════════
    //  Singleton (not in tech arch, but practical for other systems
    //  that need player position — e.g. EnemyAI, FogOfWarManager)
    // ═════════════════════════════════════════════════════════════════════

    public static PlayerController Instance { get; private set; }

    // ═════════════════════════════════════════════════════════════════════
    //  Events
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>After a successful move.</summary>
    public static event Action<Vector2Int, Vector2Int> OnPlayerMoved;

    /// <summary>After taking damage.</summary>
    public static event Action<int, int> OnPlayerDamaged;

    /// <summary>After healing.</summary>
    public static event Action<int, int> OnPlayerHealed;

    /// <summary>When the player interacts with a tile.</summary>
    public static event Action<Tile> OnPlayerInteracted;

    // ═════════════════════════════════════════════════════════════════════
    //  Serialized References
    // ═════════════════════════════════════════════════════════════════════

    [SerializeField] private WizardStats wizardStats;

    // ═════════════════════════════════════════════════════════════════════
    //  Public Properties
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Reference to the current Wizard's ScriptableObject stats.</summary>
    public WizardStats Stats => wizardStats;

    /// <summary>Current position on the grid.</summary>
    public Vector2Int GridPosition { get; private set; }

    /// <summary>Delegates to Stats movement speed.</summary>
    public int MovementSpeed => wizardStats != null ? wizardStats.baseMovementSpeed : 1;

    // ═════════════════════════════════════════════════════════════════════
    //  IDamageable Implementation
    // ═════════════════════════════════════════════════════════════════════

    public int ArmorClass => wizardStats != null ? wizardStats.armorClass : 10;
    public int CurrentHP => wizardStats != null ? wizardStats.currentHP : 0;
    public bool IsAlive => CurrentHP > 0;
    public List<Condition> ActiveConditions { get; } = new();

    // ═════════════════════════════════════════════════════════════════════
    //  Lifecycle
    // ═════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        InputHandler.OnMoveInput += HandleMoveInput;
        InputHandler.OnWaitInput += HandleWaitInput;
        InputHandler.OnInteractInput += HandleInteractInput;
        InputHandler.OnCastSpellInput += HandleCastSpellInput;
    }

    private void OnDisable()
    {
        InputHandler.OnMoveInput -= HandleMoveInput;
        InputHandler.OnWaitInput -= HandleWaitInput;
        InputHandler.OnInteractInput -= HandleInteractInput;
        InputHandler.OnCastSpellInput -= HandleCastSpellInput;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempts to move one tile in the given direction.
    /// If an enemy occupies the target tile, triggers a melee attack instead.
    /// Returns true if the action consumed the turn.
    /// </summary>
    public bool TryMove(Vector2Int direction)
    {
        Vector2Int targetPos = GridPosition + direction;

        // Check if an enemy is there — bump attack
        GameObject occupant = GridManager.Instance.GetOccupant(targetPos);
        if (occupant != null)
        {
            var enemy = occupant.GetComponent<EnemyController>();
            if (enemy != null && enemy.IsAlive)
            {
                MeleeAttack(enemy);
                return true;
            }
        }

        // Check walkability
        if (!GridManager.Instance.IsWalkable(targetPos))
            return false;

        // Move
        Vector2Int fromPos = GridPosition;
        GridPosition = targetPos;
        GridManager.Instance.SetOccupant(targetPos, gameObject);
        transform.position = GridManager.Instance.GridToWorld(targetPos);

        OnPlayerMoved?.Invoke(fromPos, targetPos);
        TurnManager.Instance.ProcessPlayerAction();
        return true;
    }

    /// <summary>
    /// Melee attack against an enemy. Delegates to CombatManager.ResolveAttack().
    /// </summary>
    public void MeleeAttack(EnemyController target)
    {
        // Calculate attack bonus from WizardStats
        // Wizards use STR for melee (or DEX for finesse weapons)
        int attackBonus = wizardStats != null
            ? wizardStats.GetModifier(AbilityScore.Strength) + wizardStats.proficiencyBonus
            : 0;

        // Default weapon: quarterstaff (1d6 + STR mod, bludgeoning)
        int damageDice = 1;
        int damageSides = 6;
        int damageBonus = wizardStats != null
            ? wizardStats.GetModifier(AbilityScore.Strength)
            : 0;
        DamageType damageType = DamageType.Bludgeoning;

        // TODO: Use equipped weapon stats from InventoryManager if available

        CombatManager.Instance.ResolveAttack(
            this, target, attackBonus, damageDice, damageSides, damageBonus, damageType
        );

        TurnManager.Instance.ProcessPlayerAction();
    }

    /// <summary>Skips the player's turn.</summary>
    public void Wait()
    {
        TurnManager.Instance.ProcessPlayerAction();
    }

    /// <summary>
    /// IDamageable: Applies damage to WizardStats. Checks for concentration break.
    /// If HP reaches 0, calls GameManager.TriggerPermadeath().
    /// </summary>
    public void TakeDamage(int amount, DamageType type)
    {
        if (wizardStats == null) return;

        // TODO: Apply resistances/immunities from equipment

        wizardStats.currentHP -= amount;
        if (wizardStats.currentHP < 0) wizardStats.currentHP = 0;

        OnPlayerDamaged?.Invoke(amount, wizardStats.currentHP);

        // Concentration check
        if (SpellManager.Instance != null &&
            SpellManager.Instance.ActiveConcentrationSpell != null)
        {
            CombatManager.Instance.CheckConcentration(this, amount);
        }

        // Permadeath
        if (!IsAlive)
        {
            GameManager.Instance.TriggerPermadeath();
        }
    }

    /// <summary>Heals the player.</summary>
    public void Heal(int amount)
    {
        if (wizardStats == null) return;

        wizardStats.currentHP = Mathf.Min(
            wizardStats.currentHP + amount,
            wizardStats.maxHP
        );

        OnPlayerHealed?.Invoke(amount, wizardStats.currentHP);
    }

    /// <summary>
    /// Checks the adjacent tile for interactable objects
    /// (doors, chests, stairs, items). Delegates to appropriate system.
    /// </summary>
    public void Interact()
    {
        // TODO: Determine facing direction or check all adjacent tiles
        // For now, check all 4 cardinal neighbors for interactable tiles
        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Tile tile = GridManager.Instance.GetTile(GridPosition + dir);
            if (tile == null) continue;

            switch (tile.Type)
            {
                case TileType.Door:
                    // TODO: Open the door (change to DoorOpen, set walkable)
                    OnPlayerInteracted?.Invoke(tile);
                    TurnManager.Instance.ProcessPlayerAction();
                    return;

                case TileType.Stairs:
                    // Advance to next floor
                    GameManager.Instance.AdvanceFloor();
                    return;

                case TileType.Campfire:
                    // TODO: Short rest — recover HP, Arcane Recovery
                    OnPlayerInteracted?.Invoke(tile);
                    TurnManager.Instance.ProcessPlayerAction();
                    return;
            }
        }

        // Check current tile for ground items
        Tile currentTile = GridManager.Instance.GetTile(GridPosition);
        if (currentTile != null && currentTile.Items.Count > 0)
        {
            // TODO: InventoryManager.Instance.PickUpFromGround(GridPosition);
            OnPlayerInteracted?.Invoke(currentTile);
            TurnManager.Instance.ProcessPlayerAction();
            return;
        }
    }

    /// <summary>
    /// Places the player at a specific grid position (used by level loading).
    /// </summary>
    public void SetPosition(Vector2Int pos)
    {
        GridPosition = pos;
        GridManager.Instance.SetOccupant(pos, gameObject);
        transform.position = GridManager.Instance.GridToWorld(pos);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Input Handlers
    // ═════════════════════════════════════════════════════════════════════

    private void HandleMoveInput(Vector2Int direction)
    {
        TryMove(direction);
    }

    private void HandleWaitInput()
    {
        Wait();
    }

    private void HandleInteractInput()
    {
        Interact();
    }

    /// <summary>
    /// Routes spell input to SpellManager. PlayerController does not
    /// handle spellcasting logic — that's entirely SpellManager's job.
    /// </summary>
    private void HandleCastSpellInput(int slotIndex)
    {
        SpellManager.Instance?.ActivateSpell(slotIndex);
    }
}