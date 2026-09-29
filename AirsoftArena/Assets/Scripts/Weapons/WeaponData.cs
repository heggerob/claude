using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Weapon classes from the design plan. The number is the class code shown in game (00-07).</summary>
    public enum WeaponClass
    {
        Melee = 0,
        AssaultRifle = 1,
        SubmachineGun = 2,
        SniperMarksman = 3,
        Pistol = 4,
        Shotgun = 5,
        BurstRifle = 6,
        LightMachineGun = 7,
    }

    public enum PowerSystem { AEG, Gas, CO2, Spring }

    /// <summary>Single = bolt or pump action (manual cycling). Semi = one shot per trigger pull.</summary>
    public enum FireMode { Single, Semi, Burst, Auto }

    [System.Flags]
    public enum FireModes { None = 0, Single = 1, Semi = 2, Burst = 4, Auto = 8 }

    /// <summary>
    /// All stats for one replica. Create assets via Assets > Create > Airsoft Arena > Weapon,
    /// or use the built-in defaults in <see cref="WeaponCatalog"/>.
    /// Shared between the 2D and future 3D versions: nothing here knows how the game is drawn.
    /// </summary>
    [CreateAssetMenu(menuName = "Airsoft Arena/Weapon", fileName = "NewWeapon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string code = "01-XX0";
        public string displayName = "Unnamed";
        public WeaponClass weaponClass = WeaponClass.AssaultRifle;
        public PowerSystem power = PowerSystem.AEG;
        public FireModes fireModes = FireModes.Semi | FireModes.Auto;
        public int burstCount = 3;

        [Header("Ballistics")]
        [Tooltip("Muzzle velocity in feet per second, measured with a 0.20 g BB (how airsoft fields rate guns).")]
        public float fps = 350f;
        [Tooltip("BB weight in grams. Heavier BBs are slower but fly straighter and resist wind.")]
        public float bbWeightGrams = 0.25f;
        [Tooltip("1 = BB flies roughly level, higher = more backspin lift.")]
        public float hopUp = 1.15f;
        [Tooltip("Random spread in degrees (standing still, not crouched).")]
        public float spreadDegrees = 1.5f;
        public int pelletsPerShot = 1;

        [Header("Handling")]
        [Tooltip("Rounds per minute.")]
        public float rpm = 900f;
        [Tooltip("BBs per magazine.")]
        public int magCapacity = 120;
        [Tooltip("Spare magazines carried into a round.")]
        public int magsCarried = 4;
        public float reloadTime = 2.2f;
        public float lengthCm = 90f;
        [Tooltip("Multiplier on walking speed. Heavy guns slow you down.")]
        public float moveSpeedMultiplier = 1f;

        [Header("Melee only")]
        public float meleeRange = 1.3f;

        public bool IsMelee { get { return weaponClass == WeaponClass.Melee; } }

        public string ClassCode { get { return ((int)weaponClass).ToString("00"); } }

        /// <summary>Muzzle velocity in m/s for the BB weight this weapon uses (same energy as the 0.20 g rating).</summary>
        public float MuzzleVelocity { get { return fps * 0.3048f * Mathf.Sqrt(0.20f / Mathf.Max(0.12f, bbWeightGrams)); } }

        /// <summary>Energy in joules, the number real fields use for their limits.</summary>
        public float Joules { get { float v = fps * 0.3048f; return 0.5f * 0.0002f * v * v; } }

        public bool Allows(FireMode mode) { return (fireModes & ToFlag(mode)) != 0; }

        public FireMode DefaultMode
        {
            get
            {
                if (Allows(FireMode.Auto)) return FireMode.Auto;
                if (Allows(FireMode.Burst)) return FireMode.Burst;
                if (Allows(FireMode.Semi)) return FireMode.Semi;
                return FireMode.Single;
            }
        }

        public static FireModes ToFlag(FireMode mode)
        {
            switch (mode)
            {
                case FireMode.Single: return FireModes.Single;
                case FireMode.Semi: return FireModes.Semi;
                case FireMode.Burst: return FireModes.Burst;
                default: return FireModes.Auto;
            }
        }

        public string PowerLabel { get { return power == PowerSystem.CO2 ? "CO2" : power.ToString(); } }

        public string StatLine
        {
            get
            {
                if (IsMelee) return "Melee · tap to eliminate · range " + meleeRange.ToString("0.0") + " m";
                return string.Format("{0} · {1} FPS · {2} RPM · {3} BBs x{4} · {5:0.00} g", PowerLabel, fps, rpm, magCapacity, magsCarried + 1, bbWeightGrams);
            }
        }
    }
}
