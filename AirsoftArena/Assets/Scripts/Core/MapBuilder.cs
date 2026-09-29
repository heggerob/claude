using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Anything that stops BBs. Height is in metres: BBs flying higher pass over it.</summary>
    public class Obstacle : MonoBehaviour
    {
        public float Height = 3f;
        /// <summary>Soft cover (bushes): you walk through it and it hides you, but most BBs get through.</summary>
        public bool Soft;

        /// <summary>Tall or dense enough to hide a standing player.</summary>
        public bool BlocksSight { get { return Soft || Height >= 2f; } }

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

        static readonly Color WallColor = new Color(0.55f, 0.42f, 0.3f);
        static readonly Color IndoorWallColor = new Color(0.62f, 0.6f, 0.56f);
        static readonly Color ContainerColor = new Color(0.36f, 0.45f, 0.5f);
        static readonly Color SandbagColor = new Color(0.78f, 0.7f, 0.5f);
        static readonly Color CrateColor = new Color(0.72f, 0.55f, 0.36f);

        public static Transform Build(Transform parent, MapDefinition map)
        {
            if (root != null) Object.Destroy(root.gameObject);
            Current = map;
            root = new GameObject("Map: " + map.name).transform;
            root.SetParent(parent, false);

            var b = map.bounds;
            var groundSprite = map.ground == GroundStyle.Concrete ? SpriteFactory.Concrete
                : map.ground == GroundStyle.ForestFloor ? SpriteFactory.ForestFloor : SpriteFactory.Grass;
            var ground = NewRenderer("Ground", root, groundSprite, -20);
            ground.transform.position = b.center;
            ground.drawMode = SpriteDrawMode.Tiled;
            // Indoors the floor stops at the walls; outdoors the grass carries on past the netting.
            ground.size = map.indoor ? new Vector2(b.width + 2f, b.height + 2f) : new Vector2(b.width + 24f, b.height + 24f);

            Zone(map.spawnZones[0], Teams.Color(Team.Blue));
            Zone(map.spawnZones[1], Teams.Color(Team.Red));

            foreach (var piece in map.pieces) Place(piece, map.indoor);
            return root;
        }

        static void Place(MapPiece p, bool indoor)
        {
            switch (p.kind)
            {
                case PieceKind.Wall: Block(p, SpriteFactory.Crate, indoor ? IndoorWallColor : WallColor, WallHeight, 2); break;
                case PieceKind.Container: Block(p, SpriteFactory.Crate, ContainerColor, WallHeight, 2); break;
                case PieceKind.Sandbags: Block(p, SpriteFactory.Sandbag, SandbagColor, LowCoverHeight, 1); break;
                case PieceKind.Shelf: Block(p, SpriteFactory.Shelf, Color.white, 2.2f, 2); break;
                case PieceKind.Crates: Block(p, SpriteFactory.Crate, CrateColor, 1.1f, 1); break;
                case PieceKind.Log: Block(p, SpriteFactory.Log, Color.white, 0.6f, 1); break;
                case PieceKind.Rock: Round(p, SpriteFactory.Rock, 1.2f, false, 1, Mathf.Min(p.size.x, p.size.y) * 0.45f, 0f); break;
                case PieceKind.Tree: Tree(p); break;
                case PieceKind.Bush: Round(p, SpriteFactory.Bush, 1.6f, true, 26, Mathf.Min(p.size.x, p.size.y) * 0.45f, 0.95f); break;
            }
        }

        static void Block(MapPiece p, Sprite sprite, Color color, float height, int order)
        {
            var sr = NewRenderer(p.kind.ToString(), root, sprite, order);
            sr.transform.position = p.center;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = p.size;
            sr.color = color;
            var col = sr.gameObject.AddComponent<BoxCollider2D>();
            col.size = p.size;
            var o = sr.gameObject.AddComponent<Obstacle>();
            o.Height = height;
        }

        static void Round(MapPiece p, Sprite sprite, float height, bool soft, int order, float radius, float foliageAlpha)
        {
            var sr = NewRenderer(p.kind.ToString(), root, sprite, order);
            sr.transform.position = p.center;
            float spriteSize = sprite.texture.width / (float)SpriteFactory.PixelsPerUnit;
            float scale = Mathf.Max(p.size.x, p.size.y) / spriteSize;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
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
