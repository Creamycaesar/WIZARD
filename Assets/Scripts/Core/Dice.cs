using UnityEngine;

/// <summary>
/// Static utility for all dice rolls in WIZARD.
/// Every d20 roll, damage roll, and saving throw goes through here.
/// This ensures a single place to swap in weighted/loaded dice for testing,
/// or to add roll history logging later.
///
/// Usage:
///   int roll    = Dice.Roll(20);          // 1d20
///   int damage  = Dice.Roll(2, 6, 3);     // 2d6 + 3
///   int check   = Dice.Roll(20) + modifier;
/// </summary>
public static class Dice
{
    /// <summary>
    /// Rolls a single die with the given number of sides.
    /// e.g. Dice.Roll(6) → 1d6 → result between 1 and 6.
    /// </summary>
    public static int Roll(int sides)
    {
        if (sides < 1)
        {
            Debug.LogWarning($"Dice.Roll: invalid die size ({sides}). Returning 1.");
            return 1;
        }
        return Random.Range(1, sides + 1);
    }

    /// <summary>
    /// Rolls multiple dice and adds a flat bonus.
    /// e.g. Dice.Roll(2, 6, 3) → 2d6+3.
    /// </summary>
    public static int Roll(int count, int sides, int bonus = 0)
    {
        int total = 0;
        for (int i = 0; i < count; i++)
            total += Roll(sides);
        return total + bonus;
    }

    /// <summary>
    /// Rolls a d20. Used for attack rolls, saving throws, and skill checks.
    /// </summary>
    public static int D20() => Roll(20);

    /// <summary>
    /// Rolls with advantage: roll 2d20, take the higher result.
    /// </summary>
    public static int Advantage() => Mathf.Max(Roll(20), Roll(20));

    /// <summary>
    /// Rolls with disadvantage: roll 2d20, take the lower result.
    /// </summary>
    public static int Disadvantage() => Mathf.Min(Roll(20), Roll(20));

    /// <summary>
    /// Returns true if a d20 roll + modifier meets or beats the DC.
    /// The standard 5e "does this succeed?" check.
    /// </summary>
    public static bool Check(int modifier, int dc) => (D20() + modifier) >= dc;
}