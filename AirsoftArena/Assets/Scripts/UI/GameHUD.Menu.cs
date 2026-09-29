using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    enum MenuTab { Play, Loadout, Shop, Referee, Profile, Settings }

    /// <summary>The main menu: tabs for playing, loadout + cosmetics, shop and crates, the referee job, profile and settings.</summary>
    public partial class GameHUD
    {
        static readonly string[] TabNames = { "PLAY", "LOADOUT", "SHOP", "REFEREE", "PROFILE", "SETTINGS" };
        const float ReelItemWidth = 118f;
        const float ReelDuration = 4.5f;
        const int ReelLength = 44;
        const int ReelWinnerIndex = 38;

        MenuTab tab = MenuTab.Play;
        CosmeticSlot cosmeticSlot = CosmeticSlot.Camo;
        int refereeIndex = 2;
        bool confirmReset;
        Vector2 loadoutScroll, cosmeticScroll, shopScroll, refScroll;

        // Crate opening animation.
        List<CosmeticItem> reel;
        CrateResult reelResult;
        float reelStart, reelJitter;

        void DrawMenu(MatchManager match)
        {
            var profile = PlayerProfile.Current;
            float pw = Mathf.Min(W - 24f, 1120f), ph = H - 24f;
            var area = new Rect((W - pw) / 2f, 12f, pw, ph);

            GUI.enabled = reel == null;
            GUILayout.BeginArea(area, panel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("AIRSOFT ARENA", title);
            GUILayout.FlexibleSpace();
            GUILayout.Label("<b>$" + profile.money + "</b>", money);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            for (int i = 0; i < TabNames.Length; i++)
            {
                var t = (MenuTab)i;
                if (Choice(tab == t, TabNames[i], GUILayout.Height(32))) Defer(() => tab = t);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            switch (tab)
            {
                case MenuTab.Play: DrawPlayTab(match, profile, pw); break;
                case MenuTab.Loadout: DrawLoadoutTab(profile, pw); break;
                case MenuTab.Shop: DrawShopTab(profile, pw); break;
                case MenuTab.Referee: DrawRefereeTab(match, profile); break;
                case MenuTab.Profile: DrawProfileTab(profile); break;
                case MenuTab.Settings: DrawSettingsTab(); break;
            }

            GUILayout.EndArea();
            GUI.enabled = true;

            if (reel != null) DrawReel(profile);
        }

        // ================================================================ PLAY

        void DrawPlayTab(MatchManager match, PlayerProfile profile, float pw)
        {
            var referees = RefereeMarket.All;
            refereeIndex = Mathf.Clamp(refereeIndex, 0, referees.Count - 1);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(pw * 0.42f));
            GUILayout.Label("<b>Your soldier</b>", label);
            DrawSoldierPreview(GUILayoutUtility.GetRect(pw * 0.4f, 110f), profile.Look, profile.Primary, Team.Blue);
            GUILayout.Label(WeaponButton(profile.Primary) + "\n" + WeaponButton(profile.Secondary) + "\n[00] Rubber Tanto", small);
            if (GUILayout.Button("Change loadout & look")) Defer(() => tab = MenuTab.Loadout);
            GUILayout.Space(10);
            GUILayout.Label("<b>Map</b>", label);
            GUILayout.BeginHorizontal();
            foreach (var map in MapLibrary.All)
            {
                var m = map;
                if (Choice(profile.mapId == m.id, m.name + (m.indoor ? " (indoor)" : ""), GUILayout.Height(28)))
                    Defer(() => SelectMap(profile, m));
            }
            GUILayout.EndHorizontal();
            var selectedMap = MapLibrary.Get(profile.mapId);
            GUILayout.Label(selectedMap.description, small);
            GUILayout.Space(4);
            GUILayout.Label("<b>Mode</b>", label);
            GUILayout.BeginHorizontal();
            foreach (GameMode gm in System.Enum.GetValues(typeof(GameMode)))
            {
                var g = gm;
                if (Choice(profile.mode == g, GameModes.Name(g), GUILayout.Height(28))) Defer(() => { profile.mode = g; PlayerProfile.Save(); });
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(GameModes.Description(profile.mode), small);
            GUILayout.Label(string.Format("4v4 vs bots  ·  {0:0}×{1:0} m  ·  3:00", selectedMap.bounds.width, selectedMap.bounds.height), small);
            GUILayout.EndVertical();

            GUILayout.Space(16);

            GUILayout.BeginVertical();
            GUILayout.Label("<b>Hire a referee</b>  every player pays the fee, better refs cost more", label);
            refScroll = GUILayout.BeginScrollView(refScroll, GUILayout.Height(230f));
            for (int i = 0; i < referees.Count; i++)
            {
                var r = referees[i];
                int index = i;
                if (Choice(i == refereeIndex, string.Format("{0}   {1}   ${2}/player", r.name, RefereeProfile.StarText(r.Stars), r.FeePerPlayer), GUILayout.Height(30)))
                    Defer(() => refereeIndex = index);
            }
            GUILayout.EndScrollView();
            var sel = referees[refereeIndex];
            GUILayout.Label("<i>\"" + sel.tagline + "\"</i>", small);
            GUILayout.Label(string.Format("{0} matches · caught {1} cheaters · missed {2} · wrong calls {3}", sel.matches, sel.caught, sel.missed, sel.wrongCalls), small);
            GUILayout.Space(6);
            bool auto = GUILayout.Toggle(GameSettings.AutoCallHits, "  Auto-call my hits (casual)");
            if (auto != GameSettings.AutoCallHits) { GameSettings.AutoCallHits = auto; GameSettings.Save(); }
            GUILayout.Label("A hit is a hit: press <b>H</b> within 2.5 s when a BB touches you. Don't, and you play on... unless the ref saw it.", small);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            var settings = new MatchSettings
            {
                role = Role.Soldier,
                primary = profile.Primary,
                secondary = profile.Secondary,
                referee = sel,
                autoCallHits = GameSettings.AutoCallHits,
                map = MapLibrary.Get(profile.mapId),
                mode = profile.mode,
            };
            int cost = settings.EntryCost;
            if (profile.money < cost)
                GUILayout.Label("<color=#ff8866>Not enough money for this referee. Pick a cheaper one, or take a referee job to earn some.</color>", label);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && profile.money >= cost;
            if (GUILayout.Button("START MATCH   (referee fee $" + cost + ")", GUILayout.Height(48))) Defer(() => match.StartMatch(settings));
            GUI.enabled = wasEnabled;
            GUILayout.Label("WASD move · mouse aim + shoot · R reload · B fire mode · 1/2/3 weapons · C crouch · Shift sprint · H call hit · Esc pause", small);
        }

        // ================================================================ LOADOUT

        void DrawLoadoutTab(PlayerProfile profile, float pw)
        {
            var primary = profile.Primary;
            var secondary = profile.Secondary;

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(pw * 0.48f));
            DrawSoldierPreview(GUILayoutUtility.GetRect(pw * 0.46f, 130f), profile.Look, primary, Team.Blue);
            loadoutScroll = GUILayout.BeginScrollView(loadoutScroll);
            GUILayout.Label("<b>Primary</b>", label);
            foreach (var w in WeaponCatalog.Primaries) WeaponChoice(profile, w, w == primary, true);
            GUILayout.Label(primary.StatLine, small);
            GUILayout.Space(6);
            GUILayout.Label("<b>Secondary</b>", label);
            foreach (var w in WeaponCatalog.Secondaries) WeaponChoice(profile, w, w == secondary, false);
            GUILayout.Label(secondary.StatLine, small);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(14);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            foreach (CosmeticSlot slot in System.Enum.GetValues(typeof(CosmeticSlot)))
            {
                var s = slot;
                if (Choice(cosmeticSlot == slot, SlotName(slot), GUILayout.Height(28))) Defer(() => cosmeticSlot = s);
            }
            GUILayout.EndHorizontal();
            cosmeticScroll = GUILayout.BeginScrollView(cosmeticScroll);
            string equipped = profile.Equipped(cosmeticSlot);
            foreach (var item in CosmeticCatalog.InSlot(cosmeticSlot)) CosmeticRow(profile, item, item.id == equipped);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        void CosmeticRow(PlayerProfile profile, CosmeticItem item, bool equipped)
        {
            bool owned = profile.Owns(item);
            GUILayout.BeginHorizontal(GUILayout.Height(44));
            DrawItemIcon(GUILayoutUtility.GetRect(44f, 44f, GUILayout.Width(44f)), item);
            GUILayout.Label(string.Format("<b>{0}</b>\n<color={1}>{2}</color>", item.name, Rarities.Hex(item.rarity), item.RarityLabel), label, GUILayout.Width(180f));
            GUILayout.FlexibleSpace();
            string action = equipped ? "EQUIPPED" : owned ? "Equip" : "BUY  $" + item.price;
            if (Choice(equipped, action, GUILayout.Width(130f), GUILayout.Height(34f)) && !equipped)
            {
                Defer(() =>
                {
                    if (!profile.Owns(item) && Shop.Buy(profile, item) != PurchaseResult.Ok) return;
                    profile.Equip(item);
                    PlayerProfile.Save();
                });
            }
            GUILayout.EndHorizontal();
        }

        static string SlotName(CosmeticSlot slot)
        {
            switch (slot)
            {
                case CosmeticSlot.Camo: return "CAMO";
                case CosmeticSlot.Uniform: return "UNIFORM";
                case CosmeticSlot.HeadGear: return "HEAD";
                default: return "BB TRACER";
            }
        }

        // ================================================================ SHOP

        void DrawShopTab(PlayerProfile profile, float pw)
        {
            GUILayout.Label("<b>Crates</b>   in-game money only · odds shown up front · duplicates pay part of the price back", label);
            GUILayout.BeginHorizontal();
            foreach (var crate in Shop.Crates)
            {
                GUILayout.BeginVertical(panel, GUILayout.Width(pw * 0.46f));
                GUILayout.Label("<b>" + crate.name + "</b>   $" + crate.price, big);
                GUILayout.Label(crate.description, small);
                string odds = "";
                for (int i = 0; i < crate.odds.Length; i++)
                {
                    if (crate.odds[i] <= 0f) continue;
                    var r = (Rarity)i;
                    odds += string.Format("<color={0}>{1} {2:0.#}%</color>   ", Rarities.Hex(r), r, crate.odds[i] * 100f);
                }
                GUILayout.Label(odds, small);
                bool wasEnabled = GUI.enabled;
                GUI.enabled = wasEnabled && profile.money >= crate.price;
                var c = crate;
                if (GUILayout.Button("OPEN  ($" + crate.price + ")", GUILayout.Height(40))) Defer(() => StartReel(profile, c));
                GUI.enabled = wasEnabled;
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("<b>Buy directly</b>   everything you don't own yet", label);
            shopScroll = GUILayout.BeginScrollView(shopScroll);
            foreach (var w in WeaponCatalog.All)
            {
                if (profile.OwnsWeapon(w)) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label(WeaponButton(w) + "   <color=#999999>" + w.StatLine + "</color>", small);
                GUILayout.FlexibleSpace();
                var weapon = w;
                if (GUILayout.Button("BUY  $" + w.price, GUILayout.Width(130f), GUILayout.Height(30f))) Defer(() => Shop.Buy(profile, weapon));
                GUILayout.EndHorizontal();
            }
            foreach (var item in CosmeticCatalog.All)
                if (!profile.Owns(item)) CosmeticRow(profile, item, false);
            GUILayout.EndScrollView();
        }

        void StartReel(PlayerProfile profile, CrateType crate)
        {
            var result = Shop.OpenCrate(profile, crate);
            if (result == null) return;
            reelResult = result;
            reel = new List<CosmeticItem>();
            for (int i = 0; i < ReelLength; i++) reel.Add(i == ReelWinnerIndex ? result.item : Shop.RandomReelItem(crate));
            reelStart = Time.unscaledTime;
            reelJitter = Random.Range(-0.4f, 0.4f) * ReelItemWidth;
        }

        void DrawReel(PlayerProfile profile)
        {
            GUI.DrawTexture(new Rect(0f, 0f, W, H), whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0f, 0f, 0f, 0.75f), 0f, 0f);

            float t = Mathf.Clamp01((Time.unscaledTime - reelStart) / ReelDuration);
            float eased = 1f - Mathf.Pow(1f - t, 4f); // fast start, slow tense finish
            float stripWidth = Mathf.Min(W - 40f, 900f);
            var strip = new Rect((W - stripWidth) / 2f, H * 0.32f, stripWidth, 130f);
            float target = ReelWinnerIndex * ReelItemWidth + ReelItemWidth / 2f + reelJitter;
            float offset = eased * target - stripWidth / 2f;

            GUI.Box(strip, GUIContent.none, panel);
            GUI.BeginGroup(strip);
            for (int i = 0; i < reel.Count; i++)
            {
                float x = i * ReelItemWidth - offset;
                if (x < -ReelItemWidth || x > stripWidth) continue;
                var r = new Rect(x + 4f, 10f, ReelItemWidth - 8f, 110f);
                DrawItemIcon(new Rect(r.x + (r.width - 70f) / 2f, r.y + 4f, 70f, 70f), reel[i]);
                GUI.Label(new Rect(r.x, r.y + 78f, r.width, 30f), "<color=" + Rarities.Hex(reel[i].rarity) + ">" + reel[i].name + "</color>", centerSmall);
            }
            GUI.EndGroup();
            GUI.DrawTexture(new Rect(W / 2f - 1.5f, strip.y - 8f, 3f, strip.height + 16f), whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(1f, 0.85f, 0.3f), 0f, 0f);

            GUI.Label(new Rect(0f, strip.y - 50f, W, 40f), reelResult.crate.name.ToUpper(), big);
            if (t < 1f) return;

            var item = reelResult.item;
            string line = "<color=" + Rarities.Hex(item.rarity) + "><b>" + item.RarityLabel + "</b>  " + item.name + "</color>";
            Shadowed(new Rect(0f, strip.yMax + 16f, W, 44f), line, big);
            string sub = reelResult.duplicate ? "Already owned: +$" + reelResult.refund + " back" : "Unlocked! Equip it in LOADOUT.";
            GUI.Label(new Rect(0f, strip.yMax + 56f, W, 28f), sub, centerSmall);

            var crate = reelResult.crate;
            if (GUI.Button(new Rect(W / 2f - 210f, strip.yMax + 96f, 200f, 40f), "Nice!")) Defer(() => reel = null);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = profile.money >= crate.price;
            if (GUI.Button(new Rect(W / 2f + 10f, strip.yMax + 96f, 200f, 40f), "Open another ($" + crate.price + ")")) Defer(() => StartReel(profile, crate));
            GUI.enabled = wasEnabled;
        }

        // ================================================================ REFEREE

        void DrawRefereeTab(MatchManager match, PlayerProfile profile)
        {
            GUILayout.Label("<b>The referee job</b>", big);
            GUILayout.Label("Walk the field and watch the fight. When a BB hits someone you'll see a white flash and \"*tak*\". Honest players raise the orange rag and walk back to spawn.", label);
            GUILayout.Label("Some players keep fighting after being hit. <b>Click them</b> to call them out. Call out someone who was never hit and the players will hate you.", label);
            GUILayout.Label("After the match every player rates you. More stars = a higher fee next time.", label);
            GUILayout.Space(12);
            GUILayout.Label(string.Format("Your rating: {0}   ·   jobs {1}   ·   correct calls {2}   ·   wrong calls {3}   ·   missed {4}",
                RefereeProfile.StarText(profile.RefStars), profile.refMatches, profile.refCorrectCalls, profile.refWrongCalls, profile.refMissed), label);
            GUILayout.Label(string.Format("Your fee: <b>${0}</b> per player  ×  8 players  =  <b>${1}</b> per match", profile.RefFeePerPlayer, profile.RefFeePerPlayer * 8), label);
            GUILayout.FlexibleSpace();
            var settings = new MatchSettings { role = Role.Referee, primary = profile.Primary, secondary = profile.Secondary, map = MapLibrary.Get(profile.mapId), mode = profile.mode };
            GUILayout.Label("Map: <b>" + settings.map.name + "</b>   Mode: <b>" + GameModes.Name(settings.mode) + "</b>   (change them in the PLAY tab)", label);
            if (GUILayout.Button("START MATCH AS REFEREE", GUILayout.Height(48))) Defer(() => match.StartMatch(settings));
        }

        // ================================================================ PROFILE

        void DrawProfileTab(PlayerProfile profile)
        {
            float winRate = profile.matches > 0 ? 100f * profile.wins / profile.matches : 0f;
            int owned = 0;
            foreach (var c in CosmeticCatalog.All) if (profile.Owns(c)) owned++;
            int guns = 0, gunTotal = 0;
            foreach (var w in WeaponCatalog.All) { gunTotal++; if (profile.OwnsWeapon(w)) guns++; }

            GUILayout.Label("<b>" + profile.playerName + "</b>", big);
            GUILayout.Label(string.Format("Money <b>${0}</b>", profile.money), label);
            GUILayout.Label(string.Format("Skill rating <b>{0:0}</b>    Honor <b>{1:0}/100</b>", profile.skillRating, profile.honor), label);
            GUILayout.Label(string.Format("Matches {0}   ·   wins {1}   ·   win rate {2:0}%", profile.matches, profile.wins, winRate), label);
            GUILayout.Space(8);
            GUILayout.Label(string.Format("Referee {0}   ·   jobs {1}   ·   correct {2}   ·   wrong {3}   ·   missed {4}",
                RefereeProfile.StarText(profile.RefStars), profile.refMatches, profile.refCorrectCalls, profile.refWrongCalls, profile.refMissed), label);
            GUILayout.Space(8);
            GUILayout.Label(string.Format("Weapons {0}/{1}   ·   cosmetics {2}/{3}   ·   crates opened {4}", guns, gunTotal, owned, CosmeticCatalog.All.Count, profile.cratesOpened), label);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(confirmReset ? "Really wipe everything?" : "Reset save", GUILayout.Width(220f), GUILayout.Height(32f)))
            {
                if (confirmReset) Defer(() => { PlayerProfile.ResetAll(); confirmReset = false; });
                else confirmReset = true;
            }
        }

        // ================================================================ SETTINGS

        void DrawSettingsTab()
        {
            GUILayout.Label("<b>Gameplay</b>", big);
            bool auto = GUILayout.Toggle(GameSettings.AutoCallHits, "  Auto-call my hits");
            bool shake = GUILayout.Toggle(GameSettings.ScreenShake, "  Screen shake");
            bool tags = GUILayout.Toggle(GameSettings.NameTags, "  Name tags above players");
            GUILayout.Space(10);
            GUILayout.Label("<b>Audio</b>", big);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Volume", label, GUILayout.Width(80f));
            float volume = GUILayout.HorizontalSlider(GameSettings.Volume, 0f, 1f, GUILayout.Width(300f));
            GUILayout.Label(Mathf.RoundToInt(volume * 100f) + "%", label);
            GUILayout.EndHorizontal();

            if (auto != GameSettings.AutoCallHits) GameSettings.AutoCallHits = auto;
            if (shake != GameSettings.ScreenShake) GameSettings.ScreenShake = shake;
            if (tags != GameSettings.NameTags) GameSettings.NameTags = tags;
            if (!Mathf.Approximately(volume, GameSettings.Volume)) GameSettings.Volume = volume;
            GameSettings.Save();
        }

        void SelectMap(PlayerProfile profile, MapDefinition map)
        {
            profile.mapId = map.id;
            PlayerProfile.Save();
            if (MapBuilder.Current != map && GameBootstrap.Instance != null) MapBuilder.Build(GameBootstrap.Instance.transform, map);
        }

        // ================================================================ weapon + sprite helpers

        /// <summary>Owned weapons select; locked ones show their price and are bought on click.</summary>
        void WeaponChoice(PlayerProfile profile, WeaponData w, bool selected, bool isPrimary)
        {
            bool owned = profile.OwnsWeapon(w);
            string text = owned ? WeaponButton(w) : WeaponButton(w) + "   <color=#ffd060>BUY $" + w.price + "</color>";
            if (!Choice(selected, text)) return;
            Defer(() =>
            {
                if (!profile.OwnsWeapon(w) && Shop.Buy(profile, w) != PurchaseResult.Ok) return;
                if (isPrimary) profile.primaryWeapon = w.code;
                else profile.secondaryWeapon = w.code;
                PlayerProfile.Save();
            });
        }

        static string WeaponButton(WeaponData w)
        {
            return string.Format("[{0}] {1}   {2}", w.ClassCode, w.displayName, w.code);
        }

        /// <summary>Draws the layered soldier sprite big, facing right, centred in the rect.</summary>
        void DrawSoldierPreview(Rect r, SoldierLook look, WeaponData weapon, Team team)
        {
            if (Event.current.type != EventType.Repaint) return;
            float s = Mathf.Floor(Mathf.Min(r.height / 20f, r.width / 40f));
            float m = SpriteFactory.PixelsPerUnit * s; // screen px per metre
            var c = new Vector2(r.center.x - 0.3f * m, r.center.y);
            DrawSprite(PixelArt.Shadow, c + new Vector2(0.05f, 0.08f) * m, s, Color.white);
            DrawSprite(PixelArt.Foot, c + new Vector2(0.08f, -0.16f) * m, s, Color.white);
            DrawSprite(PixelArt.Foot, c + new Vector2(-0.08f, 0.16f) * m, s, Color.white);
            DrawSprite(PixelArt.Body(look.camo), c, s, look.uniform);
            var art = PixelArt.Gun(weapon);
            DrawSprite(art.sprite, c + new Vector2(art.anchor.x, -art.anchor.y) * m, s, Color.white);
            DrawSprite(PixelArt.HeadGear(look.headGear), c, s, Teams.Color(team));
            DrawSprite(PixelArt.Details, c, s, Color.white);
            // Tracer sample.
            DrawSprite(SpriteFactory.BB, c + new Vector2(art.muzzleDistance + 0.5f, -art.anchor.y) * m, s, look.tracer);
            DrawSprite(SpriteFactory.BB, c + new Vector2(art.muzzleDistance + 0.9f, -art.anchor.y) * m, s, look.tracer);
        }

        void DrawItemIcon(Rect r, CosmeticItem item)
        {
            GUI.DrawTexture(r, whiteTexture, ScaleMode.StretchToFill, true, 0f, Rarities.Color(item.rarity) * 0.35f + new Color(0f, 0f, 0f, 0.6f), 0f, 0f);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 3f, r.width, 3f), whiteTexture, ScaleMode.StretchToFill, true, 0f, Rarities.Color(item.rarity), 0f, 0f);
            if (Event.current.type != EventType.Repaint) return;
            float s = Mathf.Floor(r.height / 18f);
            var c = r.center;
            switch (item.slot)
            {
                case CosmeticSlot.Camo:
                    DrawSprite(PixelArt.Body(item.camo), c, s, new Color(0.47f, 0.52f, 0.36f));
                    break;
                case CosmeticSlot.Uniform:
                    DrawSprite(PixelArt.Body(CamoPattern.Solid), c, s, item.color);
                    break;
                case CosmeticSlot.HeadGear:
                    DrawSprite(PixelArt.HeadGear(item.headGear), c, s, new Color(0.6f, 0.65f, 0.7f));
                    break;
                default:
                    for (int i = 0; i < 4; i++)
                        DrawSprite(SpriteFactory.BB, c + new Vector2(-r.width * 0.3f + i * r.width * 0.18f, 0f), s * (0.6f + i * 0.2f), item.color);
                    break;
            }
        }

        /// <summary>Draws a sprite with its pivot at a GUI point (y down), scaled by whole pixels.</summary>
        static void DrawSprite(Sprite sprite, Vector2 guiPivot, float pixelScale, Color tint)
        {
            var tex = sprite.texture;
            float w = tex.width * pixelScale, h = tex.height * pixelScale;
            var rect = new Rect(guiPivot.x - sprite.pivot.x * pixelScale, guiPivot.y - (tex.height - sprite.pivot.y) * pixelScale, w, h);
            var old = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(rect, tex);
            GUI.color = old;
        }
    }
}
