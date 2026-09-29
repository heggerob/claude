using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The wager screen at the altar: pick how much gold to risk, see the odds and what's at stake,
    /// flip, then see which god answered. Odds are always shown; no real money anywhere.
    /// </summary>
    public class CoinUI : MonoBehaviour
    {
        public static CoinUI Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        enum State { Closed, Choosing, Flipping, Result }

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
        }

        void Update()
        {
            var action = deferred;
            deferred = null;
            if (action != null) action();

            if (state == State.Choosing && GameInput.Pressed(Key.Pause)) Close();
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
            float w = 520f, h = state == State.Choosing ? 330f : 230f;
            var box = new Rect((Screen.width - w) / 2f, Screen.height * 0.12f, w, h);
            GUI.Box(box, GUIContent.none);
            GUI.Box(box, GUIContent.none);
            GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 12f, w - 32f, h - 24f));

            if (state == State.Choosing)
            {
                GUILayout.Label("ODIN'S COIN", title);
                GUILayout.Label("Odin's eye up: your wager back <b>double</b> and a <color=#ffd060>blessing</color>.\nThe serpent up: you <b>lose</b> the wager and get a <color=#88ff88>curse</color>.\nBigger wagers make both stronger and last longer.", text);
                GUILayout.Space(6);
                GUILayout.Label(string.Format("Chance of Odin's eye: <b>{0:0}%</b>{1}", fortune.HeadsChance * 100f, fortune.HeadsChance > Fortune.BaseHeadsChance ? "  (runes and favour)" : ""), text);
                GUILayout.Label("Gold: <b>" + fortune.Gold + "</b>", text);
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
                if (GUILayout.Button("FLIP  [F]", GUILayout.Height(40))) deferred += DoFlip;
                if (GUILayout.Button("Walk away  [Esc]", GUILayout.Height(40), GUILayout.Width(170f))) deferred += Close;
                GUILayout.EndHorizontal();
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
