using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the visual target-selection mode when a spell is activated.
/// Highlights valid target tiles based on spell range, AoE shape, and
/// line of sight. Listens for confirm/cancel input to resolve or abort.
///
/// File: Assets/Scripts/Spells/TargetingSystem.cs
/// Layer: 3 (Depends on Layers 0-2)
/// Dependencies: GridManager, FogOfWarManager, InputHandler, GameManager
/// </summary>
public class TargetingSystem : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════
    //  Singleton
    // ═════════════════════════════════════════════════════════════════════

    public static TargetingSystem Instance { get; private set; }

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
    //  Runtime State
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>The spell currently being targeted, or null if not in targeting mode.</summary>
    private SpellData activeSpell;

    /// <summary>Currently highlighted valid target tiles.</summary>
    private readonly List<Vector2Int> highlightedTiles = new();

    /// <summary>True if the targeting system is currently active.</summary>
    public bool IsTargeting => activeSpell != null;

    // ═════════════════════════════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Enters targeting mode for the given spell. Highlights valid target
    /// tiles based on spell range, AoE shape, and line of sight.
    /// Listens for InputHandler.OnConfirmTargetInput and OnCancelInput.
    /// </summary>
    /// <param name="spell">The spell being targeted.</param>
    public void EnterTargeting(SpellData spell)
    {
        activeSpell = spell;
        highlightedTiles.Clear();

        // TODO: Calculate valid tiles based on:
        //   - Player position (from PlayerController.Instance.GridPosition)
        //   - spell.range (in tiles)
        //   - spell.targetType (shape)
        //   - Line of sight via GridManager
        //   - Only visible tiles via FogOfWarManager
        // TODO: Visually highlight the valid tiles on the grid
        // TODO: Subscribe to InputHandler.OnConfirmTargetInput and OnCancelInput

        Debug.Log($"TargetingSystem: Entered targeting for {spell.spellName} " +
                  $"(range {spell.range}, type {spell.targetType})");
    }

    /// <summary>
    /// Exits targeting mode. Clears highlights and returns GameState to Gameplay.
    /// </summary>
    public void ExitTargeting()
    {
        activeSpell = null;
        highlightedTiles.Clear();

        // TODO: Clear visual highlights on the grid
        // TODO: GameManager.Instance.ChangeState(GameState.Gameplay);
        // TODO: Unsubscribe from InputHandler events

        Debug.Log("TargetingSystem: Exited targeting mode.");
    }

    /// <summary>
    /// Returns the list of currently highlighted valid target tiles (for UI rendering).
    /// </summary>
    public List<Vector2Int> GetHighlightedTiles()
    {
        return new List<Vector2Int>(highlightedTiles);
    }

    /// <summary>
    /// Given a target tile, returns all tiles that would be affected by
    /// the current spell's AoE shape.
    /// </summary>
    /// <param name="target">The tile the player is hovering/confirming.</param>
    /// <returns>All tiles within the AoE. For single-target spells, returns just the target.</returns>
    public List<Vector2Int> GetAoETiles(Vector2Int target)
    {
        var affected = new List<Vector2Int>();

        if (activeSpell == null)
            return affected;

        switch (activeSpell.targetType)
        {
            case TargetType.Self:
            case TargetType.Touch:
            case TargetType.SingleTile:
                affected.Add(target);
                break;

            case TargetType.Sphere:
                // All tiles within aoeRadius of the target
                for (int x = -activeSpell.aoeRadius; x <= activeSpell.aoeRadius; x++)
                {
                    for (int y = -activeSpell.aoeRadius; y <= activeSpell.aoeRadius; y++)
                    {
                        Vector2Int tile = target + new Vector2Int(x, y);
                        // Use Euclidean distance for circles
                        if (Mathf.Sqrt(x * x + y * y) <= activeSpell.aoeRadius)
                        {
                            // TODO: Check IsInBounds via GridManager
                            affected.Add(tile);
                        }
                    }
                }
                break;

            case TargetType.Cube:
                // All tiles within a square of side length = aoeRadius * 2 + 1
                int halfSide = activeSpell.aoeRadius;
                for (int x = -halfSide; x <= halfSide; x++)
                {
                    for (int y = -halfSide; y <= halfSide; y++)
                    {
                        Vector2Int tile = target + new Vector2Int(x, y);
                        // TODO: Check IsInBounds via GridManager
                        affected.Add(tile);
                    }
                }
                break;

            case TargetType.Line:
                // TODO: Requires direction from caster to target.
                // Trace tiles along the line for spell.range tiles.
                affected.Add(target);
                break;

            case TargetType.Cone:
                // TODO: Requires direction from caster.
                // Fan out tiles in a cone shape for spell.coneLength tiles.
                affected.Add(target);
                break;
        }

        return affected;
    }
}