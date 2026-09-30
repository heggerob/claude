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
        /// <summary>Breath for climbing, sprinting and swimming (seconds of effort).</summary>
        public float Stamina = StaminaRules.BaseStamina;
        public float MaxStamina { get { return StaminaRules.Max(Upgrades.Current.Endurance); } }
        public bool Climbing { get; private set; }
        /// <summary>Out of breath: no climbing or sprinting until it's mostly back.</summary>
        public bool Tired { get; private set; }
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
            // Sprinting takes breath; out of it, you're down to a walk until you've got it back.
            bool sprinting = GameInput.Held(Key.Sprint) && Stamina > 0f && !Tired;
            float speed = Swimming ? (Tired ? SwimSpeed * 0.5f : SwimSpeed) : sprinting ? RunSpeed : GameInput.Held(Key.Walk) ? StrollSpeed : WalkSpeed;
            if (Carrying != null) speed = Swimming ? CarrySwimSpeed : Abilities.Has("plunder") ? Abilities.PlunderCarrySpeed : CarrySpeed;
            if (combat != null && combat.Blocking) speed *= 0.5f;


            // 3. Gravity, jumping, climbing and swimming.
            float water = Waves.Height(transform.position.x, transform.position.z);
            Swimming = !OnShip && transform.position.y < water - 0.9f;
            // Climbing: push forward against a steep rock face (not the ship's side) and you climb it, as long as
            // your breath lasts; run out and you let go.
            bool wasClimbing = Climbing;
            Climbing = false;
            RaycastHit wall;
            if (!Swimming && !OnShip && Carrying == null && input.y > 0.3f && !Tired && Stamina > 0f
                && Physics.Raycast(transform.position + Vector3.up * 1f, transform.forward, out wall, 0.9f) && wall.collider != null
                && StaminaRules.Climbable(wall.normal) && (Ship == null || !wall.collider.transform.IsChildOfOrSelf(Ship.transform)))
                Climbing = true;
            if (Climbing)
            {
                verticalSpeed = StaminaRules.ClimbSpeed;
                speed = 0.6f;
            }
            else if (wasClimbing && input.y > 0.3f)
            {
                // Over the top: a little hop up onto the ledge.
                verticalSpeed = StaminaRules.ClimbSpeed * 1.6f;
            }
            else if (Swimming)
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

            Stamina = StaminaRules.Step(Stamina, MaxStamina, dt, Climbing, sprinting && move.sqrMagnitude > 0.01f, Swimming && move.sqrMagnitude > 0.01f, controller.isGrounded || OnShip);
            // Run dry and you must get your breath back before you can climb or sprint again.
            if (Stamina <= 0f) Tired = true;
            else if (Tired && Stamina >= MaxStamina * StaminaRules.RecoverAt) Tired = false;

            bool footing = Swimming || controller.isGrounded || Climbing;
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

        /// <summary>
        /// Where your hands reach for things: at your middle, or in first person a little in front of you, so you
        /// pick up the chest you're looking at rather than one behind you.
        /// </summary>
        Vector3 ReachPoint()
        {
            var rig = CameraRig.Instance;
            return ReachPoint(transform.position, rig != null && rig.FirstPerson ? rig.FlatForward : Vector3.zero);
        }

        /// <summary><see cref="ReachPoint()"/> for a body at <paramref name="at"/> looking along <paramref name="look"/> (zero in third person).</summary>
        public static Vector3 ReachPoint(Vector3 at, Vector3 look) { return at + Vector3.up * 0.5f + look * (InteractRange * 0.45f); }

        /// <summary>The breath bar: a small bar under the middle of the screen, shown while you're using it.</summary>
        void OnGUI()
        {
            if (Stamina >= MaxStamina - 0.01f || GameMenu.Blocking) return;
            float w = 140f, x = Screen.width / 2f - w / 2f, y = Screen.height / 2f + 40f;
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, w + 4f, 10f), Texture2D.whiteTexture);
            GUI.color = Tired ? new Color(0.9f, 0.3f, 0.2f, 0.9f) : new Color(0.55f, 0.9f, 0.45f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(Stamina / MaxStamina), 6f), Texture2D.whiteTexture);
            GUI.color = old;
        }

        void UpdatePrompt()
        {
            Prompt = null;
            if (AtHelm) { Prompt = "[E] Leave the steering oar"; return; }
            if (OnShip && Carrying == null && Vector3.Distance(transform.position, Ship.Parts.helm.position) < InteractRange) { Prompt = "[E] Take the steering oar"; return; }
            if (OnShip && NearAltar()) { Prompt = AltarPrompt(); if (Prompt != null) return; }
            if (!OnShip && Carrying == null && Townsperson.Near(transform.position) != null) { Prompt = "[E] Ask what's the news"; return; }
            Transform cairn; Place cairnPlace;
            if (!OnShip && Carrying == null && Hoards.Near(transform.position, out cairn, out cairnPlace)) { Prompt = "[E] Dig at the cairn"; return; }
            ShrineRing ring; int stone;
            if (!OnShip && Carrying == null && RuneShrines.Near(transform.position, out ring, out stone))
            {
                Prompt = ring.Puzzle.Solved ? "The rune ring is awake: its gift is yours" : "[E] Touch the stone cut with " + ring.Puzzle.Notches[stone] + " notch" + (ring.Puzzle.Notches[stone] == 1 ? "" : "es");
                if (!ring.Puzzle.Solved) return;
            }
            Transform mark; Place markPlace;
            if (!OnShip && Carrying == null && Landmarks.Near(transform.position, out mark, out markPlace)) { Prompt = "[E] " + Landmarks.SurveyVerb(Landmarks.KindFor(markPlace)); return; }
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
            var chest = TreasureChest.NearestFree(ReachPoint(), InteractRange);
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
            if (OnShip && NearAltar() && StakeAtAltar()) return;
            var talker = !OnShip && Carrying == null ? Townsperson.Near(transform.position) : null;
            if (talker != null && WorldMap.Current != null)
            {
                talker.Listen(transform.position);
                var u = Upgrades.Current;
                var here = new Vector3((float)WorldOrigin.GlobalX(transform.position), 0f, (float)WorldOrigin.GlobalZ(transform.position));
                var rumour = Rumours.Tell(here, Places.All, p => Places.Position(WorldMap.Current, p), PlaceLife.Raided, u.Dug, u.Shrines, Random.value, u.Caves);
                CombatHud.Banner("THE NEWS", rumour.HasValue ? rumour.Value.text : "Nothing much. You've seen it all, they say.");
                return;
            }
            Transform digAt; Place digPlace;
            if (!OnShip && Carrying == null && Hoards.Near(transform.position, out digAt, out digPlace))
            {
                var world = GameBootstrap.Instance != null ? GameBootstrap.Instance.transform : null;
                var dug = Hoards.Dig(digAt, digPlace, world);
                Sfx.At(SfxId.Chest, dug.transform.position, 1f);
                CombatHud.Banner("BURIED TREASURE", "Under the cairn: a " + dug.Name + " worth " + dug.Value + " gold.");
                return;
            }
            ShrineRing touched; int touchedStone;
            if (!OnShip && Carrying == null && RuneShrines.Near(transform.position, out touched, out touchedStone) && !touched.Puzzle.Solved)
            {
                var result = touched.Puzzle.Touch(touchedStone);
                touched.Show();
                if (result == RunePuzzle.Result.Wrong) { Sfx.Play(SfxId.Curse, 0.5f); CombatHud.Banner("THE STONES FALL DARK", "Not that one. Count the notches."); }
                else if (result == RunePuzzle.Result.Lit) Sfx.Play(SfxId.Blessing, 0.4f);
                else if (result == RunePuzzle.Result.Solved)
                {
                    var gift = RuneShrines.GiftOf(touched.Place);
                    var u = Upgrades.Current;
                    if (!u.Shrines.Contains(touched.Place.name)) u.Shrines.Add(touched.Place.name);
                    if (gift == RuneGift.Vitality) u.Vitality++;
                    else if (gift == RuneGift.Endurance) u.Endurance++;
                    else u.Luck++;
                    if (combat != null) combat.Health.Restore();
                    Sfx.Play(SfxId.Blessing, 1f);
                    CombatHud.Banner("THE RUNE RING WAKES", "You are given the " + RuneShrines.GiftName(gift) + ".");
                }
                return;
            }
            Transform survey; Place surveyPlace;
            if (!OnShip && Carrying == null && Landmarks.Near(transform.position, out survey, out surveyPlace))
            {
                // Like Zelda's towers: the land for miles around goes onto the chart.
                ChartReveal.Reveal((float)WorldOrigin.GlobalX(survey.position), (float)WorldOrigin.GlobalZ(survey.position), ChartReveal.SurveyRadius);
                CombatHud.Banner("THE LAND LIES OPEN", "From " + surveyPlace.name + " you can see for miles. It's all on your chart now (M).");
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
                var chest = TreasureChest.NearestFree(ReachPoint(), InteractRange);
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

        float stakeAllConfirm = -10f;

        /// <summary>What E does at Odin's altar: stake the chest in your arms, or everything on deck (asked twice).</summary>
        string AltarPrompt()
        {
            var fortune = Fortune.Current;
            if (Carrying != null)
            {
                if (!Stake.CanRaise(Carrying.Tier)) return null;
                return "[E] Stake the " + Carrying.Name + " on Odin's altar: " + Stake.Offer(Carrying.Value, Stake.WinValue(Carrying.BaseGold, Carrying.Tier, fortune));
            }
            var deck = Stake.OnDeck(Ship);
            if (deck.Count == 0) return "Odin's altar: bring treasure here to stake it";
            int now, ifWon;
            Stake.AllOrNothing(deck, fortune, out now, out ifWon);
            if (Time.time - stakeAllConfirm < Stake.ConfirmWindow) return "<color=#ffd060>[E] again to stake EVERYTHING on deck</color>: " + Stake.Offer(now, ifWon);
            return "[E] Stake all " + deck.Count + " chest" + (deck.Count == 1 ? "" : "s") + " on deck, all or nothing: " + Stake.Offer(now, ifWon);
        }

        /// <summary>Lay the treasure on the altar and throw. True if E did something here.</summary>
        bool StakeAtAltar()
        {
            var altar = CoinAltar.Instance;
            if (altar == null || !altar.ReadyForStake) return false;
            var world = GameBootstrap.Instance != null ? GameBootstrap.Instance.transform : null;
            if (Carrying != null)
            {
                if (!Stake.CanRaise(Carrying.Tier)) return false;
                // Set it down beside the altar, where everyone can watch.
                var chest = Carrying;
                Carrying = null;
                chest.Drop(altar.transform.position + altar.transform.right * 1.1f, altar.transform.eulerAngles.y, world);
                altar.FlipForStake(Stake.CurrentOdds, heads =>
                {
                    if (chest == null) return;
                    if (heads)
                    {
                        chest.SetTier(Stake.Raised(chest.Tier));
                        CombatHud.Banner("ODIN SMILES", "It's a " + chest.Name + " now, worth " + chest.Value + " gold. Get it home safe.");
                    }
                    else
                    {
                        CombatHud.Banner("ODIN TAKES IT", "The chest is gone. The serpent always wins sometimes.");
                        Destroy(chest.gameObject);
                    }
                });
                return true;
            }
            var deck = Stake.OnDeck(Ship);
            if (deck.Count == 0) return false;
            // Everything at once: only on a second press.
            if (Time.time - stakeAllConfirm >= Stake.ConfirmWindow) { stakeAllConfirm = Time.time; return true; }
            stakeAllConfirm = -10f;
            altar.FlipForStake(Stake.CurrentOdds, heads =>
            {
                int total = 0;
                foreach (var c in deck)
                {
                    if (c == null || !c.Stowed(Ship)) continue;
                    if (heads) { c.SetTier(TreasureChest.MaxTier); total += c.Value; }
                    else Destroy(c.gameObject);
                }
                if (heads) CombatHud.Banner("ODIN'S HOARD!", "Every chest on deck is Odin's hoard: " + total + " gold. Now get it home.");
                else CombatHud.Banner("THE DECK IS BARE", "Odin took everything. Back to the plundering.");
            });
            return true;
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
