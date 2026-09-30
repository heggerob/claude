using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Brings a hero who stands in one place to life (Bjorn at his barrel, Gunnar at his scales, raiders at their
    /// oars): breathing, shifting weight, glancing about, turning in steps when made to face somewhere new, and
    /// playing an attack (a raider's bow shot) when told to. Uses the same locomotion and animator as the player,
    /// so it moves just as smoothly.
    /// </summary>
    public class HeroIdle : MonoBehaviour
    {
        VikingBuilder.Parts parts;
        WeaponId weapon;
        readonly Locomotion loco = new Locomotion();
        readonly HeroAnimator animator = new HeroAnimator();
        readonly Damped look = new Damped();
        float wantYaw, nextGlance, glance, moveStart = -10f;
        bool started;

        public static HeroIdle Add(Transform root, VikingBuilder.Parts parts, WeaponId weapon)
        {
            var idle = root.gameObject.AddComponent<HeroIdle>();
            idle.parts = parts;
            idle.weapon = weapon;
            return idle;
        }

        /// <summary>Turn to face this way (world yaw, degrees): taken in steps, not snapped.</summary>
        public void FaceTowards(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude > 0.01f) wantYaw = Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;
        }

        /// <summary>Play the weapon's attack once (a bow is drawn and loosed).</summary>
        public void Attack() { moveStart = Time.time; }

        public bool Attacking { get { return Time.time - moveStart < HeroAttacks.For(weapon).duration; } }

        void Update()
        {
            if (parts == null) return;
            float dt = Time.deltaTime;
            float parentYaw = transform.parent != null ? transform.parent.eulerAngles.y : 0f;
            if (!started) { started = true; wantYaw = transform.eulerAngles.y; loco.Reset(Vector2.zero, transform.eulerAngles.y - parentYaw); nextGlance = Time.time + Random.Range(2f, 5f); }
            // Turning on the spot, in steps, towards where we've been told to face (in the parent's frame, so a
            // rolling ship carries us round).
            float local = Mathf.DeltaAngle(0f, wantYaw - parentYaw);
            var wish = Mathf.Abs(Mathf.DeltaAngle(loco.heading, local)) > 6f ? new Vector2(Mathf.Sin(local * Mathf.Deg2Rad), Mathf.Cos(local * Mathf.Deg2Rad)) * 0.3f : Vector2.zero;
            loco.Step(wish, 0f, dt);
            loco.position = Vector2.zero;
            transform.localRotation = Quaternion.Euler(0f, loco.heading, 0f);
            animator.Step(dt, loco, true, 0f, false, Time.time + transform.position.x);
            animator.Apply(parts);
            // Now and then a glance to one side.
            if (Time.time >= nextGlance) { glance = Random.Range(-35f, 35f); nextGlance = Time.time + Random.Range(2.5f, 6f); if (Random.value < 0.4f) glance = 0f; }
            if (parts.head != null) parts.head.localRotation *= Quaternion.Euler(0f, look.Step(glance, dt, 0.35f), 0f);
            // Weapon carried as when walking, or its attack.
            var carry = HeroPose.CarryFor(weapon);
            if (carry.set) HeroPose.Carry(parts, weapon, 1f);
            if (Attacking) HeroAttacks.Apply(parts, HeroAttacks.For(weapon), (Time.time - moveStart) / HeroAttacks.For(weapon).duration);
        }
    }
}
