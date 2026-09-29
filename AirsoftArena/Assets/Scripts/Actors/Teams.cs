using UnityEngine;

namespace AirsoftArena
{
    public enum Team { Blue = 0, Red = 1 }

    public static class Teams
    {
        public static Color Color(Team team)
        {
            return team == Team.Blue ? new Color(0.3f, 0.55f, 1f) : new Color(1f, 0.33f, 0.28f);
        }

        public static string Name(Team team) { return team == Team.Blue ? "BLUE" : "RED"; }

        public static Team Other(Team team) { return team == Team.Blue ? Team.Red : Team.Blue; }

        public static string Hex(Team team) { return team == Team.Blue ? "#5a8cff" : "#ff5a4a"; }
    }
}
