using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Someone going about their day in a town: strolls from one doorstep to another (never through a house),
    /// stands a while, looks about, and steps out of your way if you walk up to them. Drawn as a storybook hero in
    /// homespun (<see cref="NpcHeroes.Townsfolk"/>), walking with the same footsteps and animation as everyone.
    /// </summary>
    public class Townsperson : MonoBehaviour
    {
        public const float StrollSpeed = 1.2f, Arrived = 0.6f, GiveWay = 2.2f;

        VikingBuilder.Parts parts;
        readonly Locomotion loco = new Locomotion();
        readonly HeroAnimator animator = new HeroAnimator();
        WorldMap map;
        List<Plot> plots;
        List<Vector3> stops;
        /// <summary>The site's global position: townsfolk live in its local frame (it has no turn).</summary>
        Vector3 siteGlobal;
        int target = -1;
        float waitUntil;
        System.Random rng;

        public static Townsperson Create(Transform site, Vector3 siteGlobal, WorldMap map, List<Plot> plots, List<Vector3> stops, int seed)
        {
            var root = new GameObject("Townsperson").transform;
            root.SetParent(site, false);
            var t = root.gameObject.AddComponent<Townsperson>();
            t.map = map;
            t.plots = plots;
            t.stops = stops;
            t.siteGlobal = siteGlobal;
            t.rng = new System.Random(seed);
            var start = stops[t.rng.Next(stops.Count)];
            root.localPosition = new Vector3(start.x - siteGlobal.x, TerrainDetail.Height(map, start.x, start.z), start.z - siteGlobal.z);
            float yaw = (float)t.rng.NextDouble() * 360f;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            t.loco.Reset(Vector2.zero, yaw);
            t.parts = HeroBuilder.Build(root, NpcHeroes.Townsfolk(seed));
            t.waitUntil = Time.time + (float)t.rng.NextDouble() * 6f;
            return t;
        }

        /// <summary>
        /// The next stop to walk to from <paramref name="at"/>: one that can be reached without walking through a
        /// house, and, when <paramref name="away"/> is given, one that takes you away from it. -1 if none.
        /// </summary>
        public static int NextStop(List<Plot> plots, List<Vector3> stops, Vector3 at, int current, System.Random rng, Vector3? away)
        {
            int best = -1;
            float bestScore = float.MinValue;
            for (int tries = 0; tries < 8; tries++)
            {
                int i = rng.Next(stops.Count);
                if (i == current || !PlaceLife.ClearPath(plots, at, stops[i])) continue;
                float d = Vector3.Distance(at, stops[i]);
                if (d > 60f) continue;
                float score = away.HasValue ? Vector3.Distance(stops[i], away.Value) : (float)rng.NextDouble();
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        void Update()
        {
            if (parts == null || stops == null || stops.Count == 0) return;
            float dt = Time.deltaTime;
            var local = transform.localPosition;
            var global = new Vector3(siteGlobal.x + local.x, 0f, siteGlobal.z + local.z);

            // Someone walks right up: give way.
            var boot = GameBootstrap.Instance;
            var player = boot != null && boot.Player != null ? boot.Player.transform.position : new Vector3(1e9f, 0f, 0f);
            var playerLocal = transform.parent != null ? transform.parent.InverseTransformPoint(player) : player;
            bool crowded = new Vector2(playerLocal.x - local.x, playerLocal.z - local.z).magnitude < GiveWay;
            if (crowded && (target < 0 || Time.time < waitUntil))
            {
                var away = new Vector3(siteGlobal.x + playerLocal.x, 0f, siteGlobal.z + playerLocal.z);
                target = NextStop(plots, stops, global, target, rng, away);
                waitUntil = 0f;
            }

            Vector2 wish = Vector2.zero;
            if (Time.time >= waitUntil)
            {
                if (target < 0) target = NextStop(plots, stops, global, -1, rng, null);
                if (target >= 0)
                {
                    var to = stops[target] - global;
                    to.y = 0f;
                    if (to.magnitude < Arrived)
                    {
                        // There: stand a while, then on somewhere else.
                        target = -1;
                        waitUntil = Time.time + 4f + (float)rng.NextDouble() * 10f;
                    }
                    else wish = new Vector2(to.x, to.z).normalized;
                }
                else waitUntil = Time.time + 3f;
            }
            loco.Step(wish, StrollSpeed, dt);
            var v = loco.Velocity * dt;
            local.x += v.x;
            local.z += v.z;
            local.y = TerrainDetail.Height(map, siteGlobal.x + local.x, siteGlobal.z + local.z);
            transform.localPosition = local;
            transform.localRotation = Quaternion.Euler(0f, loco.heading, 0f);
            animator.Step(dt, loco, true, 0f, false, Time.time + local.x);
            animator.Apply(parts);
        }
    }
}
