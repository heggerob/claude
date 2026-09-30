using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The wager screen at the altar: pick how much gold to risk, see the odds and what's at stake,
    /// flip, then see which god answered. Carve runes into the coin and send Odin's ravens from here too.
    /// Odds are always shown; no real money anywhere.
    /// </summary>
    public class CoinUI : MonoBehaviour
    {
        public static CoinUI Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        enum State { Closed, Choosing, Flipping, Result, Runes }

        public bool IsOpen { get { return state != State.Closed; } }

        State state;
        int wagerIndex = 1;
        FlipResult result;
        GUIStyle title, text, small, big;
        CoinAltar altar;
        // Button actions run in Update so IMGUI's layout and repaint passes always see the same controls.
        System.Action deferred;

        void Awake() { Instance = this; }

        public void Open(CoinAltar coinAltar)
        {
            altar = coinAltar;
            state = State.Choosing;
            altar.Landed -= OnLanded;
            altar.Landed += OnLanded;
            if (CameraRig.Instance != null) CameraRig.Instance.CursorFree = true;
        }

        public void Close()
        {
            state = State.Closed;
            if (CameraRig.Instance != null) CameraRig.Instance.CursorFree = false;
        }

        void OnLanded(FlipResult r)
        {
            result = r;
            state = State.Result;
            // Heads lights up the Viking's face; tails makes them wince.
            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.Player : null;
            Face.On(player, r.heads ? Expression.Happy : Expression.Hurt, r.heads ? 2.5f : 1.2f);
        }

        void Update()
        {
            var action = deferred;
            deferred = null;
            if (action != null) action();

            if (state == State.Runes && GameInput.Pressed(Key.Pause)) state = State.Choosing;
            else if (state == State.Choosing && GameInput.Pressed(Key.Pause)) Close();
            if (state == State.Choosing && GameInput.Pressed(Key.Coin)) DoFlip();
            if (state == State.Result && (GameInput.Pressed(Key.Interact) || GameInput.Pressed(Key.Pause))) Close();
        }

        void DoFlip()
        {
            int wager = Fortune.Wagers[wagerIndex];
            if (!Fortune.Current.CanAfford(wager) || altar == null) return;
            if (altar.Flip(wager) != null) state = State.Flipping;
        }

        void OnGUI()
        {
            if (state == State.Closed || state == State.Flipping) return;
            EnsureStyles();
            var fortune = Fortune.Current;
            float w = 540f, h = state == State.Choosing ? 420f : state == State.Runes ? 460f : 230f;
            var box = new Rect((Screen.width - w) / 2f, Screen.height * 0.12f, w, h);
            GUI.Box(box, GUIContent.none);
            GUI.Box(box, GUIContent.none);
            GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 12f, w - 32f, h - 24f));

            if (state == State.Choosing)
            {
                GUILayout.Label("ODIN'S COIN", title);
                GUILayout.Label("Odin's eye up: your wager back <b>double</b> and a <color=#ffd060>blessing</color>.\nThe serpent up: you <b>lose</b> the wager and get a <color=#88ff88>curse</color>.\nBigger wagers make both stronger and last longer.", text);
                GUILayout.Space(6);
                if (fortune.NextFlipBlessed)
                    GUILayout.Label("<color=#ffd060>Muninn sits on the altar: this flip is <b>Odin's eye</b> for sure.</color>", text);
                else
                    GUILayout.Label(string.Format("Chance of Odin's eye: <b>{0:0}%</b>{1}", fortune.HeadsChance * 100f, fortune.HeadsChance != Fortune.BaseHeadsChance ? "  (runes and blessings)" : ""), text);
                    // The Seer's Foresight: she sees how the coin will fall before the wager.
                    if (Abilities.Has("foresight") && CoinAltar.Instance != null)
                        GUILayout.Label(CoinAltar.Instance.ForeseeHeads() ? "<color=#ffd060><b>Foresight:</b> the coin will show Odin's eye.</color>" : "<color=#88cc88><b>Foresight:</b> the coin will show the serpent.</color>", text);
                GUILayout.Label(string.Format("Odin's eye pays <b>{0:0.##}×</b> the wager · Gold: <b>{1}</b> · Runes: {2}", fortune.PayoutMultiplier, fortune.Gold, RuneLine(fortune)), text);
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                for (int i = 0; i < Fortune.Wagers.Length; i++)
                {
                    int wager = Fortune.Wagers[i];
                    GUI.enabled = fortune.CanAfford(wager);
                    string label = (i == wagerIndex ? "▶ " : "") + (wager == 0 ? "Free" : wager + " gold") + "\ntier " + Fortune.Tier(wager);
                    int index = i;
                    if (GUILayout.Button(label, GUILayout.Height(44))) deferred += () => wagerIndex = index;
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                float wait = altar != null ? altar.ReadyIn : 0f;
                GUI.enabled = wait <= 0f;
                if (GUILayout.Button(wait > 0f ? "The coin is still warm... " + Mathf.CeilToInt(wait) + " s" : "FLIP  [F]", GUILayout.Height(40))) deferred += DoFlip;
                GUI.enabled = true;
                if (GUILayout.Button("Walk away  [Esc]", GUILayout.Height(40), GUILayout.Width(170f))) deferred += Close;
                GUILayout.EndHorizontal();

                // Odin's ravens, when the favour meter is full.
                GUILayout.Space(8);
                GUILayout.Label(fortune.CanCallRavens
                    ? "<color=#ffd060><b>Odin's favour is full.</b> His ravens will fly for you:</color>"
                    : string.Format("<color=#aaaaaa>Odin's favour {0:0}%: win flips, raid and bring plunder home to fill it. When it's full, his ravens fly for you.</color>", fortune.Favour * 100f), small);
                GUILayout.BeginHorizontal();
                var ravens = Ravens.Instance;
                GUI.enabled = fortune.CanCallRavens && ravens != null;
                if (GUILayout.Button("Huginn\n<size=11>circles over the nearest treasure</size>", GUILayout.Height(42)))
                    deferred += () =>
                    {
                        if (ravens.SendHuginn(altar.transform.position)) { Close(); CombatHud.Banner("HUGINN FLIES", "Follow the raven to the treasure."); }
                        else CombatHud.Banner("HUGINN STAYS", "There's no treasure left for him to find.");
                    };
                GUI.enabled = fortune.CanCallRavens && ravens != null && !fortune.NextFlipBlessed;
                if (GUILayout.Button("Muninn\n<size=11>your next flip is Odin's eye</size>", GUILayout.Height(42)))
                    deferred += () => ravens.SendMuninn(altar);
                GUI.enabled = true;
                if (GUILayout.Button("Carve runes...\n<size=11>" + fortune.Carved.Count + "/" + Runes.Slots + " on the rim</size>", GUILayout.Height(42), GUILayout.Width(150f)))
                    deferred += () => state = State.Runes;
                GUILayout.EndHorizontal();
            }
            else if (state == State.Runes)
            {
                DrawRunes(fortune);
            }
            else if (state == State.Result && result != null)
            {
                var card = result.fate.card;
                GUILayout.Label(result.heads ? "<color=#ffd060>ODIN'S EYE!</color>" : "<color=#88ff88>THE SERPENT...</color>", title);
                GUILayout.Label("<b>" + card.name + "</b>  <color=#aaaaaa>tier " + result.fate.tier + " · " + Mathf.CeilToInt(result.fate.remaining) + " s</color>", big);
                GUILayout.Label(card.description, text);
                GUILayout.Space(6);
                GUILayout.Label(result.heads
                    ? (result.wager > 0 ? "You won <b>" + result.payout + "</b> gold back." : "A free blessing. Odin is generous today.")
                    : (result.wager > 0 ? "You lost <b>" + result.wager + "</b> gold." : "At least it was free."), text);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("So be it  [E]", GUILayout.Height(36))) deferred += Close;
            }
            GUILayout.EndArea();
        }

        static string RuneLine(Fortune fortune)
        {
            if (fortune.Carved.Count == 0) return "none";
            var sb = new System.Text.StringBuilder();
            foreach (var r in fortune.Carved) sb.Append(r.glyph).Append(' ');
            return sb.ToString().TrimEnd();
        }

        void DrawRunes(Fortune fortune)
        {
            GUILayout.Label("RUNES", title);
            GUILayout.Label(string.Format("The rune-carver cuts runes into the coin's rim. {0} fit; grinding one off frees the slot but the gold is gone. Gold: <b>{1}</b>", Runes.Slots, fortune.Gold), small);
            GUILayout.Space(4);
            foreach (var rune in Runes.All)
            {
                var r = rune;
                bool carved = fortune.Carved.Contains(r);
                GUILayout.BeginHorizontal();
                GUILayout.Label("<size=26>" + r.glyph + "</size>", big, GUILayout.Width(40f));
                GUILayout.Label("<b>" + r.name + "</b>  " + r.description, small);
                if (carved)
                {
                    if (GUILayout.Button("Grind off", GUILayout.Width(110f), GUILayout.Height(34))) deferred += () => fortune.GrindOff(r);
                }
                else
                {
                    GUI.enabled = fortune.CanCarve(r);
                    if (GUILayout.Button("Carve · " + r.cost + " g", GUILayout.Width(110f), GUILayout.Height(34))) deferred += () => { if (fortune.Carve(r)) { Sfx.Play(SfxId.Purchase); SaveGame.Save(); } };
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.Format("With these runes: {0:0}% chance of Odin's eye, pays {1:0.##}× · on average you get back <b>{2:0.00}</b> gold per gold wagered.", fortune.HeadsChance * 100f, fortune.PayoutMultiplier, fortune.ExpectedReturn), small);
            if (GUILayout.Button("Back  [Esc]", GUILayout.Height(32))) deferred += () => state = State.Choosing;
        }

        void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, richText = true, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(1f, 0.85f, 0.35f);
            big = new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Color.white;
            text = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = true };
            text.normal.textColor = new Color(0.92f, 0.92f, 0.88f);
            small = new GUIStyle(text) { fontSize = 12 };
            GUI.skin.button.richText = true;
        }
    }
}
