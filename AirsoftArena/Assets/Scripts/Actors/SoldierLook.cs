using UnityEngine;

namespace AirsoftArena
{
    public enum HeadGearStyle { Helmet, Cap, Boonie }

    /// <summary>How a soldier looks. Pure cosmetics: no effect on gameplay.</summary>
    [System.Serializable]
    public class SoldierLook
    {
        public CamoPattern camo = CamoPattern.Woodland;
        public Color uniform = new Color(0.47f, 0.52f, 0.36f);
        public HeadGearStyle headGear = HeadGearStyle.Helmet;
        public Color tracer = new Color(1f, 0.97f, 0.85f);

        public static readonly Color[] UniformColors =
        {
            new Color(0.47f, 0.52f, 0.36f), // olive
            new Color(0.62f, 0.56f, 0.42f), // coyote
            new Color(0.35f, 0.37f, 0.4f),  // urban grey
            new Color(0.25f, 0.27f, 0.22f), // ranger green
            new Color(0.2f, 0.2f, 0.22f),   // black
        };

        public SoldierLook Clone()
        {
            return new SoldierLook { camo = camo, uniform = uniform, headGear = headGear, tracer = tracer };
        }

        public static SoldierLook RandomBot()
        {
            return new SoldierLook
            {
                camo = (CamoPattern)Random.Range(0, 4),
                uniform = UniformColors[Random.Range(0, UniformColors.Length)],
                headGear = Random.value < 0.6f ? HeadGearStyle.Helmet : Random.value < 0.5f ? HeadGearStyle.Cap : HeadGearStyle.Boonie,
            };
        }
    }
}
