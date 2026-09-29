using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Anything that stops BBs. Height is in metres: BBs flying higher pass over it.</summary>
    public class Obstacle : MonoBehaviour
    {
        public float Height = 3f;

        /// <summary>Tall enough to hide a standing player.</summary>
        public bool BlocksSight { get { return Height >= 2f; } }
    }

    /// <summary>
    /// Builds the test map "Pallet Yard" from code. 1 world unit = 1 metre.
    /// Left half is mirrored to the right so both teams get the same field.
    /// </summary>
    public static class MapBuilder
    {
        public const float LowCoverHeight = 0.95f;
        public const float WallHeight = 3f;

        public static readonly Rect Bounds = new Rect(-22f, -13f, 44f, 26f);
        public static readonly Rect[] SpawnZones =
        {
            new Rect(-21f, -4f, 4f, 8f), // Blue
            new Rect(17f, -4f, 4f, 8f),  // Red
        };

        static readonly Color WallColor = new Color(0.55f, 0.42f, 0.3f);
        static readonly Color ContainerColor = new Color(0.36f, 0.45f, 0.5f);
        static readonly Color SandbagColor = new Color(0.78f, 0.7f, 0.5f);

        public static Transform Build(Transform parent)
        {
            var root = new GameObject("Map: Pallet Yard").transform;
            root.SetParent(parent, false);

            var ground = NewRenderer("Ground", root, SpriteFactory.Grass, -20);
            ground.drawMode = SpriteDrawMode.Tiled;
            ground.size = new Vector2(Bounds.width + 20f, Bounds.height + 20f);

            Zone(root, SpawnZones[0], Teams.Color(Team.Blue));
            Zone(root, SpawnZones[1], Teams.Color(Team.Red));

            // Outer netting / walls.
            Block(root, new Vector2(0f, Bounds.yMax + 0.5f), new Vector2(Bounds.width + 2f, 1f), false, WallColor);
            Block(root, new Vector2(0f, Bounds.yMin - 0.5f), new Vector2(Bounds.width + 2f, 1f), false, WallColor);
            Block(root, new Vector2(Bounds.xMin - 0.5f, 0f), new Vector2(1f, Bounds.height), false, WallColor);
            Block(root, new Vector2(Bounds.xMax + 0.5f, 0f), new Vector2(1f, Bounds.height), false, WallColor);

            // Spawn walls with a gap in the middle and at both ends.
            Mirrored(root, new Vector2(-14f, 6f), new Vector2(1f, 6f), false, WallColor);
            Mirrored(root, new Vector2(-14f, -6f), new Vector2(1f, 6f), false, WallColor);
            Mirrored(root, new Vector2(-11f, 0f), new Vector2(1f, 3f), true, SandbagColor);
            Mirrored(root, new Vector2(-10f, 9.5f), new Vector2(3f, 1f), true, SandbagColor);
            Mirrored(root, new Vector2(-10f, -9.5f), new Vector2(3f, 1f), true, SandbagColor);

            // Shipping containers.
            Mirrored(root, new Vector2(-6.5f, 4.5f), new Vector2(5f, 2f), false, ContainerColor);
            Mirrored(root, new Vector2(-6.5f, -4.5f), new Vector2(5f, 2f), false, ContainerColor);
            Mirrored(root, new Vector2(-6f, 0f), new Vector2(1f, 2f), true, SandbagColor);
            Mirrored(root, new Vector2(-3.5f, 10f), new Vector2(1f, 3f), true, SandbagColor);
            Mirrored(root, new Vector2(-3.5f, -10f), new Vector2(1f, 3f), true, SandbagColor);

            // Centre bunker corridor and pallets.
            Block(root, new Vector2(0f, 2.5f), new Vector2(4f, 1f), false, WallColor);
            Block(root, new Vector2(0f, -2.5f), new Vector2(4f, 1f), false, WallColor);
            Block(root, new Vector2(0f, 7f), new Vector2(2.5f, 1f), true, SandbagColor);
            Block(root, new Vector2(0f, -7f), new Vector2(2.5f, 1f), true, SandbagColor);

            return root;
        }

        static void Mirrored(Transform root, Vector2 center, Vector2 size, bool low, Color color)
        {
            Block(root, center, size, low, color);
            Block(root, new Vector2(-center.x, center.y), size, low, color);
        }

        static Obstacle Block(Transform root, Vector2 center, Vector2 size, bool low, Color color)
        {
            var sr = NewRenderer(low ? "Sandbags" : "Wall", root, low ? SpriteFactory.Sandbag : SpriteFactory.Crate, low ? 1 : 2);
            sr.transform.position = center;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.color = color;

            var col = sr.gameObject.AddComponent<BoxCollider2D>();
            col.size = size;

            var obstacle = sr.gameObject.AddComponent<Obstacle>();
            obstacle.Height = low ? LowCoverHeight : WallHeight;
            return obstacle;
        }

        static void Zone(Transform root, Rect rect, Color teamColor)
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
