using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Small helpers that hide API differences between Unity 2022 LTS and Unity 6.</summary>
    public static class Compat
    {
        public static void SetVelocity(Rigidbody2D body, Vector2 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }
    }
}
