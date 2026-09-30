using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The player's Viking. Walks on the deck while the longship rolls and sails (the ship "carries" you:
    /// your position is kept in the ship's local space), jumps, falls overboard, swims, climbs back aboard
    /// and takes the steering oar. Carries treasure chests (slowly, both hands) and sells them to Gunnar at home.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Viking : MonoBehaviour
    {
        public const float WalkSpeed = 4.2f, RunSpeed = 6.5f, SwimSpeed = 2.4f, JumpSpeed = 5.5f;
        /// <summary>Holding Ctrl: a real, unhurried walk (the default pace is a brisk jog).</summary>
        public const float StrollSpeed = 1.7f;
        /// <summary>A chest full of gold is heavy: no running, no jumping, slow swimming.</summary>
        public const float CarrySpeed = 3f, CarrySwimSpeed = 1.5f;
        const float Gravity = 18f;
        const float InteractRange = 1.8f;
        const float ClimbRange = 3.2f;

        public Longship Ship;
        public bool AtHelm { get; private set; }
        public bool Swimming { get; private set; }
        public bool OnShip { get; private set; }
        public TreasureChest Carrying { get; private set; }
        /// <summary>What pressing E would do right now (for the HUD), or null.</summary>
        public string Prompt { get; private set; }

        CharacterController controller;
        VikingBuilder.Parts parts;
        VikingCombat combat;

        public VikingBuilder.Parts Parts { get { return parts; } }
        float verticalSpeed;
        /// <summary>In first person you move in any direction while facing where you look: this is that motion.</summary>
        Vector3 strafe;
        /// <summary>How quickly you get up to speed and stop in first person (m/s²), and how little you can steer in the air.</summary>
        public const float StrafeAccel = 24f, StrafeBrake = 30f, StrafeAirAccel = 3f;
        float bailAnim;
        float facing;
        /// <summary>Speed, turning and footsteps: turning takes steps, speed builds up and dies down.</summary>
        readonly Locomotion loco = new Locomotion { sprintSpeed = RunSpeed };
        readonly HeroAnimator animator = new HeroAnimator();
        /// <summary>How long the crouch before a jump lasts (s), and how much of it is left.</summary>
        public const float JumpCrouch = 0.1f;
        float jumpCrouch;
        bool locoStarted;

        /// <summary>The Viking's walking and turning, for animation.</summary>
        public Locomotion Motion { get { return loco; } }
        // Where we stand in the ship's own coordinates, and which way we face relative to the ship.
        Vector3 shipLocal;
        float shipLocalYaw;

        public static Viking Create(Transform parent, Longship ship)
        {
            var go = new GameObject("Viking");
            go.transform.SetParent(parent, false);
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;
            var v = go.AddComponent<Viking>();
            v.controller = cc;
            v.Ship = ship;
            v.Rebuild(HeroChoice.Load());
            // Start on deck, amidships.
            v.PlaceOnShip(new Vector3(0f, ship.DeckY + 0.05f, -1f));
            return v;
        }

        void Awake() { if (controller == null) controller = GetComponent<CharacterController>(); }

        void RestWeapon()
        {
            if (parts.axe != null && Hero != null) parts.axe.localRotation = Quaternion.Euler(Weapons.RestEuler(Hero.weapon));
            HeroPose.RestGrip(parts);
        }

        /// <summary>The hero this Viking is drawn as.</summary>
        public CharacterSpec Hero { get; private set; }

        /// <summary>Swap the Viking's look for another hero (from the hero screen): the old model is thrown away.</summary>
        public void Rebuild(CharacterSpec spec)
        {
            Hero = spec;
            if (parts != null && parts.root != null && parts.root != transform) Destroy(parts.root.gameObject);
            var model = new GameObject("Hero").transform;
            model.SetParent(transform, false);
            parts = HeroBuilder.Build(model, spec);
        }

        /// <summary>Put the Viking back on deck (after dying, for example).</summary>
        public void ReturnToShip()
        {
            DropChest();
            if (AtHelm) { AtHelm = false; SetHelm(false); }
            PlaceOnShip(new Vector3(0f, Ship.DeckY + 0.05f, -1f));
        }

        void PlaceOnShip(Vector3 local)
        {
            shipLocal = local;
            shipLocalYaw = 0f;
            OnShip = true;
            Swimming = false;
            verticalSpeed = 0f;
            Teleport(Ship.transform.TransformPoint(local));
        }

        void Teleport(Vector3 world)
        {
            controller.enabled = false;
            transform.position = world;
            controller.enabled = true;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var ship = Ship.transform;

            // 1. Ride along: put us back where we were on the (moved) ship.
            if (!locoStarted) { loco.Reset(Vector2.zero, facing); locoStarted = true; }
            if (OnShip)
            {
                Teleport(ship.TransformPoint(shipLocal));
                float was = facing;
                facing = ship.eulerAngles.y + shipLocalYaw;
                // The ship turned under us: so did we (and, in first person, so did our view).
                loco.Rotate(Mathf.DeltaAngle(was, facing));
                var turning = CameraRig.Instance;
                if (turning != null && turning.FirstPerson && !AtHelm) turning.Turn(Mathf.DeltaAngle(was, facing));
            }

            if (combat == null) combat = GetComponent<VikingCombat>();
            // At the altar the coin screen has the controls; when dead, nothing moves.
            if ((CoinUI.Instance != null && CoinUI.Instance.IsOpen) || MeadHallUI.IsOpenNow || GameMenu.Blocking || (combat != null && combat.Busy))
            {
                Prompt = null;
                controller.Move(Vector3.down * 2f * dt);
                loco.Step(Vector2.zero, 0f, dt);
                Animate(dt);
                return;
            }

            UpdatePrompt();
            if (GameInput.Pressed(Key.Interact)) Interact();

            if (AtHelm)
            {
                // Stand at the steering oar and lean on it.
                shipLocal = Ship.Parts.helm.localPosition;
                shipLocalYaw = 0f;
                Teleport(ship.TransformPoint(shipLocal));
                transform.rotation = Quaternion.Euler(0f, ship.eulerAngles.y, 0f);
                facing = ship.eulerAngles.y;
                loco.Reset(Vector2.zero, facing);
                Animate(dt);
                return;
            }

            // 2. Our own movement, relative to the camera.
            Vector2 input = GameInput.Move();
            var rig = CameraRig.Instance;
            Vector3 forward = rig != null ? rig.FlatForward : Vector3.forward;
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 move = forward * input.y + right * input.x;
            float speed = Swimming ? SwimSpeed : GameInput.Held(Key.Sprint) ? RunSpeed : GameInput.Held(Key.Walk) ? StrollSpeed : WalkSpeed;
            if (Carrying != null) speed = Swimming ? CarrySwimSpeed : Abilities.Has("plunder") ? Abilities.PlunderCarrySpeed : CarrySpeed;
            if (combat != null && combat.Blocking) speed *= 0.5f;


            // 3. Gravity, jumping and swimming.
            float water = Waves.Height(transform.position.x, transform.position.z);
            Swimming = !OnShip && transform.position.y < water - 0.9f;
            if (Swimming)
            {
                // Bob at the surface, head above water.
                verticalSpeed = Mathf.Lerp(verticalSpeed, (water - 1.25f - transform.position.y) * 4f, dt * 5f);
            }
            else if (controller.isGrounded)
            {
                verticalSpeed = -2f;
                // A jump starts with a quick crouch (a tenth of a second) and springs off from it.
                if (GameInput.Pressed(Key.Jump) && Carrying == null && jumpCrouch <= 0f) jumpCrouch = JumpCrouch;
                if (jumpCrouch > 0f)
                {
                    jumpCrouch -= dt;
                    if (jumpCrouch <= 0f) { jumpCrouch = 0f; verticalSpeed = JumpSpeed; }
                }
            }
            else
            {
                verticalSpeed -= Gravity * dt;
            }

            bool footing = Swimming || controller.isGrounded;
            if (rig != null && rig.FirstPerson)
            {
                // First person: you face where you look and move any way at once, forwards, backwards or sideways.
                strafe = Strafe(strafe, move, speed, footing, dt);
                facing = rig.Yaw;
                loco.Face(facing);
                // The legs still step (for the arms, the body's sway and your shadow) at the speed you're moving.
                var ahead = new Vector2(Mathf.Sin(facing * Mathf.Deg2Rad), Mathf.Cos(facing * Mathf.Deg2Rad)) * Mathf.Clamp01(move.magnitude);
                if (footing) loco.Step(ahead, new Vector2(strafe.x, strafe.z).magnitude / Mathf.Max(0.01f, Mathf.Clamp01(move.magnitude)), dt);
                else loco.Air(ahead, dt);
                loco.Face(facing);
                controller.Move((strafe + Vector3.up * verticalSpeed) * dt);
            }
            else
            {
                // Walking, turning and stepping: the body turns by planting its feet, so it never spins on the spot.
                if (footing) loco.Step(new Vector2(move.x, move.z), speed, dt);
                else loco.Air(new Vector2(move.x, move.z), dt);
                facing = loco.heading;
                strafe = loco.Velocity;
                controller.Move((loco.Velocity + Vector3.up * verticalSpeed) * dt);
            }
            transform.rotation = Quaternion.Euler(0f, facing, 0f);

            // 4. Are we standing on the ship? Then remember where, in ship space.
            RaycastHit hit;
            bool onDeck = Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 1.2f)
                          && hit.collider != null && hit.collider.transform.IsChildOfOrSelf(ship);
            if (onDeck)
            {
                OnShip = true;
                shipLocal = ship.InverseTransformPoint(transform.position);
                shipLocalYaw = Mathf.DeltaAngle(ship.eulerAngles.y, facing);
            }
            else
            {
                // In the air: still carried along as long as we're above the hull (jumping on a moving ship
                // must not leave you behind). Past the side, you're overboard.
                Vector3 local = ship.InverseTransformPoint(transform.position);
                float hw, k, g;
                Ship.Station(Mathf.Clamp(local.z / Ship.HalfLength, -1f, 1f), out hw, out k, out g);
                bool above = Mathf.Abs(local.x) < hw && Mathf.Abs(local.z) < Ship.HalfLength && local.y > -0.3f;
                OnShip = above && !Swimming;
                if (OnShip)
                {
                    shipLocal = local;
                    shipLocalYaw = Mathf.DeltaAngle(ship.eulerAngles.y, facing);
                }
            }

            Animate(dt);
        }

        /// <summary>
        /// First-person movement: speed up towards the pushed direction and speed, brake harder than you speed up,
        /// and in the air carry on with only a little steering (a jump keeps its momentum).
        /// </summary>
        public static Vector3 Strafe(Vector3 velocity, Vector3 wish, float speed, bool footing, float dt)
        {
            wish.y = 0f;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            var target = wish * speed;
            velocity.y = 0f;
            if (!footing) return wish.sqrMagnitude < 0.01f ? velocity : Vector3.MoveTowards(velocity, target, StrafeAirAccel * dt);
            float rate = target.sqrMagnitude > velocity.sqrMagnitude ? StrafeAccel : StrafeBrake;
            return Vector3.MoveTowards(velocity, target, rate * dt);
        }

        void UpdatePrompt()
        {
            Prompt = null;
            if (AtHelm) { Prompt = "[E] Leave the steering oar"; return; }
            if (OnShip && Carrying == null && Vector3.Distance(transform.position, Ship.Parts.helm.position) < InteractRange) { Prompt = "[E] Take the steering oar"; return; }
            if (OnShip && NearAltar() && Carrying == null) { Prompt = "[E] Flip Odin's Coin"; return; }
            var home = HomeHarbour.Instance;
            if (home != null && home.NearKeeper(transform.position))
            {
                if (Carrying != null) { Prompt = "[E] Sell the chest to Bjorn (" + Carrying.Value + " gold)"; return; }
                Prompt = "[E] Enter the mead hall";
                return;
            }
            if (home != null && home.NearTrader(transform.position))
            {
                if (Carrying != null) { Prompt = "[E] Sell the chest to Gunnar (" + Carrying.Value + " gold)"; return; }
                int count;
                int gold = HomeHarbour.CargoValue(Ship, out count);
                if (count > 0 && home.ShipInRange(Ship)) { Prompt = string.Format("[E] Sell the cargo: {0} chest{1}, {2} gold", count, count == 1 ? "" : "s", gold); return; }
            }
            var market = Market.Near(transform.position);
            if (market != null)
            {
                if (Carrying != null) { Prompt = string.Format("[E] Sell the chest in {0} ({1} gold)", market.Place.name, Mathf.RoundToInt(Carrying.Value * market.Price)); return; }
                int count;
                int gold = market.CargoValue(Ship, out count);
                if (count > 0 && market.ShipInRange(Ship)) { Prompt = string.Format("[E] Sell the cargo in {0}: {1} chest{2}, {3} gold", market.Place.name, count, count == 1 ? "" : "s", gold); return; }
            }
            if (Carrying != null)
            {
                Prompt = CanClimbAboard ? "[E] Climb aboard" : "[E] Put the chest down";
                return;
            }
            var chest = TreasureChest.NearestFree(transform.position + Vector3.up * 0.5f, InteractRange);
            if (chest != null) { Prompt = "[E] Pick up the chest (" + chest.Value + " gold)"; return; }
            if (OnShip && Ship.Hull.Holes > 0) { Prompt = "[E] Plug a hole (" + Ship.Hull.Holes + " letting water in)"; return; }
            if (OnShip && Ship.Hull.Level > 0.02f) { Prompt = "[E] Bail water (" + Mathf.RoundToInt(Ship.Hull.Level * 100f) + "% full)"; return; }
            if (CanClimbAboard) Prompt = "[E] Climb aboard";
        }

        void Interact()
        {
            if (AtHelm)
            {
                AtHelm = false;
                SetHelm(false);
                return;
            }
            if (OnShip && Carrying == null && Vector3.Distance(transform.position, Ship.Parts.helm.position) < InteractRange)
            {
                AtHelm = true;
                SetHelm(true);
                return;
            }
            if (OnShip && NearAltar() && CoinUI.Instance != null && Carrying == null)
            {
                CoinUI.Instance.Open(CoinAltar.Instance);
                return;
            }
            var home = HomeHarbour.Instance;
            if (home != null && home.NearKeeper(transform.position))
            {
                if (Carrying != null)
                {
                    var chest = Carrying;
                    Carrying = null;
                    int paid = home.Sell(chest);
                    CombatHud.Banner("+" + paid + " GOLD", "Bjorn pours you a horn of mead on the house.");
                }
                else if (MeadHallUI.Instance != null) MeadHallUI.Instance.Open();
                return;
            }
            if (home != null && home.NearTrader(transform.position))
            {
                if (Carrying != null)
                {
                    var sold = Carrying;
                    Carrying = null;
                    int gold = home.Sell(sold);
                    CombatHud.Banner("+" + gold + " GOLD", "Gunnar weighs the silver and nods.");
                    return;
                }
                int count;
                int cargo = home.SellCargo(Ship, out count);
                if (count > 0)
                {
                    CombatHud.Banner("+" + cargo + " GOLD", string.Format("Gunnar buys {0} chest{1} off your ship.", count, count == 1 ? "" : "s"));
                    return;
                }
            }
            var market = Market.Near(transform.position);
            if (market != null)
            {
                if (Carrying != null)
                {
                    var sold = Carrying;
                    Carrying = null;
                    int gold = market.Sell(sold);
                    CombatHud.Banner("+" + gold + " GOLD", "The " + market.Place.name + " trader weighs the silver and pays.");
                    return;
                }
                int count;
                int cargo = market.SellCargo(Ship, out count);
                if (count > 0)
                {
                    CombatHud.Banner("+" + cargo + " GOLD", string.Format("The {0} trader buys {1} chest{2} off your ship.", market.Place.name, count, count == 1 ? "" : "s"));
                    return;
                }
            }
            if (Carrying != null && !(CanClimbAboard))
            {
                DropChest();
                return;
            }
            if (Carrying == null)
            {
                var chest = TreasureChest.NearestFree(transform.position + Vector3.up * 0.5f, InteractRange);
                if (chest != null)
                {
                    chest.PickUp(transform);
                    Carrying = chest;
                    return;
                }
                if (OnShip && (Ship.Hull.Holes > 0 || Ship.Hull.Level > 0.02f))
                {
                    Ship.Hull.Bail();
                    bailAnim = 0.35f;
                    Sfx.At(SfxId.Splash, transform.position + transform.right * 1.5f, 0.6f, 0.15f);
                    return;
                }
            }
            if (CanClimbAboard)
            {
                // Haul yourself over the side nearest to you.
                Vector3 local = Ship.transform.InverseTransformPoint(transform.position);
                float hw, k, g;
                Ship.Station(Mathf.Clamp(local.z / Ship.HalfLength, -0.7f, 0.7f), out hw, out k, out g);
                float along = Ship.HalfLength * 0.66f;
                PlaceOnShip(new Vector3(Mathf.Sign(local.x) * (hw - 0.8f), Ship.DeckY + 0.05f, Mathf.Clamp(local.z, -along, along)));
            }
        }

        /// <summary>Put the carried chest down in front of us (on deck it becomes cargo; in the sea it floats).</summary>
        public void DropChest()
        {
            if (Carrying == null) return;
            var chest = Carrying;
            Carrying = null;
            var world = GameBootstrap.Instance != null ? GameBootstrap.Instance.transform : null;
            chest.Drop(transform.position + transform.forward * 0.9f, facing, world);
        }

        void SetHelm(bool on)
        {
            var helm = Ship.GetComponent<ShipKeyboardHelm>();
            if (helm != null) helm.enabled = on;
            if (!on) { Ship.RudderInput = 0f; Ship.Rowing = false; }
            var rig = CameraRig.Instance;
            if (rig != null)
            {
                rig.Target = on ? Ship.transform : transform;
                rig.Distance = on ? 22f : 7f;
                rig.Height = on ? 3f : 1.6f;
            }
        }

        bool NearAltar()
        {
            var altar = CoinAltar.Instance;
            return altar != null && !altar.Flipping && Vector3.Distance(transform.position, altar.transform.position) < CoinAltar.UseRange;
        }

        /// <summary>Close enough to haul yourself up her side: from the water, or from a jetty or beach beside her.</summary>
        bool CanClimbAboard { get { return !OnShip && !AtHelm && DistanceToShip() < ClimbRange; } }

        float DistanceToShip()
        {
            Vector3 local = Ship.transform.InverseTransformPoint(transform.position);
            float hw, k, g;
            Ship.Station(Mathf.Clamp(local.z / Ship.HalfLength, -1f, 1f), out hw, out k, out g);
            float outside = Mathf.Max(0f, Mathf.Abs(local.x) - hw);
            float past = Mathf.Max(0f, Mathf.Abs(local.z) - Ship.HalfLength);
            return Mathf.Sqrt(outside * outside + past * past);
        }

        void Animate(float dt)
        {
            if (parts == null) return;
            // Walking, sprinting, turning, jumping and landing, all smoothed: see HeroAnimator.
            bool grounded = controller == null || !controller.enabled || controller.isGrounded || AtHelm;
            animator.crouch = jumpCrouch > 0f ? 1f - jumpCrouch / JumpCrouch : 0f;
            animator.Step(dt, loco, grounded, verticalSpeed, Swimming, Time.time);
            animator.Apply(parts);
            bailAnim = Mathf.Max(0f, bailAnim - dt);
            if (bailAnim > 0f)
            {
                // A bucket of water heaved over the side.
                float heave = Mathf.Sin(bailAnim / 0.35f * Mathf.PI) * 110f;
                parts.leftArm.localRotation = Quaternion.Euler(-heave, 0f, 10f);
                parts.rightArm.localRotation = Quaternion.Euler(-heave, 0f, -10f);
                HeroPose.Elbows(parts, -30f, -30f);
            }
            else if (Carrying != null)
            {
                // Both arms out front, hugging the chest.
                parts.leftArm.localRotation = Quaternion.Euler(-70f, 0f, 8f);
                parts.rightArm.localRotation = Quaternion.Euler(-70f, 0f, -8f);
                HeroPose.Elbows(parts, -45f, -45f);
            }
            else
            {
                // The helm hand reaches for the oar.
                if (AtHelm)
                {
                    parts.rightArm.localRotation = Quaternion.Euler(-60f, 0f, 0f);
                    if (parts.rightForearm != null) parts.rightForearm.localRotation = Quaternion.Euler(-30f, 0f, 0f);
                }
                // A big axe rides on the shoulder, like the raider on the concept sheet.
                if (!AtHelm && !Swimming && Hero != null && HeroPose.CarryFor(Hero.weapon).set) HeroPose.Carry(parts, Hero.weapon, 1f);
                else RestWeapon();
            }
            if (parts.axe != null) parts.axe.gameObject.SetActive(Carrying == null);
        }
    }

    public static class TransformExtensions
    {
        public static bool IsChildOfOrSelf(this Transform t, Transform parent)
        {
            for (var x = t; x != null; x = x.parent) if (x == parent) return true;
            return false;
        }
    }
}
