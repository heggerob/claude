using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// Built-in weapon list with fictional names. Codes are "class-model", e.g. 01-VK4 = class 01 (assault rifle), model VK4.
    /// </summary>
    public static class WeaponCatalog
    {
        static List<WeaponData> primaries, secondaries, melee;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { primaries = secondaries = melee = null; }

        public static List<WeaponData> Primaries { get { Build(); return primaries; } }
        public static List<WeaponData> Secondaries { get { Build(); return secondaries; } }
        public static List<WeaponData> MeleeWeapons { get { Build(); return melee; } }

        static void Build()
        {
            if (primaries != null) return;

            primaries = new List<WeaponData>
            {
                Make("01-VK4", "Viking K4", WeaponClass.AssaultRifle, PowerSystem.AEG, FireModes.Semi | FireModes.Auto,
                    fps: 350, rpm: 900, mag: 120, mags: 3, reload: 2.2f, bb: 0.25f, hop: 1.15f, spread: 1.6f, length: 88, speed: 1f),
                Make("01-FR7", "Fjord R7", WeaponClass.AssaultRifle, PowerSystem.Gas, FireModes.Semi | FireModes.Auto,
                    fps: 370, rpm: 750, mag: 40, mags: 5, reload: 1.8f, bb: 0.25f, hop: 1.15f, spread: 1.3f, length: 92, speed: 0.98f),
                Make("02-WS9", "Wasp 9", WeaponClass.SubmachineGun, PowerSystem.AEG, FireModes.Semi | FireModes.Auto,
                    fps: 320, rpm: 1150, mag: 90, mags: 3, reload: 1.7f, bb: 0.20f, hop: 1.1f, spread: 2.4f, length: 62, speed: 1.08f),
                Make("03-LB2", "Longboat SR", WeaponClass.SniperMarksman, PowerSystem.Spring, FireModes.Single,
                    fps: 450, rpm: 45, mag: 28, mags: 2, reload: 2.6f, bb: 0.36f, hop: 1.15f, spread: 0.35f, length: 112, speed: 0.92f),
                Make("03-RD1", "Ranger DMR", WeaponClass.SniperMarksman, PowerSystem.Gas, FireModes.Semi,
                    fps: 420, rpm: 300, mag: 20, mags: 4, reload: 2.1f, bb: 0.30f, hop: 1.15f, spread: 0.6f, length: 104, speed: 0.95f),
                Make("05-TR3", "Trio Pump", WeaponClass.Shotgun, PowerSystem.Spring, FireModes.Single,
                    fps: 330, rpm: 70, mag: 30, mags: 3, reload: 2.4f, bb: 0.20f, hop: 1.0f, spread: 5.5f, length: 96, speed: 1f, pellets: 3),
                Make("06-TT3", "Triple Tap B3", WeaponClass.BurstRifle, PowerSystem.AEG, FireModes.Semi | FireModes.Burst,
                    fps: 355, rpm: 1000, mag: 90, mags: 3, reload: 2.0f, bb: 0.25f, hop: 1.15f, spread: 1.1f, length: 86, speed: 1f),
                Make("07-TH6", "Thunder L60", WeaponClass.LightMachineGun, PowerSystem.AEG, FireModes.Auto,
                    fps: 360, rpm: 1000, mag: 1500, mags: 0, reload: 5.0f, bb: 0.25f, hop: 1.15f, spread: 2.8f, length: 104, speed: 0.82f),
            };

            secondaries = new List<WeaponData>
            {
                Make("04-PP2", "Pocket P2", WeaponClass.Pistol, PowerSystem.Gas, FireModes.Semi,
                    fps: 300, rpm: 420, mag: 22, mags: 3, reload: 1.3f, bb: 0.20f, hop: 1.05f, spread: 1.8f, length: 20, speed: 1f),
                Make("04-FJ6", "Fjell Six", WeaponClass.Pistol, PowerSystem.CO2, FireModes.Semi,
                    fps: 340, rpm: 260, mag: 6, mags: 4, reload: 2.2f, bb: 0.25f, hop: 1.1f, spread: 1.0f, length: 28, speed: 1f),
                Make("04-BZ9", "Buzz Machine Pistol", WeaponClass.Pistol, PowerSystem.Gas, FireModes.Semi | FireModes.Auto,
                    fps: 290, rpm: 1100, mag: 30, mags: 2, reload: 1.5f, bb: 0.20f, hop: 1.05f, spread: 3.2f, length: 24, speed: 1f),
            };

            melee = new List<WeaponData>
            {
                Make("00-RT1", "Rubber Tanto", WeaponClass.Melee, PowerSystem.Spring, FireModes.Semi,
                    fps: 0, rpm: 90, mag: 0, mags: 0, reload: 0, bb: 0, hop: 0, spread: 0, length: 30, speed: 1.1f),
            };
        }

        static readonly Dictionary<string, int> Prices = new Dictionary<string, int>
        {
            { "01-VK4", 0 },
            { "01-FR7", 450 },
            { "02-WS9", 0 },
            { "03-LB2", 750 },
            { "03-RD1", 600 },
            { "05-TR3", 350 },
            { "06-TT3", 500 },
            { "07-TH6", 950 },
            { "04-PP2", 0 },
            { "04-FJ6", 250 },
            { "04-BZ9", 400 },
            { "00-RT1", 0 },
        };

        public static WeaponData Get(string code)
        {
            foreach (var list in new[] { Primaries, Secondaries, MeleeWeapons })
                foreach (var w in list)
                    if (w.code == code) return w;
            return null;
        }

        public static IEnumerable<WeaponData> All
        {
            get
            {
                foreach (var w in Primaries) yield return w;
                foreach (var w in Secondaries) yield return w;
                foreach (var w in MeleeWeapons) yield return w;
            }
        }

        static WeaponData Make(string code, string name, WeaponClass cls, PowerSystem power, FireModes modes,
            float fps, float rpm, int mag, int mags, float reload, float bb, float hop, float spread, float length, float speed, int pellets = 1)
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.name = code;
            w.code = code;
            w.displayName = name;
            w.weaponClass = cls;
            w.power = power;
            w.fireModes = modes;
            w.fps = fps;
            w.rpm = rpm;
            w.magCapacity = mag;
            w.magsCarried = mags;
            w.reloadTime = reload;
            w.bbWeightGrams = bb;
            w.hopUp = hop;
            w.spreadDegrees = spread;
            w.lengthCm = length;
            w.moveSpeedMultiplier = speed;
            w.pelletsPerShot = pellets;
            int price;
            w.price = Prices.TryGetValue(code, out price) ? price : 0;
            return w;
        }
    }
}
