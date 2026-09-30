using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The land takes the wind: in the lee of an island or a mountainside the breeze drops away, as it does in the
    /// fjords, where a ship can lie becalmed under a cliff while the open water outside is white. The wind shadow
    /// reaches about a dozen times the height of the land downwind.
    /// </summary>
    public static class WindShelter
    {
        /// <summary>How far downwind the land's shadow reaches, in heights of the land; and how much wind it takes at most.</summary>
        public const float ShadowReach = 12f, MaxShelter = 0.85f;
        static readonly float[] Samples = { 150f, 400f, 900f, 1800f, 3500f };

        /// <summary>
        /// How much of the wind reaches a spot (global), 0..1, with the wind blowing towards <paramref name="windTo"/>
        /// (flat, any length): look upwind for land standing high enough to cast its shadow this far.
        /// </summary>
        public static float Factor(WorldMap map, Vector3 at, Vector3 windTo)
        {
            if (map == null) return 1f;
            var up = new Vector3(-windTo.x, 0f, -windTo.z);
            if (up.sqrMagnitude < 1e-6f) return 1f;
            up.Normalize();
            float s = WorldMap.Scale, shelter = 0f;
            foreach (float d in Samples)
            {
                var p = at + up * d * s;
                float h = TerrainDetail.Height(map, p.x, p.z) / s;
                if (h <= 2f) continue;
                shelter = Mathf.Max(shelter, Mathf.Clamp01((h - 2f) * ShadowReach / d - 0.3f));
            }
            return 1f - MaxShelter * shelter;
        }
    }
}
