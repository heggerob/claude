using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>A steel popper on the range. Falls when hit and pops back up.</summary>
    public class TrainingTarget : MonoBehaviour
    {
        public const float Height = 1.7f;
        public float Distance;
        SpriteRenderer plate;
        float upAt;

        public bool IsUp { get { return Time.time >= upAt; } }

        public static TrainingTarget Create(Transform parent, Vector2 position, float distance)
        {
            var go = new GameObject("Target " + distance + " m");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            var t = go.AddComponent<TrainingTarget>();
            t.Distance = distance;
            t.plate = go.AddComponent<SpriteRenderer>();
            t.plate.sprite = SpriteFactory.Circle;
            t.plate.sortingOrder = 9;
            var stand = new GameObject("Stand").AddComponent<SpriteRenderer>();
            stand.transform.SetParent(go.transform, false);
            stand.sprite = SpriteFactory.Pixel;
            stand.color = new Color(0.25f, 0.22f, 0.2f);
            stand.transform.localScale = new Vector3(0.12f, 0.8f, 1f);
            stand.transform.localPosition = new Vector3(0.3f, 0f, 0f);
            stand.sortingOrder = 8;
            return t;
        }

        public void Knock()
        {
            upAt = Time.time + 1.2f;
        }

        void Update()
        {
            // Orange steel when up, flat dark grey while down.
            plate.color = IsUp ? new Color(1f, 0.55f, 0.15f) : new Color(0.3f, 0.3f, 0.3f, 0.8f);
            transform.localScale = IsUp ? Vector3.one : new Vector3(0.5f, 1f, 1f);
        }
    }

    /// <summary>
    /// Training range: no bots, no referee, no money. Shoot steel at 10–60 m, see where every BB lands,
    /// and try any weapon from the catalogue with Q / E before buying it.
    /// </summary>
    public class TrainingRules : ModeRules
    {
        public static readonly float[] Distances = { 10f, 20f, 30f, 40f, 50f, 60f };

        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public float LongestHit { get; private set; }
        public float FarthestLanding { get; private set; }
        public float LastLanding { get; private set; }
        public readonly Dictionary<float, int> HitsAtDistance = new Dictionary<float, int>();

        int catalogIndex;
        float shooterLine;

        public override void Begin(MatchManager m, Transform parent)
        {
            base.Begin(m, parent);
            var map = MapBuilder.Current;
            shooterLine = map.spawnZones[0].xMax;
            float laneY = map.bounds.center.y;
            foreach (var d in Distances)
            {
                // Two poppers per distance, side by side.
                TrainingTarget.Create(root, new Vector2(shooterLine + d, laneY + 1.6f), d);
                TrainingTarget.Create(root, new Vector2(shooterLine + d, laneY - 1.6f), d);
                var line = Renderer(d + " m", root, SpriteFactory.Pixel, new Color(1f, 1f, 1f, 0.18f), -14);
                line.transform.position = new Vector2(shooterLine + d, laneY);
                line.transform.localScale = new Vector3(0.08f, map.bounds.height - 1f, 1f);
                HitsAtDistance[d] = 0;
            }
            var shooting = Renderer("Shooting Line", root, SpriteFactory.Pixel, new Color(1f, 0.85f, 0.3f, 0.5f), -14);
            shooting.transform.position = new Vector2(shooterLine, laneY);
            shooting.transform.localScale = new Vector3(0.12f, map.bounds.height - 1f, 1f);
            catalogIndex = WeaponCatalog.Primaries.IndexOf(match.PlayerSoldier.Loadout[0].Data);
        }

        /// <summary>Distance labels for the HUD: (world position, text).</summary>
        public IEnumerable<KeyValuePair<Vector2, string>> Labels()
        {
            var map = MapBuilder.Current;
            foreach (var d in Distances)
                yield return new KeyValuePair<Vector2, string>(new Vector2(shooterLine + d, map.bounds.yMax - 1f), d + " m");
        }

        public void OnShot(int bbs) { Shots += bbs; }

        public void OnTargetHit(TrainingTarget t)
        {
            if (!t.IsUp) return;
            t.Knock();
            Hits++;
            HitsAtDistance[t.Distance]++;
            LongestHit = Mathf.Max(LongestHit, t.Distance);
            Sfx.PlayAt(SfxId.Tak, t.transform.position, 1f, 0.05f);
            Effects.Text((Vector2)t.transform.position + new Vector2(0f, 1f), "DING  " + t.Distance + " m", new Color(1f, 0.85f, 0.3f), 1f, 14);
        }

        public void OnBBLanded(Vector2 point)
        {
            float d = Mathf.Max(0f, point.x - shooterLine);
            LastLanding = d;
            FarthestLanding = Mathf.Max(FarthestLanding, d);
        }

        public override void Tick(float dt)
        {
            var player = match.PlayerSoldier;
            if (player == null) return;
            // Try any primary from the catalogue, owned or not.
            int step = GameInput.Pressed(GameKey.NextWeapon) ? 1 : GameInput.Pressed(GameKey.PrevWeapon) ? -1 : 0;
            if (step != 0)
            {
                var list = WeaponCatalog.Primaries;
                catalogIndex = (catalogIndex + step + list.Count) % list.Count;
                player.SetWeapon(0, list[catalogIndex]);
                player.SwitchSlot(0);
                Sfx.Play(SfxId.MagIn, 0.6f);
            }
            // Unlimited ammo on the range.
            foreach (var w in player.Loadout)
                if (w.SpareMags == 0 && !w.IsReloading) w.Refill();
        }

        public override string PlayerHint(Soldier s)
        {
            return "TRAINING RANGE  ·  Q / E: try any primary  ·  Esc: leave";
        }
    }
}
