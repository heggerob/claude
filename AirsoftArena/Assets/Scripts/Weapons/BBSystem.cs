using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// Simulates every BB in flight. The map is flat 2D, but each BB also has a height (z) above the ground:
    /// drag slows it down, hop-up backspin keeps it up while it is fast, and when it slows it drops.
    /// Low cover only stops BBs flying below its height, so crouching behind sandbags actually matters.
    /// </summary>
    public class BBSystem : MonoBehaviour
    {
        public static BBSystem Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public const float Gravity = 9.81f;
        // 0.5 * air density * drag coefficient of a sphere * cross-section area of a 6 mm BB.
        const float DragFactor = 0.5f * 1.2f * 0.47f * Mathf.PI * 0.003f * 0.003f;
        const float StepTime = 1f / 120f; // collisions use line casts, so this only affects curve accuracy
        const float MinSpeed = 8f;
        const float MaxAge = 4f;
        // How far up the screen a BB is drawn per metre of height, so you can see it arc.
        const float HeightOnScreen = 0.3f;
        static readonly Color DefaultTracer = new Color(1f, 0.97f, 0.85f);

        /// <summary>Wind in m/s. Light BBs drift more than heavy ones.</summary>
        public Vector2 Wind;

        class BB
        {
            public Vector2 pos, vel;
            public float z, vz, mass, hop, v0, age;
            public Soldier owner;
            public SpriteRenderer sprite, shadow;
        }

        readonly List<BB> active = new List<BB>();
        readonly Stack<BB> pool = new Stack<BB>();
        readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        static readonly System.Comparison<RaycastHit2D> ByFraction = (a, b) => a.fraction.CompareTo(b.fraction);
        ContactFilter2D filter;
        Transform container;

        public int ActiveCount { get { return active.Count; } }

        void Awake()
        {
            Instance = this;
            filter = new ContactFilter2D().NoFilter();
            container = new GameObject("BBs").transform;
            container.SetParent(transform, false);
        }

        public void Fire(Soldier owner, Vector2 origin, Vector2 direction, float muzzleHeight, WeaponData weapon, int count, float spreadMultiplier)
        {
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float spread = weapon.spreadDegrees * spreadMultiplier;
            for (int i = 0; i < count; i++)
            {
                // Sum of two randoms: most BBs go near the centre, a few fly wide.
                float angle = (baseAngle + (Random.value + Random.value - 1f) * spread) * Mathf.Deg2Rad;
                float pitch = (Random.value + Random.value - 1f) * spread * 0.25f * Mathf.Deg2Rad;
                float speed = weapon.MuzzleVelocity * Random.Range(0.97f, 1.03f);

                var bb = Get();
                bb.owner = owner;
                bb.pos = origin;
                bb.vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                bb.z = muzzleHeight;
                bb.vz = speed * Mathf.Tan(pitch);
                bb.mass = Mathf.Max(0.12f, weapon.bbWeightGrams) / 1000f;
                bb.hop = weapon.hopUp * Random.Range(0.95f, 1.05f);
                bb.v0 = speed;
                bb.age = 0f;
                var look = owner != null ? owner.Look : null;
                bb.sprite.color = look != null ? look.tracer : DefaultTracer;
                active.Add(bb);
                Draw(bb);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < active.Count; i++) Release(active[i]);
            active.Clear();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / StepTime));
            float h = dt / steps;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var bb = active[i];
                bool alive = true;
                for (int s = 0; s < steps && alive; s++) alive = Step(bb, h);

                if (alive)
                {
                    Draw(bb);
                }
                else
                {
                    Release(bb);
                    int last = active.Count - 1;
                    active[i] = active[last];
                    active.RemoveAt(last);
                }
            }
        }

        bool Step(BB bb, float h)
        {
            // Quadratic air drag, relative to the moving air (that is what makes wind push BBs sideways).
            Vector2 rel = bb.vel - Wind;
            bb.vel -= rel * (DragFactor / bb.mass * rel.magnitude * h);
            float speed = bb.vel.magnitude;

            // Hop-up backspin lift, strongest while the BB is fast.
            float lift = Gravity * bb.hop * Mathf.Sqrt(Mathf.Clamp01(speed / bb.v0));
            bb.vz += (lift - Gravity) * h;

            Vector2 next = bb.pos + bb.vel * h;
            float nextZ = bb.z + bb.vz * h;
            bb.age += h;

            hits.Clear();
            int count = Physics2D.Linecast(bb.pos, next, filter, hits);
            if (count > 1) hits.Sort(ByFraction);

            for (int k = 0; k < hits.Count; k++)
            {
                var hit = hits[k];
                float zAt = Mathf.Lerp(bb.z, nextZ, hit.fraction);
                if (zAt <= 0f) break; // hits the ground before reaching this collider

                var col = hit.collider;
                var soldier = col.GetComponent<Soldier>();
                if (soldier != null)
                {
                    if (soldier == bb.owner || zAt > soldier.Height) continue;
                    Effects.Flash(hit.point, Color.white, 0.5f, 0.15f);
                    soldier.ReceiveHit(bb.owner, hit.point);
                    return false;
                }

                var obstacle = col.GetComponent<Obstacle>();
                if (obstacle != null)
                {
                    if (zAt > obstacle.Height) continue; // flies over low cover
                    Effects.Flash(hit.point, new Color(0.9f, 0.85f, 0.6f), 0.25f, 0.08f);
                    return false;
                }

                var referee = col.GetComponent<RefereeNPC>();
                if (referee != null)
                {
                    if (zAt > RefereeNPC.Height) continue;
                    Effects.Flash(hit.point, Color.white, 0.5f, 0.15f);
                    referee.OnHitByBB(bb.owner);
                    return false;
                }

                return false;
            }

            bb.pos = next;
            bb.z = nextZ;

            if (bb.z <= 0f)
            {
                Effects.GroundDot(bb.pos);
                if (MatchManager.Instance != null) MatchManager.Instance.OnBBLanded(bb.pos, bb.owner);
                return false;
            }
            return speed >= MinSpeed && bb.age <= MaxAge;
        }

        void Draw(BB bb)
        {
            bb.sprite.transform.position = new Vector3(bb.pos.x, bb.pos.y + bb.z * HeightOnScreen, 0f);
            bb.shadow.transform.position = new Vector3(bb.pos.x, bb.pos.y, 0f);
            var c = bb.shadow.color;
            c.a = Mathf.Lerp(0.45f, 0.15f, bb.z / 2f);
            bb.shadow.color = c;
        }

        BB Get()
        {
            BB bb;
            if (pool.Count > 0)
            {
                bb = pool.Pop();
            }
            else
            {
                bb = new BB();
                bb.sprite = MakeRenderer("BB", new Color(1f, 0.97f, 0.85f), 20);
                bb.shadow = MakeRenderer("BB Shadow", new Color(0f, 0f, 0f, 0.4f), 3);
            }
            bb.sprite.enabled = true;
            bb.shadow.enabled = true;
            return bb;
        }

        void Release(BB bb)
        {
            bb.owner = null;
            bb.sprite.enabled = false;
            bb.shadow.enabled = false;
            pool.Push(bb);
        }

        SpriteRenderer MakeRenderer(string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(container, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.BB;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
