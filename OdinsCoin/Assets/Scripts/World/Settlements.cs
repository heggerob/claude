using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>One building of a settlement: what it is, where (global game coordinates, y = the ground) and which way it faces.</summary>
    public struct Plot
    {
        public BuildingKind kind;
        public Vector3 at;
        public float yaw;
    }

    /// <summary>
    /// Lays out a real place's settlement on the real land: a jetty from the shore out to its harbour, and the
    /// buildings its kind calls for (a town's longhouses and storehouses, a jarl's great hall and watchtower,
    /// a monastery's church, a fortress's palisade) on dry, fairly level ground near where the place really
    /// was, facing the water. Pure maths on the map, so it can be tested and every visit builds the same place.
    /// </summary>
    public static class Settlements
    {
        /// <summary>The buildings each kind of place gets (the jetty comes on top).</summary>
        public static BuildingKind[] Plan(PlaceKind kind)
        {
            switch (kind)
            {
                case PlaceKind.Town: return new[] { BuildingKind.Longhouse, BuildingKind.Longhouse, BuildingKind.Longhouse, BuildingKind.Longhouse, BuildingKind.Storehouse, BuildingKind.Storehouse, BuildingKind.Boathouse, BuildingKind.Boathouse, BuildingKind.Longhouse, BuildingKind.Storehouse };
                case PlaceKind.Hall: return new[] { BuildingKind.GreatHall, BuildingKind.Longhouse, BuildingKind.Longhouse, BuildingKind.Boathouse, BuildingKind.Storehouse, BuildingKind.Watchtower };
                case PlaceKind.Monastery: return new[] { BuildingKind.Church, BuildingKind.Longhouse, BuildingKind.Longhouse, BuildingKind.Storehouse };
                case PlaceKind.Fortress: return new[] { BuildingKind.GreatHall, BuildingKind.Watchtower, BuildingKind.Watchtower, BuildingKind.Longhouse, BuildingKind.Longhouse, BuildingKind.Palisade, BuildingKind.Palisade, BuildingKind.Palisade, BuildingKind.Boathouse };
                default: return new[] { BuildingKind.Longhouse, BuildingKind.Boathouse, BuildingKind.Storehouse };
            }
        }

        /// <summary>Is this spot (global) good ground to build on: dry, above the tide, not steep?</summary>
        public static bool Buildable(WorldMap map, Vector3 at, float radius)
        {
            float s = WorldMap.Scale;
            float h = TerrainDetail.Height(map, at.x, at.z) / s;
            // Viking towns were built right down by the water: anything clear of the tide will do.
            if (h < 0.8f || h > 400f) return false;
            float hx = TerrainDetail.Height(map, at.x + radius, at.z) / s, hz = TerrainDetail.Height(map, at.x, at.z + radius) / s;
            float hx2 = TerrainDetail.Height(map, at.x - radius, at.z) / s, hz2 = TerrainDetail.Height(map, at.x, at.z - radius) / s;
            float spread = Mathf.Max(Mathf.Max(hx, hx2), Mathf.Max(hz, hz2)) - Mathf.Min(Mathf.Min(hx, hx2), Mathf.Min(hz, hz2));
            return Mathf.Min(Mathf.Min(hx, hx2), Mathf.Min(hz, hz2)) > 0.3f && spread < radius * 0.35f;
        }

        /// <summary>The settlement for a place: the jetty first, then its buildings. Empty if it has no harbour.</summary>
        public static List<Plot> Layout(WorldMap map, Place place)
        {
            var plots = new List<Plot>();
            Vector3 harbour;
            if (!Places.Harbour(map, place, out harbour)) return plots;
            float s = WorldMap.Scale;
            // The shore: walk from the harbour towards the place until the ground comes up out of the water.
            var centre = Places.Position(map, place);
            var toLand = centre - harbour;
            toLand.y = 0f;
            if (toLand.sqrMagnitude < 1f) toLand = Vector3.forward;
            toLand.Normalize();
            Vector3 shore = harbour;
            for (float d = 0f; d < 4000f * s; d += 10f * s)
            {
                var p = harbour + toLand * d;
                if (TerrainDetail.Height(map, p.x, p.z) > 0f) { shore = p; break; }
            }
            float seaward = Mathf.Atan2(-toLand.x, -toLand.z) * Mathf.Rad2Deg;
            // The jetty runs from the shore out towards the harbour.
            plots.Add(new Plot { kind = BuildingKind.Jetty, at = Ground(map, shore), yaw = seaward });

            // Buildings: on good ground near the shore and the place itself, facing the water, never overlapping.
            var taken = new List<Vector4>();
            var rng = new System.Random(place.name.GetHashCode() & 0x7fffffff);
            foreach (var kind in Plan(place.kind))
            {
                var foot = Buildings.Footprint(kind);
                float r = Mathf.Max(foot.x, foot.z) * s;
                bool placed = false;
                for (int tries = 0; tries < 400 && !placed; tries++)
                {
                    // Spiral out from just inland of the shore.
                    float ring = 30f * s + tries * 4f * s;
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    var at = shore + toLand * (25f * s) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ring;
                    if (!Buildable(map, at, r)) continue;
                    bool clear = true;
                    foreach (var t in taken) if (new Vector2(t.x - at.x, t.z - at.z).magnitude < t.w + r + 4f * s) { clear = false; break; }
                    if (!clear) continue;
                    // Face the sea (with a little variety), longhouses side-on to the slope.
                    var toSea = shore - at;
                    float yaw = Mathf.Atan2(toSea.x, toSea.z) * Mathf.Rad2Deg + (float)(rng.NextDouble() - 0.5) * 30f;
                    plots.Add(new Plot { kind = kind, at = Ground(map, at), yaw = yaw });
                    taken.Add(new Vector4(at.x, 0f, at.z, r));
                    placed = true;
                }
            }
            return plots;
        }

        static Vector3 Ground(WorldMap map, Vector3 at) { return new Vector3(at.x, Mathf.Max(0f, TerrainDetail.Height(map, at.x, at.z)), at.z); }
    }
}
