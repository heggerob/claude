using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The shipwright at the home fjord: bigger hulls for gold. You start with a Wavewolf; a Stormbreaker or a
    /// Krakenhall carries more sail and more oars, and a Skerrycutter slips through skerries the big ships can't.
    /// Ships you've bought stay yours: swap between them at the home jetty.
    /// </summary>
    public static class Shipwright
    {
        /// <summary>The ship every voyage starts with.</summary>
        public static ShipDesign Starter { get { return ShipDesign.Wavewolf; } }

        /// <summary>What the shipwright asks for a hull (gold).</summary>
        public static int Price(ShipDesign d)
        {
            if (d == ShipDesign.Skerrycutter) return 400;
            if (d == ShipDesign.Wavewolf) return 0;
            if (d == ShipDesign.Stormbreaker) return 2500;
            if (d == ShipDesign.Krakenhall) return 6000;
            return 99999;
        }

        public static ShipDesign Find(string id)
        {
            foreach (var d in ShipDesign.All) if (d.id == id) return d;
            return null;
        }

        /// <summary>A line of the ship's numbers for the shipwright's board.</summary>
        public static string Numbers(ShipDesign d)
        {
            return string.Format("{0:0} m · {1:0} t · {2} sail{3}, {4:0} m² · {5} oars a side · draws {6:0.0} m",
                d.length, d.Mass / 1000f, d.sails.Length, d.sails.Length == 1 ? "" : "s", d.TotalSailArea, d.oarsPerSide, d.draught);
        }
    }
}
