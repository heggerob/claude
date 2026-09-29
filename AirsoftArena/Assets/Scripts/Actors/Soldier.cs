using UnityEngine;

namespace AirsoftArena
{
    public enum SoldierState
    {
        /// <summary>Playing normally.</summary>
        Alive,
        /// <summary>Just got hit and has a few seconds to call it. Can still move and shoot... if they dare.</summary>
        Hit,
        /// <summary>Called out (or caught by the referee). Walking back to spawn with the dead rag up.</summary>
        Out,
        /// <summary>Standing in their spawn zone waiting to re-enter.</summary>
        Respawning,
    }

    public enum OutReason { CalledHit, CalledLate, CaughtByReferee, WrongReferee }

    public class SoldierStats
    {
        public int hitsLanded;      // BBs that hit an opponent who was in play
        public int pointsScored;    // hits that ended up counting (called, or caught by the ref)
        public int timesHit;
        public int hitsCalled;      // called within the time window
        public int hitsNotCalled;   // let the window run out
        public int caughtByReferee;
        public int wronglyCalledOut;
        public int overshoots;      // shot someone who already had their rag up
        public int shotTheReferee;
        public int captures;        // Capture the Flag
        public int flagReturns;
        public float hillSeconds;   // King of the Hill
    }

    /// <summary>
    /// One player on the field, human or bot. Controllers (PlayerController, BotController) only feed it intentions;
    /// all rules live here so a future networked or 3D version can reuse them.
    /// </summary>
    public class Soldier : MonoBehaviour
    {
        public const float Radius = 0.35f;
        public const float HitCallWindow = 2.5f;
        public const float RespawnDelay = 3f;
        const float WalkSpeed = 4f;
        const float SprintMultiplier = 1.55f;
        const float CrouchMultiplier = 0.45f;
        const float OutWalkSpeed = 3.6f;

        public string DisplayName { get; private set; }
        public Team Team { get; private set; }
        public bool IsHuman { get; private set; }
        /// <summary>0..1 chance a bot calls its own hits. Humans decide for themselves.</summary>
        public float Honesty = 1f;

        public SoldierState State { get; private set; }
        /// <summary>True while playing on after a hit that was never called ("zombie").</summary>
        public bool HasUncalledHit { get; private set; }
        public float HitWindowLeft { get; private set; }
        public float RespawnLeft { get; private set; }
        public Soldier LastHitBy { get; private set; }
        public float LastHitTime { get; private set; }
        public float OutSince { get; private set; }
        /// <summary>Whether the referee had eyes on the most recent hit.</summary>
        public bool LastHitSeenByReferee { get; set; }

        public Vector2 AimDirection { get; private set; }
        public bool Crouching { get; private set; }
        public bool Sprinting { get; private set; }
        public WeaponInstance[] Loadout { get; private set; }
        public int Slot { get; private set; }
        public WeaponInstance Weapon { get { return Loadout[Slot]; } }
        public readonly SoldierStats Stats = new SoldierStats();

        public Vector2 Position { get { return transform.position; } }
        public bool InPlay { get { return State == SoldierState.Alive || State == SoldierState.Hit; } }
        public bool CanShoot { get { return InPlay && !(Sprinting && moveInput.sqrMagnitude > 0.01f); } }
        public float Height { get { return Crouching ? 1.1f : 1.75f; } }
        public float MuzzleHeight { get { return Crouching ? 1.0f : 1.4f; } }
        public Collider2D Collider { get; private set; }

        Rigidbody2D body;
        SoldierView view;
        Vector2 moveInput;

        public SoldierLook Look { get { return view != null ? view.Look : null; } }

        public static Soldier Create(Transform parent, string name, Team team, bool human, WeaponData[] loadout, SoldierLook look, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = Radius;

            var soldier = go.AddComponent<Soldier>();
            soldier.Init(name, team, human, loadout, rb, col);
            soldier.view = go.AddComponent<SoldierView>();
            soldier.view.Build(soldier, look);
            return soldier;
        }

        void Init(string name, Team team, bool human, WeaponData[] loadout, Rigidbody2D rb, Collider2D col)
        {
            DisplayName = name;
            Team = team;
            IsHuman = human;
            body = rb;
            Collider = col;
            Loadout = new WeaponInstance[loadout.Length];
            for (int i = 0; i < loadout.Length; i++) Loadout[i] = new WeaponInstance(loadout[i]);
            AimDirection = team == Team.Blue ? Vector2.right : Vector2.left;
        }

        // ---------------------------------------------------------------- intentions from controllers

        public void SetMove(Vector2 direction, bool sprint)
        {
            moveInput = Vector2.ClampMagnitude(direction, 1f);
            Sprinting = sprint && !Crouching;
        }

        public void SetCrouch(bool crouch) { Crouching = crouch && InPlay; }

        public void SetAim(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f) AimDirection = direction.normalized;
        }

        public void PullTrigger(bool held, bool pressed)
        {
            if (!CanShoot)
            {
                Weapon.PullTrigger(false, false, Time.time);
                return;
            }

            var weapon = Weapon;
            int bbs = weapon.PullTrigger(held, pressed, Time.time);
            if (bbs == 0)
            {
                if (pressed && weapon.IsEmpty) weapon.StartReload(Time.time);
                return;
            }

            if (weapon.Data.IsMelee)
            {
                Stab();
                return;
            }

            Vector2 muzzle = view != null ? view.MuzzlePosition : Position + AimDirection * (Radius + 0.2f);
            if (view != null) view.OnFired();
            float spread = Crouching ? 0.6f : 1f;
            if (moveInput.sqrMagnitude > 0.01f) spread *= 1.6f;
            BBSystem.Instance.Fire(this, muzzle, AimDirection, MuzzleHeight, weapon.Data, bbs, spread);
        }

