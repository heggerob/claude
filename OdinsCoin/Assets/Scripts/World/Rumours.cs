using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// What people tell you, instead of a quest list: talk to the townsfolk and they pass on a rumour about
    /// somewhere worth going: a rich place not yet plundered, a hoard buried somewhere, a rune ring still
    /// asleep. Rumours give the rough way and distance ("two days' sail to the south-west"), never a marker; it's
    /// up to you to go and look.
    /// </summary>
    public static class Rumours
    {
        public enum Kind { Plunder, Hoard, RuneRing, Cave }

        public struct Rumour
        {
            public Kind kind;
            public Place place;
            public string text;
        }

        static readonly string[] points = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };

        /// <summary>A bearing (degrees, clockwise from north) as one of the eight points of the compass.</summary>
        public static string Compass(float bearing)
        {
            int i = Mathf.RoundToInt(Mathf.Repeat(bearing, 360f) / 45f) % 8;
            return points[i];
        }

        /// <summary>A distance as a sailor would put it.</summary>
        public static string HowFar(float metres)
        {
            if (metres < 3000f) return "close by";
            if (metres < 15000f) return "a short sail";
            if (metres < 60000f) return "half a day's sail";
            if (metres < 150000f) return "a day's sail";
            if (metres < 400000f) return "a few days' sail";
            return "far across the sea";
        }

        /// <summary>
        /// A rumour for someone at <paramref name="from"/> (global), from the places not yet plundered, hoards not dug
        /// and rings not woken; <paramref name="pick"/> 0..1 chooses among them. <paramref name="position"/> gives a
        /// place's global position. Null when there's nothing left to tell.
        /// </summary>
        public static Rumour? Tell(Vector3 from, IList<Place> places, System.Func<Place, Vector3> position,
            ICollection<string> raided, ICollection<string> dug, ICollection<string> shrines, float pick, ICollection<string> caves = null)
        {
            var options = new List<Rumour>();
            foreach (var p in places)
            {
                var at = position(p);
                float dx = at.x - from.x, dz = at.z - from.z, d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < 1500f) continue; // not about the place you're standing in
                string where = HowFar(d) + " to the " + Compass(Mathf.Atan2(dx, dz) * Mathf.Rad2Deg);
                var plunder = PlaceLife.PlunderOf(p.kind);
                if (plunder.chests > 0 && !raided.Contains(p.name))
                    options.Add(new Rumour { kind = Kind.Plunder, place = p, text = "They say " + p.name + " is rich and " + (plunder.guards <= 2 ? "hardly guarded" : "well guarded") + ". " + Capital(where) + "." });
                if (!dug.Contains(p.name))
                    options.Add(new Rumour { kind = Kind.Hoard, place = p, text = "My grandfather swore there's a hoard buried out beyond the houses at " + p.name + ", under a cairn. " + Capital(where) + "." });
                if (caves != null && !caves.Contains(p.name))
                    options.Add(new Rumour { kind = Kind.Cave, place = p, text = "There's a cave in the hills behind " + p.name + ". Something dead guards it, and it guards gold. " + Capital(where) + "." });
                if (!shrines.Contains(p.name))
                    options.Add(new Rumour { kind = Kind.RuneRing, place = p, text = "The rune ring by the " + LandmarkWord(Landmarks.KindFor(p)) + " at " + p.name + " still sleeps. Count the notches. " + Capital(where) + "." });
            }
            if (options.Count == 0) return null;
            // Nearer places come up more often: sort by distance and lean the pick towards the front.
            options.Sort((a, b) => (position(a.place) - from).sqrMagnitude.CompareTo((position(b.place) - from).sqrMagnitude));
            int i = Mathf.Clamp(Mathf.FloorToInt(pick * pick * options.Count), 0, options.Count - 1);
            return options[i];
        }

        static string LandmarkWord(LandmarkKind k)
        {
            switch (k)
            {
                case LandmarkKind.BellTower: return "bell tower";
                case LandmarkKind.GreatAsh: return "great ash";
                case LandmarkKind.RuneStone: return "runestone";
                default: return "beacon tower";
            }
        }

        static string Capital(string s) { return s.Length == 0 ? s : char.ToUpper(s[0]) + s.Substring(1); }
    }
}
