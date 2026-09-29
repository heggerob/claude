using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// The referee walking the field in a hi-vis vest. Either run by AI (using the hired profile's hidden skill)
    /// or controlled by the human when they take the referee job.
    /// </summary>
    public class RefereeNPC : MonoBehaviour
    {
        public const float Height = 1.8f;
        public const float SightRange = 32f;
        const float Speed = 3.6f;

        public RefereeProfile Profile { get; private set; }
        public bool HumanControlled { get; private set; }
        public Vector2 Position { get { return transform.position; } }
        public Collider2D Collider { get; private set; }

        class Judgement
        {
            public Soldier soldier;
            public float at;
            public float chance;
            public bool wrongCall;
        }

        readonly List<Judgement> pending = new List<Judgement>();
        Rigidbody2D body;
        Transform rig;
        Vector2 moveInput;
        Vector2 facing = Vector2.up;
        Vector2 lastAction;
        float lastActionTime = -99f;
        Vector2 wanderOffset;
        float nextWander;

        public static RefereeNPC Create(Transform parent, RefereeProfile profile, bool human, Vector2 position)
        {
            var go = new GameObject("Referee: " + profile.name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = Soldier.Radius;

            // Same pixel figure as the players, in a hi-vis vest and black cap, no gun.
            var rig = new GameObject("Rig").transform;
            rig.SetParent(go.transform, false);
            Layer(go.transform, "Shadow", PixelArt.Shadow, Color.white, 4);
            Layer(rig, "Hi-vis Body", PixelArt.Body(CamoPattern.Solid), new Color(1f, 0.92f, 0.15f), 10);
            Layer(rig, "Cap", PixelArt.Cap, new Color(0.12f, 0.12f, 0.12f), 14);
            Layer(rig, "Goggles", PixelArt.Details, Color.white, 15);

            var referee = go.AddComponent<RefereeNPC>();
            referee.Profile = profile;
            referee.HumanControlled = human;
            referee.body = rb;
            referee.Collider = col;
            referee.rig = rig;
            return referee;
        }

        static void Layer(Transform parent, string name, Sprite sprite, Color color, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false);
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
        }

        /// <summary>Length above 1 = jogging (human referee holding shift).</summary>
        public void SetMove(Vector2 direction) { moveInput = Vector2.ClampMagnitude(direction, 1.5f); }

        public bool CanSee(Vector2 point)
        {
            return Vector2.Distance(point, Position) <= SightRange && MatchManager.HasLineOfSight(Position, point, false);
        }

        public void Whistle(string text)
        {
            Sfx.PlayAt(SfxId.Whistle, Position, 1f, 0.02f);
            Effects.Text(Position + new Vector2(0f, 0.9f), "*FWEEET*", new Color(1f, 0.95f, 0.3f), 1.2f, 15);
            if (!string.IsNullOrEmpty(text)) Effects.Text(Position + new Vector2(0f, 1.4f), text, Color.white, 1.8f, 13);
        }

        // ---------------------------------------------------------------- events from the match (AI only)

        public void OnSoldierHit(Soldier victim, Vector2 point)
        {
            lastAction = point;
            lastActionTime = Time.time;
        }

        /// <summary>A soldier let the hit-call window run out. Will the ref notice?</summary>
        public void OnHitNotCalled(Soldier soldier)
        {
            if (HumanControlled) return;
            float skill = Profile.skill;
            float sawIt = soldier.LastHitSeenByReferee ? 1f : 0.3f; // didn't see it, but maybe heard the "tak"
            float distance = Vector2.Distance(soldier.Position, Position);
            float closeness = Mathf.Clamp01(1.15f - distance / SightRange);
            pending.Add(new Judgement
            {
                soldier = soldier,
                at = Time.time + Mathf.Lerp(3.5f, 0.8f, skill) + Random.Range(0f, 1f),
                chance = 0.05f + skill * sawIt * closeness,
            });
        }

        /// <summary>A BB landed right next to a player. A bad ref may think it was a hit.</summary>
        public void OnNearMiss(Soldier soldier)
        {
            if (HumanControlled || soldier.State != SoldierState.Alive || soldier.HasUncalledHit) return;
            if (!CanSee(soldier.Position)) return;
            if (Random.value >= (1f - Profile.skill) * 0.05f) return;
            pending.Add(new Judgement { soldier = soldier, at = Time.time + Random.Range(0.8f, 1.6f), chance = 1f, wrongCall = true });
        }

        public void OnHitByBB(Soldier shooter)
        {
            Effects.Text(Position + new Vector2(0f, 0.9f), "OW! WATCH IT!", new Color(1f, 0.5f, 0.3f), 1.5f, 14);
            var match = MatchManager.Instance;
            if (match != null && shooter != null) match.OnRefereeShot(shooter);
        }

        public void ClearPending() { pending.Clear(); }

        // ---------------------------------------------------------------- update

        void Update()
        {
            // Face where we walk.
            if (moveInput.sqrMagnitude > 0.01f) facing = Vector2.Lerp(facing, moveInput.normalized, Time.deltaTime * 8f);
            if (rig != null) rig.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);

            var match = MatchManager.Instance;
            if (match == null || !match.IsPlaying) return;
            if (!HumanControlled)
            {
                Judge(match);
                ThinkMovement(match);
            }
        }

        void Judge(MatchManager match)
        {
            float now = Time.time;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var j = pending[i];
                if (now < j.at) continue;
                pending.RemoveAt(i);
                if (j.soldier == null) continue;

                if (j.wrongCall)
                {
                    if (j.soldier.State == SoldierState.Alive && !j.soldier.HasUncalledHit) match.RefereeCallOut(j.soldier, this);
                }
                else if (j.soldier.HasUncalledHit && j.soldier.InPlay && Random.value < j.chance)
                {
                    match.RefereeCallOut(j.soldier, this);
                }
            }
        }

        void ThinkMovement(MatchManager match)
        {
            float now = Time.time;
            Vector2 focus;
            if (now - lastActionTime < 4f)
            {
                focus = lastAction;
            }
            else
            {
                // Centre of everyone still playing.
                focus = Vector2.zero;
                int n = 0;
                foreach (var s in match.Soldiers)
                {
                    if (!s.InPlay) continue;
                    focus += s.Position;
                    n++;
                }
                focus = n > 0 ? focus / n : Vector2.zero;
            }

            if (now >= nextWander)
            {
                wanderOffset = Random.insideUnitCircle * 6f;
                nextWander = now + Random.Range(3f, 6f);
            }

            // Good referees keep closer to the action.
            float keepDistance = Mathf.Lerp(12f, 5f, Profile.skill);
            Vector2 target = focus + wanderOffset;
            Vector2 to = target - Position;
            if (to.magnitude < keepDistance * 0.5f) { SetMove(Vector2.zero); return; }
            SetMove(Steering.Avoid(Position, to, Collider) * Mathf.Clamp01(to.magnitude / keepDistance));
        }

        void FixedUpdate()
        {
            var match = MatchManager.Instance;
            bool playing = match != null && match.IsPlaying;
            Compat.SetVelocity(body, playing ? moveInput * Speed : Vector2.zero);
        }
    }
}
