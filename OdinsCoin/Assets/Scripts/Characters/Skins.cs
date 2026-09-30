using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>A colour set for one outfit, bought for gold in the mead hall.</summary>
    public class SkinDef
    {
        public string id, name, blurb;
        public OutfitId outfit;
        public int cost;
        /// <summary>Repaints the outfit's default palette (hair is left alone: that's the player's own).</summary>
        public System.Action<Palette> paint;
    }

    /// <summary>
    /// Every skin in the game. Each outfit has its classic colours (free) and two more to buy. Skins only change
    /// colours, never the shape or the abilities, so they are purely for looks.
    /// </summary>
    public static class Skins
    {
        public static readonly SkinDef[] All =
        {
            Classic(OutfitId.Raider),
            new SkinDef { id = "raider.ash", outfit = OutfitId.Raider, name = "Ash and Iron", cost = 250, blurb = "Grey wool and snow-white fur, a black banner.",
                paint = p => { p.cloth = new Color(0.36f, 0.36f, 0.37f); p.clothDark = new Color(0.22f, 0.22f, 0.23f); p.fur = new Color(0.92f, 0.9f, 0.86f); p.furShadow = new Color(0.66f, 0.64f, 0.6f); p.accent = new Color(0.12f, 0.12f, 0.13f); p.emblem = new Color(0.85f, 0.85f, 0.82f); } },
            new SkinDef { id = "raider.bloodmoon", outfit = OutfitId.Raider, name = "Blood Moon", cost = 500, blurb = "Dyed deep red for the raid under the full moon.",
                paint = p => { p.cloth = new Color(0.42f, 0.1f, 0.09f); p.clothDark = new Color(0.22f, 0.06f, 0.06f); p.fur = new Color(0.36f, 0.3f, 0.26f); p.furShadow = new Color(0.22f, 0.18f, 0.15f); p.accent = new Color(0.1f, 0.08f, 0.08f); p.emblem = new Color(0.9f, 0.75f, 0.4f); } },

            Classic(OutfitId.Jarl),
            new SkinDef { id = "jarl.whitewolf", outfit = OutfitId.Jarl, name = "White Wolf", cost = 400, blurb = "Pale coat, silver trim and an ice-blue cloak.",
                paint = p => { p.cloth = new Color(0.78f, 0.78f, 0.8f); p.clothDark = new Color(0.55f, 0.56f, 0.6f); p.accent = new Color(0.3f, 0.45f, 0.62f); p.brass = new Color(0.78f, 0.8f, 0.84f); p.fur = new Color(0.95f, 0.95f, 0.94f); } },
            new SkinDef { id = "jarl.midnight", outfit = OutfitId.Jarl, name = "Midnight Court", cost = 700, blurb = "Night-blue velvet and a cloak like the winter sky.",
                paint = p => { p.cloth = new Color(0.1f, 0.12f, 0.24f); p.clothDark = new Color(0.06f, 0.07f, 0.14f); p.accent = new Color(0.16f, 0.2f, 0.42f); p.fur = new Color(0.3f, 0.28f, 0.3f); p.furShadow = new Color(0.18f, 0.17f, 0.19f); } },

            Classic(OutfitId.Navigator),
            new SkinDef { id = "navigator.seaglass", outfit = OutfitId.Navigator, name = "Sea Glass", cost = 250, blurb = "Pale linen and a cloak the green of shallow water.",
                paint = p => { p.cloth = new Color(0.72f, 0.68f, 0.58f); p.clothDark = new Color(0.5f, 0.47f, 0.4f); p.accent = new Color(0.24f, 0.52f, 0.5f); p.cloth2 = new Color(0.42f, 0.36f, 0.26f); } },
            new SkinDef { id = "navigator.amber", outfit = OutfitId.Navigator, name = "Amber Road", cost = 450, blurb = "Traded for amber on the eastern rivers.",
                paint = p => { p.cloth = new Color(0.6f, 0.38f, 0.14f); p.clothDark = new Color(0.36f, 0.22f, 0.1f); p.accent = new Color(0.55f, 0.2f, 0.12f); p.cloth2 = new Color(0.86f, 0.62f, 0.2f); } },

            Classic(OutfitId.SpearGuard),
            new SkinDef { id = "guard.oak", outfit = OutfitId.SpearGuard, name = "Oak and Iron", cost = 250, blurb = "Forest green and a shield painted like old oak.",
                paint = p => { p.cloth = new Color(0.28f, 0.36f, 0.22f); p.clothDark = new Color(0.16f, 0.2f, 0.13f); p.accent = new Color(0.45f, 0.3f, 0.15f); } },
            new SkinDef { id = "guard.winter", outfit = OutfitId.SpearGuard, name = "Winter Watch", cost = 450, blurb = "For long nights on the wall: white fur, frost-blue cloak.",
                paint = p => { p.cloth = new Color(0.62f, 0.64f, 0.68f); p.clothDark = new Color(0.4f, 0.42f, 0.46f); p.accent = new Color(0.3f, 0.42f, 0.6f); p.fur = new Color(0.95f, 0.95f, 0.94f); p.furShadow = new Color(0.72f, 0.73f, 0.75f); } },

            Classic(OutfitId.Seer),
            new SkinDef { id = "seer.raven", outfit = OutfitId.Seer, name = "Raven", cost = 350, blurb = "Black feathers and runes that burn violet.",
                paint = p => { p.cloth = new Color(0.08f, 0.08f, 0.1f); p.clothDark = new Color(0.03f, 0.03f, 0.04f); p.emblem = new Color(0.72f, 0.45f, 1f); p.cloth2 = new Color(0.4f, 0.36f, 0.34f); } },
            new SkinDef { id = "seer.moss", outfit = OutfitId.Seer, name = "Moss Witch", cost = 550, blurb = "Grey-green like the bog, runes glowing like foxfire.",
                paint = p => { p.cloth = new Color(0.26f, 0.32f, 0.24f); p.clothDark = new Color(0.15f, 0.19f, 0.14f); p.emblem = new Color(0.55f, 1f, 0.5f); p.cloth2 = new Color(0.6f, 0.58f, 0.4f); } },

            Classic(OutfitId.Scout),
            new SkinDef { id = "scout.autumn", outfit = OutfitId.Scout, name = "Autumn", cost = 250, blurb = "Russet and gold, for hunting when the leaves fall.",
                paint = p => { p.accent = new Color(0.62f, 0.32f, 0.12f); p.cloth = new Color(0.36f, 0.24f, 0.14f); p.emblem = new Color(0.9f, 0.72f, 0.3f); } },
            new SkinDef { id = "scout.snowhare", outfit = OutfitId.Scout, name = "Snow Hare", cost = 450, blurb = "White on white: gone in the first snowfall.",
                paint = p => { p.cloth = new Color(0.82f, 0.82f, 0.8f); p.clothDark = new Color(0.6f, 0.6f, 0.6f); p.accent = new Color(0.55f, 0.57f, 0.6f); p.fur = new Color(0.97f, 0.97f, 0.96f); p.furShadow = new Color(0.78f, 0.78f, 0.78f); } },
        };

        static SkinDef Classic(OutfitId o)
        {
            return new SkinDef { id = ClassicId(o), outfit = o, name = "Classic", cost = 0, blurb = "The colours of the old tales.", paint = p => { } };
        }

        public static string ClassicId(OutfitId o) { return o.ToString().ToLowerInvariant() + ".classic"; }

        public static SkinDef Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in All) if (s.id == id) return s;
            return null;
        }

        public static List<SkinDef> For(OutfitId o)
        {
            var list = new List<SkinDef>();
            foreach (var s in All) if (s.outfit == o) list.Add(s);
            return list;
        }

        /// <summary>The palette for an outfit in a skin; an unknown skin, or one for another outfit, gives the classic colours.</summary>
        public static Palette Paint(OutfitId outfit, string skinId)
        {
            var p = Outfits.Get(outfit).palette();
            var s = Get(skinId);
            if (s != null && s.outfit == outfit) s.paint(p);
            return p;
        }
    }

    /// <summary>
    /// The skins the player owns. Kept in its own PlayerPrefs key, like the hero, so what you bought stays yours
    /// across voyages. Classic colours are always owned.
    /// </summary>
    public class SkinLocker
    {
        public const string Key = "odinscoin.skins";
        readonly HashSet<string> owned = new HashSet<string>();

        static SkinLocker current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { current = null; }

        public static SkinLocker Current { get { return current ?? (current = Parse(PlayerPrefs.GetString(Key, ""))); } }

        public bool Owns(SkinDef s) { return s != null && (s.cost == 0 || owned.Contains(s.id)); }
        public bool Owns(string id) { return Owns(Skins.Get(id)); }

        public bool CanBuy(SkinDef s, Fortune f) { return s != null && !Owns(s) && f.Gold >= s.cost; }

        /// <summary>Pay for a skin. Returns false (and takes nothing) if it's owned already or you can't afford it.</summary>
        public bool Buy(SkinDef s, Fortune f)
        {
            if (!CanBuy(s, f)) return false;
            f.Gold -= s.cost;
            owned.Add(s.id);
            return true;
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, Serialize());
            PlayerPrefs.Save();
        }

        public string Serialize()
        {
            var ids = new List<string>(owned);
            ids.Sort();
            return string.Join(",", ids.ToArray());
        }

        /// <summary>Unknown ids are dropped, so a skin removed from the game can't linger.</summary>
        public static SkinLocker Parse(string text)
        {
            var l = new SkinLocker();
            if (string.IsNullOrEmpty(text)) return l;
            foreach (var id in text.Split(','))
            {
                var s = Skins.Get(id.Trim());
                if (s != null && s.cost > 0) l.owned.Add(s.id);
            }
            return l;
        }
    }

    /// <summary>
    /// The turning stand in the mead hall: a hero on a plinth, slowly spinning, wearing whatever skin you're
    /// looking at in Bjorn's shop.
    /// </summary>
    public class SkinStand : MonoBehaviour
    {
        public static SkinStand Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        /// <summary>Degrees per second.</summary>
        public float spin = 35f;
        Transform turntable, figure;
        string showing;

        public static SkinStand Create(Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject("Skin Stand");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;
            var stand = go.AddComponent<SkinStand>();
            LongshipBuilder.Deco(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.08f, 0f), new Vector3(1.1f, 0.08f, 1.1f), Materials.DarkWood);
            stand.turntable = new GameObject("Turntable").transform;
            stand.turntable.SetParent(go.transform, false);
            stand.turntable.localPosition = new Vector3(0f, 0.16f, 0f);
            Instance = stand;
            return stand;
        }

        /// <summary>Dress the figure on the stand (rebuilt only when it changes).</summary>
        public void Show(CharacterSpec spec)
        {
            string key = HeroChoice.Serialize(spec);
            if (key == showing) return;
            showing = key;
            if (figure != null) Destroy(figure.gameObject);
            figure = new GameObject("Figure").transform;
            figure.SetParent(turntable, false);
            HeroBuilder.Build(figure, spec);
        }

        void Update()
        {
            if (turntable != null) turntable.Rotate(0f, spin * Time.deltaTime, 0f);
        }
    }
}
