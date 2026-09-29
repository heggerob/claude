using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Odin's ravens. When the favour meter is full you can send one from the altar:
    /// <b>Huginn</b> (thought) flies out and circles over the nearest hidden treasure;
    /// <b>Muninn</b> (memory) perches on the altar and the next flip is Odin's eye for sure.
    /// </summary>
    public class Ravens : MonoBehaviour
    {
        public static Ravens Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public const float FavourPerKill = 0.08f, FavourPerChest = 0.06f;
        public const float HuginnTime = 150f;
        public const int HuginnTargets = 3;

        /// <summary>Chests Huginn is circling over right now.</summary>
        public readonly List<TreasureChest> Marked = new List<TreasureChest>();
        public float HuginnLeft { get; private set; }
        public bool MuninnPerched { get { return muninn != null; } }

        readonly List<RavenBird> huginn = new List<RavenBird>();
        RavenBird muninn;

        void Awake() { Instance = this; }

        /// <summary>Indices of the (at most <paramref name="max"/>) points nearest <paramref name="from"/>, nearest first.</summary>
        public static List<int> Nearest(IList<Vector3> points, Vector3 from, int max)
        {
            var order = new List<int>();
            for (int i = 0; i < points.Count; i++) order.Add(i);
            order.Sort((a, b) => (points[a] - from).sqrMagnitude.CompareTo((points[b] - from).sqrMagnitude));
            if (order.Count > max) order.RemoveRange(max, order.Count - max);
            return order;
        }

        /// <summary>Send Huginn to find treasure. Spends a full favour meter; false if there's nothing to find.</summary>
        public bool SendHuginn(Vector3 from)
        {
            var ship = GameBootstrap.Instance != null ? GameBootstrap.Instance.Ship : null;
            var chests = new List<TreasureChest>();
            var points = new List<Vector3>();
            foreach (var c in TreasureChest.All)
            {
                if (c == null || c.Carried || c.Sold || c.Stowed(ship)) continue;
                chests.Add(c);
                points.Add(c.transform.position);
            }
            if (chests.Count == 0 || !Fortune.Current.SpendFavour()) return false;

            ClearHuginn();
            foreach (int i in Nearest(points, from, HuginnTargets))
            {
                Marked.Add(chests[i]);
                huginn.Add(RavenBird.Create(transform, from + Vector3.up * 2f, chests[i].transform, false));
            }
            HuginnLeft = HuginnTime;
            Sfx.Play(SfxId.Raven, 0.8f);
            return true;
        }

        /// <summary>Send Muninn to the altar: the next flip lands Odin's eye up.</summary>
        public bool SendMuninn(CoinAltar altar)
        {
            if (altar == null || Fortune.Current.NextFlipBlessed || !Fortune.Current.SpendFavour()) return false;
            Fortune.Current.NextFlipBlessed = true;
            Sfx.Play(SfxId.Raven, 0.8f);
            muninn = RavenBird.Create(transform, altar.transform.position + new Vector3(0f, 12f, -8f), altar.transform, true);
            return true;
        }

        void Update()
        {
            if (HuginnLeft > 0f)
            {
                HuginnLeft -= Time.deltaTime;
                // A chest that's been picked up no longer needs a raven over it.
                for (int i = Marked.Count - 1; i >= 0; i--)
                    if (Marked[i] == null || Marked[i].Carried || Marked[i].Sold)
                    {
                        Marked.RemoveAt(i);
                        if (i < huginn.Count) { huginn[i].FlyAway(); huginn.RemoveAt(i); }
                    }
                if (HuginnLeft <= 0f || Marked.Count == 0) ClearHuginn();
            }
            // Muninn leaves once the blessed flip has been used.
            if (muninn != null && !Fortune.Current.NextFlipBlessed)
            {
                muninn.FlyAway();
                muninn = null;
            }
        }

        void ClearHuginn()
        {
            foreach (var b in huginn) if (b != null) b.FlyAway();
            huginn.Clear();
            Marked.Clear();
            HuginnLeft = 0f;
        }

        /// <summary>The marked chest nearest a point, for the HUD.</summary>
        public TreasureChest NearestMarked(Vector3 p, out float distance)
        {
            TreasureChest best = null;
            distance = float.MaxValue;
            foreach (var c in Marked)
            {
                if (c == null) continue;
                float d = Vector3.Distance(p, c.transform.position);
                if (d < distance) { distance = d; best = c; }
            }
            return best;
        }
    }

    /// <summary>A black low-poly raven that flies to a target and circles over it, or perches on it.</summary>
    public class RavenBird : MonoBehaviour
    {
        const float Speed = 16f;

        Transform target;
        bool perch;
        bool leaving;
        float phase;
        Transform leftWing, rightWing;
        Vector3 awayDir;

        public static RavenBird Create(Transform parent, Vector3 start, Transform target, bool perch)
        {
            var go = new GameObject(perch ? "Muninn" : "Huginn");
            go.transform.SetParent(parent, true);
            go.transform.position = start;
            var black = new Color(0.06f, 0.06f, 0.08f);
            var shine = new Color(0.14f, 0.15f, 0.22f);
            LongshipBuilder.Deco(PrimitiveType.Sphere, go.transform, Vector3.zero, new Vector3(0.22f, 0.2f, 0.5f), black);
            LongshipBuilder.Deco(PrimitiveType.Sphere, go.transform, new Vector3(0f, 0.08f, 0.26f), new Vector3(0.16f, 0.16f, 0.18f), shine);
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.06f, 0.4f), new Vector3(0.05f, 0.05f, 0.14f), new Color(0.2f, 0.2f, 0.2f));
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.02f, -0.34f), new Vector3(0.16f, 0.03f, 0.2f), black); // tail
            var bird = go.AddComponent<RavenBird>();
            bird.leftWing = Wing(go.transform, -1f, black);
            bird.rightWing = Wing(go.transform, 1f, black);
            bird.target = target;
            bird.perch = perch;
            bird.phase = Random.value * 10f;
            return bird;
        }

        static Transform Wing(Transform body, float side, Color color)
        {
            var pivot = new GameObject("Wing").transform;
            pivot.SetParent(body, false);
            pivot.localPosition = new Vector3(side * 0.08f, 0.05f, 0f);
            LongshipBuilder.Deco(PrimitiveType.Cube, pivot, new Vector3(side * 0.32f, 0f, 0f), new Vector3(0.62f, 0.03f, 0.26f), color);
            return pivot;
        }

        public void FlyAway()
        {
            leaving = true;
            awayDir = new Vector3(Random.Range(-1f, 1f), 0.6f, Random.Range(-1f, 1f)).normalized;
            Destroy(gameObject, 6f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            phase += dt;
            Vector3 goal;
            bool perched = false;
            if (leaving || target == null)
            {
                if (!leaving) FlyAway();
                goal = transform.position + awayDir * 20f;
            }
            else if (perch)
            {
                // Land on the altar's edge.
                goal = target.position + target.rotation * new Vector3(0.35f, 1.05f, 0f);
                perched = Vector3.Distance(transform.position, goal) < 0.3f;
            }
            else
            {
                // Circle high over the treasure, so you can see it from the ship.
                float a = phase * 0.9f;
                goal = target.position + new Vector3(Mathf.Cos(a) * 6f, 9f + Mathf.Sin(phase * 0.7f), Mathf.Sin(a) * 6f);
            }

            Vector3 to = goal - transform.position;
            Vector3 step = Vector3.ClampMagnitude(to, Speed * dt * (perch && to.magnitude < 3f ? 0.35f : 1f));
            transform.position += step;
            if (step.sqrMagnitude > 1e-6f)
            {
                var look = Quaternion.LookRotation(new Vector3(step.x, step.y * 0.5f, step.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, look, dt * 6f);
            }
            // Flap on the way, glide in circles, fold the wings when perched.
            float flap = perched ? 70f : (to.magnitude > 8f || leaving ? Mathf.Sin(phase * 14f) * 45f : Mathf.Sin(phase * 3f) * 12f);
            leftWing.localRotation = Quaternion.Euler(0f, 0f, flap);
            rightWing.localRotation = Quaternion.Euler(0f, 0f, -flap);
        }
    }
}
