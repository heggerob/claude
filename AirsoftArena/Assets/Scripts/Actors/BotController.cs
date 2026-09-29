using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// Simple bot: find a visible enemy, keep a comfortable distance, shoot in bursts.
    /// Each bot has a hidden honesty value: some of them "forget" to call their hits.
    /// </summary>
    [RequireComponent(typeof(Soldier))]
    public class BotController : MonoBehaviour
    {
        const float ThinkInterval = 0.25f;
        const float SightRange = 30f;

        /// <summary>0..1, how well the bot aims and reacts.</summary>
        public float Skill = 0.6f;

        Soldier soldier;
        Soldier target;
        Vector2 goal;
        Vector2 aimOffset;
        Vector2 stuckDirection;
        Vector2 lastPosition;
        float nextThink, nextGoal, burstUntil, nextBurst, nextSemiClick, stuckUntil, stuckCheck;
        float strafeSign = 1f, nextStrafeFlip;
        float callDecisionAt = -1f;
        bool willCall, wasHeld, crouchWhenShooting;

        void Awake()
        {
            soldier = GetComponent<Soldier>();
            crouchWhenShooting = Random.value < 0.5f;
        }

        void Update()
        {
            var match = MatchManager.Instance;
            if (match == null || !match.IsPlaying)
            {
                soldier.SetMove(Vector2.zero, false);
                return;
            }
            float now = Time.time;

            // --- The moment of truth: call the hit or keep playing?
            if (soldier.State == SoldierState.Hit)
            {
                if (callDecisionAt < 0f)
                {
                    willCall = Random.value < soldier.Honesty;
                    callDecisionAt = now + Random.Range(0.3f, 1.4f);
                }
                if (willCall && now >= callDecisionAt) soldier.CallHit();
            }
            else if (soldier.State == SoldierState.Alive)
            {
                callDecisionAt = -1f;
            }

            if (!soldier.InPlay)
            {
                target = null;
                soldier.SetCrouch(false);
                soldier.PullTrigger(false, false);
                MoveTowards(match.SpawnCenter(soldier.Team), false);
                return;
            }

            if (now >= nextThink)
            {
                nextThink = now + ThinkInterval;
                Think(match);
            }

            if (target != null) Fight(now);
            else Patrol(match, now);
        }

        void Think(MatchManager match)
        {
            Soldier best = null;
            float bestDistance = SightRange;
            foreach (var other in match.Soldiers)
            {
                if (other.Team == soldier.Team || !other.InPlay) continue;
                float d = Vector2.Distance(other.Position, soldier.Position);
                if (d >= bestDistance) continue;
                if (!MatchManager.HasLineOfSight(soldier.Position, other.Position, false)) continue;
                best = other;
                bestDistance = d;
            }
            target = best;

            // Worse bots aim further off, and more so at long range.
            float error = (1.1f - Skill) * 0.09f * bestDistance;
            aimOffset = Random.insideUnitCircle * error;

            var weapon = soldier.Weapon;
            if (weapon.Data.IsMelee || (weapon.IsEmpty && !weapon.CanReload)) soldier.SwitchSlot((soldier.Slot + 1) % 2);
            if (weapon.IsEmpty || (target == null && weapon.AmmoInMag < weapon.Data.magCapacity * 0.3f)) soldier.Reload();
        }

        void Fight(float now)
        {
            if (!target.InPlay) { target = null; return; }
            Vector2 to = target.Position - soldier.Position;
            float distance = to.magnitude;
            soldier.SetAim(to + aimOffset);

            // Keep 7-17 m away and strafe sideways.
            if (now >= nextStrafeFlip)
            {
                strafeSign = Random.value < 0.5f ? -1f : 1f;
                nextStrafeFlip = now + Random.Range(0.8f, 2.2f);
            }
            Vector2 dir = to / Mathf.Max(0.01f, distance);
            Vector2 move = new Vector2(-dir.y, dir.x) * strafeSign * 0.7f;
            if (distance > 17f) move += dir;
            else if (distance < 7f) move -= dir;

            bool shooting = now < burstUntil;
            soldier.SetCrouch(shooting && crouchWhenShooting && distance > 8f);
            if (soldier.Crouching) move *= 0.3f;
            MoveDirect(move, false);

            if (now >= nextBurst)
            {
                float reaction = Mathf.Lerp(0.7f, 0.15f, Skill);
                burstUntil = now + reaction * 0.2f + Random.Range(0.25f, 0.8f);
                nextBurst = burstUntil + Random.Range(0.3f, 1.1f);
            }

            bool held = shooting;
            bool pressed = false;
            if (held && soldier.Weapon.Mode != FireMode.Auto && now >= nextSemiClick)
            {
                pressed = true;
                nextSemiClick = now + Random.Range(0.15f, 0.3f);
            }
            if (held && !wasHeld) pressed = true;
            wasHeld = held;
            soldier.PullTrigger(held, pressed);
        }

        void Patrol(MatchManager match, float now)
        {
            soldier.SetCrouch(false);
            soldier.PullTrigger(false, false);
            wasHeld = false;
            if (now >= nextGoal || Vector2.Distance(soldier.Position, goal) < 1.5f)
            {
                // Push towards the other side, drifting across the whole field.
                Rect enemy = match.SpawnZone(Teams.Other(soldier.Team));
                float x = Mathf.Lerp(soldier.Position.x, enemy.center.x, Random.Range(0.3f, 0.9f));
                goal = new Vector2(x, Random.Range(MapBuilder.Bounds.yMin + 2f, MapBuilder.Bounds.yMax - 2f));
                nextGoal = now + Random.Range(4f, 8f);
            }
            MoveTowards(goal, false);
            Vector2 look = goal - soldier.Position;
            soldier.SetAim(look);
        }

        void MoveTowards(Vector2 point, bool sprint)
        {
            MoveDirect(point - soldier.Position, sprint);
        }

        void MoveDirect(Vector2 desired, bool sprint)
        {
            float now = Time.time;
            if (desired.sqrMagnitude < 0.0001f)
            {
                soldier.SetMove(Vector2.zero, false);
                return;
            }

            // If we barely moved in the last second, pick a random side-step for a moment.
            if (now >= stuckCheck)
            {
                if (Vector2.Distance(lastPosition, soldier.Position) < 0.4f && desired.sqrMagnitude > 0.2f)
                {
                    stuckDirection = Steering.Rotate(desired.normalized, Random.value < 0.5f ? 100f : -100f);
                    stuckUntil = now + 0.8f;
                }
                lastPosition = soldier.Position;
                stuckCheck = now + 1f;
            }

            Vector2 dir = now < stuckUntil ? stuckDirection : Steering.Avoid(soldier.Position, desired, soldier.Collider);
            soldier.SetMove(dir * Mathf.Min(1f, desired.magnitude), sprint);
        }
    }
}
