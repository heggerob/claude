using UnityEngine;

namespace OdinsCoin
{
    /// <summary>How a sail is rigged: a square sail on a yard across the ship, or a fore-and-aft (lateen) sail that swings nearly in line with it.</summary>
    public enum Rig { Square, Lateen }

    /// <summary>One mast's sail.</summary>
    public class SailPlan
    {
        public string name;
        public Rig rig;
        /// <summary>Sail area at full sail (m²).</summary>
        public float area;
        /// <summary>Where the mast stands, metres forward of midships.</summary>
        public float x;
        /// <summary>Height of the sail's centre of effort above the waterline (m).</summary>
        public float height;
        /// <summary>How far the yard can be swung round from square across the ship (degrees).</summary>
        public float maxBrace;
    }

    /// <summary>
    /// A ship class: its hull, rig, oars and rudder, in real units. The physics (<see cref="ShipPhysics"/>) works from
    /// these numbers alone, so a new class is just new numbers. The classes are our own invention: bigger and
    /// fiercer than any real longship, with high stern castles, several masts and banks of oars, but built and
    /// sailed like wooden ships of the north.
    /// </summary>
    public class ShipDesign
    {
        public string id, title, blurb;
        /// <summary>Waterline length, beam and draught (m); freeboard amidships (m).</summary>
        public float length, beam, draught, freeboard;
        /// <summary>Block coefficient: how full the underwater hull is compared to a box (slim ships ~0.4).</summary>
        public float blockCoef = 0.45f;
        /// <summary>Waterplane coefficient: how full the hull is at the waterline.</summary>
        public float waterplaneCoef = 0.7f;
        /// <summary>Height of the centre of gravity above the keel, as a fraction of draught + freeboard.</summary>
        public float cgHeight = 0.42f;
        public SailPlan[] sails = new SailPlan[0];
        /// <summary>Rowers on each side (one per oar).</summary>
        public int oarsPerSide;
        /// <summary>Rudder blade area (m²) and its aspect ratio (depth / chord).</summary>
        public float rudderArea, rudderAspect = 1.5f;
        /// <summary>Extra lateral area of the keel and skeg below the hull (m²).</summary>
        public float keelArea;

        public const float RhoWater = 1025f, Gravity = 9.81f;

        /// <summary>Displaced volume (m³) and so mass (kg) when floating at its design draught.</summary>
        public float Volume { get { return blockCoef * length * beam * draught; } }
        public float Mass { get { return RhoWater * Volume; } }
        /// <summary>Wetted surface (m²), Mumford's estimate.</summary>
        public float WettedArea { get { return length * (1.7f * draught + blockCoef * beam); } }
        /// <summary>The underwater side area that resists sliding sideways: hull plus keel (m²).</summary>
        public float LateralArea { get { return 0.9f * length * draught + keelArea; } }
        public float WaterplaneArea { get { return waterplaneCoef * length * beam; } }
        /// <summary>The speed where a displacement hull starts climbing its own bow wave (m/s): about 1.34 √(L in feet) knots.</summary>
        public float HullSpeed { get { return 0.4f * Mathf.Sqrt(Gravity * length); } }
        /// <summary>Metacentric height (m): how stiff she is against heeling.</summary>
        public float GM
        {
            get
            {
                float kb = 0.53f * draught;
                float bm = waterplaneCoef * waterplaneCoef * beam * beam / (12f * blockCoef * draught) * 0.9f;
                float kg = cgHeight * (draught + freeboard);
                return kb + bm - kg;
            }
        }
        public float TotalSailArea { get { float a = 0f; foreach (var s in sails) a += s.area; return a; } }

        // ---------------------------------------------------------------- the classes

        /// <summary>A small, quick fjord runner: shallow enough for the skerries, one sail, a few oars.</summary>
        public static readonly ShipDesign Skerrycutter = new ShipDesign
        {
            id = "skerrycutter", title = "Skerrycutter",
            blurb = "Small, shallow and quick: threads the skerries where big ships would ground.",
            length = 17f, beam = 3.8f, draught = 0.8f, freeboard = 1.1f, blockCoef = 0.4f,
            sails = new[] { new SailPlan { name = "Main", rig = Rig.Square, area = 75f, x = 0.5f, height = 6.5f, maxBrace = 50f } },
            oarsPerSide = 8, rudderArea = 1.1f, keelArea = 2.5f,
        };

        /// <summary>The raider: a long, lean two-master with a lateen mizzen that lets her point higher than any square-rigger.</summary>
        public static readonly ShipDesign Wavewolf = new ShipDesign
        {
            id = "wavewolf", title = "Wavewolf",
            blurb = "A lean raider with a lateen mizzen: points higher into the wind than a square-rigger, and rows hard.",
            length = 30f, beam = 5.6f, draught = 1.3f, freeboard = 1.6f, blockCoef = 0.42f,
            sails = new[] {
                new SailPlan { name = "Main", rig = Rig.Square, area = 170f, x = 2f, height = 10f, maxBrace = 50f },
                new SailPlan { name = "Mizzen", rig = Rig.Lateen, area = 70f, x = -9f, height = 7f, maxBrace = 80f } },
            oarsPerSide = 18, rudderArea = 2.4f, keelArea = 6f,
        };

        /// <summary>The war galley: three masts, a towering stern castle and thirty oars a side.</summary>
        public static readonly ShipDesign Stormbreaker = new ShipDesign
        {
            id = "stormbreaker", title = "Stormbreaker",
            blurb = "A three-masted war galley with a stern castle and thirty oars a side: slow to turn, impossible to stop.",
            length = 44f, beam = 8.5f, draught = 2f, freeboard = 2.4f, blockCoef = 0.46f,
            sails = new[] {
                new SailPlan { name = "Fore", rig = Rig.Square, area = 140f, x = 13f, height = 11f, maxBrace = 50f },
                new SailPlan { name = "Main", rig = Rig.Square, area = 300f, x = 1f, height = 15f, maxBrace = 50f },
                new SailPlan { name = "Mizzen", rig = Rig.Lateen, area = 110f, x = -13f, height = 10f, maxBrace = 80f } },
            oarsPerSide = 30, rudderArea = 5f, keelArea = 12f,
        };

        /// <summary>The flagship: four masts and a hall on the deck. Built for jarls who want to be seen from the horizon.</summary>
        public static readonly ShipDesign Krakenhall = new ShipDesign
        {
            id = "krakenhall", title = "Krakenhall",
            blurb = "A four-masted floating hall for a jarl's fleet: a crew of a hundred, and a wake you can see for miles.",
            length = 62f, beam = 12f, draught = 2.8f, freeboard = 3.2f, blockCoef = 0.5f,
            sails = new[] {
                new SailPlan { name = "Fore", rig = Rig.Square, area = 260f, x = 20f, height = 14f, maxBrace = 50f },
                new SailPlan { name = "Main", rig = Rig.Square, area = 520f, x = 5f, height = 20f, maxBrace = 50f },
                new SailPlan { name = "Middle", rig = Rig.Square, area = 360f, x = -8f, height = 17f, maxBrace = 50f },
                new SailPlan { name = "Mizzen", rig = Rig.Lateen, area = 180f, x = -21f, height = 12f, maxBrace = 80f } },
            oarsPerSide = 40, rudderArea = 8.5f, keelArea = 20f,
        };

        public static readonly ShipDesign[] All = { Skerrycutter, Wavewolf, Stormbreaker, Krakenhall };
    }
}
