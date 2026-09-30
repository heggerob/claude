using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The North is full size: Kaupang to Hedeby is 300 km, a day and a night under sail. On a quiet passage you
    /// can let the hours slip by faster (T cycles 1x, 2x, 4x, 8x, 16x). It drops back to real time the moment
    /// anything needs you: raiders close, the serpent, a storm, running aground, or leaving the ship.
    /// </summary>
    public class TimeWarp : MonoBehaviour
    {
        public static readonly int[] Levels = { 1, 2, 4, 8, 16 };
        /// <summary>How close an enemy ship may be before time runs normally again (m).</summary>
        public const float EnemyRange = 600f;
        /// <summary>A storm stronger than this needs you at the helm.</summary>
        public const float StormLimit = 0.15f;

        public static int Factor { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Factor = 1; }

        /// <summary>Can time run fast now? And if not, why not (for the HUD).</summary>
        public static bool Allowed(bool onShip, float nearestEnemy, bool serpent, float storm, bool aground, out string why)
        {
            why = null;
            if (!onShip) why = "Only while you're aboard.";
            else if (nearestEnemy < EnemyRange) why = "Not with raiders in sight.";
            else if (serpent) why = "Not with the serpent in the water.";
            else if (storm > StormLimit) why = "Not in a storm.";
            else if (aground) why = "Not while she's aground.";
            return why == null;
        }

        /// <summary>The next speed up from <paramref name="current"/>, back to real time after the fastest.</summary>
        public static int Next(int current)
        {
            for (int i = 0; i < Levels.Length - 1; i++) if (Levels[i] == current) return Levels[i + 1];
            return 1;
        }

        void Update()
        {
            if (GameMenu.Blocking || MeadHallUI.IsOpenNow) return; // the menus own the clock while they're up
            if (Factor < 1) Factor = 1;
            string why;
            bool ok = Allowed(out why);
            if (GameInput.Pressed(Key.TimeWarp))
            {
                int next = Next(Factor);
                if (next > 1 && !ok) CombatHud.Banner("TIME RUNS AS IT WILL", why);
                else Factor = next;
            }
            else if (Factor > 1 && !ok)
            {
                Factor = 1;
                CombatHud.Banner("BACK TO REAL TIME", why);
            }
            Time.timeScale = Factor;
        }

        static bool Allowed(out string why)
        {
            var boot = GameBootstrap.Instance;
            var ship = boot != null ? boot.Ship : null;
            bool onShip = boot != null && boot.Player != null && boot.Player.OnShip;
            float nearest = float.MaxValue;
            if (ship != null)
                foreach (var r in Raider.All)
                    if (r != null && !r.Sinking) nearest = Mathf.Min(nearest, Vector3.Distance(r.transform.position, ship.transform.position));
            float storm = Storm.Instance != null ? Storm.Instance.Intensity : 0f;
            return Allowed(onShip, nearest, Serpent.Instance != null, storm, ship != null && ship.Aground, out why);
        }
    }
}
