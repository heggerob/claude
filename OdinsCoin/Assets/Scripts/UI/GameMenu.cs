using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Title screen over the live harbour (Continue / New voyage / Settings / Quit), the pause menu on Esc,
    /// settings (volume, mouse, invert) and autosaving.
    /// </summary>
    public class GameMenu : MonoBehaviour
    {
        public static GameMenu Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        enum State { Title, Playing, Paused, Settings, Hero }

        /// <summary>True while a menu is up: the Viking and the camera don't take input.</summary>
        public static bool Blocking { get { return Instance != null && Instance.state != State.Playing; } }
        public static bool OnTitle { get { return Instance != null && (Instance.state == State.Title || Instance.state == State.Hero || (Instance.state == State.Settings && Instance.settingsFrom == State.Title)); } }

        public const string VolumeKey = "odinscoin.volume", SensitivityKey = "odinscoin.mouse", InvertKey = "odinscoin.invert";

        State state = State.Title, settingsFrom;
        bool confirmNew, otherUiLastFrame;
        float autosave;
        GUIStyle title, text, small;
        // The hero being edited on the hero screen, and when the model was last rebuilt for the sliders.
        CharacterSpec editing;
        bool heroDirty;
        float lastRebuild;
        System.Action deferred;

        void Awake()
        {
            Instance = this;
            ApplySettings();
            SetCursorFree(true);
        }

        public static void ApplySettings()
        {
            AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            if (CameraRig.Instance != null)
            {
                CameraRig.Instance.Sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 2.2f);
                CameraRig.Instance.InvertY = PlayerPrefs.GetInt(InvertKey, 0) == 1;
                CameraRig.Instance.FirstPerson = PlayerPrefs.GetInt(CameraRig.FirstPersonKey, 1) == 1;
            }
        }

        static void SetCursorFree(bool free) { if (CameraRig.Instance != null) CameraRig.Instance.MenuOpen = free; }

        void Update()
        {
            var action = deferred;
            deferred = null;
            if (action != null) action();

            bool otherUi = MeadHallUI.IsOpenNow;
            if (GameInput.Pressed(Key.Pause))
            {
                // Esc closes the coin or hall screen first; only a free Esc opens the pause menu.
                if (state == State.Playing && !otherUi && !otherUiLastFrame) Pause();
                else if (state == State.Paused) Resume();
                else if (state == State.Settings) CloseSettings();
                else if (state == State.Hero) CloseHero();
            }
            otherUiLastFrame = otherUi;

            if (state == State.Playing)
            {
                autosave += Time.unscaledDeltaTime;
                if (autosave >= SaveGame.AutosaveEvery) { autosave = 0f; SaveGame.Save(); }
            }
        }

        void OnApplicationQuit() { if (state != State.Title) SaveGame.Save(); }

        void Begin(bool load)
        {
            if (load) SaveGame.Load();
            else SaveGame.NewGame();
            // The ship at the jetty is the one the save says you sail.
            if (GameBootstrap.Instance != null) GameBootstrap.Instance.SwapShip(Upgrades.Current.SailingDesign);
            // The places near home were put up behind the title screen: put them up again as this save has them.
            if (PlaceSites.Instance != null) PlaceSites.Instance.Refresh();
            // Her crew, as many as the save's oars upgrade has hired.
            if (GameBootstrap.Instance != null && GameBootstrap.Instance.Ship != null) Crew.Create(GameBootstrap.Instance.Ship);
            // And she's where the voyage was left, if that was out at sea.
            var u = Upgrades.Current;
            SkyClock.Set(u.ClockDay, u.ClockHours);
            PlaceLife.Raided.Clear();
            foreach (var name in u.Raided) PlaceLife.Raided.Add(name);
            ChartReveal.Deserialize(u.Seen);
            // Your home waters are always on the chart.
            if (RealWorld.Active)
            {
                var home = HomeHarbour.HomeCentre + HomeHarbour.Drift;
                ChartReveal.Reveal((float)WorldOrigin.GlobalX(home), (float)WorldOrigin.GlobalZ(home), ChartReveal.SurveyRadius);
            }
            if (load && u.AtSea && GameBootstrap.Instance != null) GameBootstrap.Instance.ResumeAt(u.SeaX, u.SeaZ, u.SeaHeading);
            state = State.Playing;
            autosave = 0f;
            SetCursorFree(false);
            Sfx.Play(SfxId.Blessing, 0.6f);
            CombatHud.Banner(load ? "WELCOME BACK" : "A NEW VOYAGE", load
                ? Fortune.Current.Gold + " gold in your chest. Odin's coin waits on the altar."
                : "Take the steering oar, raise the sail and find some plunder. Gunnar buys it on the jetty.");
        }

        void Pause()
        {
            state = State.Paused;
            Time.timeScale = 0f;
            SetCursorFree(true);
            SaveGame.Save();
        }

        void Resume()
        {
            state = State.Playing;
            Time.timeScale = 1f;
            SetCursorFree(false);
        }

        void OpenSettings() { settingsFrom = state; state = State.Settings; }

        static Viking PlayerViking { get { return GameBootstrap.Instance != null ? GameBootstrap.Instance.Player : null; } }

        void OpenHero()
        {
            var v = PlayerViking;
            editing = HeroChoice.Parse(HeroChoice.Serialize(v != null && v.Hero != null ? v.Hero : HeroChoice.Load()));
            heroDirty = false;
            state = State.Hero;
        }

        void CloseHero()
        {
            HeroChoice.Save(editing);
            RebuildPlayer();
            state = State.Title;
        }

        void RebuildPlayer()
        {
            var v = PlayerViking;
            if (v != null) v.Rebuild(HeroChoice.Parse(HeroChoice.Serialize(editing)));
            heroDirty = false;
            lastRebuild = Time.unscaledTime;
        }

        void CloseSettings()
        {
            PlayerPrefs.Save();
            state = settingsFrom;
        }

        void Quit()
        {
            if (state != State.Title) SaveGame.Save();
            Application.Quit();
        }

        void OnGUI()
        {
            if (state == State.Playing) return;
            EnsureStyles();
            if (OnTitle)
            {
                // Darken the edges so the title reads over the harbour.
                var old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.35f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = old;
            }
            float w = state == State.Hero ? 420f : 380f, h = state == State.Hero ? 470f : state == State.Settings ? 330f : OnTitle ? 400f : 250f;
            var box = new Rect((Screen.width - w) / 2f, Screen.height * 0.2f, w, h);
            GUI.Box(box, GUIContent.none);
            GUI.Box(box, GUIContent.none);
            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 14f, w - 40f, h - 28f));

            if (state == State.Settings) DrawSettings();
            else if (state == State.Hero) DrawHero();
            else if (state == State.Title) DrawTitle();
            else DrawPause();

            GUILayout.EndArea();
        }

        string shownSave;
        Fortune shownFortune;
        Upgrades shownUpgrades;

        void DrawTitle()
        {
            GUILayout.Label("VIKINGFERD", title);
            GUILayout.Label("<color=#aaaaaa>Sail, raid, explore, and stake your treasure on the All-Father's coin.</color>", small);
            GUILayout.Space(12);
            if (SaveGame.Exists)
            {
                // Read the save once, not every frame the title is drawn.
                var raw = PlayerPrefs.GetString(SaveGame.Key, "");
                if (raw != shownSave) { SaveGame.Deserialize(raw, out shownFortune, out shownUpgrades); shownSave = raw; }
                var f = shownFortune; var u = shownUpgrades;
                if (GUILayout.Button("<b>Continue</b>\n<size=12>" + f.Gold + " gold · " + f.ChestsSold + " chests brought home · " + (u.Shrines.Count + u.Dug.Count + u.Caves.Count + u.Feathers.Count) + " secrets found</size>", GUILayout.Height(52)))
                    deferred += () => Begin(true);
            }
            string newLabel = confirmNew ? "<color=#ff9966>Really start over? Your save is lost.</color>" : "New voyage";
            if (GUILayout.Button(newLabel, GUILayout.Height(40)))
                deferred += () => { if (SaveGame.Exists && !confirmNew) confirmNew = true; else Begin(false); };
            var hero = PlayerViking != null && PlayerViking.Hero != null ? PlayerViking.Hero : HeroChoice.Load();
            if (GUILayout.Button("Your hero: <b>" + Outfits.Get(hero.outfit).title + "</b>", GUILayout.Height(34))) deferred += OpenHero;
            if (GUILayout.Button("Settings", GUILayout.Height(34))) deferred += OpenSettings;
            if (GUILayout.Button("Quit", GUILayout.Height(34))) deferred += Quit;
            GUILayout.FlexibleSpace();
            GUILayout.Label("<color=#888888>In-game gold only. No real money, ever.</color>", small);
        }

        void DrawPause()
        {
            GUILayout.Label("PAUSED", title);
            GUILayout.Label("<color=#aaaaaa>Game saved.</color>", small);
            GUILayout.Space(10);
            if (GUILayout.Button("Back to the sea  [Esc]", GUILayout.Height(40))) deferred += Resume;
            if (GUILayout.Button("Settings", GUILayout.Height(34))) deferred += OpenSettings;
            if (GUILayout.Button("Save and quit", GUILayout.Height(34))) deferred += Quit;
        }

        void DrawHero()
        {
            GUILayout.Label("YOUR HERO", title);
            var outfit = Outfits.Get(editing.outfit);
            // Outfit: changing it brings that outfit's own hair, weapon and off-hand, but keeps the body.
            if (Stepper(outfit.title, s =>
            {
                var body = editing.body;
                int tone = editing.skinTone;
                editing = CharacterSpec.Default(HeroChoice.Cycle(editing.outfit, s));
                editing.body = body;
                editing.skinTone = tone;
            })) RebuildPlayer();
            foreach (var a in outfit.abilities)
                GUILayout.Label("<b>" + a.name + "</b>  <color=#aaaaaa>" + a.description + "</color>", small);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            foreach (Gender g in System.Enum.GetValues(typeof(Gender)))
            {
                bool on = editing.body.gender == g;
                if (GUILayout.Toggle(on, " " + g, GUI.skin.button, GUILayout.Height(28)) && !on) { editing.body.gender = g; RebuildPlayer(); }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Height: " + editing.body.height.ToString("0.00") + " m", text);
            float nh = GUILayout.HorizontalSlider(editing.body.height, 1.45f, 1.9f);
            if (Mathf.Abs(nh - editing.body.height) > 0.001f) { editing.body.height = BodyShape.ClampHeight(nh); heroDirty = true; }
            GUILayout.Label("Build: " + (editing.body.width < 0.9f ? "slim" : editing.body.width > 1.1f ? "broad" : "sturdy"), text);
            float nw = GUILayout.HorizontalSlider(editing.body.width, 0.8f, 1.25f);
            if (Mathf.Abs(nw - editing.body.width) > 0.001f) { editing.body.width = BodyShape.ClampWidth(nw); heroDirty = true; }
            if (Stepper("Skin: " + SkinTones.Name(editing.skinTone), s => editing.skinTone = ((editing.skinTone + s) % SkinTones.Count + SkinTones.Count) % SkinTones.Count)) RebuildPlayer();
            // Sliders rebuild the model a few times a second at most, and once more when they stop moving.
            if (heroDirty && Time.unscaledTime - lastRebuild > 0.25f) RebuildPlayer();
            GUILayout.Space(8);

            if (Stepper("Weapon: " + HeroChoice.Name(editing.weapon), s => editing.weapon = HeroChoice.Cycle(editing.weapon, s))) RebuildPlayer();
            if (Stepper("Off hand: " + HeroChoice.Name(editing.offHand), s => editing.offHand = HeroChoice.Cycle(editing.offHand, s))) RebuildPlayer();
            // Colours: the skins you own for this outfit (more are sold in the mead hall).
            var owned = Skins.For(editing.outfit).FindAll(k => SkinLocker.Current.Owns(k));
            int at = Mathf.Max(0, owned.FindIndex(k => k.id == (editing.skin ?? Skins.ClassicId(editing.outfit))));
            if (Stepper("Colours: " + owned[at].name, s =>
            {
                var next = owned[((at + s) % owned.Count + owned.Count) % owned.Count];
                editing.skin = next.IsClassic ? null : next.id;
            })) RebuildPlayer();
            if (owned.Count < Skins.For(editing.outfit).Count)
                GUILayout.Label("<color=#888888>More colours are sold in the mead hall.</color>", small);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Done  [Esc]", GUILayout.Height(34))) deferred += CloseHero;
        }

        /// <summary>A "◀ label ▶" row; returns true when a button was pressed (after calling change with -1 or +1).</summary>
        bool Stepper(string label, System.Action<int> change)
        {
            bool pressed = false;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(36), GUILayout.Height(30))) { change(-1); pressed = true; }
            GUILayout.Label(label, text, GUILayout.Height(30));
            if (GUILayout.Button(">", GUILayout.Width(36), GUILayout.Height(30))) { change(1); pressed = true; }
            GUILayout.EndHorizontal();
            return pressed;
        }

        void DrawSettings()
        {
            GUILayout.Label("SETTINGS", title);
            GUILayout.Space(8);
            float vol = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            GUILayout.Label("Volume: " + Mathf.RoundToInt(vol * 100f) + "%", text);
            float nv = GUILayout.HorizontalSlider(vol, 0f, 1f);
            if (Mathf.Abs(nv - vol) > 0.001f) { PlayerPrefs.SetFloat(VolumeKey, nv); ApplySettings(); }

            float sens = PlayerPrefs.GetFloat(SensitivityKey, 2.2f);
            GUILayout.Space(6);
            GUILayout.Label("Mouse sensitivity: " + sens.ToString("0.0"), text);
            float ns = GUILayout.HorizontalSlider(sens, 0.5f, 6f);
            if (Mathf.Abs(ns - sens) > 0.001f) { PlayerPrefs.SetFloat(SensitivityKey, ns); ApplySettings(); }

            GUILayout.Space(6);
            bool inv = PlayerPrefs.GetInt(InvertKey, 0) == 1;
            bool ni = GUILayout.Toggle(inv, " Invert mouse Y");
            if (ni != inv) { PlayerPrefs.SetInt(InvertKey, ni ? 1 : 0); ApplySettings(); }
            bool fp = PlayerPrefs.GetInt(CameraRig.FirstPersonKey, 1) == 1;
            bool nfp = GUILayout.Toggle(fp, " First person (V switches in play)");
            if (nfp != fp) { PlayerPrefs.SetInt(CameraRig.FirstPersonKey, nfp ? 1 : 0); ApplySettings(); }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Back  [Esc]", GUILayout.Height(34))) deferred += CloseSettings;
        }

        void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, richText = true, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(1f, 0.82f, 0.35f);
            text = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = true };
            text.normal.textColor = new Color(0.92f, 0.92f, 0.88f);
            small = new GUIStyle(text) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            GUI.skin.button.richText = true;
        }
    }
}
