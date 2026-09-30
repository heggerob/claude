using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A wooden chest with iron bands and gold spilling out. Holds a base amount of gold; Freya's Gift and
    /// Fenrir's Hunger change what you get when you cash it in. Pick it up, stow it on the ship and sell it
    /// to Gunnar in the home fjord. Dropped in the sea, it floats.
    /// </summary>
    public class TreasureChest : MonoBehaviour
    {
        /// <summary>Every chest in the world that hasn't been sold yet.</summary>
        public static readonly List<TreasureChest> All = new List<TreasureChest>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public int BaseGold;
        /// <summary>Plundered from a place (it may hold a map to a buried hoard, found when first picked up).</summary>
        public bool FromPlunder;
        /// <summary>The place whose cave this chest lay in (the cave stays empty once it's taken), or null.</summary>
        public string Cave;
        /// <summary>How far Odin has raised it: 0 a plain chest, 1 silver, 2 gold, 3 Odin's hoard. Each step doubles its worth.</summary>
        public int Tier;
        public const int MaxTier = 3;
        readonly List<Transform> trim = new List<Transform>();
        Light shine;
        /// <summary>Held in the Viking's arms right now.</summary>
        public bool Carried;
        public bool Sold;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static TreasureChest Create(Transform parent, Vector3 worldPos, float yaw, int gold)
        {
            var go = new GameObject("Treasure Chest");
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var wood = new Color(0.48f, 0.3f, 0.16f);
            var iron = new Color(0.3f, 0.3f, 0.32f);
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.6f, 0.65f), wood);
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.68f, 0f), new Vector3(1.02f, 0.18f, 0.67f), wood * 0.85f);
            var bandL = LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(-0.3f, 0.4f, 0f), new Vector3(0.08f, 0.82f, 0.69f), iron);
            var bandR = LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(0.3f, 0.4f, 0f), new Vector3(0.08f, 0.82f, 0.69f), iron);
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.45f, 0.34f), new Vector3(0.14f, 0.16f, 0.04f), Materials.Gold);
            LongshipBuilder.Deco(PrimitiveType.Sphere, go.transform, new Vector3(0.15f, 0.8f, -0.05f), new Vector3(0.3f, 0.12f, 0.3f), Materials.Gold);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.38f, 0f);
            col.size = new Vector3(1f, 0.76f, 0.65f);
            var chest = go.AddComponent<TreasureChest>();
            chest.BaseGold = gold;
            chest.trim.Add(bandL);
            chest.trim.Add(bandR);
            return chest;
        }

        /// <summary>What this chest is worth right now: its gold, doubled for each step Odin raised it, after blessings and curses.</summary>
        public int Value { get { return Worth(BaseGold, Tier, Fortune.Current); } }

        public static int Worth(int baseGold, Fortune fortune) { return Worth(baseGold, 0, fortune); }

        public static int Worth(int baseGold, int tier, Fortune fortune)
        {
            return Mathf.RoundToInt(baseGold * (1 << Mathf.Clamp(tier, 0, MaxTier)) * fortune.LootMultiplier);
        }

        /// <summary>What a chest of this tier is called in the game.</summary>
        public static string TierName(int tier)
        {
            switch (Mathf.Clamp(tier, 0, MaxTier))
            {
                case 1: return "silver chest";
                case 2: return "gold chest";
                case 3: return "Odin's hoard";
                default: return "chest";
            }
        }

        public string Name { get { return TierName(Tier); } }

        /// <summary>The colour of the bands for a tier: iron, silver, gold, and gold again for the hoard (which also shines).</summary>
        public static Color TrimColour(int tier)
        {
            switch (Mathf.Clamp(tier, 0, MaxTier))
            {
                case 1: return new Color(0.8f, 0.82f, 0.86f);
                case 2: case 3: return new Color(0.95f, 0.75f, 0.25f);
                default: return new Color(0.3f, 0.3f, 0.32f);
            }
        }

        /// <summary>Raise (or set) the chest's tier: its bands turn silver or gold, and Odin's hoard glows.</summary>
        public void SetTier(int tier)
        {
            Tier = Mathf.Clamp(tier, 0, MaxTier);
            foreach (var t in trim)
            {
                var r = t != null ? t.GetComponent<Renderer>() : null;
                if (r != null) r.sharedMaterial = Materials.Get(TrimColour(Tier));
            }
            if (Tier == MaxTier && shine == null)
            {
                shine = new GameObject("Hoard Glow").AddComponent<Light>();
                shine.transform.SetParent(transform, false);
                shine.transform.localPosition = new Vector3(0f, 1f, 0f);
                shine.type = LightType.Point;
                shine.color = new Color(1f, 0.8f, 0.35f);
                shine.range = 4f;
                shine.intensity = 1.2f;
            }
        }

        /// <summary>The nearest chest that can be picked up within <paramref name="range"/>, or null.</summary>
        public static TreasureChest NearestFree(Vector3 p, float range)
        {
            TreasureChest best = null;
            float bestD = range;
            foreach (var c in All)
            {
                if (c == null || c.Carried || c.Sold) continue;
                float d = Vector3.Distance(p, c.transform.position + Vector3.up * 0.4f);
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }

        /// <summary>Into the Viking's arms: no collider while carried, no bobbing.</summary>
        /// <summary>Picked up for the first time: whatever was tucked inside is found.</summary>
        public static event System.Action<TreasureChest> Opened;

        public void PickUp(Transform carrier)
        {
            Carried = true;
            if (FromPlunder) { FromPlunder = false; if (Opened != null) Opened(this); }
            if (Cave != null) { if (!Upgrades.Current.Caves.Contains(Cave)) { Upgrades.Current.Caves.Add(Cave); Secrets.Found(Cave); } Cave = null; }
            var floater = GetComponent<Floater>();
            if (floater != null) Destroy(floater);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            transform.SetParent(carrier, false);
            transform.localPosition = new Vector3(0f, 0.72f, 0.62f);
            transform.localRotation = Quaternion.identity;
            Sfx.At(SfxId.Chest, transform.position, 0.8f);
        }

        /// <summary>Put it down on whatever is under it (ship, land or jetty), or let it float if that's the sea.</summary>
        public void Drop(Vector3 at, float yaw, Transform world)
        {
            Carried = false;
            var col = GetComponent<Collider>();
            RaycastHit hit;
            bool ground = Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out hit, 4f) && hit.collider != null
                          && hit.point.y > Waves.Height(at.x, at.z) - 0.3f;
            // Stowed on the ship it becomes part of the ship and sails with it.
            Transform parent = world;
            if (ground && GameBootstrap.Instance != null && GameBootstrap.Instance.Ship != null && hit.collider.transform.IsChildOfOrSelf(GameBootstrap.Instance.Ship.transform))
                parent = GameBootstrap.Instance.Ship.transform;
            transform.SetParent(parent, true);
            transform.position = ground ? hit.point : at;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (col != null) col.enabled = true;
            if (!ground) gameObject.AddComponent<Floater>().sink = 0.35f;
            Sfx.At(ground ? SfxId.Chest : SfxId.Splash, transform.position, 0.8f);
        }

        public bool Stowed(Longship ship) { return !Carried && !Sold && ship != null && transform.IsChildOfOrSelf(ship.transform); }
    }
}
