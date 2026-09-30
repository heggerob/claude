using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Storms roll in from upwind every few minutes and drift across the sea. Inside one the waves pile up,
    /// the wind howls, fog closes in, rain lashes the deck, lightning flashes and water comes over the side.
    /// Heimdall's Eye keeps the fog back.
    /// </summary>
    public class Storm : MonoBehaviour
    {
        public static Storm Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public const float Radius = 120f, DriftSpeed = 3f, Lifetime = 330f;

        public bool Active { get; private set; }
        public Vector2 Centre { get; private set; }
        /// <summary>0..1 at the player's ship right now.</summary>
        public float Intensity { get; private set; }

        float age, nextStorm = 150f, flash, nextFlash;
        readonly List<Transform> rain = new List<Transform>();
        Transform rainRoot;

        void Awake() { Instance = this; }

        /// <summary>Start a storm this far upwind of a point, so the wind carries it over.</summary>
        public void Brew(Vector3 near, float distance)
        {
            Vector3 up = -Wind.Direction;
            Centre = new Vector2(near.x + up.x * distance, near.z + up.z * distance);
            Active = true;
            age = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.Ship == null) return;
            Vector3 shipPos = boot.Ship.transform.position;

            if (!Active)
            {
                nextStorm -= dt;
                // Never brew one over the home fjord.
                if (nextStorm <= 0f && Vector2.Distance(new Vector2(shipPos.x, shipPos.z), HomeHarbour.CentreNow) > 140f)
                {
                    Brew(shipPos, Radius + 80f);
                    nextStorm = Random.Range(240f, 420f);
                }
            }
            else
            {
                age += dt;
                Centre += new Vector2(Wind.Direction.x, Wind.Direction.z) * DriftSpeed * dt;
                if (age > Lifetime) Active = false;
            }

            // Grow in, blow out.
            float life = Active ? Mathf.Clamp01(age / 30f) * Mathf.Clamp01((Lifetime - age) / 40f) : 0f;
            float target = Active ? SeaMath.StormIntensity(Centre, Radius, new Vector2(shipPos.x, shipPos.z)) * life : 0f;
            Intensity = Mathf.MoveTowards(Intensity, target, dt * 0.15f);

            Apply(boot, dt);
            if (boot.Ship.PlayerShip) boot.Ship.Hull.Tick(dt, Intensity);
        }

        void Apply(GameBootstrap boot, float dt)
        {
            float i = Intensity;
            Waves.Roughness = 1f + 1.3f * i;
            if (i > 0.3f) Wind.Set(Wind.Angle + Mathf.Sin(Time.time * 0.3f) * 20f * dt, 0.8f + 0.2f * i, 2f);

            // Fog and gloom. Heimdall's Eye sees through it.
            bool heimdall = Fortune.Current.Has(FateEffect.SeeThroughFog, FateKind.Blessing);
            RenderSettings.fogEndDistance = Mathf.Lerp(GameBootstrap.FogEnd, heimdall ? 170f : 55f, i);
            RenderSettings.fogStartDistance = Mathf.Lerp(GameBootstrap.FogStart, heimdall ? 30f : 5f, i);
            Color gloom = Color.Lerp(GameBootstrap.SkyColor, new Color(0.22f, 0.25f, 0.28f), i);

            // Lightning: a white flash now and then in the heart of the storm.
            if (i > 0.55f)
            {
                nextFlash -= dt;
                if (nextFlash <= 0f) { flash = 1f; nextFlash = Random.Range(4f, 11f); Sfx.Play(SfxId.Thunder, 0.5f + 0.5f * i); }
            }
            flash = Mathf.MoveTowards(flash, 0f, dt * 7f);
            gloom = Color.Lerp(gloom, new Color(0.9f, 0.92f, 1f), flash * 0.8f);
            RenderSettings.fogColor = gloom;
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = gloom;
            if (boot.Sun != null) boot.Sun.intensity = Mathf.Lerp(GameBootstrap.SunIntensity, 0.3f, i) + flash * 2.5f;
            RenderSettings.ambientLight = Color.Lerp(GameBootstrap.AmbientColor, new Color(0.25f, 0.28f, 0.32f), i) + Color.white * flash * 0.4f;

            Rain(cam, i, dt);
        }

        void Rain(Camera cam, float i, float dt)
        {
            if (cam == null) return;
            if (rainRoot == null)
            {
                rainRoot = new GameObject("Rain").transform;
                rainRoot.SetParent(transform, false);
                for (int k = 0; k < 140; k++)
                {
                    var drop = LongshipBuilder.Deco(PrimitiveType.Cube, rainRoot, Vector3.zero, new Vector3(0.02f, 0.7f, 0.02f), new Color(0.7f, 0.75f, 0.85f));
                    drop.gameObject.SetActive(false);
                    rain.Add(drop);
                }
            }
            int show = Mathf.RoundToInt(rain.Count * Mathf.Clamp01((i - 0.1f) / 0.6f));
            Vector3 c = cam.transform.position;
            Vector3 slant = new Vector3(Wind.Direction.x, 0f, Wind.Direction.z) * 6f;
            for (int k = 0; k < rain.Count; k++)
            {
                var d = rain[k];
                bool on = k < show;
                if (d.gameObject.activeSelf != on) d.gameObject.SetActive(on);
                if (!on) continue;
                Vector3 p = d.position + (Vector3.down * 22f + slant) * dt;
                // Recycle drops that fell out of the box around the camera.
                if (p.y < c.y - 6f || (p - c).sqrMagnitude > 20f * 20f)
                    p = c + new Vector3(Random.Range(-14f, 14f), Random.Range(4f, 12f), Random.Range(-14f, 14f));
                d.position = p;
                d.rotation = Quaternion.FromToRotation(Vector3.up, -(Vector3.down * 22f + slant).normalized);
            }
        }
    }
}
