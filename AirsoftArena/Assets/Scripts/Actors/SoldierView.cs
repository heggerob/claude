using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// Draws a <see cref="Soldier"/>: layered pixel sprites that turn towards the aim, walking feet,
    /// the held weapon, muzzle flash and the orange dead rag. Only reads the soldier's state, never changes it.
    /// </summary>
    [RequireComponent(typeof(Soldier))]
    public class SoldierView : MonoBehaviour
    {
        Soldier soldier;
        SoldierLook look;
        Transform rig;
        SpriteRenderer body, gear, armband, headGear, goggles, gun, rag, leftFoot, rightFoot, flash;
        PixelArt.GunArt gunArt;
        Vector3 lastPosition;
        float walkPhase;
        float flashUntil;

        public SoldierLook Look { get { return look; } }

        public void Build(Soldier owner, SoldierLook soldierLook)
        {
            soldier = owner;
            look = soldierLook ?? new SoldierLook();
            lastPosition = transform.position;

            Layer("Shadow", transform, PixelArt.Shadow, Color.white, 4, new Vector2(0.05f, -0.08f));

            rig = new GameObject("Rig").transform;
            rig.SetParent(transform, false);

            leftFoot = Layer("Left Boot", rig, PixelArt.Foot, Color.white, 8, new Vector2(0f, 0.16f));
            rightFoot = Layer("Right Boot", rig, PixelArt.Foot, Color.white, 8, new Vector2(0f, -0.16f));
            body = Layer("Body", rig, PixelArt.Body(look.camo), look.uniform, 10, Vector2.zero);
            gear = Layer("Gear", rig, PixelArt.Gear, Color.white, 11, Vector2.zero);
            armband = Layer("Armband", rig, PixelArt.Armband, Teams.Color(soldier.Team), 12, Vector2.zero);
            gun = Layer("Gun", rig, null, Color.white, 13, Vector2.zero);
            headGear = Layer("Head Gear", rig, PixelArt.HeadGear(look.headGear), Teams.Color(soldier.Team), 14, Vector2.zero);
            goggles = Layer("Goggles", rig, PixelArt.Details, Color.white, 15, Vector2.zero);
            flash = Layer("Muzzle Flash", rig, SpriteFactory.SmallCircle, new Color(1f, 0.85f, 0.4f), 16, Vector2.zero);
            flash.enabled = false;

            rag = Layer("Dead Rag", transform, SpriteFactory.Pixel, new Color(1f, 0.45f, 0.05f), 17, new Vector2(0f, 0.6f));
            rag.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            rag.enabled = false;

            RefreshGun();
        }

        public void RefreshGun()
        {
            gunArt = PixelArt.Gun(soldier.Weapon.Data);
            gun.sprite = gunArt.sprite;
            gun.transform.localPosition = gunArt.anchor;
        }

        /// <summary>World position of the barrel tip, where BBs leave.</summary>
        public Vector2 MuzzlePosition
        {
            get
            {
                Vector2 a = soldier.AimDirection;
                Vector2 side = new Vector2(-a.y, a.x);
                float scale = rig.localScale.x;
                return soldier.Position + (a * gunArt.muzzleDistance + side * gunArt.anchor.y) * scale;
            }
        }

        public void OnFired()
        {
            if (soldier.Weapon.Data.IsMelee) return;
            flash.transform.localPosition = new Vector2(gunArt.muzzleDistance + 0.08f, gunArt.anchor.y);
            flash.transform.localScale = Vector3.one * Random.Range(0.45f, 0.7f);
            flash.enabled = true;
            flashUntil = Time.time + 0.035f;
        }

        void LateUpdate()
        {
            if (soldier == null) return;
            float dt = Time.deltaTime;
            float angle = Mathf.Atan2(soldier.AimDirection.y, soldier.AimDirection.x) * Mathf.Rad2Deg;
            rig.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Feet shuffle forward/back in step with how fast we move.
            Vector3 pos = transform.position;
            float speed = dt > 0f ? (pos - lastPosition).magnitude / dt : 0f;
            lastPosition = pos;
            walkPhase += speed * dt * 5.5f;
            float step = speed > 0.3f ? Mathf.Sin(walkPhase) * 0.13f : 0f;
            leftFoot.transform.localPosition = new Vector2(step, 0.16f);
            rightFoot.transform.localPosition = new Vector2(-step, -0.16f);

            bool inPlay = soldier.InPlay;
            float s = soldier.Crouching ? 0.86f : 1f;
            rig.localScale = new Vector3(s, s, 1f);
            gun.enabled = inPlay;
            if (Time.time >= flashUntil) flash.enabled = false;

            float alpha = inPlay ? 1f : 0.55f;
            body.color = WithAlpha(look.uniform, alpha);
            gear.color = WithAlpha(Color.white, alpha);
            armband.color = WithAlpha(Teams.Color(soldier.Team), alpha);
            headGear.color = WithAlpha(Teams.Color(soldier.Team), alpha);
            goggles.color = WithAlpha(Color.white, alpha);
            rag.enabled = !inPlay && Mathf.Repeat(Time.time * 3f, 1f) < 0.7f;
        }

        static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        static SpriteRenderer Layer(string name, Transform parent, Sprite sprite, Color color, int order, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
