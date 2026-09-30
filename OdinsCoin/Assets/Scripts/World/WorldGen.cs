using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Lays out the islands of the first sea. Fixed seeds, so every player sails the same waters:
    /// a few small islets with chests, and monasteries worth raiding further out.
    /// </summary>
    public static class WorldGen
    {
        public static readonly List<Island> Islands = new List<Island>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Islands.Clear(); }

        public static IslandSpec[] Specs =
        {
            new IslandSpec { name = "Gull Rock",     centre = new Vector2(70f, 110f),   radius = 28f, height = 9f,  seed = 3,  trees = 12, chests = 1 },
            new IslandSpec { name = "Lindholm",      centre = new Vector2(-120f, 180f), radius = 60f, height = 18f, seed = 7,  trees = 40, chests = 2, monastery = true },
            new IslandSpec { name = "Seal Skerries", centre = new Vector2(200f, 40f),   radius = 22f, height = 6f,  seed = 11, trees = 5,  chests = 1 },
            new IslandSpec { name = "Ravensey",      centre = new Vector2(180f, 260f),  radius = 48f, height = 22f, seed = 19, trees = 35, chests = 3 },
            new IslandSpec { name = "Iona Minor",    centre = new Vector2(20f, 400f),   radius = 70f, height = 16f, seed = 23, trees = 50, chests = 2, monastery = true },
            new IslandSpec { name = "Wolf's Tooth",  centre = new Vector2(-230f, 40f),  radius = 35f, height = 26f, seed = 29, trees = 20, chests = 2 },
        };

        public static void Build(Transform parent)
        {
            Islands.Clear();
            var root = new GameObject("Islands").transform;
            root.SetParent(parent, false);
            foreach (var spec in Specs) Islands.Add(Island.Create(root, spec));
        }

        /// <summary>The island nearest a world point, and the distance to its centre.</summary>
        public static Island Nearest(Vector3 p, out float distance)
        {
            Island best = null;
            distance = float.MaxValue;
            foreach (var i in Islands)
            {
                float d = Vector2.Distance(new Vector2(p.x, p.z), i.Spec.centre);
                if (d < distance) { distance = d; best = i; }
            }
            return best;
        }
    }
}
