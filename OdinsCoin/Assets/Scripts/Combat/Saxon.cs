using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A Saxon guard defending a monastery. Patrols near home, charges the Viking when it sees him,
    /// telegraphs each blow (sword raised) so you can raise your shield, and gives up if led too far away.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Saxon : MonoBehaviour
    {
        public static readonly List<Saxon> All = new List<Saxon>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        const float Speed = 3.4f, SightRange = 16f, LeashRange = 28f, AttackRange = 1.9f;
        const float Windup = 0.6f, Recover = 0.7f, BaseDamage = 14f;

        public Health Health { get; private set; }
        public bool Blocking { get; private set; }

        CharacterController controller;
        VikingBuilder.Parts parts;
        Vector3 home;
        Vector3 wanderTarget;
        float nextWander, attackStart = -10f, staggerUntil, verticalSpeed, walkCycle, deathTime;
        bool attackLanded;

        public static Saxon Create(Transform parent, Vector3 position)
        {
            var go = new GameObject("Saxon Guard");
            go.transform.SetParent(parent, true);
            go.transform.position = position + Vector3.up * 0.2f;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.95f, 0f);
            var s = go.AddComponent<Saxon>();
            s.controller = cc;
            s.home = position;
            s.wanderTarget = position;
            s.parts = VikingBuilder.Build(go.transform, new Color(0.35f, 0.42f, 0.25f), new Color(0.4f, 0.3f, 0.2f), new Color(0.2f, 0.3f, 0.6f), true);
            s.Health = go.AddComponent<Health>();
            s.Health.BaseMax = 60f;
            s.Health.Damaged += (amount, from) => CombatHud.Number(go.transform.position + Vector3.up * 2.2f, Mathf.RoundToInt(amount).ToString(), new Color(1f, 0.9f, 0.4f));
            s.Health.Died += s.OnDied;
            All.Add(s);
            return s;
        }

        void OnDestroy() { All.Remove(this); }

        public void Stagger(Vector3 from)
        {
            // A solid hit interrupts their swing.
            staggerUntil = Time.time + 0.35f;
            attackStart = -10f;
        }

        void OnDied()
        {
            deathTime = Time.time;
            controller.enabled = false;
            int gold = Mathf.RoundToInt(12 * Fortune.Current.LootMultiplier);
            Fortune.Current.Gold += gold;
            Fortune.Current.AddFavour(Ravens.FavourPerKill);
            Sfx.At(SfxId.Gold, transform.position + Vector3.up, 0.7f);
            CombatHud.Number(transform.position + Vector3.up * 1.2f, "+" + gold + " gold", Materials.Gold);
        }

        void Update()
        {
            if (Health.Dead)
            {
                // Topple over, then vanish.
                float t = Mathf.Clamp01((Time.time - deathTime) / 0.5f);
                parts.body.localRotation = Quaternion.Euler(-85f * t, 0f, 0f);
                if (Time.time - deathTime > 8f) Destroy(gameObject);
                return;
            }

            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.Player : null;
            var combat = player != null ? player.GetComponent<VikingCombat>() : null;
            Vector3 move = Vector3.zero;
            float dt = Time.deltaTime;

            bool playerAlive = combat != null && !combat.Health.Dead;
            float toPlayer = playerAlive ? Vector3.Distance(transform.position, player.transform.position) : float.MaxValue;
            bool engaged = playerAlive && toPlayer < SightRange && Vector3.Distance(player.transform.position, home) < LeashRange && CanSee(player.transform.position);

            if (Time.time < staggerUntil) { /* reeling from a hit */ }
            else if (attackStart > 0f)
            {
                float t = Time.time - attackStart;
                if (!attackLanded && t >= Windup)
                {
                    attackLanded = true;
                    if (playerAlive && CombatMath.InArc(transform.position, transform.forward, player.transform.position, AttackRange + 0.4f, 100f))
                    {
                        bool front = CombatMath.FromFront(player.transform.position, player.transform.forward, transform.position);
                        combat.Health.TakeDamage(CombatMath.Damage(BaseDamage, 1f, combat.Blocking, front), transform.position);
                        if (combat.Blocking && front) CombatHud.Number(player.transform.position + Vector3.up * 2.4f, "BLOCKED", Color.white);
                        if (combat.Blocking && front) Sfx.At(SfxId.ShieldBlock, player.transform.position + Vector3.up);
                    }
                }
                if (t >= Windup + Recover) attackStart = -10f;
            }
            else if (engaged)
            {
                Face(player.transform.position, dt);
                if (toPlayer <= AttackRange) { attackStart = Time.time; attackLanded = false; }
                else move = (player.transform.position - transform.position).normalized * Speed;
            }
            else
            {
                // Wander around home.
                if (Time.time >= nextWander || Vector3.Distance(transform.position, wanderTarget) < 1f)
                {
                    wanderTarget = home + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(0f, 6f);
                    nextWander = Time.time + Random.Range(3f, 7f);
                }
                Vector3 to = wanderTarget - transform.position;
                to.y = 0f;
                if (to.magnitude > 1f) { Face(wanderTarget, dt); move = to.normalized * Speed * 0.4f; }
            }

            move.y = 0f;
            verticalSpeed = controller.isGrounded ? -2f : verticalSpeed - 18f * dt;
            controller.Move((move + Vector3.up * verticalSpeed) * dt);
            Animate(move.magnitude, dt);
        }

        bool CanSee(Vector3 target)
        {
            Vector3 eye = transform.position + Vector3.up * 1.6f;
            Vector3 to = target + Vector3.up * 1.2f - eye;
            RaycastHit hit;
            if (!Physics.Raycast(eye, to.normalized, out hit, to.magnitude)) return true;
            return hit.collider != null && hit.collider.GetComponent<Viking>() != null;
        }

        void Face(Vector3 target, float dt)
        {
            Vector3 to = target - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 8f);
        }

        void Animate(float speed, float dt)
        {
            walkCycle += dt * (speed > 0.1f ? 2f + speed * 1.4f : 0f);
            float swing = speed > 0.1f ? Mathf.Sin(walkCycle) * Mathf.Clamp(speed * 8f, 0f, 35f) : 0f;
            parts.leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            parts.rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            // Sword raised high during the windup (the tell), then brought down.
            float armAngle = swing * 0.8f;
            if (attackStart > 0f)
            {
                float t = Time.time - attackStart;
                armAngle = t < Windup ? Mathf.Lerp(0f, -160f, t / Windup) : Mathf.Lerp(-160f, 40f, Mathf.Clamp01((t - Windup) / 0.15f));
            }
            parts.rightArm.localRotation = Quaternion.Euler(armAngle, 0f, 0f);
            // They keep their shield up while waiting to strike.
            Blocking = attackStart < 0f && speed < 0.5f;
            parts.shield.localPosition = Blocking ? new Vector3(-0.3f, 1.3f, 0.5f) : new Vector3(0f, 1.25f, -0.24f);
        }
    }
}
