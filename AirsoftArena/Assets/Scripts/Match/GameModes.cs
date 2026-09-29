using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    public enum GameMode { TeamDeathmatch, CaptureTheFlag, KingOfTheHill }

    public static class GameModes
    {
        public static string Name(GameMode m)
        {
            switch (m)
            {
                case GameMode.CaptureTheFlag: return "Capture the Flag";
                case GameMode.KingOfTheHill: return "King of the Hill";
                default: return "Team Deathmatch";
            }
        }

        public static string Description(GameMode m)
        {
            switch (m)
            {
                case GameMode.CaptureTheFlag: return "Grab the enemy flag and carry it to your own. Get hit while carrying it and you drop it. First to 3 captures.";
                case GameMode.KingOfTheHill: return "Hold the zone in the middle. Only one team inside = 1 point per second. First to 100.";
                default: return "Every called hit is a point. First to 20.";
            }
        }

        public static int ScoreLimit(GameMode m)
        {
            switch (m)
            {
                case GameMode.CaptureTheFlag: return 3;
                case GameMode.KingOfTheHill: return 100;
                default: return 20;
            }
        }

        public static string Unit(GameMode m)
        {
            switch (m)
            {
                case GameMode.CaptureTheFlag: return "captures";
                case GameMode.KingOfTheHill: return "points";
                default: return "hits";
            }
        }

        public static ModeRules Create(GameMode m)
        {
            switch (m)
            {
                case GameMode.CaptureTheFlag: return new CaptureTheFlagRules();
                case GameMode.KingOfTheHill: return new KingOfTheHillRules();
                default: return new TeamDeathmatchRules();
            }
        }
    }

    /// <summary>What a game mode adds on top of the shared hit / referee rules.</summary>
    public abstract class ModeRules
    {
        protected MatchManager match;
        protected Transform root;

        /// <summary>Whether a called hit gives the shooter's team a point.</summary>
        public virtual bool HitsScore { get { return false; } }

        public virtual void Begin(MatchManager m, Transform parent)
        {
            match = m;
            root = new GameObject("Mode Objects").transform;
            root.SetParent(parent, false);
        }

        public virtual void Tick(float dt) { }
        public virtual void OnSoldierOut(Soldier s) { }

        /// <summary>Where a bot should go for the objective. Urgent = go there even while fighting.</summary>
        public virtual bool BotObjective(Soldier s, out Vector2 point, out bool urgent)
        {
            point = Vector2.zero;
            urgent = false;
            return false;
        }

        /// <summary>A short line for the player's HUD, or null.</summary>
        public virtual string PlayerHint(Soldier s) { return null; }

        protected static SpriteRenderer Renderer(string name, Transform parent, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }

    public class TeamDeathmatchRules : ModeRules
    {
        public override bool HitsScore { get { return true; } }
    }

    // ==================================================================== Capture the Flag

    public class CaptureTheFlagRules : ModeRules
    {
        const float PickupRadius = 0.9f;
        const float CaptureRadius = 1.5f;
        const float AutoReturnTime = 20f;

        public class Flag
        {
            public Team team;
            public Vector2 home;
            public Vector2 position;
            public Soldier carrier;
            public float droppedAt;
            public bool AtHome { get { return carrier == null && Vector2.Distance(position, home) < 0.01f; } }
            public Transform visual;
        }

        public readonly Flag[] Flags = new Flag[2];

        public override void Begin(MatchManager m, Transform parent)
        {
            base.Begin(m, parent);
            var map = MapBuilder.Current;
            for (int t = 0; t < 2; t++)
            {
                var team = (Team)t;
                var f = new Flag { team = team, home = map.flagPoints[t], position = map.flagPoints[t] };
                var pad = Renderer(Teams.Name(team) + " Flag Base", root, SpriteFactory.Ring, Teams.Color(team) * 0.9f, -14);
                pad.transform.position = f.home;
                pad.transform.localScale = Vector3.one * (CaptureRadius * 2f / 4f);

                f.visual = new GameObject(Teams.Name(team) + " Flag").transform;
                f.visual.SetParent(root, false);
                var pole = Renderer("Pole", f.visual, SpriteFactory.Pixel, new Color(0.15f, 0.12f, 0.1f), 16);
                pole.transform.localScale = new Vector3(0.08f, 0.9f, 1f);
                pole.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                var cloth = Renderer("Cloth", f.visual, SpriteFactory.Pixel, Teams.Color(team), 17);
                cloth.transform.localScale = new Vector3(0.5f, 0.32f, 1f);
                cloth.transform.localPosition = new Vector3(0.27f, 0.58f, 0f);
                Flags[t] = f;
            }
        }

        public override void Tick(float dt)
        {
            float now = Time.time;
            foreach (var f in Flags)
            {
                if (f.carrier != null)
                {
                    if (!f.carrier.InPlay) Drop(f);
                    else f.position = f.carrier.Position;
                }
                else
                {
                    foreach (var s in match.Soldiers)
                    {
                        if (!s.InPlay || Vector2.Distance(s.Position, f.position) > PickupRadius) continue;
                        if (s.Team != f.team && Carrying(s) == null)
                        {
                            f.carrier = s;
                            match.AddFeed(MatchManager.Colored(s) + " grabbed the <color=" + Teams.Hex(f.team) + ">" + Teams.Name(f.team) + " flag</color>!");
                            break;
                        }
                        if (s.Team == f.team && !f.AtHome)
                        {
                            f.position = f.home;
                            s.Stats.flagReturns++;
                            match.AddFeed(MatchManager.Colored(s) + " returned the <color=" + Teams.Hex(f.team) + ">" + Teams.Name(f.team) + " flag</color>");
                            break;
                        }
                    }
                    if (f.carrier == null && !f.AtHome && now - f.droppedAt > AutoReturnTime)
                    {
                        f.position = f.home;
                        match.AddFeed("The <color=" + Teams.Hex(f.team) + ">" + Teams.Name(f.team) + " flag</color> returned to base");
                    }
                }

                // Capture: carrying the enemy flag into your own base while your flag is home.
                if (f.carrier != null)
                {
                    var own = Flags[(int)f.carrier.Team];
                    if (own.AtHome && Vector2.Distance(f.carrier.Position, own.home) < CaptureRadius)
                    {
                        var c = f.carrier;
                        match.Score[(int)c.Team]++;
                        c.Stats.captures++;
                        match.AddFeed("<b>" + MatchManager.Colored(c) + " CAPTURED the <color=" + Teams.Hex(f.team) + ">" + Teams.Name(f.team) + " flag</color>!</b>");
                        Effects.Text(c.Position + new Vector2(0f, 1.2f), "CAPTURE!", Teams.Color(c.Team), 2f, 22);
                        Sfx.Play(SfxId.Capture, 0.8f);
                        f.carrier = null;
                        f.position = f.home;
                    }
                }

                Vector2 offset = f.carrier != null ? new Vector2(0.25f, 0.25f) : Vector2.zero;
                f.visual.position = f.position + offset;
            }
        }

        void Drop(Flag f)
        {
            match.AddFeed(MatchManager.Colored(f.carrier) + " dropped the <color=" + Teams.Hex(f.team) + ">" + Teams.Name(f.team) + " flag</color>");
            f.position = f.carrier.Position;
            f.carrier = null;
            f.droppedAt = Time.time;
        }

        public override void OnSoldierOut(Soldier s)
        {
            var f = Carrying(s);
            if (f != null) Drop(f);
        }

        public Flag Carrying(Soldier s)
        {
            foreach (var f in Flags) if (f.carrier == s) return f;
            return null;
        }

        public override bool BotObjective(Soldier s, out Vector2 point, out bool urgent)
        {
            var own = Flags[(int)s.Team];
            var enemy = Flags[1 - (int)s.Team];
            urgent = false;

            if (Carrying(s) != null) { point = own.home; urgent = true; return true; }
            // Someone dropped our flag nearby: go pick it up.
            if (!own.AtHome && own.carrier == null && Vector2.Distance(s.Position, own.position) < 16f) { point = own.position; urgent = true; return true; }
            // Our flag is being carried: hunt the carrier.
            if (own.carrier != null) { point = own.carrier.Position; return true; }

            int index = match.Soldiers.IndexOf(s);
            if (index % 3 == 0)
            {
                // Defender: hang around our flag.
                point = own.home + new Vector2(Mathf.Sin(index * 1.7f), Mathf.Cos(index * 2.3f)) * 4f;
                return true;
            }
            point = enemy.carrier != null ? enemy.carrier.Position : enemy.position;
            return true;
        }

        public override string PlayerHint(Soldier s)
        {
            var own = Flags[(int)s.Team];
            if (Carrying(s) != null)
                return own.AtHome ? "You have the flag! Run it back to your base" : "You have the flag, but they have yours... get it back first!";
            if (own.carrier != null) return "<color=#ff8866>Your flag has been taken! Stop " + own.carrier.DisplayName + "!</color>";
            if (!own.AtHome) return "Your flag is on the ground: touch it to return it";
            return null;
        }
    }

    // ==================================================================== King of the Hill

    public class KingOfTheHillRules : ModeRules
    {
        readonly float[] progress = new float[2];
        SpriteRenderer ring, fill;
        readonly Dictionary<Soldier, Vector2> spots = new Dictionary<Soldier, Vector2>();

        /// <summary>Team holding the hill alone, or null when empty or contested.</summary>
        public Team? Holder { get; private set; }
        public bool Contested { get; private set; }

        public override void Begin(MatchManager m, Transform parent)
        {
            base.Begin(m, parent);
            var map = MapBuilder.Current;
            float d = map.hillRadius * 2f;
            fill = Renderer("Hill", root, SpriteFactory.Disc64, new Color(1f, 1f, 1f, 0.1f), -14);
            fill.transform.position = map.hill;
            fill.transform.localScale = Vector3.one * (d / 4f);
            ring = Renderer("Hill Ring", root, SpriteFactory.Ring, Color.white, -13);
            ring.transform.position = map.hill;
            ring.transform.localScale = Vector3.one * (d / 4f);
        }

        public override void Tick(float dt)
        {
            var map = MapBuilder.Current;
            bool blue = false, red = false;
            foreach (var s in match.Soldiers)
            {
                if (!s.InPlay || Vector2.Distance(s.Position, map.hill) > map.hillRadius) continue;
                if (s.Team == Team.Blue) blue = true; else red = true;
            }
            Contested = blue && red;
            Holder = Contested || (!blue && !red) ? (Team?)null : blue ? Team.Blue : Team.Red;

            if (Holder.HasValue)
            {
                int t = (int)Holder.Value;
                progress[t] += dt;
                while (progress[t] >= 1f)
                {
                    progress[t] -= 1f;
                    match.Score[t]++;
                }
                foreach (var s in match.Soldiers)
                    if (s.Team == Holder.Value && s.InPlay && Vector2.Distance(s.Position, map.hill) <= map.hillRadius) s.Stats.hillSeconds += dt;
            }

            Color c = Contested ? (Mathf.Repeat(Time.time * 3f, 1f) < 0.5f ? Color.white : new Color(1f, 0.85f, 0.3f))
                : Holder.HasValue ? Teams.Color(Holder.Value) : new Color(1f, 1f, 1f, 0.7f);
            ring.color = c;
            c.a = 0.14f;
            fill.color = c;
        }

        public override bool BotObjective(Soldier s, out Vector2 point, out bool urgent)
        {
            var map = MapBuilder.Current;
            Vector2 spot;
            if (!spots.TryGetValue(s, out spot))
            {
                spot = map.hill + Random.insideUnitCircle * map.hillRadius * 0.6f;
                spots[s] = spot;
            }
            point = spot;
            // Once the fight is on, bots still drift onto the hill.
            urgent = Vector2.Distance(s.Position, map.hill) > map.hillRadius;
            return true;
        }

        public override string PlayerHint(Soldier s)
        {
            var map = MapBuilder.Current;
            bool onHill = s.InPlay && Vector2.Distance(s.Position, map.hill) <= map.hillRadius;
            if (Contested) return onHill ? "<color=#ffd060>HILL CONTESTED: clear it!</color>" : "The hill is contested";
            if (Holder.HasValue) return Holder.Value == s.Team ? (onHill ? "Holding the hill" : "Your team holds the hill") : "<color=#ff8866>" + Teams.Name(Holder.Value) + " holds the hill!</color>";
            return onHill ? "Holding the hill" : "The hill is empty: take it!";
        }
    }
}
