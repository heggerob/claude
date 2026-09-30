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
        float nextWander, attackStart = -10f, staggerUntil, verticalSpeed, deathTime;
        bool attackLanded;
        // Guards move like the player's hero: turning takes steps, speed builds up, every joint eases.
        readonly Locomotion loco = new Locomotion { sprintSpeed = Speed };
        readonly HeroAnimator animator = new HeroAnimator();
        static AttackMove Slash { get { return HeroAttacks.For(WeaponId.Sword); } }

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
            s.loco.Reset(Vector2.zero, Random.Range(0f, 360f));
            s.parts = HeroBuilder.Build(go.transform, NpcHeroes.Saxon(All.Count + Mathf.RoundToInt(position.x * 7f + position.z * 13f)));
            s.Health = go.AddComponent<Health>();
            s.Health.BaseMax = 60f;
            s.Health.Damaged += (amount, from) => OdinsCoin.Face.On(s, Expression.Hurt, 0.7f);
            s.Health.Damaged += (amount, from) => CombatHud.Number(go.transform.position + Vector3.up * 2.2f, Mathf.RoundToInt(amount).ToString(), new Color(1f, 0.9f, 0.4f));
            s.Health.Died += s.OnDied;
            All.Add(s);
            return s;
        }

        void OnDestroy() { All.Remove(this); }
        void OnEnable() { WorldOrigin.Shifted += OnShift; }
        void OnDisable() { WorldOrigin.Shifted -= OnShift; }
        /// <summary>The floating origin moved the world back under us: so did the spot we guard.</summary>
        void OnShift(Vector3 shift) { home -= shift; wanderTarget -= shift; }

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
            // Where they want to go (on the ground), how hard, and at what top speed.
            Vector2 wish = Vector2.zero;
            float pace = Speed;

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
                        combat.Health.TakeDamage(Abilities.Taken(BaseDamage, combat.Blocking, front), transform.position);
                        if (combat.Blocking && front) CombatHud.Number(player.transform.position + Vector3.up * 2.4f, "BLOCKED", Color.white);
                        if (combat.Blocking && front) Sfx.At(SfxId.ShieldBlock, player.transform.position + Vector3.up);
                    }
                }
                if (t >= Windup + Recover) attackStart = -10f;
            }
            else if (engaged)
            {
                Vector3 to = player.transform.position - transform.position;
                to.y = 0f;
                // Close enough: turn to face them (stepping round on the spot), and swing once facing them.
                if (toPlayer <= AttackRange)
                {
                    wish = new Vector2(to.x, to.z).normalized * 0.2f;
                    pace = 0f;
                    if (Vector3.Angle(transform.forward, to) < 30f) { attackStart = Time.time; attackLanded = false; }
                }
                else wish = new Vector2(to.x, to.z).normalized;
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
                if (to.magnitude > 1f) { wish = new Vector2(to.x, to.z).normalized; pace = Speed * 0.4f; }
            }

            // Walking and turning in steps, like the hero.
            if (attackStart > 0f || Time.time < staggerUntil) wish = Vector2.zero;
            loco.Step(wish, pace, dt);
            transform.rotation = Quaternion.Euler(0f, loco.heading, 0f);
            move = loco.Velocity;
            verticalSpeed = controller.isGrounded ? -2f : verticalSpeed - 18f * dt;
            controller.Move((move + Vector3.up * verticalSpeed) * dt);
            Animate(dt);
        }

        bool CanSee(Vector3 target)
        {
            Vector3 eye = transform.position + Vector3.up * 1.6f;
            Vector3 to = target + Vector3.up * 1.2f - eye;
            RaycastHit hit;
            if (!Physics.Raycast(eye, to.normalized, out hit, to.magnitude)) return true;
            return hit.collider != null && hit.collider.GetComponent<Viking>() != null;
        }

        /// <summary>
        /// Where in the slash they are, for <paramref name="elapsed"/> seconds since it began: the move's wind-up is
        /// stretched over the guard's long, readable tell, the blow lands when the damage does, and the recovery
        /// takes the rest.
        /// </summary>
        public static float SlashPhase(float elapsed)
        {
            var m = Slash;
            if (elapsed < Windup) return elapsed / Windup * m.hitAt;
            return Mathf.Min(1f, m.hitAt + (elapsed - Windup) / Recover * (1f - m.hitAt));
        }

        void Animate(float dt)
        {
            animator.Step(dt, loco, controller.isGrounded, verticalSpeed, false, Time.time + home.x);
            animator.Apply(parts);
            var carry = HeroPose.CarryFor(WeaponId.Sword);
            if (carry.set) HeroPose.Carry(parts, WeaponId.Sword, 1f);
            // The slash: the sword raised high during the wind-up (the tell), then cut down.
            if (attackStart > 0f) HeroAttacks.Apply(parts, Slash, SlashPhase(Time.time - attackStart));
            // They keep their shield up while waiting to strike.
            Blocking = attackStart < 0f && loco.speed < 0.5f;
            HeroPose.Block(parts, Blocking ? 1f : 0f);
        }
    }
}
