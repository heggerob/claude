using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Where your ship went down. The treasure that was on deck isn't gone: the chests lie awash where she sank,
    /// round a broken mast still floating to mark the place, and you can sail back and swim them up. The wreck is
    /// on the HUD and the chart until the last chest is fetched.
    /// </summary>
    public static class Wreck
    {
        /// <summary>The wreck's global position (x, z), or null when there's none with treasure left.</summary>
        public static Vector3? At;
        public static readonly List<TreasureChest> Chests = new List<TreasureChest>();
        static Transform marker;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { At = null; Chests.Clear(); marker = null; }

        /// <summary>How far the chests are strewn round the wreck (m).</summary>
        public const float Spread = 6f;

        /// <summary>Where the <paramref name="i"/>th of the chests comes to rest round the wreck: a loose ring, none on top of another.</summary>
        public static Vector3 Scatter(int i, int count)
        {
            if (count <= 1) return new Vector3(1.5f, 0f, 0f);
            float a = i / (float)count * Mathf.PI * 2f + 0.4f * i;
            float r = Spread * (0.45f + 0.55f * ((i * 37) % 11) / 10f);
            return new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        /// <summary>How many of the wreck's chests are still out there.</summary>
        public static int Left
        {
            get
            {
                Chests.RemoveAll(c => c == null || c.Sold);
                int n = 0;
                foreach (var c in Chests) if (!c.Carried && !c.transform.IsChildOfOrSelf(ShipTransform)) n++;
                return n;
            }
        }

        static Transform ShipTransform { get { var b = GameBootstrap.Instance; return b != null && b.Ship != null ? b.Ship.transform : null; } }

        /// <summary>The ship has gone down at <paramref name="scenePos"/>: strew what was on deck round the spot and mark it.</summary>
        public static void Sink(Transform world, Vector3 scenePos, List<TreasureChest> onDeck)
        {
            if (onDeck.Count == 0) return;
            At = new Vector3((float)WorldOrigin.GlobalX(scenePos), 0f, (float)WorldOrigin.GlobalZ(scenePos));
            for (int i = 0; i < onDeck.Count; i++)
            {
                var chest = onDeck[i];
                if (chest == null) continue;
                chest.transform.SetParent(world, true);
                chest.transform.position = scenePos + Scatter(i, onDeck.Count);
                var f = chest.gameObject.AddComponent<Floater>();
                f.sink = 0.45f;
                f.drift = 0f;
                if (!Chests.Contains(chest)) Chests.Add(chest);
            }
            // A broken mast floating over the place.
            if (marker != null) Object.Destroy(marker.gameObject);
            marker = new GameObject("Wreck").transform;
            marker.SetParent(world, false);
            marker.position = scenePos;
            LongshipBuilder.Deco(PrimitiveType.Cylinder, marker, new Vector3(0f, 0.2f, 0f), new Vector3(0.35f, 2.6f, 0.35f), new Color(0.36f, 0.26f, 0.16f)).localRotation = Quaternion.Euler(0f, 0f, 78f);
            LongshipBuilder.Deco(PrimitiveType.Cube, marker, new Vector3(0.6f, 0.35f, 0.4f), new Vector3(1.8f, 0.05f, 1.2f), new Color(0.62f, 0.2f, 0.15f));
            var buoy = marker.gameObject.AddComponent<Floater>();
            buoy.sink = 0.1f;
            buoy.drift = 0f;
        }

        /// <summary>Forget the wreck once its last chest is fetched.</summary>
        public static void Tidy()
        {
            if (At.HasValue && Left == 0)
            {
                At = null;
                if (marker != null) Object.Destroy(marker.gameObject);
                marker = null;
            }
        }
    }
}
