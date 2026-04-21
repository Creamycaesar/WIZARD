using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Encapsulates the decision-making logic for an enemy's turn.
/// Plain C# class (not a MonoBehaviour) — strategy pattern.
/// Uses a simple priority-based decision tree per the tech arch.
///
/// File: Assets/Scripts/Enemies/EnemyAI.cs
/// Dependencies: GridManager (pathfinding, line of sight),
///               PlayerController (to know player position)
/// </summary>
public class EnemyAI
{
    /// <summary>
    /// Decides and executes an action for the given enemy.
    /// Called by EnemyController.TakeTurn().
    ///
    /// Priority order:
    /// 1. If HP below retreat threshold and escape route exists → retreat
    /// 2. If adjacent to player → use best available attack
    /// 3. If within movement range of player → move toward and attack if adjacent
    /// 4. If player is visible but out of range → move toward player (A* pathfinding)
    /// 5. If player is not visible → patrol (random movement or guard position)
    /// </summary>
    public void DecideAction(EnemyController enemy)
    {
        if (PlayerController.Instance == null || !PlayerController.Instance.IsAlive)
            return;

        Vector2Int enemyPos = enemy.GridPosition;
        Vector2Int playerPos = PlayerController.Instance.GridPosition;
        int distance = ManhattanDistance(enemyPos, playerPos);
        EnemyData data = enemy.Data;

        // Priority 1: Retreat if low HP
        float hpPercent = (float)enemy.CurrentHP / data.maxHP;
        if (hpPercent <= data.retreatThreshold && data.retreatThreshold > 0f)
        {
            if (TryRetreat(enemy, playerPos))
                return;
        }

        // Priority 2: Adjacent to player — attack
        if (distance <= 1)
        {
            AttackPlayer(enemy, distance);
            return;
        }

        // Priority 3: Within movement range — move and attack if possible
        if (distance <= data.movementSpeed + 1)
        {
            MoveToward(enemy, playerPos);
            // Check if now adjacent after moving
            int newDistance = ManhattanDistance(enemy.GridPosition, playerPos);
            if (newDistance <= 1)
            {
                AttackPlayer(enemy, newDistance);
            }
            return;
        }

        // Priority 4: Player visible but out of range — move toward
        if (distance <= data.aggroRadius)
        {
            MoveToward(enemy, playerPos);
            return;
        }

        // Priority 5: Player not visible — patrol
        Patrol(enemy);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Actions
    // ═════════════════════════════════════════════════════════════════════

    private void AttackPlayer(EnemyController enemy, int distance)
    {
        EnemyAttack? bestAttack = enemy.Data.GetBestAttackAtRange(distance);
        if (bestAttack == null) return;

        var attack = bestAttack.Value;
        CombatManager.Instance.ResolveAttack(
            enemy,
            PlayerController.Instance,
            attack.attackBonus,
            attack.damageDiceCount,
            attack.damageDiceSides,
            attack.damageBonus,
            attack.damageType
        );

        // Handle multiattack
        if (enemy.Data.hasMultiattack)
        {
            for (int i = 1; i < enemy.Data.multiattackCount; i++)
            {
                if (!PlayerController.Instance.IsAlive) break;

                CombatManager.Instance.ResolveAttack(
                    enemy,
                    PlayerController.Instance,
                    attack.attackBonus,
                    attack.damageDiceCount,
                    attack.damageDiceSides,
                    attack.damageBonus,
                    attack.damageType
                );
            }
        }
    }

    private void MoveToward(EnemyController enemy, Vector2Int target)
    {
        // Simple greedy movement — move in the direction that reduces distance most
        // TODO: Replace with A* pathfinding via Pathfinding.cs
        Vector2Int bestMove = enemy.GridPosition;
        int bestDist = ManhattanDistance(enemy.GridPosition, target);

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Vector2Int candidate = enemy.GridPosition + dir;
            if (!GridManager.Instance.IsWalkable(candidate)) continue;

            int dist = ManhattanDistance(candidate, target);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestMove = candidate;
            }
        }

        if (bestMove != enemy.GridPosition)
        {
            enemy.MoveTo(bestMove);
        }
    }

    private bool TryRetreat(EnemyController enemy, Vector2Int playerPos)
    {
        // Move away from player — pick the direction that maximizes distance
        Vector2Int bestMove = enemy.GridPosition;
        int bestDist = ManhattanDistance(enemy.GridPosition, playerPos);

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Vector2Int candidate = enemy.GridPosition + dir;
            if (!GridManager.Instance.IsWalkable(candidate)) continue;

            int dist = ManhattanDistance(candidate, playerPos);
            if (dist > bestDist)
            {
                bestDist = dist;
                bestMove = candidate;
            }
        }

        if (bestMove != enemy.GridPosition)
        {
            enemy.MoveTo(bestMove);
            return true;
        }

        return false; // Cornered — no escape route
    }

    private void Patrol(EnemyController enemy)
    {
        // Random movement to an adjacent walkable tile
        List<Tile> neighbors = GridManager.Instance.GetNeighbors(enemy.GridPosition);
        if (neighbors.Count > 0)
        {
            Tile chosen = neighbors[Dice.Roll(neighbors.Count) - 1];
            enemy.MoveTo(chosen.GridPos);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Utility
    // ═════════════════════════════════════════════════════════════════════

    private int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}