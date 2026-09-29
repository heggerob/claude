using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Runtime state of one carried weapon: ammo, fire mode, cooldown and reload.</summary>
    public class WeaponInstance
    {
        const float ClickBuffer = 0.12f;

        public readonly WeaponData Data;
        public int AmmoInMag { get; private set; }
        public int SpareMags { get; private set; }
        public FireMode Mode { get; private set; }
        public bool IsReloading { get; private set; }

        float nextShotTime;
        float reloadStart, reloadEnd;
        float lastClickTime = -1f;
        int burstLeft;

        public WeaponInstance(WeaponData data)
        {
            Data = data;
            Mode = data.DefaultMode;
            Refill();
        }

        public bool IsEmpty { get { return !Data.IsMelee && AmmoInMag <= 0; } }
        public bool CanReload { get { return !Data.IsMelee && !IsReloading && SpareMags > 0 && AmmoInMag < Data.magCapacity; } }

        public float ReloadProgress(float now)
        {
            if (!IsReloading) return 0f;
            return Mathf.InverseLerp(reloadStart, reloadEnd, now);
        }

        public void Refill()
        {
            AmmoInMag = Data.magCapacity;
            SpareMags = Data.magsCarried;
            IsReloading = false;
            burstLeft = 0;
        }

        public void CycleMode()
        {
            for (int i = 1; i <= 4; i++)
            {
                var next = (FireMode)(((int)Mode + i) % 4);
                if (Data.Allows(next)) { Mode = next; break; }
            }
            burstLeft = 0;
        }

        public bool StartReload(float now)
        {
            if (!CanReload) return false;
            IsReloading = true;
            reloadStart = now;
            reloadEnd = now + Data.reloadTime;
            burstLeft = 0;
            return true;
        }

        public void CancelReload() { IsReloading = false; }

        public void Tick(float now)
        {
            if (IsReloading && now >= reloadEnd)
            {
                // Mag swap: whatever was left in the old mag goes back in your pouch, like on a real field.
                IsReloading = false;
                AmmoInMag = Data.magCapacity;
                SpareMags--;
            }
        }

        /// <summary>
        /// Feed the trigger state every frame. Returns how many BBs leave the barrel this frame
        /// (0 = nothing, melee weapons return 1 for a swing).
        /// </summary>
        public int PullTrigger(bool held, bool pressed, float now)
        {
            Tick(now);
            if (pressed) lastClickTime = now;
            if (IsReloading) return 0;

            bool wantsShot;
            switch (Mode)
            {
                case FireMode.Auto:
                    wantsShot = held;
                    break;
                case FireMode.Burst:
                    if (pressed && burstLeft == 0) burstLeft = Mathf.Max(1, Data.burstCount);
                    wantsShot = burstLeft > 0;
                    break;
                default:
                    // Semi/Single: remember a click briefly so fast clicking during cooldown is not swallowed.
                    wantsShot = lastClickTime >= 0f && now - lastClickTime <= ClickBuffer;
                    break;
            }

            if (!wantsShot || now < nextShotTime) return 0;

            nextShotTime = now + 60f / Mathf.Max(1f, Data.rpm);
            lastClickTime = -1f;

            if (Data.IsMelee) return 1;

            if (AmmoInMag <= 0)
            {
                burstLeft = 0;
                return 0;
            }

            int bbs = Mathf.Min(AmmoInMag, Mathf.Max(1, Data.pelletsPerShot));
            AmmoInMag -= bbs;
            if (Mode == FireMode.Burst) burstLeft--;
            return bbs;
        }
    }
}
