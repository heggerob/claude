using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Very small obstacle avoidance for bots and the AI referee. Good enough for an open map without a navmesh.</summary>
    public static class Steering
    {
        static readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        static readonly float[] Angles = { 35f, -35f, 70f, -70f, 105f, -105f, 140f, -140f };

        public static bool Blocked(Vector2 from, Vector2 direction, float distance, Collider2D self)
        {
            hits.Clear();
            Physics2D.CircleCast(from, 0.3f, direction, new ContactFilter2D().NoFilter(), hits, distance);
            foreach (var hit in hits)
            {
                if (hit.collider == self) continue;
                if (hit.collider.GetComponent<Obstacle>() != null) return true;
            }
            return false;
        }

        /// <summary>Returns the desired direction, or the closest free direction if a wall is in the way.</summary>
        public static Vector2 Avoid(Vector2 from, Vector2 desired, Collider2D self, float lookAhead = 1.3f)
        {
            if (desired.sqrMagnitude < 0.0001f) return Vector2.zero;
            desired.Normalize();
            if (!Blocked(from, desired, lookAhead, self)) return desired;
            foreach (float a in Angles)
            {
                Vector2 dir = Rotate(desired, a);
                if (!Blocked(from, dir, lookAhead, self)) return dir;
            }
            return -desired;
        }

        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
