using UnityEngine;

namespace OdinsCoin
{
    /// <summary>Pure combat rules: damage after blessings, curses and shields; who is inside a swing.</summary>
    public static class CombatMath
    {
        public const float ShieldArc = 130f;      // degrees in front that a raised shield covers
        public const float ShieldReduction = 0.85f;

        /// <summary>Damage dealt after the attacker's blessings and the defender's shield.</summary>
        public static float Damage(float baseDamage, float attackerMultiplier, bool defenderBlocking, bool hitFromFront)
        {
            float d = baseDamage * attackerMultiplier;
            if (defenderBlocking && hitFromFront) d *= 1f - ShieldReduction;
            return d;
        }

        /// <summary>Is the target inside a swing: within range and inside the arc in front of the attacker?</summary>
        public static bool InArc(Vector3 attacker, Vector3 forward, Vector3 target, float range, float arcDegrees)
        {
            Vector3 to = target - attacker;
            to.y = 0f;
            forward.y = 0f;
            if (to.magnitude > range) return false;
            if (to.sqrMagnitude < 0.0001f) return true;
            return Vector3.Angle(forward, to) <= arcDegrees / 2f;
        }

        /// <summary>Does an attack from <paramref name="attacker"/> land on the front of the defender (so a shield counts)?</summary>
        public static bool FromFront(Vector3 defender, Vector3 defenderForward, Vector3 attacker)
        {
            return InArc(defender, defenderForward, attacker, float.MaxValue, ShieldArc);
        }
    }

    /// <summary>Hit points for anyone who can fight. Max health follows Hel's Chill for the player.</summary>
    public class Health : MonoBehaviour
    {
        public float BaseMax = 100f;
        public bool IsPlayer;
        public float Current { get; private set; }
        public bool Dead { get { return Current <= 0f; } }
        public float LastHitTime { get; private set; }
        public event System.Action<float, Vector3> Damaged; // amount, from
        public event System.Action Died;

        public float Max { get { return IsPlayer ? BaseMax * Fortune.Current.HealthMultiplier : BaseMax; } }

        void Awake() { Current = BaseMax; }

        public void Heal(float amount) { if (!Dead) Current = Mathf.Min(Max, Current + amount); }

        public void Restore() { Current = Max; }

        public void TakeDamage(float amount, Vector3 from)
        {
            if (Dead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            LastHitTime = Time.time;
            if (Damaged != null) Damaged(amount, from);
            if (Current <= 0f && Died != null) Died();
        }

        void Update()
        {
            // Hel's Chill can lower the max while you're hurt; slow regeneration out of combat.
            if (Current > Max) Current = Max;
            if (!Dead && Time.time - LastHitTime > 6f) Current = Mathf.Min(Max, Current + Time.deltaTime * 4f);
        }
    }
}
