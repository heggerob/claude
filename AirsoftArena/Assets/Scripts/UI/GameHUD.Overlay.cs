using UnityEngine;

namespace AirsoftArena
{
    /// <summary>In-match overlays: crosshair with hit marker, and the minimap.</summary>
    public partial class GameHUD
    {
        const float MinimapWidth = 190f;
        const float SpotRange = 30f;

        static float hitMarkerAt = -10f;
        Texture2D vignette;
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
            if (rangeStyle == null) rangeStyle = new GUIStyle(small) { fontSize = 12, wordWrap = false };

            Vector2 mouse = GameInput.MousePosition();
            var m = new Vector2(mouse.x / scale, (Screen.height - mouse.y) / scale);

            // Looking through a scope: darken the edges of the screen.
            if (s.Scoped) GUI.DrawTexture(new Rect(0f, 0f, W, H), Vignette(), ScaleMode.StretchToFill, true);

            // The gap opens up when moving and closes when crouched or aiming, matching the real spread.
            var d = s.Weapon.Data;
            float spread = d.IsMelee ? 0.5f : d.spreadDegrees * s.SpreadMultiplier;
            if (s.Sprinting) spread *= 3f;
            float gap = 4f + spread * 5f;
            var c = GameSettings.CrosshairColors[GameSettings.CrosshairColor];
            c.a = s.CanShoot ? 0.95f : 0.35f;
            var outline = new Color(0f, 0f, 0f, c.a * 0.6f);
            int style = GameSettings.CrosshairStyle;
            if (style == 0 || style == 3)
            {
                Tick(new Rect(m.x - gap - 8f, m.y - 1f, 8f, 2f), c, outline);
                Tick(new Rect(m.x + gap, m.y - 1f, 8f, 2f), c, outline);
                Tick(new Rect(m.x - 1f, m.y - gap - 8f, 2f, 8f), c, outline);
                Tick(new Rect(m.x - 1f, m.y + gap, 2f, 8f), c, outline);
            }
            if (style == 2) Ring(m, gap + 6f, c, outline);
            if (style != 0) Tick(new Rect(m.x - 1.5f, m.y - 1.5f, 3f, 3f), c, outline);
            else Line(new Rect(m.x - 1f, m.y - 1f, 2f, 2f), c);

            // Range finder: metres to the cursor, and whether the BB still carries at that range.
            if (GameSettings.RangeFinder && !d.IsMelee && cam != null)
            {
                Vector2 world = cam.ScreenToWorldPoint(mouse);
                float dist = Vector2.Distance(world, s.Position);
                float z = Ballistics.HeightAtDistance(d, s.MuzzleHeight, dist);
                string verdict;
                Color vc;
                if (z < 0f) { verdict = "OUT OF RANGE"; vc = new Color(1f, 0.4f, 0.35f); }
                else if (z < 0.5f) { verdict = "BB DROPS: legs"; vc = new Color(1f, 0.75f, 0.3f); }
                else if (z < 1.0f) { verdict = "BB DROPS: waist"; vc = new Color(1f, 0.9f, 0.45f); }
                else { verdict = ""; vc = new Color(0.7f, 1f, 0.7f); }
                vc.a = 0.9f;
                rangeStyle.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
                string text = dist.ToString("0") + " m" + (verdict.Length > 0 ? "  " + verdict : "");
                GUI.Label(new Rect(m.x + gap + 13f, m.y + 7f, 200f, 18f), text, rangeStyle);
                rangeStyle.normal.textColor = vc;
                GUI.Label(new Rect(m.x + gap + 12f, m.y + 6f, 200f, 18f), text, rangeStyle);
                // Drop hint: a little tick below the centre showing how far under the aim point the BB falls.
                if (z >= 0f && z < s.MuzzleHeight - 0.15f)
                {
                    float drop = Mathf.Clamp((s.MuzzleHeight - z) * 14f, 0f, 30f);
                    Tick(new Rect(m.x - 3f, m.y + drop, 6f, 2f), vc, outline);
                }
            }

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

        Texture2D Vignette()
        {
            if (vignette != null) return vignette;
            const int n = 64;
            vignette = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x + 0.5f - n / 2f, y + 0.5f - n / 2f).magnitude / (n / 2f);
                    vignette.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Clamp01((d - 0.55f) * 1.6f) * 0.85f));
                }
            vignette.Apply();
            return vignette;
        }

        GUIStyle rangeStyle;

        /// <summary>A crosshair line with a dark outline so it reads on any ground.</summary>
        void Tick(Rect r, Color c, Color outline)
        {
            Line(new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f), outline);
            Line(r, c);
        }

        void Ring(Vector2 centre, float radius, Color c, Color outline)
        {
            int segments = Mathf.Clamp(Mathf.RoundToInt(radius * 1.5f), 16, 64);
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var p = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                Line(new Rect(p.x - 1.5f, p.y - 1.5f, 3f, 3f), outline);
            }
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var p = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                Line(new Rect(p.x - 1f, p.y - 1f, 2f, 2f), c);
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
