using UnityEngine;

// State for the current run, kept across scene reloads between levels, plus the saved high score.
// Score = sum of (kill points x level when killed) - DamagePenalty per heart of damage taken (never below 0).
public static class GameSession
{
    public const int StartingHearts = 5;
    public const int MaxHeartsCap = 10;
    public const int DamagePenalty = 100;

    const string HighScoreKey = "HighScore";
    const string BestLevelKey = "BestLevel";

    public static int Level { get; private set; } = 1;
    public static int Kills { get; private set; }
    public static int DamageTaken { get; private set; }
    public static int KillPoints { get; private set; }
    public static int MaxHearts { get; private set; } = StartingHearts;
    // Index of the sword hiding spot used last level, so the next level picks a different one.
    public static int LastSwordSpot { get; set; } = -1;
    // Room index of the boss's lair last level, so the next level picks a different corner.
    public static int LastBossRoom { get; set; } = -1;
    public static bool RunEnded { get; private set; }

    public static int Score => Mathf.Max(0, KillPoints - DamageTaken * DamagePenalty);
    public static int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);
    public static int BestLevel => PlayerPrefs.GetInt(BestLevelKey, 0);

    // Reset when entering Play mode, even with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad() => StartNewRun();

    public static void StartNewRun()
    {
        Level = 1;
        Kills = 0;
        DamageTaken = 0;
        KillPoints = 0;
        MaxHearts = StartingHearts;
        LastSwordSpot = -1;
        LastBossRoom = -1;
        RunEnded = false;
    }

#if UNITY_EDITOR
    // Editor-only: jump to a level for testing and tuning.
    public static void SetLevelForTesting(int level) => Level = Mathf.Max(1, level);
#endif

    public static void AddKill(int basePoints)
    {
        if (RunEnded) return;
        Kills++;
        KillPoints += basePoints * Level;
    }

    public static void AddDamage(int hearts)
    {
        if (RunEnded) return;
        DamageTaken += hearts;
    }

    // Called after the boss falls: next level, hearts refill and grow.
    public static void AdvanceLevel()
    {
        Level++;
        MaxHearts = Mathf.Min(MaxHeartsCap, MaxHearts + 1);
        SaveBest();
    }

    // Ends the run (death or quitting) and records the high score. Returns true if it's a new high score.
    public static bool EndRun()
    {
        if (RunEnded) return false;
        RunEnded = true;
        return SaveBest();
    }

    static bool SaveBest()
    {
        bool newHigh = Score > HighScore;
        if (newHigh) PlayerPrefs.SetInt(HighScoreKey, Score);
        if (Level > BestLevel) PlayerPrefs.SetInt(BestLevelKey, Level);
        PlayerPrefs.Save();
        return newHigh;
    }
}
