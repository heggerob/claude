using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AirsoftArena
{
    public enum GameKey { Up, Down, Left, Right, Sprint, Crouch, Reload, CallHit, FireMode, Pause, Slot1, Slot2, Slot3, PrevWeapon, NextWeapon }

    /// <summary>
    /// Works with both the old Input Manager and the new Input System package,
    /// so the project runs no matter which one the Unity template enabled.
    /// </summary>
    public static class GameInput
    {
        public static Vector2 Move()
        {
            float x = (Held(GameKey.Right) ? 1f : 0f) - (Held(GameKey.Left) ? 1f : 0f);
            float y = (Held(GameKey.Up) ? 1f : 0f) - (Held(GameKey.Down) ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        public static int SlotPressed()
        {
            if (Pressed(GameKey.Slot1)) return 0;
            if (Pressed(GameKey.Slot2)) return 1;
            if (Pressed(GameKey.Slot3)) return 2;
            return -1;
        }

#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
        public static bool Held(GameKey key)
        {
            var k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case GameKey.Up: return k.wKey.isPressed || k.upArrowKey.isPressed;
                case GameKey.Down: return k.sKey.isPressed || k.downArrowKey.isPressed;
                case GameKey.Left: return k.aKey.isPressed || k.leftArrowKey.isPressed;
                case GameKey.Right: return k.dKey.isPressed || k.rightArrowKey.isPressed;
                case GameKey.Sprint: return k.leftShiftKey.isPressed;
                case GameKey.Crouch: return k.cKey.isPressed || k.leftCtrlKey.isPressed;
                default:
                    var control = Control(k, key);
                    return control != null && control.isPressed;
            }
        }

        public static bool Pressed(GameKey key)
        {
            var k = Keyboard.current;
            if (k == null) return false;
            var control = Control(k, key);
            return control != null && control.wasPressedThisFrame;
        }

        static UnityEngine.InputSystem.Controls.KeyControl Control(Keyboard k, GameKey key)
        {
            switch (key)
            {
                case GameKey.Reload: return k.rKey;
                case GameKey.CallHit: return k.hKey;
                case GameKey.FireMode: return k.bKey;
                case GameKey.Pause: return k.escapeKey;
                case GameKey.Slot1: return k.digit1Key;
                case GameKey.Slot2: return k.digit2Key;
                case GameKey.Slot3: return k.digit3Key;
                case GameKey.PrevWeapon: return k.qKey;
                case GameKey.NextWeapon: return k.eKey;
                case GameKey.Sprint: return k.leftShiftKey;
                case GameKey.Crouch: return k.cKey;
                default: return null;
            }
        }

        public static bool FireHeld() { var m = Mouse.current; return m != null && m.leftButton.isPressed; }
        public static bool AimHeld() { var m = Mouse.current; return m != null && m.rightButton.isPressed; }
        public static float Scroll() { var m = Mouse.current; return m != null ? m.scroll.ReadValue().y / 120f : 0f; }
        public static bool FirePressed() { var m = Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; }
        public static Vector2 MousePosition() { var m = Mouse.current; return m != null ? m.position.ReadValue() : Vector2.zero; }
#else
        public static bool Held(GameKey key)
        {
            switch (key)
            {
                case GameKey.Up: return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
                case GameKey.Down: return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
                case GameKey.Left: return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                case GameKey.Right: return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
                case GameKey.Sprint: return Input.GetKey(KeyCode.LeftShift);
                case GameKey.Crouch: return Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
                default: return Input.GetKey(Code(key));
            }
        }

        public static bool Pressed(GameKey key) { return Input.GetKeyDown(Code(key)); }

        static KeyCode Code(GameKey key)
        {
            switch (key)
            {
                case GameKey.Reload: return KeyCode.R;
                case GameKey.CallHit: return KeyCode.H;
                case GameKey.FireMode: return KeyCode.B;
                case GameKey.Pause: return KeyCode.Escape;
                case GameKey.Slot1: return KeyCode.Alpha1;
                case GameKey.Slot2: return KeyCode.Alpha2;
                case GameKey.Slot3: return KeyCode.Alpha3;
                case GameKey.PrevWeapon: return KeyCode.Q;
                case GameKey.NextWeapon: return KeyCode.E;
                case GameKey.Sprint: return KeyCode.LeftShift;
                case GameKey.Crouch: return KeyCode.C;
                default: return KeyCode.None;
            }
        }

        public static bool FireHeld() { return Input.GetMouseButton(0); }
        public static bool AimHeld() { return Input.GetMouseButton(1); }
        public static float Scroll() { return Input.mouseScrollDelta.y; }
        public static bool FirePressed() { return Input.GetMouseButtonDown(0); }
        public static Vector2 MousePosition() { return Input.mousePosition; }
#endif
    }
}
