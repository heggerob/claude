using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Anything that stops BBs. Height is in metres: BBs flying higher pass over it.</summary>
    public class Obstacle : MonoBehaviour
    {
        public float Height = 3f;
        /// <summary>Soft cover (bushes): you walk through it and it hides you, but most BBs get through.</summary>
        public bool Soft;

        /// <summary>Netting and fences: you can see through them even when they stop BBs.</summary>
        public bool SeeThrough;

        /// <summary>Tall or dense enough to hide a standing player.</summary>
        public bool BlocksSight { get { return !SeeThrough && (Soft || Height >= 2f); } }

        /// <summary>Solid things steer bots and stop movement; soft ones don't.</summary>
        public bool BlocksMovement { get { return !Soft; } }
    }

    /// <summary>Fades tree crowns and bushes when the local player is underneath, so you can see yourself.</summary>
    public class Foliage : MonoBehaviour
    {
        public float radius = 1.5f;
        public float normalAlpha = 0.95f;
        SpriteRenderer sr;

        void Awake() { sr = GetComponent<SpriteRenderer>(); }

        void LateUpdate()
        {
            var match = MatchManager.Instance;
            Vector2 me = Vector2.zero;
            bool has = false;
            if (match != null && match.PlayerSoldier != null) { me = match.PlayerSoldier.Position; has = true; }
            else if (match != null && match.Referee != null && match.Referee.HumanControlled) { me = match.Referee.Position; has = true; }
            bool under = has && Vector2.Distance(me, transform.position) < radius;
            var c = sr.color;
            c.a = Mathf.MoveTowards(c.a, under ? 0.35f : normalAlpha, Time.deltaTime * 3f);
            sr.color = c;
        }
    }

    /// <summary>
    /// Builds a <see cref="MapDefinition"/> into GameObjects. Only one map exists at a time;
    /// building a new one replaces the old. 1 world unit = 1 metre.
    /// </summary>
    public static class MapBuilder
    {
        public const float LowCoverHeight = 0.95f;
        public const float WallHeight = 3f;

        static Transform root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { root = null; Current = null; }

        public static MapDefinition Current { get; private set; }

        public static Rect Bounds { get { return (Current ?? MapLibrary.All[0]).bounds; } }
        public static Rect[] SpawnZones { get { return (Current ?? MapLibrary.All[0]).spawnZones; } }

        static readonly Color ContainerColor = new Color(0.42f, 0.55f, 0.62f);

        const float ChunkSize = 16f;

        public static Transform Build(Transform parent, MapDefinition map)
        {
            if (root != null) Object.Destroy(root.gameObject);
            Current = map;
            root = new GameObject("Map: " + map.name).transform;
            root.SetParent(parent, false);

            BuildGround(map);
            Zone(map.spawnZones[0], Teams.Color(Team.Blue));
            Zone(map.spawnZones[1], Teams.Color(Team.Red));

            foreach (var piece in map.pieces) Place(piece, map.indoor);
            return root;
        }

        /// <summary>
        /// The ground is one unique painted texture per 16 m chunk (paths, puddles, flowers...), so nothing
        /// visibly repeats. Outdoors it carries on a little past the netting.
        /// </summary>
        static void BuildGround(MapDefinition map)
        {
            var b = map.bounds;
            float margin = map.indoor ? 1f : 8f;
            float x0 = b.xMin - margin, y0 = b.yMin - margin, x1 = b.xMax + margin, y1 = b.yMax + margin;
            int ppm = GroundPainter.PixelsPerMetre;
            var ground = new GameObject("Ground").transform;
            ground.SetParent(root, false);
            for (float cy = y0; cy < y1; cy += ChunkSize)
            {
                for (float cx = x0; cx < x1; cx += ChunkSize)
                {
                    float w = Mathf.Min(ChunkSize, x1 - cx), h = Mathf.Min(ChunkSize, y1 - cy);
                    int pw = Mathf.CeilToInt(w * ppm), ph = Mathf.CeilToInt(h * ppm);
                    var tex = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.SetPixels32(GroundPainter.Paint(map, cx, cy, pw, ph));
                    tex.Apply();
                    var sprite = Sprite.Create(tex, new Rect(0, 0, pw, ph), Vector2.zero, ppm, 0, SpriteMeshType.FullRect);
                    var sr = NewRenderer("Ground Chunk", ground, sprite, -20);
                    sr.transform.position = new Vector3(cx, cy, 0f);
                }
            }
        }

        static void Place(MapPiece p, bool indoor)
        {
            switch (p.kind)
            {
                case PieceKind.Wall: Block(p, indoor ? MapArt.IndoorWall : MapArt.Plywood, Color.white, WallHeight, 2); break;
                case PieceKind.Container: Block(p, MapArt.Container, ContainerColor, WallHeight, 2); break;
                case PieceKind.Sandbags: Block(p, MapArt.Sandbags, Color.white, LowCoverHeight, 1); break;
                case PieceKind.Shelf: Block(p, MapArt.Shelf, Color.white, 2.2f, 2); break;
                case PieceKind.Crates: Block(p, MapArt.Crates, Color.white, 1.1f, 1); break;
                case PieceKind.Log: Block(p, MapArt.Log, Color.white, 0.6f, 1); break;
                case PieceKind.HayBale: Block(p, MapArt.Hay, Color.white, 1.2f, 1); break;
                case PieceKind.Fence: Block(p, MapArt.Fence, Color.white, 1.1f, 1).SeeThrough = true; break;
                case PieceKind.Netting: Block(p, MapArt.Netting, Color.white, WallHeight, 2).SeeThrough = true; break;
                case PieceKind.Pallet: Decor(p, MapArt.Pallet); break;
                case PieceKind.Barrel: Round(p, MapArt.Barrel, 1f, false, 1, Mathf.Min(p.size.x, p.size.y) * 0.5f, 0f, BarrelColor(p.center)); break;
                case PieceKind.Tires: Round(p, MapArt.Tires, 0.8f, false, 1, Mathf.Min(p.size.x, p.size.y) * 0.5f, 0f, Color.white); break;
                case PieceKind.Rock: Round(p, SpriteFactory.Rock, 1.2f, false, 1, Mathf.Min(p.size.x, p.size.y) * 0.45f, 0f, Color.white); break;
                case PieceKind.Tree: Tree(p); break;
                case PieceKind.Bush: Round(p, SpriteFactory.Bush, 1.6f, true, 26, Mathf.Min(p.size.x, p.size.y) * 0.45f, 0.95f, Color.white); break;
            }
        }

        static Color BarrelColor(Vector2 at)
        {
            Color[] colors = { new Color(0.3f, 0.45f, 0.75f), new Color(0.75f, 0.25f, 0.2f), new Color(0.35f, 0.55f, 0.3f), new Color(0.8f, 0.7f, 0.3f) };
            int i = Mathf.Abs(Mathf.RoundToInt(at.x * 7f + at.y * 13f)) % colors.Length;
            return colors[i];
        }

        static Obstacle Block(MapPiece p, Sprite sprite, Color color, float height, int order)
        {
            // Tile patterns run along the long side, so vertical pieces are turned 90 degrees.
            bool vertical = p.size.y > p.size.x;
            Vector2 size = vertical ? new Vector2(p.size.y, p.size.x) : p.size;

            Shadow(p.center, p.size, height);
            var sr = NewRenderer(p.kind.ToString(), root, sprite, order);
            sr.transform.position = p.center;
            if (vertical) sr.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.color = color;
            var col = sr.gameObject.AddComponent<BoxCollider2D>();
            col.size = size;
            var o = sr.gameObject.AddComponent<Obstacle>();
            o.Height = height;
            return o;
        }

        static void Decor(MapPiece p, Sprite sprite)
        {
            var sr = NewRenderer(p.kind.ToString(), root, sprite, -8);
            sr.transform.position = p.center;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Repeat(p.center.x * 37f + p.center.y * 11f, 20f) - 10f);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = p.size;
        }

        /// <summary>A soft drop shadow down and to the right; taller things cast longer shadows.</summary>
        static void Shadow(Vector2 center, Vector2 size, float height)
        {
            float length = Mathf.Min(height, 2.5f) * 0.14f;
            var sr = NewRenderer("Shadow", root, SpriteFactory.Pixel, -7);
            sr.transform.position = center + new Vector2(length, -length);
            sr.transform.localScale = new Vector3(size.x + 0.05f, size.y + 0.05f, 1f);
            sr.color = new Color(0f, 0f, 0f, 0.3f);
        }

        static void Round(MapPiece p, Sprite sprite, float height, bool soft, int order, float radius, float foliageAlpha, Color color)
        {
            var sr = NewRenderer(p.kind.ToString(), root, sprite, order);
            sr.transform.position = p.center;
            sr.color = color;
            float spriteSize = sprite.texture.width / (float)SpriteFactory.PixelsPerUnit;
            float scale = Mathf.Max(p.size.x, p.size.y) / spriteSize;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            if (!soft)
            {
                var shadow = NewRenderer("Shadow", root, PixelArt.Shadow, -7);
                shadow.transform.position = p.center + new Vector2(0.12f, -0.12f) * Mathf.Min(height, 2f);
                float s = Mathf.Max(p.size.x, p.size.y) * 1.25f;
                shadow.transform.localScale = new Vector3(s, s, 1f);
            }
            var col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.radius = radius / scale;
            col.isTrigger = soft;
            var o = sr.gameObject.AddComponent<Obstacle>();
            o.Height = height;
            o.Soft = soft;
            if (foliageAlpha > 0f)
            {
                var f = sr.gameObject.AddComponent<Foliage>();
                f.radius = radius + 0.4f;
                f.normalAlpha = foliageAlpha;
            }
        }

        static void Tree(MapPiece p)
        {
            var shadow = NewRenderer("Tree Shadow", root, PixelArt.Shadow, -7);
            shadow.transform.position = p.center + new Vector2(0.6f, -0.6f);
            shadow.transform.localScale = new Vector3(3.2f, 3.2f, 1f);

            // The trunk is what stops BBs and players...
            var trunk = NewRenderer("Tree", root, SpriteFactory.SmallCircle, 3);
            trunk.transform.position = p.center;
            trunk.color = new Color(0.36f, 0.25f, 0.16f);
            trunk.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            var col = trunk.gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.35f / 1.4f;
            var o = trunk.gameObject.AddComponent<Obstacle>();
            o.Height = WallHeight;

            // ...the crown hangs above everyone and fades when you stand under it.
            var crown = NewRenderer("Crown", trunk.transform, SpriteFactory.Canopy, 30);
            crown.transform.localScale = new Vector3(1f / 1.4f, 1f / 1.4f, 1f);
            crown.color = new Color(1f, 1f, 1f, 0.92f);
            var f = crown.gameObject.AddComponent<Foliage>();
            f.radius = 1.6f;
            f.normalAlpha = 0.92f;
        }

        static void Zone(Rect rect, Color teamColor)
        {
            var sr = NewRenderer("Spawn Zone", root, SpriteFactory.Pixel, -15);
            sr.transform.position = rect.center;
            sr.transform.localScale = new Vector3(rect.width, rect.height, 1f);
            teamColor.a = 0.22f;
            sr.color = teamColor;
        }

        static SpriteRenderer NewRenderer(string name, Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
