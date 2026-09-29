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

        public SoldierLook Clone()
        {
            return new SoldierLook { camo = camo, uniform = uniform, headGear = headGear, tracer = tracer };
        }
    }
}
