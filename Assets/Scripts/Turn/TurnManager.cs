using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the turn loop. The player acts (implicit turn), then all world actors
/// take their turns in Dexterity order. No manager dependencies — called by
/// PlayerController via ProcessPlayerAction(), calls ITurnActor.TakeTurn() on
/// registered actors. Communication flows through events.
///
/// File: Assets/Scripts/Turn/TurnManager.cs
/// Layer: 0 (Foundation — no manager dependencies)
/// </summary>
public class TurnManager : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════
    //  Singleton
    // ═════════════════════════════════════════════════════════════════════

    public static TurnManager Instance { get; private set; }

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

    /// <summary>After the world turn completes. The player can now act.</summary>
    public static event Action OnPlayerTurnStarted;

    /// <summary>After the player takes an action, before world actors move.</summary>
    public static event Action OnPlayerTurnEnded;

    /// <summary>After all world actors have taken their turns.</summary>
    public static event Action<int> OnWorldTurnEnded;

    /// <summary>Before each individual world actor takes its turn.</summary>
    public static event Action<ITurnActor> OnActorTurnStarted;

    /// <summary>After each individual world actor finishes its turn.</summary>
    public static event Action<ITurnActor> OnActorTurnEnded;

    // ═════════════════════════════════════════════════════════════════════
    //  State
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Current turn number, incremented after each world turn.</summary>
    public int TurnNumber { get; private set; }

    private readonly List<ITurnActor> worldActors = new();
    private bool actorListDirty;

    // ═════════════════════════════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a world actor (enemy) for turn processing.
    /// Actors are sorted by Dexterity descending, then SpawnOrder ascending.
    /// </summary>
    public void RegisterActor(ITurnActor actor)
    {
        if (!worldActors.Contains(actor))
        {
            worldActors.Add(actor);
            actorListDirty = true;
        }
    }

    /// <summary>Removes an actor (on death or despawn).</summary>
    public void UnregisterActor(ITurnActor actor)
    {
        worldActors.Remove(actor);
    }

    /// <summary>
    /// Called by PlayerController after the player completes any action.
    /// Fires OnPlayerTurnEnded, iterates all world actors in Dex order
    /// calling TakeTurn(), fires OnWorldTurnEnded, increments TurnNumber,
    /// and fires OnPlayerTurnStarted.
    /// </summary>
    public void ProcessPlayerAction()
    {
        OnPlayerTurnEnded?.Invoke();

        // Sort actors if the list changed
        if (actorListDirty)
        {
            SortActors();
            actorListDirty = false;
        }

        // World turn: each actor takes their turn in Dex order
        // Iterate a copy in case an actor dies and unregisters mid-loop
        var actorsThisTurn = new List<ITurnActor>(worldActors);
        foreach (var actor in actorsThisTurn)
        {
            // Actor may have been removed (killed) by a previous actor this turn
            if (!worldActors.Contains(actor)) continue;

            OnActorTurnStarted?.Invoke(actor);
            actor.TakeTurn();
            OnActorTurnEnded?.Invoke(actor);
        }

        TurnNumber++;
        OnWorldTurnEnded?.Invoke(TurnNumber);
        OnPlayerTurnStarted?.Invoke();
    }

    /// <summary>
    /// Returns all registered actors sorted by Dexterity descending,
    /// then SpawnOrder ascending. Read-only for UI/debug.
    /// </summary>
    public List<ITurnActor> GetWorldActors()
    {
        if (actorListDirty)
        {
            SortActors();
            actorListDirty = false;
        }
        return new List<ITurnActor>(worldActors);
    }

    /// <summary>Removes all world actors. Called on level change.</summary>
    public void ClearAllActors()
    {
        worldActors.Clear();
        actorListDirty = false;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Internal
    // ═════════════════════════════════════════════════════════════════════

    private void SortActors()
    {
        worldActors.Sort((a, b) =>
        {
            int dexCompare = b.Dexterity.CompareTo(a.Dexterity); // Descending
            if (dexCompare != 0) return dexCompare;
            return a.SpawnOrder.CompareTo(b.SpawnOrder);          // Ascending
        });
    }
}