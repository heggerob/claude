using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Raven feathers, Odin's little secrets: three hidden round every place, each on the highest ground of a small
    /// patch of land out beyond the houses (a knoll, a ridge, a clifftop). They hover and turn slowly, black and
    /// glinting; walk into one to take it: a little gold and Odin's favour. How many you've found is kept.
    /// </summary>
    public static class Feathers
    {
        public const int PerPlace = 3;
        /// <summary>How far out from the place's main building feathers lie (m).</summary>
        public const float MinDistance = 60f, MaxDistance = 450f;
        /// <summary>How close you must come to take one (m), and what it gives.</summary>
        public const float PickRange = 1.4f;
        public const int Gold = 15;
        public const float Favour = 0.04f;

        /// <summary>The key a feather is saved under.</summary>
        public static string Key(Place place, int index) { return place.name + "#" + index; }

        /// <summary>
        /// Where a place's feathers are (global, hovering height above the ground): for each, the highest of a few
        /// dry spots tried round a random point between <see cref="MinDistance"/> and <see cref="MaxDistance"/>,
        /// clear of the buildings. The same spots every time.
        /// </summary>
        public static List<Vector3> Spots(Place place, List<Plot> plots, System.Func<float, float, float> height)
        {
            var list = new List<Vector3>();
            var main = PlaceLife.MainBuilding(plots);
            var rng = new System.Random(place.name.GetHashCode() ^ 0xfea7);
            for (int f = 0, attempt = 0; f < PerPlace && attempt < 24; attempt++)
            {
                // Round a random point (tried again elsewhere if it fell in the sea).
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Lerp(MinDistance, MaxDistance, (float)rng.NextDouble());
                var centre = new Vector3(main.at.x + Mathf.Sin(a) * r, 0f, main.at.z + Mathf.Cos(a) * r);
                Vector3 best = Vector3.zero;
                float bestH = float.MinValue;
                for (int t = 0; t < 12; t++)
                {
                    var p = centre + new Vector3(((float)rng.NextDouble() - 0.5f) * 60f, 0f, ((float)rng.NextDouble() - 0.5f) * 60f);
                    float h = height(p.x, p.z);
                    if (h < 2f * WorldMap.Scale) continue;
                    bool clear = true;
                    foreach (var plot in plots) if (PlaceLife.InsideBuilding(plot, p, 6f)) { clear = false; break; }
                    if (!clear || h <= bestH) continue;
                    bestH = h;
                    best = new Vector3(p.x, h, p.z);
                }
                if (bestH > float.MinValue) { list.Add(best); f++; }
            }
            return list;
        }

        static readonly Color Vane = new Color(0.1f, 0.1f, 0.13f), Sheen = new Color(0.24f, 0.28f, 0.42f), Quill = new Color(0.85f, 0.82f, 0.74f);

        /// <summary>A long black feather, standing on its quill, with a blue-black sheen down one side of the vane.</summary>
        public static VikingModel Model()
        {
            var m = new MergedModel();
            m.Put(Quill, SurfaceKind.Plain, MeshData.Tube(new[] { new Vector3(0f, 0f, 0f), new Vector3(0.02f, 0.35f, 0f), new Vector3(0.06f, 0.7f, 0f) }, new[] { 0.012f, 0.01f, 0.004f }, 5));
            m.Put(Vane, SurfaceKind.Fur, MeshData.Ellipsoid(new Vector3(0.03f, 0.42f, 0f), new Vector3(0.09f, 0.3f, 0.015f), 8, 6));
            m.Put(Sheen, SurfaceKind.Fur, MeshData.Ellipsoid(new Vector3(0.07f, 0.44f, 0.004f), new Vector3(0.04f, 0.24f, 0.012f), 6, 5));
            return m.ToModel("Feather");
        }

        /// <summary>Put up a place's feathers that haven't been taken yet.</summary>
        public static void Build(Transform parent, Place place, List<Vector3> spots)
        {
            for (int i = 0; i < spots.Count; i++)
            {
                if (Upgrades.Current.Feathers.Contains(Key(place, i))) continue;
                var t = new GameObject("Raven Feather").transform;
                t.SetParent(parent, false);
                t.position = WorldOrigin.ToScene(spots[i].x, spots[i].z, spots[i].y + 0.9f);
                ModelView.Show(Model(), t);
                var f = t.gameObject.AddComponent<RavenFeather>();
                f.key = Key(place, i);
            }
        }

        /// <summary>How many feathers there are in the world (for "found 7 of 78").</summary>
        public static int Total { get { return Places.All.Length * PerPlace; } }
    }

    /// <summary>One feather: bobs and turns, and is taken when the player walks into it.</summary>
    public class RavenFeather : MonoBehaviour
    {
        public string key;

        public static readonly List<RavenFeather> All = new List<RavenFeather>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>The feather still lying nearest to a scene position within <paramref name="range"/> m, or null.</summary>
        public static RavenFeather Nearest(Vector3 scenePos, float range)
        {
            RavenFeather best = null;
            float bestD = range;
            foreach (var f in All)
            {
                if (f == null) continue;
                float d = Vector3.Distance(f.transform.position, scenePos);
                if (d < bestD) { bestD = d; best = f; }
            }
            return best;
        }
        Vector3 home;
        bool started;

        void Update()
        {
            if (!started) { home = transform.localPosition; started = true; }
            transform.localPosition = home + Vector3.up * Mathf.Sin(Time.time * 1.6f + home.x) * 0.12f;
            transform.localRotation = Quaternion.Euler(0f, Time.time * 50f, 12f);
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.Player == null) return;
            var d = boot.Player.transform.position + Vector3.up * 0.9f - transform.position;
            if (d.magnitude > Feathers.PickRange) return;
            var u = Upgrades.Current;
            if (!u.Feathers.Contains(key)) u.Feathers.Add(key);
            Fortune.Current.Gold += Feathers.Gold;
            Fortune.Current.AddFavour(Feathers.Favour);
            Sfx.At(SfxId.Blessing, transform.position, 0.8f, 0.1f);
            CombatHud.Banner("A RAVEN FEATHER", "Odin's raven passed this way. " + u.Feathers.Count + " of " + Feathers.Total + " found, and " + Feathers.Gold + " gold.");
            Destroy(gameObject);
        }
    }
}
