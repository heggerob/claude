using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    public enum CosmeticSlot { Camo, Uniform, HeadGear, Tracer }

    public enum Rarity { Common, Rare, Epic, Legendary }

    /// <summary>One thing you can wear. Cosmetics never change gameplay.</summary>
    public class CosmeticItem
    {
        public string id;
        public string name;
        public CosmeticSlot slot;
        public Rarity rarity;
        public int price;
        /// <summary>Owned by everyone from the start.</summary>
        public bool starter;
        public CamoPattern camo;
        public HeadGearStyle headGear;
        public Color color;

        public string RarityLabel { get { return rarity.ToString().ToUpper(); } }
    }

    public static class Rarities
    {
        public static Color Color(Rarity r)
        {
            switch (r)
            {
                case Rarity.Rare: return new Color(0.35f, 0.6f, 1f);
                case Rarity.Epic: return new Color(0.75f, 0.4f, 1f);
                case Rarity.Legendary: return new Color(1f, 0.75f, 0.2f);
                default: return new Color(0.8f, 0.8f, 0.78f);
            }
        }

        public static string Hex(Rarity r)
        {
            switch (r)
            {
                case Rarity.Rare: return "#5a99ff";
                case Rarity.Epic: return "#bf66ff";
                case Rarity.Legendary: return "#ffbf33";
                default: return "#cccccc";
            }
        }
    }

    /// <summary>Every cosmetic in the game.</summary>
    public static class CosmeticCatalog
    {
        static List<CosmeticItem> all;
        static Dictionary<string, CosmeticItem> byId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { all = null; byId = null; }

        public static List<CosmeticItem> All { get { Build(); return all; } }

        public static CosmeticItem Get(string id)
        {
            Build();
            CosmeticItem item;
            return id != null && byId.TryGetValue(id, out item) ? item : null;
        }

        public static List<CosmeticItem> InSlot(CosmeticSlot slot)
        {
            var list = new List<CosmeticItem>();
            foreach (var c in All) if (c.slot == slot) list.Add(c);
            return list;
        }

        public static string DefaultId(CosmeticSlot slot)
        {
            switch (slot)
            {
                case CosmeticSlot.Camo: return "camo_woodland";
                case CosmeticSlot.Uniform: return "uni_olive";
                case CosmeticSlot.HeadGear: return "head_helmet";
                default: return "bb_white";
            }
        }

        static void Build()
        {
            if (all != null) return;
            all = new List<CosmeticItem>
            {
                Camo("camo_solid", "Plain", CamoPattern.Solid, Rarity.Common, 0, true),
                Camo("camo_woodland", "Woodland", CamoPattern.Woodland, Rarity.Common, 0, true),
                Camo("camo_digital", "Digital", CamoPattern.Digital, Rarity.Rare, 300),
                Camo("camo_tiger", "Tiger Stripe", CamoPattern.Tiger, Rarity.Epic, 650),

                Uniform("uni_olive", "Olive Drab", new Color(0.47f, 0.52f, 0.36f), Rarity.Common, 0, true),
                Uniform("uni_coyote", "Coyote", new Color(0.62f, 0.56f, 0.42f), Rarity.Common, 0, true),
                Uniform("uni_urban", "Urban Grey", new Color(0.35f, 0.37f, 0.4f), Rarity.Common, 120),
                Uniform("uni_ranger", "Ranger Green", new Color(0.25f, 0.27f, 0.22f), Rarity.Common, 120),
                Uniform("uni_black", "Night Ops", new Color(0.2f, 0.2f, 0.22f), Rarity.Rare, 280),
                Uniform("uni_arctic", "Arctic", new Color(0.88f, 0.9f, 0.92f), Rarity.Rare, 320),
                Uniform("uni_pink", "Hot Pink", new Color(1f, 0.45f, 0.7f), Rarity.Epic, 700),
                Uniform("uni_gold", "Solid Gold", new Color(0.9f, 0.72f, 0.22f), Rarity.Legendary, 1600),

                Head("head_helmet", "Tactical Helmet", HeadGearStyle.Helmet, Rarity.Common, 0, true),
                Head("head_cap", "Baseball Cap", HeadGearStyle.Cap, Rarity.Common, 90),
                Head("head_boonie", "Boonie Hat", HeadGearStyle.Boonie, Rarity.Rare, 220),

                Tracer("bb_white", "White BBs", new Color(1f, 0.97f, 0.85f), Rarity.Common, 0, true),
                Tracer("bb_green", "Green Tracer", new Color(0.45f, 1f, 0.4f), Rarity.Rare, 260),
                Tracer("bb_red", "Red Tracer", new Color(1f, 0.35f, 0.3f), Rarity.Rare, 260),
                Tracer("bb_blue", "Ice Tracer", new Color(0.45f, 0.8f, 1f), Rarity.Epic, 520),
                Tracer("bb_gold", "Golden BBs", new Color(1f, 0.82f, 0.2f), Rarity.Legendary, 1300),
            };
            byId = new Dictionary<string, CosmeticItem>();
            foreach (var c in all) byId[c.id] = c;
        }

        static CosmeticItem Camo(string id, string name, CamoPattern p, Rarity r, int price, bool starter = false)
        {
            return new CosmeticItem { id = id, name = name, slot = CosmeticSlot.Camo, camo = p, rarity = r, price = price, starter = starter, color = Color.white };
        }

        static CosmeticItem Uniform(string id, string name, Color c, Rarity r, int price, bool starter = false)
        {
            return new CosmeticItem { id = id, name = name, slot = CosmeticSlot.Uniform, color = c, rarity = r, price = price, starter = starter };
        }

        static CosmeticItem Head(string id, string name, HeadGearStyle h, Rarity r, int price, bool starter = false)
        {
            return new CosmeticItem { id = id, name = name, slot = CosmeticSlot.HeadGear, headGear = h, rarity = r, price = price, starter = starter, color = Color.white };
        }

        static CosmeticItem Tracer(string id, string name, Color c, Rarity r, int price, bool starter = false)
        {
            return new CosmeticItem { id = id, name = name, slot = CosmeticSlot.Tracer, color = c, rarity = r, price = price, starter = starter };
        }

        /// <summary>Builds a look from equipped item ids. Unknown ids fall back to the defaults.</summary>
        public static SoldierLook BuildLook(string camoId, string uniformId, string headId, string tracerId)
        {
            var look = new SoldierLook();
            var camo = Get(camoId) ?? Get(DefaultId(CosmeticSlot.Camo));
            var uniform = Get(uniformId) ?? Get(DefaultId(CosmeticSlot.Uniform));
            var head = Get(headId) ?? Get(DefaultId(CosmeticSlot.HeadGear));
            var tracer = Get(tracerId) ?? Get(DefaultId(CosmeticSlot.Tracer));
            look.camo = camo.camo;
            look.uniform = uniform.color;
            look.headGear = head.headGear;
            look.tracer = tracer.color;
            return look;
        }

        /// <summary>A random look from the whole catalogue, weighted towards common items, for bots.</summary>
        public static SoldierLook RandomLook()
        {
            return BuildLook(RandomId(CosmeticSlot.Camo), RandomId(CosmeticSlot.Uniform), RandomId(CosmeticSlot.HeadGear), RandomId(CosmeticSlot.Tracer));
        }

        static string RandomId(CosmeticSlot slot)
        {
            var items = InSlot(slot);
            float total = 0f;
            foreach (var i in items) total += Weight(i.rarity);
            float roll = Random.value * total;
            foreach (var i in items)
            {
                roll -= Weight(i.rarity);
                if (roll <= 0f) return i.id;
            }
            return items[0].id;
        }

        public static float Weight(Rarity r)
        {
            switch (r)
            {
                case Rarity.Rare: return 0.3f;
                case Rarity.Epic: return 0.1f;
                case Rarity.Legendary: return 0.03f;
                default: return 1f;
            }
        }
    }
}
