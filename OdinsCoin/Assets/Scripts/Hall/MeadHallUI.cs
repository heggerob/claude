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

        enum Tab { Upgrades, Dice, Boasts }
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
            foreach (Tab t in new[] { Tab.Upgrades, Tab.Dice, Tab.Boasts })
            {
                var tt = t;
                string label = (t == tab ? "▶ " : "") + (t == Tab.Upgrades ? "Upgrades" : t == Tab.Dice ? "Dice with Bjorn" : "Boasting board");
                GUI.enabled = dice != DiceState.Rolling && dice != DiceState.Reroll;
                if (GUILayout.Button(label, GUILayout.Height(30))) deferred += () => { tab = tt; if (dice == DiceState.Done) dice = DiceState.Betting; };
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (tab == Tab.Upgrades) DrawUpgrades();
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
