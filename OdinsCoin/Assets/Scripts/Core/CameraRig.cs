using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The camera. First person by default: you look out of your hero's eyes, the mouse turns your head and body,
    /// and your own head is hidden so it never blocks the view (you still see your arms, weapon and shield).
    /// V switches to a third-person orbit camera (scroll zooms), which stays above the waves.
    /// The cursor is locked while playing; menus and the coin/hall screens free it.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(200)]
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public Transform Target;
        public float Distance = 9f;
        public float MinDistance = 3f, MaxDistance = 40f;
        public float Height = 1.6f;
        public float Sensitivity = 2.2f;
        public bool InvertY;

        public const string FirstPersonKey = "OdinsCoin.FirstPerson";
        /// <summary>Looking out of the hero's eyes (the default), or the orbit camera behind them.</summary>
        public bool FirstPerson = true;
        /// <summary>How far up and down you can look in first person (degrees).</summary>
        public const float LookLimit = 80f;
        public const float FirstPersonFov = 75f, ThirdPersonFov = 60f;

        public float Yaw { get; private set; }
        /// <summary>How far the view looks down (degrees; negative looks up).</summary>
        public float Pitch { get { return pitch; } }
        float pitch = 18f;
        Viking player;
        Renderer[] hiddenHead;
        bool headHidden;
        Vector3 smoothedEye;
        // A jolt of the view (degrees of pitch and roll), dying away; the hit marker and the hurt flash, counting down.
        float kick, kickRoll, hitMark, hurtFlash;
        /// <summary>How quickly a jolt dies away (per second), and how long the hit marker and hurt flash last (s).</summary>
        public const float KickDecay = 12f, HitMarkTime = 0.22f, HurtFlashTime = 0.45f;

        /// <summary>Jolt the view: a small one when your blow lands, a bigger one when you're hit.</summary>
        public void Kick(float degrees)
        {
            kick += degrees;
            kickRoll += (Random.value - 0.5f) * degrees;
        }

        /// <summary>Your blow landed: flash the crosshair.</summary>
        public void HitMarker() { hitMark = HitMarkTime; }

        /// <summary>You were hurt: redden the edges of the view and jolt it.</summary>
        public void Hurt(float amount)
        {
            hurtFlash = HurtFlashTime;
            Kick(Mathf.Clamp(amount * 0.12f, 1.5f, 6f));
        }

        /// <summary>What's left of a jolt after <paramref name="dt"/> seconds.</summary>
        public static float Settle(float jolt, float dt) { return jolt * Mathf.Exp(-KickDecay * dt); }
        Vector3 smoothedTarget;
        /// <summary>The coin and hall screens set this to free the cursor and stop mouse-look.</summary>
        public bool CursorFree;
        /// <summary>The title and pause menus set this.</summary>
        public bool MenuOpen;

        /// <summary>Horizontal forward direction of the camera, for movement relative to the view.</summary>
        public Vector3 FlatForward
        {
            get
            {
                var f = transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
            }
        }

        void Awake()
        {
            Instance = this;
            FirstPerson = PlayerPrefs.GetInt(FirstPersonKey, 1) == 1;
        }

        /// <summary>Where the eyes are: up from the neck joint to the middle of the head, and a little forward.</summary>
        public static Vector3 EyePosition(Vector3 neck, Vector3 up, Vector3 forward, float eyeHeight, float scale)
        {
            return neck + up * eyeHeight + forward * 0.06f * scale;
        }

        /// <summary>The pitch after a mouse movement, kept within what each view allows.</summary>
        public static float ClampPitch(float pitch, bool firstPerson)
        {
            return firstPerson ? Mathf.Clamp(pitch, -LookLimit, LookLimit) : Mathf.Clamp(pitch, -10f, 75f);
        }
        void OnEnable() { WorldOrigin.Shifted += OnShift; }
        void OnDisable() { WorldOrigin.Shifted -= OnShift; }

        /// <summary>The floating origin moved everything back: move the camera and its aim with it.</summary>
        void OnShift(Vector3 shift)
        {
            smoothedTarget -= shift;
            smoothedEye -= shift;
            transform.position -= shift;
        }

        /// <summary>Your own head (face, hair, hat) casts its shadow but isn't drawn while you look out of it.</summary>
        void HideHead(bool hide)
        {
            if (player == null || player.Parts == null || player.Parts.head == null) return;
            if (hiddenHead == null) hiddenHead = player.Parts.head.GetComponentsInChildren<Renderer>();
            if (hide == headHidden) return;
            headHidden = hide;
            foreach (var r in hiddenHead)
                if (r != null) r.shadowCastingMode = hide ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
        }

        void LateUpdate()
        {
            bool look = !CursorFree && !MenuOpen;
            Cursor.lockState = look ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !look;

            if (look && GameInput.Pressed(Key.View))
            {
                FirstPerson = !FirstPerson;
                PlayerPrefs.SetInt(FirstPersonKey, FirstPerson ? 1 : 0);
                if (!FirstPerson) pitch = Mathf.Max(pitch, 10f);
            }
            if (look)
            {
                Vector2 d = GameInput.MouseDelta();
                Yaw += d.x * Sensitivity;
                pitch = ClampPitch(pitch - d.y * Sensitivity * (InvertY ? -1f : 1f), FirstPerson);
            }

            if (Target == null) return;
            var found = Target.GetComponent<Viking>();
            if (found != null && found != player) { player = found; hiddenHead = null; headHidden = false; }
            var cam = GetComponent<Camera>();

            // First person: out of the hero's eyes, even at the steering oar. (Not while lying dead on the deck.)
            var combat = player != null ? player.GetComponent<VikingCombat>() : null;
            bool dead = combat != null && combat.Health != null && combat.Health.Dead;
            if (FirstPerson && player != null && player.Parts != null && player.Parts.head != null && !dead)
            {
                HideHead(true);
                var parts = player.Parts;
                var body = player.transform;
                var eye = EyePosition(parts.head.position, body.up, body.forward, parts.eyeHeight, parts.scale);
                // The head bobs with every step: follow it closely, but not every jolt.
                smoothedEye = (smoothedEye - eye).sqrMagnitude > 4f ? eye : Vector3.Lerp(smoothedEye, eye, 1f - Mathf.Exp(-30f * Time.deltaTime));
                transform.position = smoothedEye;
                kick = Settle(kick, Time.deltaTime);
                kickRoll = Settle(kickRoll, Time.deltaTime);
                transform.rotation = Quaternion.Euler(pitch - kick, Yaw, kickRoll);
                if (cam != null) { cam.nearClipPlane = 0.05f; cam.fieldOfView = FirstPersonFov; }
                smoothedTarget = Target.position + Vector3.up * Height;
                return;
            }
            HideHead(false);
            if (cam != null) { cam.nearClipPlane = 0.3f; cam.fieldOfView = ThirdPersonFov; }
            if (!MenuOpen) Distance = Mathf.Clamp(Distance - GameInput.Scroll() * 1.5f, MinDistance, MaxDistance);
            if (pitch < -10f) pitch = -10f;
            Vector3 goal = Target.position + Vector3.up * Height;
            smoothedTarget = Vector3.Lerp(smoothedTarget, goal, 1f - Mathf.Exp(-12f * Time.deltaTime));

            Quaternion rot = Quaternion.Euler(pitch, Yaw, 0f);
            Vector3 pos = smoothedTarget - rot * Vector3.forward * Distance;
            // Never dip under the sea surface.
            float water = Waves.Height(pos.x, pos.z) + 0.6f;
            if (pos.y < water) pos.y = water;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(smoothedTarget - pos);
        }

        /// <summary>Turn the view with whatever you stand on (the deck of a turning ship).</summary>
        public void Turn(float degrees) { Yaw += degrees; }

        public void SnapTo(Transform target, float yaw)
        {
            Target = target;
            Yaw = yaw;
            smoothedTarget = target.position + Vector3.up * Height;
            smoothedEye = target.position + Vector3.up * 1.6f;
        }

        /// <summary>A small crosshair in the middle of the screen while you look out of your hero's eyes.</summary>
        void OnGUI()
        {
            hitMark = Mathf.Max(0f, hitMark - Time.deltaTime);
            hurtFlash = Mathf.Max(0f, hurtFlash - Time.deltaTime);
            if (!FirstPerson || CursorFree || MenuOpen || !headHidden) return;
            var old = GUI.color;
            float cx = Screen.width / 2f, cy = Screen.height / 2f;
            if (hurtFlash > 0f)
            {
                // Blood-red at the edges of the view, fading.
                GUI.color = new Color(0.7f, 0.05f, 0.03f, 0.45f * hurtFlash / HurtFlashTime);
                float band = Screen.height * 0.08f;
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, band), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, Screen.height - band, Screen.width, band), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, band, band, Screen.height - band * 2f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width - band, band, band, Screen.height - band * 2f), Texture2D.whiteTexture);
            }
            if (hitMark > 0f)
            {
                // A struck blow: four ticks out on the diagonals.
                GUI.color = new Color(1f, 0.85f, 0.4f, hitMark / HitMarkTime);
                for (int i = 0; i < 4; i++)
                {
                    float sx = (i & 1) == 0 ? -1f : 1f, sy = i < 2 ? -1f : 1f;
                    for (int k = 5; k < 11; k++) GUI.DrawTexture(new Rect(cx + sx * k - 1f, cy + sy * k - 1f, 2f, 2f), Texture2D.whiteTexture);
                }
            }
            GUI.color = new Color(1f, 0.97f, 0.9f, 0.85f);
            GUI.DrawTexture(new Rect(cx - 9f, cy - 1f, 6f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 3f, cy - 1f, 6f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy - 9f, 2f, 6f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy + 3f, 2f, 6f), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
