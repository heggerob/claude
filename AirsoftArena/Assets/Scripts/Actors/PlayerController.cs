using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Keyboard + mouse control of the human's soldier.</summary>
    [RequireComponent(typeof(Soldier))]
    public class PlayerController : MonoBehaviour
    {
        /// <summary>Casual setting: call hits automatically.</summary>
        public bool AutoCallHits;

        Soldier soldier;
        Camera cam;

        void Awake() { soldier = GetComponent<Soldier>(); }

        void Update()
        {
            var match = MatchManager.Instance;
            if (match == null || !match.IsPlaying || match.Paused)
            {
                soldier.SetMove(Vector2.zero, false);
                return;
            }
            if (cam == null) cam = Camera.main;

            soldier.SetCrouch(GameInput.Held(GameKey.Crouch));
            soldier.SetAiming(GameInput.AimHeld());
            soldier.SetMove(GameInput.Move(), GameInput.Held(GameKey.Sprint));

            if (cam != null)
            {
                Vector2 mouse = cam.ScreenToWorldPoint(GameInput.MousePosition());
                soldier.SetAim(mouse - soldier.Position);
            }

            soldier.PullTrigger(GameInput.FireHeld(), GameInput.FirePressed());

            if (GameInput.Pressed(GameKey.Reload)) soldier.Reload();
            if (GameInput.Pressed(GameKey.FireMode)) soldier.CycleFireMode();
            int slot = GameInput.SlotPressed();
            if (slot >= 0) soldier.SwitchSlot(slot);

            if (GameInput.Pressed(GameKey.CallHit) || (AutoCallHits && soldier.State == SoldierState.Hit))
                soldier.CallHit();
        }
    }
}
