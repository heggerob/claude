using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// The human works as referee: walk around, watch for BB hits (white flash + "*tak*"),
    /// and click a player who got hit but never raised their rag. Wrong calls hurt your rating.
    /// </summary>
    [RequireComponent(typeof(RefereeNPC))]
    public class PlayerRefereeController : MonoBehaviour
    {
        const float ClickRadius = 0.8f;

        RefereeNPC referee;
        Camera cam;

        /// <summary>The soldier under the mouse cursor, for the HUD highlight.</summary>
        public Soldier Hovered { get; private set; }

        void Awake() { referee = GetComponent<RefereeNPC>(); }

        void Update()
        {
            var match = MatchManager.Instance;
            if (match == null || !match.IsPlaying || match.Paused)
            {
                referee.SetMove(Vector2.zero);
                return;
            }
            if (cam == null) cam = Camera.main;

            float speed = GameInput.Held(GameKey.Sprint) ? 1.5f : 1f;
            referee.SetMove(GameInput.Move() * speed);

            Hovered = null;
            if (cam == null) return;
            Vector2 mouse = cam.ScreenToWorldPoint(GameInput.MousePosition());
            float best = ClickRadius;
            foreach (var s in match.Soldiers)
            {
                if (!s.InPlay) continue;
                float d = Vector2.Distance(mouse, s.Position);
                if (d < best) { best = d; Hovered = s; }
            }

            if (Hovered != null && GameInput.FirePressed())
            {
                if (!referee.CanSee(Hovered.Position))
                {
                    Effects.Text(Hovered.Position + new Vector2(0f, 1f), "You can't see them from here", new Color(1f, 1f, 1f, 0.8f), 1.2f, 12);
                    return;
                }
                match.RefereeCallOut(Hovered, referee);
            }
        }
    }
}
