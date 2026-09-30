using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The player's axe and shield. Left mouse swings (Thor's Wrath hits harder, Loki's Trick makes you miss),
    /// right mouse raises the shield (blocks most damage from the front). Dying sends you back to your ship.
    /// </summary>
    [RequireComponent(typeof(Viking))]
    public class VikingCombat : MonoBehaviour
    {
        public const float SwingTime = 0.42f, SwingDamage = 26f, Reach = 2.1f, Arc = 110f;
        const float RespawnDelay = 3.5f;

        public Health Health { get; private set; }
        public bool Blocking { get; private set; }
        public float DeadUntil { get; private set; }

        Viking viking;
        float swingStart = -10f;
        bool swingHit;
        float block;

        void Awake()
        {
            viking = GetComponent<Viking>();
            Health = gameObject.AddComponent<Health>();
            Health.IsPlayer = true;
            Health.BaseMax = 100f;
            Health.Damaged += (amount, from) => Sfx.At(SfxId.Hurt, transform.position + Vector3.up, 0.8f, 0.1f);
            Health.Damaged += (amount, from) => Face.On(this, Expression.Hurt, 0.7f);
            Health.Damaged += (amount, from) => CombatHud.Number(transform.position + Vector3.up * 2.2f, "-" + Mathf.RoundToInt(amount), new Color(1f, 0.35f, 0.3f));
            Health.Died += OnDied;
        }

        public bool Busy { get { return Health.Dead; } }

        /// <summary>The attack for the weapon in hand (an axe chop, a sword slash, a spear thrust...).</summary>
        public AttackMove Move { get { return HeroAttacks.For(viking != null && viking.Hero != null ? viking.Hero.weapon : WeaponId.TwoHandAxe); } }

        void Update()
        {
            if (Health.Dead)
            {
                if (Time.time >= DeadUntil) Respawn();
                return;
            }
            bool free = !viking.AtHelm && !viking.Swimming && viking.Carrying == null && !MeadHallUI.IsOpenNow && !GameMenu.Blocking && (CoinUI.Instance == null || !CoinUI.Instance.IsOpen);
            Blocking = free && GameInput.BlockHeld();
            var move = Move;
            if (free && !Blocking && GameInput.AttackPressed() && Time.time - swingStart > move.duration) { swingStart = Time.time; swingHit = false; Sfx.At(SfxId.AxeSwing, transform.position + Vector3.up, 0.6f, 0.12f); }

            // The blow lands when the weapon comes down (or the arrow flies) in the move.
            float t = (Time.time - swingStart) / move.duration;
            if (!swingHit && t >= move.hitAt && t < 1f)
            {
                swingHit = true;
                Strike();
            }
        }

        void Strike()
        {
            var fortune = Fortune.Current;
            if (Random.value < fortune.MissChance)
            {
                CombatHud.Number(transform.position + transform.forward * 1.2f + Vector3.up * 1.8f, "MISS (Loki!)", new Color(0.6f, 1f, 0.6f));
                return;
            }
            foreach (var saxon in Saxon.All)
            {
                if (saxon.Health.Dead) continue;
                if (!CombatMath.InArc(transform.position, transform.forward, saxon.transform.position, Reach, Arc)) continue;
                bool front = CombatMath.FromFront(saxon.transform.position, saxon.transform.forward, transform.position);
                float dmg = CombatMath.Damage(SwingDamage, fortune.MeleeDamageMultiplier * Upgrades.Current.AxeMultiplier, saxon.Blocking, front);
                saxon.Health.TakeDamage(dmg, transform.position);
                Sfx.At(saxon.Blocking && front ? SfxId.ShieldBlock : SfxId.AxeHit, saxon.transform.position + Vector3.up);
                saxon.Stagger(transform.position);
            }
            float blow = SwingDamage * fortune.MeleeDamageMultiplier * Upgrades.Current.AxeMultiplier;
            // Jörmungandr's head, while it lies stunned on the gunwale.
            var serpent = Serpent.Instance;
            if (serpent != null && serpent.HeadInReach(transform.position, transform.forward, Reach, Arc) && serpent.TakeHit(blow))
                Sfx.At(SfxId.AxeHit, serpent.Head.position, 1f);
            // Hacking at a raider's strakes from alongside (or aboard).
            foreach (var raider in Raider.All.ToArray())
                if (!raider.Sinking && raider.WithinReach(transform.position + transform.forward * 1.2f, 0.9f))
                {
                    float chop = blow * 0.35f;
                    raider.TakeDamage(chop, transform.position);
                    Sfx.At(SfxId.ArrowThunk, transform.position + transform.forward, 1f, 0.2f);
                    CombatHud.Number(transform.position + transform.forward * 1.5f + Vector3.up * 1.2f, "-" + Mathf.RoundToInt(chop) + " hull", new Color(0.9f, 0.7f, 0.4f));
                }
        }

        void LateUpdate()
        {
            var parts = viking.Parts;
            if (parts == null) return;
            // The attack: wind-up, blow, follow-through, eased in and out of whatever the hero was doing.
            var move = Move;
            float t = (Time.time - swingStart) / move.duration;
            if (t >= 0f && t < 1f) HeroAttacks.Apply(parts, move, t);
            // Shield: slides from the back to the front arm while blocking.
            block = Mathf.MoveTowards(block, Blocking ? 1f : 0f, Time.deltaTime * 6f);
            HeroPose.Block(parts, block);
            if (Health.Dead) parts.body.localRotation = Quaternion.Euler(-80f, 0f, 0f);
        }

        void OnDied()
        {
            DeadUntil = Time.time + RespawnDelay;
            viking.DropChest();
            CombatHud.Banner("YOU FELL", "Valhalla can wait. Back to the ship...");
        }

        void Respawn()
        {
            var fortune = Fortune.Current;
            int lost = fortune.Gold / 10;
            fortune.Gold -= lost;
            Health.Restore();
            viking.ReturnToShip();
            if (lost > 0) CombatHud.Banner("BACK ABOARD", "The crew fished you out. It cost " + lost + " gold.");
        }
    }
}
