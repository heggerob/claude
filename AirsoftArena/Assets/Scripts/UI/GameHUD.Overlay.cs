using UnityEngine;

namespace AirsoftArena
{
    /// <summary>In-match overlays: crosshair with hit marker, and the minimap.</summary>
    public partial class GameHUD
    {
        const float MinimapWidth = 190f;
        const float SpotRange = 30f;

        static float hitMarkerAt = -10f;
        static bool hitMarkerEnemy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOverlayStatics() { hitMarkerAt = -10f; }

        /// <summary>Called when the human's BB (or knife) touches another soldier.</summary>
        public static void RegisterHitMarker(bool enemy)
        {
            hitMarkerAt = Time.time;
            hitMarkerEnemy = enemy;
            Sfx.Play(SfxId.ReelTick, 0.45f, enemy ? 1.3f : 0.8f);
        }

        void DrawCrosshair(MatchManager match)
        {
            var s = match.PlayerSoldier;
            bool show = s != null && s.InPlay && match.Phase == MatchPhase.Playing && !match.Paused;
            Cursor.visible = !show;
            if (!show || Event.current.type != EventType.Repaint) return;

            Vector2 mouse = GameInput.MousePosition();
            var m = new Vector2(mouse.x / scale, (Screen.height - mouse.y) / scale);

            // The gap opens up when moving and closes when crouched, matching the real spread.
            var d = s.Weapon.Data;
            float spread = d.IsMelee ? 0.5f : d.spreadDegrees;
            if (s.Crouching) spread *= 0.6f;
            if (s.Sprinting) spread *= 3f;
            float gap = 5f + spread * 5f;
            var c = s.CanShoot ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.35f);
            Line(new Rect(m.x - gap - 8f, m.y - 1f, 8f, 2f), c);
            Line(new Rect(m.x + gap, m.y - 1f, 8f, 2f), c);
            Line(new Rect(m.x - 1f, m.y - gap - 8f, 2f, 8f), c);
            Line(new Rect(m.x - 1f, m.y + gap, 2f, 8f), c);
            Line(new Rect(m.x - 1f, m.y - 1f, 2f, 2f), c);

            // Hit marker: a quick X, orange if you hit a teammate.
            float age = Time.time - hitMarkerAt;
            if (age < 0.3f)
            {
                var hc = hitMarkerEnemy ? Color.white : new Color(1f, 0.55f, 0.2f);
                hc.a = 1f - age / 0.3f;
                float r = 7f + age * 20f;
                var old = GUI.matrix;
                for (int i = 0; i < 4; i++)
                {
                    GUIUtility.RotateAroundPivot(45f + 90f * i, m * scale);
                    Line(new Rect(m.x + r, m.y - 1.5f, 9f, 3f), hc);
                    GUI.matrix = old;
                }
            }
        }

        void Line(Rect r, Color c)
        {
            GUI.DrawTexture(r, whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        }

        void DrawMinimap(MatchManager match)
        {
            var map = MapBuilder.Current;
            if (map == null || map.trainingOnly) return;
            var tex = Minimap.For(map);
            float w = MinimapWidth, h = w * map.bounds.height / map.bounds.width;
            var area = new Rect(8f, match.Referee != null ? 64f : 8f, w, h);
            GUI.Box(new Rect(area.x - 3f, area.y - 3f, w + 6f, h + 6f), GUIContent.none, panel);
            GUI.DrawTexture(area, tex);
            if (Event.current.type != EventType.Repaint) return;

            var me = match.PlayerSoldier;
            Team myTeam = me != null ? me.Team : Team.Blue;
            bool referee = match.PlayerReferee != null;

            // Objectives.
            var ctf = match.Rules as CaptureTheFlagRules;
            if (ctf != null)
                foreach (var f in ctf.Flags) Dot(area, map, f.position, Teams.Color(f.team), 7f, true);
            var koth = match.Rules as KingOfTheHillRules;
            if (koth != null)
            {
                var hc = koth.Holder.HasValue ? Teams.Color(koth.Holder.Value) : new Color(1f, 0.85f, 0.3f);
                hc.a = 0.5f;
                Dot(area, map, map.hill, hc, map.hillRadius * 2f * w / map.bounds.width, false);
            }

            foreach (var s in match.Soldiers)
            {
                if (s == me) continue;
                bool friendly = s.Team == myTeam;
                // Enemies only show up when you can actually see them (the referee sees everyone).
                if (!friendly && !referee)
                {
                    if (me == null || Vector2.Distance(me.Position, s.Position) > SpotRange || !MatchManager.HasLineOfSight(me.Position, s.Position, false)) continue;
                }
                var c = Teams.Color(s.Team);
                if (!s.InPlay) c.a = 0.4f;
                Dot(area, map, s.Position, c, 4f, false);
            }
            if (match.Referee != null) Dot(area, map, match.Referee.Position, new Color(1f, 0.92f, 0.2f), referee ? 6f : 4f, referee);
            if (me != null) Dot(area, map, me.Position, Color.white, 6f, true);
        }

        void Dot(Rect area, MapDefinition map, Vector2 world, Color c, float size, bool outline)
        {
            var n = Minimap.Normalized(map, world);
            float x = area.x + n.x * area.width, y = area.yMax - n.y * area.height;
            if (outline) Line(new Rect(x - size / 2f - 1f, y - size / 2f - 1f, size + 2f, size + 2f), Color.black);
            Line(new Rect(x - size / 2f, y - size / 2f, size, size), c);
        }
    }
}
