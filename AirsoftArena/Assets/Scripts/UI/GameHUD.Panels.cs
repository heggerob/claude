using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// Detailed in-match HUD: weapon panel with mag icons and BB bar, stamina, team pips, off-screen
    /// objective arrows, the "hit from" indicator and the Tab scoreboard.
    /// </summary>
    public partial class GameHUD
    {
        // ================================================================ weapon panel (bottom right)

        void DrawWeaponPanel(Soldier s)
        {
            var w = s.Weapon;
            var d = w.Data;
            float pw = 340f, ph = 128f;
            var box = new Rect(W - pw - 10f, H - ph - 10f, pw, ph);
            GUI.Box(box, GUIContent.none, panel);

            // Weapon picture and name.
            if (Event.current.type == EventType.Repaint)
            {
                var art = PixelArt.Gun(d);
                float scaleGun = Mathf.Min(4f, Mathf.Floor(150f / art.sprite.texture.width));
                DrawSprite(art.sprite, new Vector2(box.x + 14f, box.y + 30f), Mathf.Max(1f, scaleGun), Color.white);
            }
            GUI.Label(new Rect(box.x + 10f, box.y + 46f, 190f, 20f), string.Format("<b>{0}</b>  <color=#aaaaaa>{1} · class {2}</color>", d.displayName, d.code, d.ClassCode), small);

            if (d.IsMelee)
            {
                GUI.Label(new Rect(box.x + 10f, box.y + 70f, 320f, 24f), "Knife: sneak up and tap them", label);
            }
            else
            {
                // Big ammo count.
                string ammoColor = w.IsEmpty ? "#ff6655" : w.AmmoInMag < d.magCapacity * 0.2f ? "#ffcc55" : "#ffffff";
                GUI.Label(new Rect(box.x + 200f, box.y + 6f, 130f, 40f), string.Format("<size=32><b><color={0}>{1}</color></b></size><color=#aaaaaa> /{2}</color>", ammoColor, w.AmmoInMag, d.magCapacity), label);
                DrawFireMode(new Rect(box.x + 204f, box.y + 50f, 120f, 14f), w);

                // BB bar for the current mag.
                var bar = new Rect(box.x + 10f, box.y + 70f, pw - 20f, 7f);
                Line(bar, new Color(1f, 1f, 1f, 0.15f));
                float fill = w.IsReloading ? w.ReloadProgress(Time.time) : w.AmmoInMag / (float)Mathf.Max(1, d.magCapacity);
                Line(new Rect(bar.x, bar.y, bar.width * fill, bar.height), w.IsReloading ? new Color(1f, 0.85f, 0.3f) : new Color(0.9f, 0.95f, 1f, 0.9f));

                // Spare mags as little icons.
                int shown = Mathf.Min(w.SpareMags, 8);
                for (int i = 0; i < shown; i++) Line(new Rect(box.x + 10f + i * 11f, box.y + 82f, 7f, 14f), new Color(0.85f, 0.8f, 0.6f, 0.9f));
                string magText = w.SpareMags > 8 ? "+" + (w.SpareMags - 8) : w.SpareMags == 0 ? "no spare mags" : "";
                GUI.Label(new Rect(box.x + 12f + shown * 11f, box.y + 80f, 150f, 18f), magText, small);
                if (w.IsReloading) GUI.Label(new Rect(box.x + 200f, box.y + 80f, 130f, 18f), "<color=#ffd060>RELOADING...</color>", small);
                else if (w.IsEmpty) GUI.Label(new Rect(box.x + 160f, box.y + 80f, 175f, 18f), w.SpareMags > 0 ? "<color=#ff8866>EMPTY: press R</color>" : "<color=#ff8866>OUT OF BBs: switch</color>", small);
            }

            // Weapon slots.
            for (int i = 0; i < s.Loadout.Length; i++)
            {
                var slot = new Rect(box.x + 10f + i * 108f, box.y + 102f, 102f, 20f);
                bool current = i == s.Slot;
                Line(slot, current ? new Color(1f, 1f, 1f, 0.18f) : new Color(1f, 1f, 1f, 0.05f));
                GUI.Label(new Rect(slot.x + 4f, slot.y + 1f, slot.width - 6f, slot.height), (current ? "<b>" : "<color=#999999>") + (i + 1) + " " + s.Loadout[i].Data.displayName + (current ? "</b>" : "</color>"), small);
            }
        }

        void DrawFireMode(Rect r, WeaponInstance w)
        {
            // Pips: single/semi = one, burst = three, auto = a solid bar.
            var c = new Color(1f, 1f, 1f, 0.85f);
            switch (w.Mode)
            {
                case FireMode.Auto: Line(new Rect(r.x, r.y + 3f, 26f, 6f), c); break;
                case FireMode.Burst: for (int i = 0; i < 3; i++) Line(new Rect(r.x + i * 8f, r.y + 3f, 6f, 6f), c); break;
                default: Line(new Rect(r.x, r.y + 3f, 6f, 6f), c); break;
            }
            GUI.Label(new Rect(r.x + 32f, r.y - 3f, 90f, 18f), w.Mode.ToString().ToUpper(), small);
        }

        // ================================================================ stamina + stance (bottom left)

        void DrawStatusPanel(Soldier s)
        {
            var box = new Rect(10f, H - 66f, 250f, 56f);
            GUI.Box(box, GUIContent.none, panel);
            GUI.Label(new Rect(box.x + 10f, box.y + 4f, 80f, 18f), "Stamina", small);
            var bar = new Rect(box.x + 70f, box.y + 9f, 168f, 8f);
            Line(bar, new Color(1f, 1f, 1f, 0.15f));
            Color sc = s.Winded ? new Color(1f, 0.45f, 0.35f) : new Color(0.45f, 0.85f, 1f);
            Line(new Rect(bar.x, bar.y, bar.width * s.Stamina, bar.height), sc);
            string stance = s.Sprinting ? "SPRINTING (can't shoot)" : s.Winded ? "<color=#ff8866>OUT OF BREATH</color>" : "";
            if (s.Scoped) stance = "SCOPED";
            else if (s.Aiming) stance = "AIMING";
            if (s.Crouching) stance += (stance.Length > 0 ? " · " : "") + "CROUCHED";
            GUI.Label(new Rect(box.x + 10f, box.y + 26f, 235f, 20f), stance.Length > 0 ? stance : "<color=#888888>H = call your hit</color>", small);
        }

        // ================================================================ team pips (under the score)

        void DrawTeamPips(MatchManager match)
        {
            float y = 60f;
            var blue = new List<Soldier>();
            var red = new List<Soldier>();
            foreach (var s in match.Soldiers) (s.Team == Team.Blue ? blue : red).Add(s);
            DrawPipRow(blue, W / 2f - 12f, y, -1f);
            DrawPipRow(red, W / 2f + 12f, y, 1f);
        }

        void DrawPipRow(List<Soldier> team, float startX, float y, float direction)
        {
            for (int i = 0; i < team.Count; i++)
            {
                var s = team[i];
                float x = startX + direction * (i * 13f) - (direction < 0 ? 10f : 0f);
                var c = Teams.Color(s.Team);
                if (!s.InPlay) c = new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 0.8f);
                Line(new Rect(x - 1f, y - 1f, 12f, 12f), new Color(0f, 0f, 0f, 0.6f));
                Line(new Rect(x, y, 10f, 10f), c);
                if (s.IsHuman) Line(new Rect(x + 3f, y + 3f, 4f, 4f), Color.white);
            }
        }

        // ================================================================ hit direction (around the player)

        void DrawHitFrom(Soldier s)
        {
            float age = Time.time - s.LastHitTime;
            if (age > 1.6f || s.LastHitTime <= 0f || cam == null || Event.current.type != EventType.Repaint) return;
            Vector2 me = ToGui(s.Position);
            Vector2 from = ToGui(s.LastHitFrom);
            Vector2 dir = (from - me).normalized;
            if (dir.sqrMagnitude < 0.01f) return;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var c = new Color(1f, 0.2f, 0.15f, 0.85f * (1f - age / 1.6f));
            var old = GUI.matrix;
            // A short arc of blocks pointing at the shooter.
            for (int i = -3; i <= 3; i++)
            {
                GUIUtility.RotateAroundPivot(angle + i * 7f, me * scale);
                Line(new Rect(me.x + 70f, me.y - 3f, 10f - Mathf.Abs(i), 6f), c);
                GUI.matrix = old;
            }
        }

        // ================================================================ off-screen objective arrows

        void DrawObjectiveArrows(MatchManager match, Soldier s)
        {
            if (cam == null || Event.current.type != EventType.Repaint) return;
            var ctf = match.Rules as CaptureTheFlagRules;
            var koth = match.Rules as KingOfTheHillRules;
            if (!s.InPlay) EdgeArrow(match.SpawnCenter(s.Team), "SPAWN", Teams.Color(s.Team), s);
            if (ctf != null)
            {
                var own = ctf.Flags[(int)s.Team];
                var enemy = ctf.Flags[1 - (int)s.Team];
                if (!own.AtHome) EdgeArrow(own.position, "YOUR FLAG", Teams.Color(s.Team), s);
                if (ctf.Carrying(s) != null) EdgeArrow(own.home, "CAPTURE", Teams.Color(s.Team), s);
                else EdgeArrow(enemy.position, "ENEMY FLAG", Teams.Color(enemy.team), s);
            }
            if (koth != null) EdgeArrow(MapBuilder.Current.hill, "HILL", koth.Holder.HasValue ? Teams.Color(koth.Holder.Value) : new Color(1f, 0.85f, 0.3f), s);
        }

        void EdgeArrow(Vector2 world, string text, Color color, Soldier s)
        {
            Vector2 p = ToGui(world);
            const float margin = 40f;
            bool onScreen = p.x > margin && p.x < W - margin && p.y > 90f && p.y < H - 150f;
            if (onScreen)
            {
                // On screen: a small label over the objective.
                GUI.Label(new Rect(p.x - 60f, p.y - 34f, 120f, 18f), "<b>" + text + "</b>", centerSmall);
                return;
            }
            Vector2 centre = new Vector2(W / 2f, H / 2f);
            Vector2 dir = (p - centre).normalized;
            // Walk from the centre to the screen edge along the direction.
            float tx = dir.x != 0f ? ((dir.x > 0 ? W - margin : margin) - centre.x) / dir.x : float.MaxValue;
            float ty = dir.y != 0f ? ((dir.y > 0 ? H - 150f : 90f) - centre.y) / dir.y : float.MaxValue;
            Vector2 edge = centre + dir * Mathf.Min(tx, ty);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, edge * scale);
            Line(new Rect(edge.x - 10f, edge.y - 4f, 18f, 8f), new Color(0f, 0f, 0f, 0.6f));
            Line(new Rect(edge.x - 9f, edge.y - 3f, 16f, 6f), color);
            Line(new Rect(edge.x + 5f, edge.y - 6f, 5f, 12f), color);
            GUI.matrix = old;
            float metres = Vector2.Distance(world, s.Position);
            GUI.Label(new Rect(edge.x - 70f - dir.x * 50f, edge.y - 22f - dir.y * 26f, 140f, 18f), "<b>" + text + "</b> " + metres.ToString("0") + " m", centerSmall);
        }

        // ================================================================ scoreboard (hold Tab)

        void DrawScoreboard(MatchManager match)
        {
            if (!GameInput.Held(GameKey.Scoreboard)) return;
            float pw = Mathf.Min(W - 40f, 820f), ph = 90f + 24f * Mathf.Max(1, match.Settings.teamSize) * 2f;
            var box = new Rect((W - pw) / 2f, (H - ph) / 2f, pw, ph);
            GUI.Box(box, GUIContent.none, panel);
            GUI.Label(new Rect(box.x + 14f, box.y + 8f, pw, 26f), "<b>" + GameModes.Name(match.Settings.mode) + "</b>   " + (MapBuilder.Current != null ? MapBuilder.Current.name : ""), label);
            float y = box.y + 38f;
            string[] headers = { "Player", "Status", "Points", "Hit", "Called", "Caught", "Obj." };
            float[] cols = { 0f, 200f, 300f, 380f, 450f, 530f, 610f };
            for (int i = 0; i < headers.Length; i++) GUI.Label(new Rect(box.x + 14f + cols[i], y, 100f, 20f), "<color=#aaaaaa>" + headers[i] + "</color>", small);
            y += 20f;
            foreach (Team team in new[] { Team.Blue, Team.Red })
            {
                foreach (var s in match.Soldiers)
                {
                    if (s.Team != team) continue;
                    var st = s.Stats;
                    string status = s.State == SoldierState.Out ? "out" : s.State == SoldierState.Respawning ? "respawning" : "in play";
                    string obj = st.captures > 0 || st.flagReturns > 0 ? st.captures + " cap / " + st.flagReturns + " ret" : st.hillSeconds > 0.5f ? Mathf.FloorToInt(st.hillSeconds) + " s hill" : "";
                    string[] values = { "<color=" + Teams.Hex(team) + ">" + (s.IsHuman ? "<b>" + s.DisplayName + "</b>" : s.DisplayName) + "</color>", status, st.pointsScored.ToString(), st.timesHit.ToString(), st.hitsCalled.ToString(), st.caughtByReferee.ToString(), obj };
                    if (s.IsHuman) Line(new Rect(box.x + 8f, y, pw - 16f, 22f), new Color(1f, 1f, 1f, 0.07f));
                    for (int i = 0; i < values.Length; i++) GUI.Label(new Rect(box.x + 14f + cols[i], y + 2f, 190f, 20f), values[i], small);
                    y += 22f;
                }
                y += 6f;
            }
        }
    }
}
