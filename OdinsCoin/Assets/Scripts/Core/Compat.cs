using UnityEngine;

namespace OdinsCoin
{
    /// <summary>Hides API renames between Unity 2022 LTS and Unity 6.</summary>
    public static class Compat
    {
        public static Vector3 Velocity(Rigidbody body)
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }
    }
}
