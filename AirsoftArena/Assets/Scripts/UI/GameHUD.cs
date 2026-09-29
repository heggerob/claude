using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// All prototype UI in one place, drawn with IMGUI so it needs no Canvas or prefabs.
    /// Swap for a proper uGUI / UI Toolkit interface once the gameplay is locked in.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        const float RefHeight = 720f;

        GUIStyle label, small, big, huge, title, panel, feed, tag, center;
        Texture2D panelTexture, whiteTexture;
        float scale, W, H;
        Camera cam;

        // Lobby choices.
        Role role = Role.Soldier;
        int primaryIndex, secondaryIndex, refereeIndex = 2;
        bool autoCallHits;
        bool confirmReset;

        // Button actions that change what is drawn run in Update, so IMGUI's layout and repaint passes always match.
        System.Action deferred;

        void Defer(System.Action action) { deferred += action; }

        void Update()
        {
            var action = deferred;
            deferred = null;
            if (action != null) action();
        }

        void OnGUI()
        {
            var match = MatchManager.Instance;
            if (match == null) return;
            EnsureStyles();
            if (cam == null) cam = Camera.main;

            scale = Screen.height / RefHeight;
            W = Screen.width / scale;
            H = RefHeight;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            switch (match.Phase)
            {
                case MatchPhase.Lobby:
                    DrawLobby(match);
                    break;
                case MatchPhase.Countdown:
                case MatchPhase.Playing:
                    DrawWorldLabels(match);
                    DrawMatchHud(match);
                    if (match.Phase == MatchPhase.Countdown) Shadowed(new Rect(0, H * 0.3f, W, 120), Mathf.CeilToInt(match.CountdownLeft).ToString(), huge);
                    if (match.Paused) DrawPause(match);
                    break;
                case MatchPhase.Results:
                    DrawResults(match);
                    break;
            }
        }

        // ================================================================ lobby

        void DrawLobby(MatchManager match)
        {
            var profile = PlayerProfile.Current;
            var primaries = WeaponCatalog.Primaries;
            var secondaries = WeaponCatalog.Secondaries;
            var referees = RefereeMarket.All;
            primaryIndex = Mathf.Clamp(primaryIndex, 0, primaries.Count - 1);
            secondaryIndex = Mathf.Clamp(secondaryIndex, 0, secondaries.Count - 1);
            refereeIndex = Mathf.Clamp(refereeIndex, 0, referees.Count - 1);

            float pw = Mathf.Min(W - 32f, 1000f), ph = Mathf.Min(H - 32f, 680f);
            GUILayout.BeginArea(new Rect((W - pw) / 2f, (H - ph) / 2f, pw, ph), panel);

            GUILayout.Label("AIRSOFT ARENA", title);
            GUILayout.Label("2D prototype  ·  Team Deathmatch 4v4  ·  Map: Pallet Yard", small);
            GUILayout.Space(6);
            GUILayout.Label(string.Format("<b>${0}</b>    Skill {1:0}    Honor {2:0}/100    Matches {3} (wins {4})    Referee {5} ({6} jobs)",
                profile.money, profile.skillRating, profile.honor, profile.matches, profile.wins, RefereeProfile.StarText(profile.RefStars), profile.refMatches), label);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (Choice(role == Role.Soldier, "PLAY  (soldier)", GUILayout.Height(34))) Defer(() => role = Role.Soldier);
            if (Choice(role == Role.Referee, "WORK AS REFEREE  (get paid)", GUILayout.Height(34))) Defer(() => role = Role.Referee);
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (role == Role.Soldier)
            {
                GUILayout.BeginVertical(GUILayout.Width(pw * 0.5f));
                GUILayout.Label("<b>Primary</b>", label);
                for (int i = 0; i < primaries.Count; i++)
                    if (Choice(i == primaryIndex, WeaponButton(primaries[i]))) primaryIndex = i;
                GUILayout.Label(primaries[primaryIndex].StatLine, small);
                GUILayout.Space(6);
                GUILayout.Label("<b>Secondary</b>", label);
                for (int i = 0; i < secondaries.Count; i++)
                    if (Choice(i == secondaryIndex, WeaponButton(secondaries[i]))) secondaryIndex = i;
                GUILayout.Label(secondaries[secondaryIndex].StatLine, small);
                GUILayout.Label("Melee: [00] Rubber Tanto (always carried)", small);
                GUILayout.EndVertical();

                GUILayout.Space(12);

                GUILayout.BeginVertical();
                GUILayout.Label("<b>Hire a referee</b>  (every player pays the fee: better refs cost more)", label);
                for (int i = 0; i < referees.Count; i++)
                {
                    var r = referees[i];
                    if (Choice(i == refereeIndex, string.Format("{0}   {1}   ${2}/player", r.name, RefereeProfile.StarText(r.Stars), r.FeePerPlayer)))
                        refereeIndex = i;
                }
                var sel = referees[refereeIndex];
                GUILayout.Label("<i>\"" + sel.tagline + "\"</i>", small);
                GUILayout.Label(string.Format("{0} matches · caught {1} cheaters · missed {2} · wrong calls {3}", sel.matches, sel.caught, sel.missed, sel.wrongCalls), small);
                GUILayout.Space(10);
                autoCallHits = GUILayout.Toggle(autoCallHits, "  Auto-call my hits (casual)");
                GUILayout.Label("A hit is a hit: when a BB touches you, press <b>H</b> within 2.5 s to call it. Don't call it and you keep playing... unless the referee saw it.", small);
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginVertical();
                GUILayout.Label("<b>The referee job</b>", label);
                GUILayout.Label("Walk the field and watch the fight. When a BB hits someone you'll see a white flash and \"*tak*\". Honest players raise the orange rag and walk back to spawn.", label);
                GUILayout.Label("Some players keep fighting after being hit. <b>Click them</b> to call them out. Call out someone who was never hit and the players will hate you.", label);
                GUILayout.Label("After the match every player rates you. More stars = you can charge more.", label);
                GUILayout.Space(10);
                GUILayout.Label(string.Format("Your rating: {0}   ·   jobs {1}   ·   correct calls {2}   ·   wrong calls {3}   ·   missed {4}",
                    RefereeProfile.StarText(profile.RefStars), profile.refMatches, profile.refCorrectCalls, profile.refWrongCalls, profile.refMissed), label);
                GUILayout.Label(string.Format("Your fee: <b>${0}</b> per player  ×  8 players  =  <b>${1}</b> per match", profile.RefFeePerPlayer, profile.RefFeePerPlayer * 8), label);
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            var settings = new MatchSettings
            {
                role = role,
                primary = primaries[primaryIndex],
                secondary = secondaries[secondaryIndex],
                referee = referees[refereeIndex],
                autoCallHits = autoCallHits,
            };
            int cost = settings.EntryCost;
            bool canAfford = profile.money >= cost;
            if (!canAfford)
                GUILayout.Label("<color=#ff8866>Not enough money for this referee. Pick a cheaper one, or work as a referee to earn some.</color>", label);

            GUI.enabled = canAfford;
            string start = role == Role.Referee ? "START MATCH AS REFEREE" : "START MATCH   (pay $" + cost + ")";
            if (GUILayout.Button(start, GUILayout.Height(46))) Defer(() => match.StartMatch(settings));
            GUI.enabled = true;

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label("WASD move · mouse aim + shoot · R reload · B fire mode · 1/2/3 weapons · C crouch · Shift sprint · H call hit · Esc pause", small);
            if (GUILayout.Button(confirmReset ? "Really reset?" : "Reset save", GUILayout.Width(110)))
            {
                if (confirmReset) Defer(() => { PlayerProfile.ResetAll(); confirmReset = false; });
                else confirmReset = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        static string WeaponButton(WeaponData w)
        {
            return string.Format("[{0}] {1}   {2}", w.ClassCode, w.displayName, w.code);
        }

        bool Choice(bool selected, string text, params GUILayoutOption[] options)
        {
            var old = GUI.backgroundColor;
            GUI.backgroundColor = selected ? new Color(0.45f, 1f, 0.5f) : new Color(0.8f, 0.8f, 0.8f);
            bool clicked = GUILayout.Button((selected ? "▶ " : "") + text, options);
            GUI.backgroundColor = old;
            return clicked;
        }

        // ================================================================ in match

        void DrawMatchHud(MatchManager match)
        {
            // Score and timer.
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, match.TimeLeft));
            string score = string.Format("<color={0}>BLUE  {1}</color>     {2}:{3:00}     <color={4}>{5}  RED</color>",
                Teams.Hex(Team.Blue), match.Score[0], seconds / 60, seconds % 60, Teams.Hex(Team.Red), match.Score[1]);
            GUI.Box(new Rect(W / 2f - 190f, 8f, 380f, 50f), GUIContent.none, panel);
            GUI.Label(new Rect(W / 2f - 190f, 10f, 380f, 30f), score, big);
            GUI.Label(new Rect(W / 2f - 190f, 36f, 380f, 20f), "first to " + match.Settings.scoreLimit, centerSmall);

            // Referee and wind.
            var referee = match.Referee;
            if (referee != null)
            {
                GUI.Box(new Rect(8f, 8f, 300f, 50f), GUIContent.none, panel);
                string refText = referee.HumanControlled
                    ? "Referee: <b>YOU</b>"
                    : "Referee: <b>" + referee.Profile.name + "</b>  " + RefereeProfile.StarText(referee.Profile.Stars);
                GUI.Label(new Rect(16f, 10f, 290f, 22f), refText, label);
                var wind = BBSystem.Instance.Wind;
                GUI.Label(new Rect(16f, 32f, 290f, 22f), "Wind " + wind.magnitude.ToString("0.0") + " m/s " + Arrow(wind), small);
            }

            DrawFeed(match);

            if (match.PlayerSoldier != null) DrawSoldierHud(match.PlayerSoldier);
            else if (match.PlayerReferee != null) DrawRefereeHud(match);
        }

        void DrawSoldierHud(Soldier s)
        {
            var w = s.Weapon;
            var d = w.Data;
            float x = 12f, y = H - 118f;
            GUI.Box(new Rect(x - 4f, y - 4f, 360f, 112f), GUIContent.none, panel);
            GUI.Label(new Rect(x + 4f, y, 350f, 24f), string.Format("<b>[{0}] {1}</b>   {2}", d.ClassCode, d.displayName, d.code), label);
            if (d.IsMelee)
            {
                GUI.Label(new Rect(x + 4f, y + 24f, 350f, 30f), "Knife: sneak up and tap them", label);
            }
            else
            {
                string ammo = string.Format("<b>{0}</b>  <size=26>{1}</size> / {2}   mags {3}", w.Mode.ToString().ToUpper(), w.AmmoInMag, d.magCapacity, w.SpareMags);
                GUI.Label(new Rect(x + 4f, y + 22f, 350f, 34f), ammo, label);
                if (w.IsReloading)
                {
                    float p = w.ReloadProgress(Time.time);
                    GUI.DrawTexture(new Rect(x + 4f, y + 58f, 330f, 6f), whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(1f, 1f, 1f, 0.2f), 0f, 0f);
                    GUI.DrawTexture(new Rect(x + 4f, y + 58f, 330f * p, 6f), whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(1f, 0.85f, 0.3f), 0f, 0f);
                }
                else if (w.IsEmpty)
                {
                    GUI.Label(new Rect(x + 4f, y + 52f, 350f, 20f), w.SpareMags > 0 ? "<color=#ff8866>EMPTY - press R</color>" : "<color=#ff8866>OUT OF BBs - switch weapon</color>", small);
                }
            }

            string slots = "";
            for (int i = 0; i < s.Loadout.Length; i++)
            {
                string n = (i + 1) + " " + s.Loadout[i].Data.displayName;
                slots += i == s.Slot ? "<b><color=#ffffff>" + n + "</color></b>   " : "<color=#999999>" + n + "</color>   ";
            }
            GUI.Label(new Rect(x + 4f, y + 68f, 350f, 20f), slots, small);
            string stance = s.Crouching ? "CROUCHED (steadier aim, harder to hit behind cover)" : s.Sprinting ? "SPRINTING (can't shoot)" : "";
            GUI.Label(new Rect(x + 4f, y + 86f, 350f, 20f), stance, small);

            // Big centre messages.
            var r = new Rect(0f, H * 0.2f, W, 60f);
            switch (s.State)
            {
                case SoldierState.Hit:
                    bool blink = Mathf.Repeat(Time.time * 4f, 1f) < 0.6f;
                    Shadowed(r, blink ? "<color=#ff4a3a>YOU'RE HIT!</color>" : "YOU'RE HIT!", huge);
                    Shadowed(new Rect(0f, H * 0.2f + 64f, W, 30f), string.Format("Press <b>H</b> to call it ({0:0.0} s)... or keep playing and hope the ref didn't see", s.HitWindowLeft), big);
                    break;
                case SoldierState.Out:
                    Vector2 to = MatchManager.Instance.SpawnCenter(s.Team) - s.Position;
                    Shadowed(r, "OUT", huge);
                    Shadowed(new Rect(0f, H * 0.2f + 64f, W, 30f), "Walk back to your spawn  " + Arrow(to), big);
                    break;
                case SoldierState.Respawning:
                    Shadowed(r, "Back in the game in " + s.RespawnLeft.ToString("0.0"), big);
                    break;
                default:
                    if (s.HasUncalledHit)
                        Shadowed(new Rect(0f, H * 0.2f, W, 30f), "<color=#ffb050>You didn't call your hit... the referee might have seen it.  (H = call it late)</color>", label);
                    break;
            }
        }

        void DrawRefereeHud(MatchManager match)
        {
            float x = 12f, y = H - 92f;
            GUI.Box(new Rect(x - 4f, y - 4f, 460f, 86f), GUIContent.none, panel);
            GUI.Label(new Rect(x + 4f, y, 450f, 24f), "<b>REFEREE</b>   WASD walk · Shift jog · click a player to call them OUT", label);
            GUI.Label(new Rect(x + 4f, y + 26f, 450f, 24f), string.Format("Correct calls <b>{0}</b>     Wrong calls <b>{1}</b>", match.CorrectCalls, match.WrongCalls), label);
            GUI.Label(new Rect(x + 4f, y + 52f, 450f, 24f), "Look for the white flash + *tak* on a player who does NOT raise the orange rag.", small);
        }

        void DrawFeed(MatchManager match)
        {
            float y = 66f;
            int shown = 0;
            for (int i = match.Feed.Count - 1; i >= 0 && shown < 7; i--)
            {
                var e = match.Feed[i];
                float age = Time.time - e.time;
                if (age > 8f) break;
                var rect = new Rect(W - 470f, y, 460f, 24f);
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(8f - age));
                GUI.Box(rect, GUIContent.none, panel);
                GUI.Label(new Rect(rect.x + 8f, rect.y + 2f, rect.width - 12f, rect.height), e.text, feed);
                GUI.color = Color.white;
                y += 26f;
                shown++;
            }
        }

        void DrawWorldLabels(MatchManager match)
        {
            if (cam == null) return;
            var hovered = match.PlayerReferee != null ? match.PlayerReferee.Hovered : null;

            foreach (var s in match.Soldiers)
            {
                Vector2 p = ToGui(s.Position + new Vector2(0f, 0.75f));
                string text = s.DisplayName;
                if (s.State == SoldierState.Out) text += "  <color=#ff9933>OUT</color>";
                else if (s.State == SoldierState.Respawning) text += "  <color=#ff9933>" + s.RespawnLeft.ToString("0") + "</color>";
                if (s == hovered) text = "<color=#ffe14a>[CLICK: call OUT]</color>\n" + text;
                tag.normal.textColor = Teams.Color(s.Team);
                GUI.Label(new Rect(p.x - 100f, p.y - (s == hovered ? 34f : 18f), 200f, s == hovered ? 36f : 18f), text, tag);
            }

            if (match.Referee != null)
            {
                Vector2 p = ToGui(match.Referee.Position + new Vector2(0f, 0.75f));
                tag.normal.textColor = new Color(1f, 0.92f, 0.2f);
                GUI.Label(new Rect(p.x - 60f, p.y - 18f, 120f, 18f), match.Referee.HumanControlled ? "REF (you)" : "REF", tag);
            }

            var effects = Effects.Instance;
            if (effects == null) return;
            foreach (var t in effects.Texts)
            {
                float age = (Time.time - t.born) / t.life;
                Vector2 p = ToGui(t.position + new Vector2(0f, age * 0.6f));
                var c = t.color;
                c.a *= 1f - age * age;
                var old = center.fontSize;
                center.fontSize = t.size;
                center.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
                GUI.Label(new Rect(p.x - 149f, p.y - 11f, 300f, 24f), t.text, center);
                center.normal.textColor = c;
                GUI.Label(new Rect(p.x - 150f, p.y - 12f, 300f, 24f), t.text, center);
                center.fontSize = old;
            }
        }

        void DrawPause(MatchManager match)
        {
            GUI.DrawTexture(new Rect(0f, 0f, W, H), whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0f, 0f, 0f, 0.5f), 0f, 0f);
            GUILayout.BeginArea(new Rect(W / 2f - 160f, H / 2f - 90f, 320f, 180f), panel);
            GUILayout.Label("PAUSED", title);
            if (GUILayout.Button("Resume", GUILayout.Height(36))) Defer(() => match.SetPaused(false));
            if (GUILayout.Button("End match now", GUILayout.Height(36))) Defer(match.EndMatch);
            GUILayout.EndArea();
        }

        // ================================================================ results

        void DrawResults(MatchManager match)
        {
            var r = match.LastResult;
            if (r == null) return;
            float pw = Mathf.Min(W - 32f, 940f), ph = Mathf.Min(H - 32f, 600f);
            GUILayout.BeginArea(new Rect((W - pw) / 2f, (H - ph) / 2f, pw, ph), panel);

            string headline = r.draw ? "DRAW" : "<color=" + Teams.Hex(r.winner) + ">" + Teams.Name(r.winner) + " WINS</color>";
            GUILayout.Label(headline, title);
            GUILayout.Label(string.Format("<color={0}>BLUE {1}</color>  -  <color={2}>{3} RED</color>", Teams.Hex(Team.Blue), r.blueScore, Teams.Hex(Team.Red), r.redScore), big);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(pw * 0.5f));
            if (r.role == Role.Soldier && r.playerStats != null)
            {
                var s = r.playerStats;
                GUILayout.Label(r.playerWon ? "<b>You won!</b>" : r.draw ? "<b>Even match.</b>" : "<b>You lost this one.</b>", label);
                GUILayout.Label(string.Format("Hits scored {0}   ·   times hit {1}", s.pointsScored, s.timesHit), label);
                GUILayout.Label(string.Format("Hits called {0}   ·   not called {1}   ·   caught by ref {2}", s.hitsCalled, s.hitsNotCalled, s.caughtByReferee), label);
                if (s.wronglyCalledOut > 0) GUILayout.Label("Wrongly called out by the ref: " + s.wronglyCalledOut, label);
                if (s.overshoots > 0) GUILayout.Label("Overshoots (shot players who were out): " + s.overshoots, label);
                if (s.shotTheReferee > 0) GUILayout.Label("Shot the referee: " + s.shotTheReferee, label);
                GUILayout.Space(6);
                GUILayout.Label(string.Format("Skill {0:0} → <b>{1:0}</b>    Honor {2:0} → <b>{3:0}</b>", r.skillBefore, r.skillAfter, r.honorBefore, r.honorAfter), label);
            }
            else
            {
                GUILayout.Label("<b>Your shift as referee</b>", label);
                GUILayout.Label(string.Format("Correct calls {0}   ·   caught cheaters {1}", r.refCorrectCalls, r.refCaught), label);
                GUILayout.Label(string.Format("Missed cheaters {0}   ·   wrong calls {1}", r.refMissed, r.refWrongCalls), label);
            }
            GUILayout.Space(8);
            GUILayout.Label("<b>Money</b>", label);
            foreach (var line in r.moneyLines) GUILayout.Label(line, small);
            GUILayout.Label(string.Format("${0} → <b>${1}</b>", r.moneyBefore, r.moneyAfter), label);
            GUILayout.EndVertical();

            GUILayout.Space(16);

            GUILayout.BeginVertical();
            if (r.role == Role.Soldier)
            {
                GUILayout.Label("<b>Referee: " + r.refereeName + "</b>", label);
                GUILayout.Label(string.Format("Caught {0} cheaters · missed {1} · wrong calls {2}", r.refCaught, r.refMissed, r.refWrongCalls), label);
                GUILayout.Label("The other players rated: " + RefereeProfile.StarText(r.refLobbyStars), label);
                GUILayout.Space(8);
                if (!r.playerRatedReferee)
                {
                    GUILayout.Label("How was the referee?", label);
                    GUILayout.BeginHorizontal();
                    for (int stars = 1; stars <= 5; stars++)
                    {
                        int rating = stars;
                        if (GUILayout.Button(stars + " ★", GUILayout.Height(34))) Defer(() => match.RateReferee(rating));
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("Thanks for rating!", label);
                }
                GUILayout.Label(string.Format("Rating {0:0.00} → <b>{1:0.00}</b>", r.refStarsBefore, r.refStarsAfter), label);
            }
            else
            {
                GUILayout.Label("<b>The players rated you</b>", label);
                GUILayout.Label(RefereeProfile.StarText(r.refLobbyStars), big);
                GUILayout.Label(string.Format("Your rating {0:0.00} → <b>{1:0.00}</b>", r.refStarsBefore, r.refStarsAfter), label);
                GUILayout.Label(string.Format("Your fee next match: <b>${0}</b> per player", PlayerProfile.Current.RefFeePerPlayer), label);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("CONTINUE", GUILayout.Height(44))) Defer(match.ReturnToLobby);
            GUILayout.EndArea();
        }

        // ================================================================ helpers

        Vector2 ToGui(Vector2 world)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            return new Vector2(sp.x / scale, (Screen.height - sp.y) / scale);
        }

        void Shadowed(Rect r, string text, GUIStyle style)
        {
            var old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), StripColor(text), style);
            style.normal.textColor = old;
            GUI.Label(r, text, style);
        }

        static string StripColor(string s)
        {
            return System.Text.RegularExpressions.Regex.Replace(s, "</?color[^>]*>", "");
        }

        static string Arrow(Vector2 v)
        {
            if (v.sqrMagnitude < 0.01f) return "";
            string[] arrows = { "→", "↗", "↑", "↖", "←", "↙", "↓", "↘" };
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            int i = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 45f) % 8;
            return arrows[i];
        }

        GUIStyle centerSmall;

        void EnsureStyles()
        {
            if (label != null) return;

            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(0.05f, 0.06f, 0.05f, 0.82f));
            panelTexture.Apply();
            whiteTexture = Texture2D.whiteTexture;

            panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(16, 16, 12, 12) };
            panel.normal.background = panelTexture;

            label = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = true };
            label.normal.textColor = new Color(0.93f, 0.93f, 0.9f);
            small = new GUIStyle(label) { fontSize = 12 };
            small.normal.textColor = new Color(0.75f, 0.75f, 0.72f);
            feed = new GUIStyle(label) { fontSize = 13, wordWrap = false, alignment = TextAnchor.MiddleLeft };
            big = new GUIStyle(label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            huge = new GUIStyle(label) { fontSize = 52, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            title = new GUIStyle(label) { fontSize = 34, fontStyle = FontStyle.Bold, wordWrap = false };
            title.normal.textColor = new Color(1f, 0.85f, 0.3f);
            tag = new GUIStyle(label) { fontSize = 11, alignment = TextAnchor.LowerCenter, wordWrap = false };
            center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            centerSmall = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };

            GUI.skin.button.richText = true;
            GUI.skin.button.fontSize = 13;
            GUI.skin.toggle.fontSize = 13;
        }
    }
}
