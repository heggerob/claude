using System;
using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// XP, ranks and the daily bonus. Ranks unlock the right to buy better weapons,
    /// and every rank-up pays a cash reward.
    /// </summary>
    public static class Progression
    {
        public static readonly string[] RankNames =
        {
            "Recruit", "Private", "Corporal", "Sergeant", "Staff Sergeant",
            "Lieutenant", "Captain", "Major", "Colonel", "General",
        };

        public static int MaxLevel { get { return RankNames.Length; } }

        /// <summary>Total XP needed to reach a level (level 1 = 0 XP). 250, 750, 1500, 2500...</summary>
        public static int XpForLevel(int level)
        {
            level = Mathf.Clamp(level, 1, MaxLevel);
            int n = level - 1;
            return 250 * n * (n + 1) / 2;
        }

        public static int LevelForXp(int xp)
        {
            int level = 1;
            while (level < MaxLevel && xp >= XpForLevel(level + 1)) level++;
            return level;
        }

        public static string RankName(int level) { return RankNames[Mathf.Clamp(level, 1, MaxLevel) - 1]; }

        /// <summary>Progress 0..1 towards the next level (1 at max rank).</summary>
        public static float LevelProgress(int xp)
        {
            int level = LevelForXp(xp);
            if (level >= MaxLevel) return 1f;
            int from = XpForLevel(level), to = XpForLevel(level + 1);
            return (xp - from) / (float)(to - from);
        }

        public static int RankUpReward(int newLevel) { return 100 * newLevel; }

        /// <summary>Adds XP, pays rank-up rewards and returns the levels gained.</summary>
        public static int AddXp(PlayerProfile profile, int amount, List<string> lines)
        {
            int before = LevelForXp(profile.xp);
            profile.xp += Mathf.Max(0, amount);
            int after = LevelForXp(profile.xp);
            for (int level = before + 1; level <= after; level++)
            {
                int reward = RankUpReward(level);
                profile.money += reward;
                if (lines != null) lines.Add("RANK UP: " + RankName(level) + "!  +$" + reward);
            }
            return after - before;
        }

        /// <summary>XP for playing a match as a soldier.</summary>
        public static int SoldierXp(SoldierStats s, bool won, bool draw, List<string> lines)
        {
            int xp = 0;
            xp += Line(lines, 100, "Match played");
            if (won) xp += Line(lines, 60, "Victory");
            else if (draw) xp += Line(lines, 30, "Draw");
            xp += Line(lines, s.pointsScored * 12, "Hits x" + s.pointsScored);
            xp += Line(lines, s.hitsCalled * 8, "Honest hit calls x" + s.hitsCalled);
            xp += Line(lines, s.captures * 60, "Flag captures x" + s.captures);
            xp += Line(lines, s.flagReturns * 20, "Flag returns x" + s.flagReturns);
            xp += Line(lines, Mathf.FloorToInt(s.hillSeconds), "Seconds on the hill");
            return xp;
        }

        /// <summary>XP for a referee shift.</summary>
        public static int RefereeXp(int correct, int wrong, List<string> lines)
        {
            int xp = 0;
            xp += Line(lines, 120, "Referee shift");
            xp += Line(lines, correct * 25, "Correct calls x" + correct);
            if (wrong > 0) xp -= Mathf.Min(xp - 50, wrong * 15);
            return Mathf.Max(50, xp);
        }

        static int Line(List<string> lines, int xp, string label)
        {
            if (xp > 0 && lines != null) lines.Add(label + "  +" + xp + " XP");
            return xp;
        }

        // ---------------------------------------------------------------- daily bonus

        public const int MaxStreak = 7;

        public static string DayKey(DateTime day) { return day.ToString("yyyy-MM-dd"); }

        public static bool DailyAvailable(PlayerProfile profile, DateTime today)
        {
            return profile.lastDailyDay != DayKey(today);
        }

        /// <summary>What today's bonus would pay: grows with the streak of days in a row.</summary>
        public static int DailyAmount(int streak) { return 50 + 25 * (Mathf.Clamp(streak, 1, MaxStreak) - 1); }

        /// <summary>Claims the daily bonus. Returns the amount, or 0 if already claimed today.</summary>
        public static int ClaimDaily(PlayerProfile profile, DateTime today)
        {
            if (!DailyAvailable(profile, today)) return 0;
            bool continued = profile.lastDailyDay == DayKey(today.AddDays(-1));
            profile.dailyStreak = continued ? Mathf.Min(profile.dailyStreak + 1, MaxStreak) : 1;
            profile.lastDailyDay = DayKey(today);
            int amount = DailyAmount(profile.dailyStreak);
            profile.money += amount;
            return amount;
        }
    }
}