        public void Reload() { if (InPlay) Weapon.StartReload(Time.time); }

        public void CycleFireMode() { Weapon.CycleMode(); }

        public void SwitchSlot(int slot)
        {
            if (slot < 0 || slot >= Loadout.Length || slot == Slot) return;
            Weapon.CancelReload();
            Slot = slot;
            if (view != null) view.RefreshGun();
        }

        /// <summary>The honest thing to do. Raises the dead rag and sends you back to spawn.</summary>
        public void CallHit()
        {
            if (State == SoldierState.Hit) GoOut(OutReason.CalledHit);
            else if (State == SoldierState.Alive && HasUncalledHit) GoOut(OutReason.CalledLate);
        }

        // ---------------------------------------------------------------- rules

        public void ReceiveHit(Soldier shooter, Vector2 point)
        {
            var match = MatchManager.Instance;
            if (match == null || !match.IsPlaying) return;
            if (State == SoldierState.Out || State == SoldierState.Respawning)
            {
                if (shooter != null) match.OnOvershoot(shooter, this);
                return;
            }
            if (State == SoldierState.Hit) return; // already hit, window still running

            Stats.timesHit++;
            if (shooter != null && shooter.Team != Team) shooter.Stats.hitsLanded++;
            LastHitBy = shooter;
            LastHitTime = Time.time;
            State = SoldierState.Hit;
            HitWindowLeft = HitCallWindow;
            Effects.Text(point + new Vector2(0f, 0.4f), "*tak*", new Color(1f, 1f, 1f, 0.85f), 0.6f, 11);
            match.OnSoldierHit(this, shooter, point);
        }

        /// <summary>Called by the match when this soldier leaves play.</summary>
        public void GoOut(OutReason reason)
        {
            if (!InPlay) return;
            var previous = State;
            State = SoldierState.Out;
            OutSince = Time.time;
            Crouching = false;
            Weapon.CancelReload();

            switch (reason)
            {
                case OutReason.CalledHit: Stats.hitsCalled++; break;
                case OutReason.CaughtByReferee: Stats.caughtByReferee++; break;
                case OutReason.WrongReferee: Stats.wronglyCalledOut++; break;
            }

            Effects.Text(Position + new Vector2(0f, 0.9f), reason == OutReason.WrongReferee ? "WHAT?!" : "HIT!", new Color(1f, 0.6f, 0.2f), 1.4f, 16);
            var match = MatchManager.Instance;
            if (match != null) match.OnSoldierOut(this, reason, previous == SoldierState.Hit || HasUncalledHit);
            HasUncalledHit = false;
        }

        void Stab()
        {
            float range = Weapon.Data.meleeRange + Radius;
            var match = MatchManager.Instance;
            if (match == null) return;
            Effects.Flash(Position + AimDirection * 0.8f, new Color(1f, 1f, 1f, 0.6f), 0.35f, 0.1f);
            foreach (var other in match.Soldiers)
            {
                if (other == this || other.Team == Team || !other.InPlay) continue;
                Vector2 to = other.Position - Position;
                if (to.magnitude > range || Vector2.Dot(to.normalized, AimDirection) < 0.5f) continue;
                if (!MatchManager.HasLineOfSight(Position, other.Position, true)) continue;
                other.ReceiveHit(this, other.Position);
                break;
            }
        }

        public void Respawn()
        {
            State = SoldierState.Alive;
            HasUncalledHit = false;
            foreach (var w in Loadout) w.Refill();
        }

        public void Freeze()
        {
            moveInput = Vector2.zero;
            Sprinting = false;
            Compat.SetVelocity(body, Vector2.zero);
        }

        void Update()
        {
            var match = MatchManager.Instance;
            bool playing = match != null && match.IsPlaying;

            if (playing)
            {
                switch (State)
                {
                    case SoldierState.Hit:
                        HitWindowLeft -= Time.deltaTime;
                        if (HitWindowLeft <= 0f)
                        {
                            State = SoldierState.Alive;
                            if (!HasUncalledHit)
                            {
                                HasUncalledHit = true;
                                Stats.hitsNotCalled++;
                                match.OnHitNotCalled(this);
                            }
                        }
                        break;
                    case SoldierState.Out:
                        if (match.InSpawnZone(Team, Position))
                        {
                            State = SoldierState.Respawning;
                            RespawnLeft = RespawnDelay;
                        }
                        break;
                    case SoldierState.Respawning:
                        if (!match.InSpawnZone(Team, Position)) { State = SoldierState.Out; break; }
                        RespawnLeft -= Time.deltaTime;
                        if (RespawnLeft <= 0f) Respawn();
                        break;
                }
                foreach (var w in Loadout) w.Tick(Time.time);
            }
        }

        void FixedUpdate()
        {
            var match = MatchManager.Instance;
            if (match == null || !match.IsPlaying)
            {
                Compat.SetVelocity(body, Vector2.zero);
                return;
            }

            float speed;
            if (!InPlay) speed = OutWalkSpeed;
            else
            {
                speed = WalkSpeed * Weapon.Data.moveSpeedMultiplier;
                if (Crouching) speed *= CrouchMultiplier;
                else if (Sprinting) speed *= SprintMultiplier;
            }
            Compat.SetVelocity(body, moveInput * speed);
        }
    }
}
