using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    public enum LandmarkKind { BeaconTower, BellTower, GreatAsh, RuneStone }

    /// <summary>
    /// Something high or bright at every place, seen from far out at sea, so you always have somewhere to head for.
    /// Towns and fortresses keep a beacon tower with a fire burning on top, monasteries a tall bell tower, a jarl's
    /// hall a great ash tree, and a landing a standing runestone glowing blue. Each stands on the highest dry
    /// ground near the place, clear of its buildings.
    /// </summary>
    public static class Landmarks
    {
        /// <summary>How far round the place's main building its landmark may stand (m).</summary>
        public const float SearchRadius = 260f;

        public static LandmarkKind KindFor(Place place)
        {
            switch (place.kind)
            {
                case PlaceKind.Monastery: return LandmarkKind.BellTower;
                case PlaceKind.Hall: return LandmarkKind.GreatAsh;
                case PlaceKind.Landing: return LandmarkKind.RuneStone;
                default: return LandmarkKind.BeaconTower;
            }
        }

        /// <summary>How tall each landmark stands (m).</summary>
        public static float Height(LandmarkKind kind)
        {
            switch (kind)
            {
                case LandmarkKind.BeaconTower: return 22f;
                case LandmarkKind.BellTower: return 20f;
                case LandmarkKind.GreatAsh: return 26f;
                default: return 7f;
            }
        }

        /// <summary>
        /// Where the landmark stands (global, with its ground height): the highest dry spot within
        /// <see cref="SearchRadius"/> of the main building that's clear of every building and field.
        /// <paramref name="height"/> gives the ground height at a global spot.
        /// </summary>
        public static Vector3 Spot(List<Plot> plots, List<Field> fields, System.Func<float, float, float> height)
        {
            var main = PlaceLife.MainBuilding(plots);
            var best = main.at;
            float bestH = float.MinValue;
            for (int ring = 1; ring <= 8; ring++)
                for (int k = 0; k < 16; k++)
                {
                    float a = k / 16f * Mathf.PI * 2f + ring * 0.3f, r = SearchRadius * ring / 8f;
                    var p = new Vector3(main.at.x + Mathf.Sin(a) * r, 0f, main.at.z + Mathf.Cos(a) * r);
                    float h = height(p.x, p.z);
                    if (h < 1f * WorldMap.Scale || h <= bestH) continue;
                    bool clear = true;
                    foreach (var plot in plots) if (PlaceLife.InsideBuilding(plot, p, 6f)) { clear = false; break; }
                    if (clear && fields != null) foreach (var f in fields) if (Fields.Inside(f, p)) { clear = false; break; }
                    if (!clear) continue;
                    bestH = h;
                    best = new Vector3(p.x, h, p.z);
                }
            if (bestH == float.MinValue) best.y = height(best.x, best.z);
            return best;
        }

        static readonly Color Stone = new Color(0.55f, 0.53f, 0.5f), StoneDark = new Color(0.4f, 0.39f, 0.37f),
            Timber = new Color(0.33f, 0.23f, 0.15f), Roof = new Color(0.26f, 0.2f, 0.15f), Fire = new Color(1f, 0.6f, 0.2f),
            Bark = new Color(0.36f, 0.3f, 0.24f), Leaf = new Color(0.3f, 0.46f, 0.26f), LeafDark = new Color(0.22f, 0.36f, 0.2f),
            RuneBlue = new Color(0.45f, 0.8f, 1f);

        /// <summary>The landmark drawn in the storybook style, standing on its origin.</summary>
        public static VikingModel Model(LandmarkKind kind)
        {
            var m = new MergedModel();
            switch (kind)
            {
                case LandmarkKind.BeaconTower:
                    // A tapering timber tower on four legs with a platform and a brazier on top.
                    foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) })
                        m.Put(Timber, SurfaceKind.Wood, MeshData.Tube(new[] { new Vector3(c.x * 2.6f, 0f, c.y * 2.6f), new Vector3(c.x * 1.3f, 18f, c.y * 1.3f) }, new[] { 0.3f, 0.22f }, 6));
                    for (int b = 1; b <= 4; b++)
                    {
                        float y = b * 4f, w = Mathf.Lerp(2.6f, 1.3f, y / 18f);
                        m.Put(Timber, SurfaceKind.Wood, MeshData.Box(new Vector3(0f, y, 0f), new Vector3(w * 2f, 0.25f, 0.25f)));
                        m.Put(Timber, SurfaceKind.Wood, MeshData.Box(new Vector3(0f, y, 0f), new Vector3(0.25f, 0.25f, w * 2f)));
                    }
                    m.Put(Timber, SurfaceKind.Wood, MeshData.Box(new Vector3(0f, 18.2f, 0f), new Vector3(3.6f, 0.4f, 3.6f)));
                    m.Put(StoneDark, SurfaceKind.Plain, MeshData.Lathe(new[] { new Vector2(0.9f, 18.4f), new Vector2(1.3f, 19.4f), new Vector2(1.1f, 19.6f) }, 10));
                    m.Put(Fire, SurfaceKind.Plain, MeshData.Lathe(new[] { new Vector2(1f, 19.4f), new Vector2(0.7f, 20.6f), new Vector2(0.01f, 22f) }, 8));
                    break;
                case LandmarkKind.BellTower:
                    // A square stone tower with a pointed roof and a cross.
                    m.Put(Stone, SurfaceKind.Plain, MeshData.Box(new Vector3(0f, 7f, 0f), new Vector3(4.5f, 14f, 4.5f)));
                    m.Put(StoneDark, SurfaceKind.Plain, MeshData.Box(new Vector3(0f, 12f, 2.26f), new Vector3(1.2f, 2.2f, 0.05f)));
                    m.Put(Roof, SurfaceKind.Wood, MeshData.Lathe(new[] { new Vector2(3.4f, 14f), new Vector2(0.01f, 19f) }, 4));
                    m.Put(Timber, SurfaceKind.Wood, MeshData.Box(new Vector3(0f, 19.6f, 0f), new Vector3(0.2f, 1.4f, 0.2f)));
                    m.Put(Timber, SurfaceKind.Wood, MeshData.Box(new Vector3(0f, 19.9f, 0f), new Vector3(0.9f, 0.2f, 0.2f)));
                    break;
                case LandmarkKind.GreatAsh:
                    // Yggdrasil's little sister: a huge, broad ash with a thick trunk and a wide crown.
                    m.Put(Bark, SurfaceKind.Wood, MeshData.Lathe(new[] { new Vector2(2.2f, -0.5f), new Vector2(1.4f, 2f), new Vector2(1f, 9f), new Vector2(0.5f, 14f) }, 10));
                    foreach (var b in new[] { new Vector3(4f, 13f, 1f), new Vector3(-3.6f, 14f, -1.5f), new Vector3(0.5f, 15f, 4f), new Vector3(-1f, 16f, -3.5f) })
                        m.Put(Bark, SurfaceKind.Wood, MeshData.Tube(new[] { new Vector3(0f, 10f, 0f), b }, new[] { 0.6f, 0.25f }, 6));
                    m.Put(Leaf, SurfaceKind.Cloth, MeshData.Ellipsoid(new Vector3(0f, 19f, 0f), new Vector3(9f, 6f, 9f), 12, 7));
                    m.Put(LeafDark, SurfaceKind.Cloth, MeshData.Ellipsoid(new Vector3(5f, 16f, 2f), new Vector3(5f, 4f, 5f), 10, 6));
                    m.Put(LeafDark, SurfaceKind.Cloth, MeshData.Ellipsoid(new Vector3(-5f, 16.5f, -2f), new Vector3(5f, 4f, 5f), 10, 6));
                    m.Put(Leaf, SurfaceKind.Cloth, MeshData.Ellipsoid(new Vector3(0f, 23f, 1f), new Vector3(5f, 3.5f, 5f), 10, 6));
                    break;
                default:
                    // A tall standing stone with a band of runes cut in it, glowing blue.
                    m.Put(Stone, SurfaceKind.Plain, MeshData.Lathe(new[] { new Vector2(1.1f, -0.5f), new Vector2(1.2f, 2f), new Vector2(0.9f, 5.5f), new Vector2(0.4f, 7f) }, 6)
                        .Transformed(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 0.45f)));
                    for (int i = 0; i < 6; i++)
                    {
                        // A column of rune strokes down the face, on the stone's surface.
                        float y = 1.4f + i * 0.75f;
                        float face = Mathf.Lerp(1.2f, 0.9f, Mathf.Clamp01((y - 2f) / 3.5f)) * 0.45f + 0.03f;
                        m.Put(RuneBlue, SurfaceKind.Plain, MeshData.Box(Vector3.zero, new Vector3(0.12f, 0.5f, 0.05f)).Transformed(new Vector3(0f, y, face), Quaternion.Euler(0f, 0f, i % 2 == 0 ? 20f : -20f), Vector3.one));
                    }
                    break;
            }
            return m.ToModel("Landmark");
        }

        /// <summary>The light it gives off (fire orange, rune blue), or none, and where on it (m up).</summary>
        public static bool Glow(LandmarkKind kind, out Color colour, out float at)
        {
            colour = kind == LandmarkKind.RuneStone ? RuneBlue : Fire;
            at = kind == LandmarkKind.BeaconTower ? 20.5f : 4f;
            return kind == LandmarkKind.BeaconTower || kind == LandmarkKind.RuneStone;
        }

        /// <summary>Put a place's landmark up in the scene under <paramref name="parent"/>, with its light.</summary>
        public static Transform Build(Transform parent, Place place, Vector3 global)
        {
            var kind = KindFor(place);
            var t = new GameObject("Landmark " + kind).transform;
            t.SetParent(parent, false);
            t.position = WorldOrigin.ToScene(global.x, global.z, global.y - 0.3f);
            ModelView.Show(Model(kind), t);
            var col = t.gameObject.AddComponent<CapsuleCollider>();
            col.radius = kind == LandmarkKind.GreatAsh ? 1.6f : kind == LandmarkKind.RuneStone ? 1f : 2.4f;
            col.height = Height(kind);
            col.center = new Vector3(0f, col.height / 2f, 0f);
            Color c; float at;
            if (Glow(kind, out c, out at))
            {
                var light = new GameObject("Glow").AddComponent<Light>();
                light.transform.SetParent(t, false);
                light.transform.localPosition = new Vector3(0f, at, 0f);
                light.type = LightType.Point;
                light.color = c;
                light.range = kind == LandmarkKind.BeaconTower ? 40f : 12f;
                light.intensity = 2f;
            }
            return t;
        }
    }
}
