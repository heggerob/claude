using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The mead hall, run by Bjorn: buy ship and gear upgrades, play dice for gold, read the boasting board.
    /// In-game gold only; the dice odds are printed on the table.
    /// </summary>
    public class MeadHallUI : MonoBehaviour
    {
        public static MeadHallUI Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public static bool IsOpenNow { get { return Instance != null && Instance.open; } }

        enum Tab { Upgrades, Ships, Commissions, Skins, Dice, Boasts }
        enum DiceState { Betting, Rolling, Reroll, Done }

        const float RollTime = 1.1f;

        public Longship Ship;
        bool open;
        Tab tab;
        DiceState dice;
        int stakeIndex = 1;
        int[] mine = { 1, 1, 1 }, bjorn = { 1, 1, 1 };
        int outcome, change;
        float rollStart;
        bool rerollUsed;
        readonly System.Random rng = new System.Random();
        GUIStyle title, text, small, big;
        System.Action deferred;

        void Awake() { Instance = this; }

        public void Open()
        {
            open = true;
            tab = Tab.Upgrades;
            dice = DiceState.Betting;
            if (CameraRig.Instance != null) CameraRig.Instance.CursorFree = true;
        }

        public void Close()
        {
            if (dice == DiceState.Rolling) return;
            // Walking off mid-game with a reroll pending settles the hand as it stands.
            if (dice == DiceState.Reroll) Finish();
            open = false;
            if (CameraRig.Instance != null) CameraRig.Instance.CursorFree = false;
        }

        void Update()
        {
            var action = deferred;
            deferred = null;
            if (action != null) action();
            if (!open) return;
            if (GameInput.Pressed(Key.Pause)) Close();
            if (dice == DiceState.Rolling && Time.time - rollStart >= RollTime) Landed();
        }

        // ---------------------------------------------------------------- dice

        void Roll()
        {
            int stake = MeadDice.Stakes[stakeIndex];
            if (Fortune.Current.Gold < stake) return;
            mine = MeadDice.Roll(rng);
            bjorn = MeadDice.Roll(rng);
            rerollUsed = false;
            rollStart = Time.time;
            dice = DiceState.Rolling;
            Sfx.Play(SfxId.DiceRattle, 0.8f);
        }

        void Landed()
        {
            // Odin's Favour: if you're not already winning, you may reroll your lowest die once.
            bool favoured = Fortune.Current.Has(FateEffect.Luck, FateKind.Blessing);
            if (favoured && !rerollUsed && MeadDice.Compare(mine, bjorn) <= 0) { dice = DiceState.Reroll; return; }
            Finish();
        }

        void Reroll()
        {
            rerollUsed = true;
            mine[MeadDice.Lowest(mine)] = rng.Next(1, 7);
            Finish();
        }

        void Finish()
        {
            outcome = MeadDice.Compare(mine, bjorn);
            change = MeadDice.Settle(Fortune.Current, MeadDice.Stakes[stakeIndex], outcome);
            Sfx.Play(SfxId.DiceLand, 0.8f);
            if (outcome > 0) Sfx.Play(SfxId.Gold, 0.7f);
            dice = DiceState.Done;
        }

        // ---------------------------------------------------------------- drawing

        void OnGUI()
        {
            if (!open) return;
            EnsureStyles();
            float w = 600f, h = 470f;
            var box = new Rect((Screen.width - w) / 2f, Screen.height * 0.1f, w, h);
            GUI.Box(box, GUIContent.none);
            GUI.Box(box, GUIContent.none);
            GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 12f, w - 32f, h - 24f));
            GUILayout.Label("THE MEAD HALL", title);
            GUILayout.Label("<color=#aaaaaa>Bjorn wipes a horn and nods at you.</color>   Gold: <b>" + Fortune.Current.Gold + "</b>", text);
            GUILayout.BeginHorizontal();
            foreach (Tab t in new[] { Tab.Upgrades, Tab.Ships, Tab.Commissions, Tab.Skins, Tab.Dice, Tab.Boasts })
            {
                var tt = t;
                string label = (t == tab ? "▶ " : "") + (t == Tab.Upgrades ? "Upgrades" : t == Tab.Ships ? "Shipwright" : t == Tab.Commissions ? "Commissions" : t == Tab.Skins ? "Colours" : t == Tab.Dice ? "Dice with Bjorn" : "Boasting board");
                GUI.enabled = dice != DiceState.Rolling && dice != DiceState.Reroll;
                if (GUILayout.Button(label, GUILayout.Height(30))) deferred += () => { tab = tt; if (dice == DiceState.Done) dice = DiceState.Betting; };
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (tab == Tab.Upgrades) DrawUpgrades();
            else if (tab == Tab.Ships) DrawShips();
            else if (tab == Tab.Commissions) DrawCommissions();
            else if (tab == Tab.Skins) DrawSkins();
            else if (tab == Tab.Dice) DrawDice();
            else DrawBoasts();

            GUILayout.FlexibleSpace();
            GUI.enabled = dice != DiceState.Rolling;
            if (GUILayout.Button("Leave the hall  [Esc]", GUILayout.Height(32))) deferred += Close;
            GUI.enabled = true;
            GUILayout.EndArea();

            if (tab == Tab.Dice) DrawDiceFaces(box);
        }

        void DrawUpgrades()
        {
            var up = Upgrades.Current;
            var fortune = Fortune.Current;
            foreach (var def in Upgrades.All)
            {
                var d = def;
                int level = up.Level(d.kind);
                GUILayout.BeginHorizontal();
                string have = level == 0 ? "<color=#888888>none</color>" : d.levels[level - 1];
                string pips = new string('●', level) + new string('○', d.costs.Length - level);
                GUILayout.Label(string.Format("<b>{0}</b> {1}\n<size=12>{2} · <color=#aaaaaa>{3}</color></size>", d.name, pips, have, d.effect), text);
                if (up.Maxed(d.kind))
                {
                    GUI.enabled = false;
                    GUILayout.Button("The best there is", GUILayout.Width(190f), GUILayout.Height(40));
                }
                else
                {
                    GUI.enabled = up.CanBuy(d.kind, fortune);
                    if (GUILayout.Button(d.levels[level] + "\n<size=12>" + up.NextCost(d.kind) + " gold</size>", GUILayout.Width(190f), GUILayout.Height(40)))
                        deferred += () => { if (up.Buy(d.kind, fortune)) { Sfx.Play(SfxId.Purchase); SaveGame.Save(); } if (up.Level(d.kind) > level) CombatHud.Banner(d.levels[level].ToUpper(), "Bjorn's cousin will have it done by the time you're aboard."); };
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            // Cargo still on the ship can be sold from here too, if the ship is moored.
            var home = HomeHarbour.Instance;
            int count;
            int gold = HomeHarbour.CargoValue(Ship, out count);
            if (home != null && count > 0 && home.ShipInRange(Ship))
            {
                GUILayout.Space(4);
                if (GUILayout.Button(string.Format("Have Gunnar fetch the cargo off your ship: {0} chest{1}, {2} gold", count, count == 1 ? "" : "s", gold), GUILayout.Height(30)))
                    deferred += () => { int n; int g = home.SellCargo(Ship, out n); CombatHud.Banner("+" + g + " GOLD", "Gunnar hauls " + n + " chest" + (n == 1 ? "" : "s") + " up the hill."); };
            }
        }

        /// <summary>Where home is, in global map coordinates.</summary>
        static Vector3 HomeGlobal()
        {
            var home = HomeHarbour.HomeCentre + HomeHarbour.Drift;
            return new Vector3((float)WorldOrigin.GlobalX(home), 0f, (float)WorldOrigin.GlobalZ(home));
        }

        /// <summary>Bjorn's commissions: one raid at a time on a real place, for a bonus when it's plundered.</summary>
        void DrawCommissions()
        {
            var up = Upgrades.Current;
            var map = WorldMap.Current;
            if (map == null || !RealWorld.Active) { GUILayout.Label("Bjorn has no work for you on the storybook isles.", text); return; }
            var homeGlobal = HomeGlobal();
            if (!string.IsNullOrEmpty(up.Commission))
            {
                var place = Places.Find(up.Commission);
                float km = place != null ? Vector3.Distance(Places.Position(map, place), homeGlobal) / 1000f : 0f;
                GUILayout.Label(string.Format("<b>Your commission:</b> plunder <b>{0}</b> ({1}), {2:0} km from home.\n<size=12>Bjorn pays <b>{3} gold</b> the moment the last chest is carried off.</size>",
                    up.Commission, place != null ? place.modern : "?", km, up.CommissionReward), text);
                if (GUILayout.Button("Give it up", GUILayout.Width(190f), GUILayout.Height(32)))
                    deferred += () => { up.Commission = null; up.CommissionReward = 0; SaveGame.Save(); };
                return;
            }
            GUILayout.Label("<size=12>\"There's silver to be had,\" says Bjorn. \"Strip one of these and I'll pay you on top of what you carry off.\"</size>", text);
            foreach (var offer in Commissions.Offers(map, homeGlobal, PlaceLife.Raided, 3, SkyClock.Day * 7919 + 17))
            {
                var o = offer;
                float km = Vector3.Distance(Places.Position(map, o.place), homeGlobal) / 1000f;
                var plunder = PlaceLife.PlunderOf(o.place.kind);
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("<b>{0}</b> ({1}), {2:0} km\n<size=12>{3} chests, {4} guards · <color=#aaaaaa>{5}</color></size>", o.place.name, o.place.modern, km, plunder.chests, plunder.guards, o.place.blurb), text);
                if (GUILayout.Button("Take it on\n<size=12>+" + o.reward + " gold</size>", GUILayout.Width(190f), GUILayout.Height(48)))
                    deferred += () => { up.Commission = o.place.name; up.CommissionReward = o.reward; SaveGame.Save(); CombatHud.Banner("A COMMISSION", "Plunder " + o.place.name + " for Bjorn: +" + o.reward + " gold when it's done."); };
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// The shipwright's board: every hull, what it costs, and the ones you own to choose between. A new ship is
        /// rigged at the jetty, so she has to be lying there to swap.
        /// </summary>
        void DrawShips()
        {
            var up = Upgrades.Current;
            var fortune = Fortune.Current;
            var home = HomeHarbour.Instance;
            bool atJetty = home != null && Ship != null && home.ShipInRange(Ship);
            foreach (var design in ShipDesign.All)
            {
                var d = design;
                bool owned = up.Owns(d), sailing = Ship != null && Ship.Design == d;
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("<b>{0}</b>{1}\n<size=12>{2}\n<color=#aaaaaa>{3}</color></size>", d.title,
                    sailing ? "  <color=#ffd060>(at the jetty)</color>" : owned ? "  <color=#88cc88>(yours)</color>" : "", d.blurb, Shipwright.Numbers(d)), text);
                if (sailing)
                {
                    GUI.enabled = false;
                    GUILayout.Button("Your ship", GUILayout.Width(190f), GUILayout.Height(52));
                }
                else if (owned)
                {
                    GUI.enabled = atJetty;
                    if (GUILayout.Button("Sail her\n<size=12>rigged at the jetty</size>", GUILayout.Width(190f), GUILayout.Height(52)))
                        deferred += () => { up.Sailing = d.id; GameBootstrap.Instance.SwapShip(d); SaveGame.Save(); CombatHud.Banner(d.title.ToUpper(), "Your crew carry the cargo and the coin across to her."); };
                }
                else
                {
                    GUI.enabled = atJetty && up.CanBuyShip(d, fortune);
                    if (GUILayout.Button("Buy her\n<size=12>" + Shipwright.Price(d) + " gold</size>", GUILayout.Width(190f), GUILayout.Height(52)))
                        deferred += () =>
                        {
                            if (!up.BuyShip(d, fortune)) return;
                            Sfx.Play(SfxId.Purchase);
                            GameBootstrap.Instance.SwapShip(d);
                            SaveGame.Save();
                            CombatHud.Banner(d.title.ToUpper(), "The shipwright's crew rig her at the jetty. She's yours.");
                        };
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            if (!atJetty) GUILayout.Label("<size=12><color=#ff9a6a>Bring your ship alongside the jetty to buy or change hulls.</color></size>", text);
        }

        /// <summary>
        /// Skins for the hero you're playing: try one on the turning stand, buy it, wear it. Only colours change;
        /// the abilities stay the same.
        /// </summary>
        void DrawSkins()
        {
            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.Player : null;
            var hero = player != null && player.Hero != null ? player.Hero : HeroChoice.Load();
            var locker = SkinLocker.Current;
            var fortune = Fortune.Current;
            GUILayout.Label("<color=#aaaaaa>Colours for " + Outfits.Get(hero.outfit).title + ". Look at the stand by the door. Only the look changes.</color>", small);
            string wearing = hero.skin ?? Skins.ClassicId(hero.outfit);
            foreach (var skin in Skins.For(hero.outfit))
            {
                var s = skin;
                bool owned = locker.Owns(s), worn = s.id == wearing;
                GUILayout.BeginHorizontal();
                GUILayout.Label("<b>" + s.name + "</b>" + (worn ? "  <color=#ffd060>(wearing)</color>" : "") + "\n<size=12><color=#aaaaaa>" + s.blurb + "</color></size>", text);
                if (GUILayout.Button("Try on", GUILayout.Width(80f), GUILayout.Height(40))) deferred += () => ShowOnStand(hero, s);
                if (owned)
                {
                    GUI.enabled = !worn;
                    if (GUILayout.Button(worn ? "Worn" : "Wear", GUILayout.Width(110f), GUILayout.Height(40))) deferred += () => Wear(hero, s);
                }
                else if (s.rare)
                {
                    // Rare skins aren't for sale: they're found in plundered chests (the chance is printed).
                    GUI.enabled = false;
                    GUILayout.Button("Found in plunder\n<size=12>" + Mathf.RoundToInt(Skins.RareChance * 100f) + "% per chest sold</size>", GUILayout.Width(110f), GUILayout.Height(40));
                }
                else
                {
                    GUI.enabled = locker.CanBuy(s, fortune);
                    if (GUILayout.Button("Buy\n<size=12>" + s.cost + " gold</size>", GUILayout.Width(110f), GUILayout.Height(40)))
                        deferred += () => { if (locker.Buy(s, fortune)) { locker.Save(); SaveGame.Save(); Sfx.Play(SfxId.Purchase); Wear(hero, s); CombatHud.Banner(s.name.ToUpper(), "Bjorn's wife has it dyed and stitched while you drink."); } };
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        static CharacterSpec WithSkin(CharacterSpec hero, SkinDef s)
        {
            var spec = HeroChoice.Parse(HeroChoice.Serialize(hero));
            spec.palette = null;
            spec.skin = s.IsClassic ? null : s.id;
            return spec;
        }

        static void ShowOnStand(CharacterSpec hero, SkinDef s)
        {
            if (SkinStand.Instance != null) SkinStand.Instance.Show(WithSkin(hero, s));
        }

        static void Wear(CharacterSpec hero, SkinDef s)
        {
            var spec = WithSkin(hero, s);
            HeroChoice.Save(spec);
            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.Player : null;
            if (player != null) player.Rebuild(spec);
            ShowOnStand(spec, s);
        }

        void DrawDice()
        {
            var fortune = Fortune.Current;
            GUILayout.Label("Three dice each. Higher total wins the stake; any triple beats any total. A tie gives your stake back.\n<color=#aaaaaa>An even game: you win as often as Bjorn does. With Odin's Favour you may reroll your lowest die once.</color>", small);
            GUILayout.Space(116); // the dice are drawn here
            if (dice == DiceState.Rolling)
            {
                GUILayout.Label("The dice rattle across the table...", big);
                return;
            }
            if (dice == DiceState.Reroll)
            {
                GUILayout.Label(string.Format("You: {0}   Bjorn: {1}", MeadDice.Describe(mine), MeadDice.Describe(bjorn)), big);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<color=#ffd060>Odin's Favour: reroll your lowest die</color>", GUILayout.Height(36))) deferred += Reroll;
                if (GUILayout.Button("Keep it", GUILayout.Height(36), GUILayout.Width(140f))) deferred += Finish;
                GUILayout.EndHorizontal();
                return;
            }
            if (dice == DiceState.Done)
            {
                string verdict = outcome > 0 ? "<color=#ffd060>You win " + change + " gold!</color>"
                               : outcome < 0 ? "<color=#ff8866>Bjorn wins " + (-change) + " gold.</color>"
                               : "A tie. Stakes back.";
                GUILayout.Label(string.Format("You: {0}   Bjorn: {1}\n{2}", MeadDice.Describe(mine), MeadDice.Describe(bjorn), verdict), big);
            }
            GUILayout.BeginHorizontal();
            for (int i = 0; i < MeadDice.Stakes.Length; i++)
            {
                int index = i;
                GUI.enabled = fortune.Gold >= MeadDice.Stakes[i];
                if (GUILayout.Button((i == stakeIndex ? "▶ " : "") + MeadDice.Stakes[i] + " gold", GUILayout.Height(32))) deferred += () => stakeIndex = index;
                GUI.enabled = true;
            }
            GUI.enabled = fortune.Gold >= MeadDice.Stakes[stakeIndex];
            if (GUILayout.Button("<b>ROLL</b>", GUILayout.Height(32), GUILayout.Width(110f))) deferred += Roll;
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        void DrawDiceFaces(Rect box)
        {
            bool rolling = dice == DiceState.Rolling;
            float y = box.y + 190f;
            GUI.Label(new Rect(box.x + 40f, y - 22f, 200f, 20f), "<b>You</b>", text);
            GUI.Label(new Rect(box.x + box.width - 240f, y - 22f, 200f, 20f), "<b>Bjorn</b>", text);
            for (int i = 0; i < 3; i++)
            {
                int a = rolling ? rng.Next(1, 7) : mine[i];
                int b = rolling ? rng.Next(1, 7) : bjorn[i];
                float wobble = rolling ? Mathf.Sin(Time.time * 30f + i) * 4f : 0f;
                DrawDie(new Rect(box.x + 40f + i * 62f, y + wobble, 52f, 52f), a, dice == DiceState.Reroll && i == MeadDice.Lowest(mine));
                DrawDie(new Rect(box.x + box.width - 240f + i * 62f, y - wobble, 52f, 52f), b, false);
            }
        }

        /// <summary>A bone die with pips, drawn from rectangles so it doesn't depend on the font.</summary>
        static void DrawDie(Rect r, int face, bool highlight)
        {
            var old = GUI.color;
            GUI.color = highlight ? new Color(1f, 0.85f, 0.4f) : new Color(0.93f, 0.9f, 0.8f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(0.15f, 0.1f, 0.08f);
            float s = r.width / 4f, p = r.width * 0.16f;
            bool[,] pips = Pips(face);
            for (int gx = 0; gx < 3; gx++)
                for (int gy = 0; gy < 3; gy++)
                    if (pips[gx, gy])
                        GUI.DrawTexture(new Rect(r.x + s * (gx + 1) - p / 2f, r.y + s * (gy + 1) - p / 2f, p, p), Texture2D.whiteTexture);
            GUI.color = old;
        }

        static bool[,] Pips(int face)
        {
            var g = new bool[3, 3];
            if (face % 2 == 1) g[1, 1] = true;
            if (face >= 2) { g[0, 0] = true; g[2, 2] = true; }
            if (face >= 4) { g[2, 0] = true; g[0, 2] = true; }
            if (face == 6) { g[0, 1] = true; g[2, 1] = true; }
            return g;
        }

        void DrawBoasts()
        {
            var f = Fortune.Current;
            GUILayout.Label("<b>The boasting board</b>  <color=#aaaaaa>(carved into the hall's main post)</color>", text);
            GUILayout.Space(4);
            GUILayout.Label(string.Format(
                "Chests brought home: <b>{0}</b>\nGold plundered: <b>{1}</b>\nOdin's coin flipped: <b>{2}</b> times, Odin's eye <b>{3}</b> times\nDice with Bjorn: <b>{4}</b> won, <b>{5}</b> lost\nRunes on your coin: <b>{6}</b>",
                f.ChestsSold, f.GoldPlundered, f.Flips, f.HeadsCount, f.DiceWon, f.DiceLost, f.Carved.Count), text);
        }

        void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, richText = true, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(1f, 0.75f, 0.4f);
            big = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            big.normal.textColor = Color.white;
            text = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = true };
            text.normal.textColor = new Color(0.92f, 0.92f, 0.88f);
            small = new GUIStyle(text) { fontSize = 12 };
            GUI.skin.button.richText = true;
        }
    }
}
