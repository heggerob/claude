using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OdinsCoin
{
    public enum Key { Up, Down, Left, Right, Jump, Interact, Sprint, Coin, Pause, SailUp, SailDown, Walk, Anchor, TimeWarp, Chart, View }

    /// <summary>Keyboard + mouse that works with both the old Input Manager and the new Input System.</summary>
    public static class GameInput
    {
        public static Vector2 Move()
        {
            float x = (Held(Key.Right) ? 1f : 0f) - (Held(Key.Left) ? 1f : 0f);
            float y = (Held(Key.Up) ? 1f : 0f) - (Held(Key.Down) ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
        static UnityEngine.InputSystem.Controls.KeyControl Control(Keyboard k, Key key)
        {
            switch (key)
            {
                case Key.Up: return k.wKey;
                case Key.Down: return k.sKey;
                case Key.Left: return k.aKey;
                case Key.Right: return k.dKey;
                case Key.Jump: return k.spaceKey;
                case Key.Interact: return k.eKey;
                case Key.Sprint: return k.leftShiftKey;
                case Key.Walk: return k.leftCtrlKey;
                case Key.Coin: return k.fKey;
                case Key.Pause: return k.escapeKey;
                case Key.SailUp: return k.rKey;
                case Key.SailDown: return k.qKey;
                case Key.Anchor: return k.gKey;
                case Key.TimeWarp: return k.tKey;
                case Key.Chart: return k.mKey;
                case Key.View: return k.vKey;
                default: return null;
            }
        }

        public static bool Held(Key key)
        {
            var k = Keyboard.current;
            if (k == null) return false;
            var c = Control(k, key);
            return c != null && c.isPressed;
        }

        public static bool Pressed(Key key)
        {
            var k = Keyboard.current;
            if (k == null) return false;
            var c = Control(k, key);
            return c != null && c.wasPressedThisFrame;
        }

        public static Vector2 MouseDelta()
        {
            var m = Mouse.current;
            return m != null ? m.delta.ReadValue() * 0.1f : Vector2.zero;
        }

        public static float Scroll()
        {
            var m = Mouse.current;
            return m != null ? m.scroll.ReadValue().y / 120f : 0f;
        }

        public static bool AttackPressed() { var m = Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; }
        public static bool BlockHeld() { var m = Mouse.current; return m != null && m.rightButton.isPressed; }
#else
        static KeyCode Code(Key key)
        {
            switch (key)
            {
                case Key.Up: return KeyCode.W;
                case Key.Down: return KeyCode.S;
                case Key.Left: return KeyCode.A;
                case Key.Right: return KeyCode.D;
                case Key.Jump: return KeyCode.Space;
                case Key.Interact: return KeyCode.E;
                case Key.Sprint: return KeyCode.LeftShift;
                case Key.Walk: return KeyCode.LeftControl;
                case Key.Coin: return KeyCode.F;
                case Key.Pause: return KeyCode.Escape;
                case Key.SailUp: return KeyCode.R;
                case Key.SailDown: return KeyCode.Q;
                case Key.Anchor: return KeyCode.G;
                case Key.TimeWarp: return KeyCode.T;
                case Key.Chart: return KeyCode.M;
                case Key.View: return KeyCode.V;
                default: return KeyCode.None;
            }
        }

        public static bool Held(Key key) { return Input.GetKey(Code(key)); }
        public static bool Pressed(Key key) { return Input.GetKeyDown(Code(key)); }
        public static Vector2 MouseDelta() { return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")); }
        public static float Scroll() { return Input.mouseScrollDelta.y; }
        public static bool AttackPressed() { return Input.GetMouseButtonDown(0); }
        public static bool BlockHeld() { return Input.GetMouseButton(1); }
#endif
    }
}
