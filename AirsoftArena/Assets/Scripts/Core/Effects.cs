using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Cheap pooled visual feedback: hit flashes, BBs lying on the ground and floating text.</summary>
    public class Effects : MonoBehaviour
    {
        public static Effects Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public class FloatingText
        {
            public Vector2 position;
            public string text;
            public Color color;
            public float born, life;
            public int size;
        }

        class FlashFx { public SpriteRenderer sr; public float born, life; public Color color; }

        const int MaxGroundBBs = 250;

        public readonly List<FloatingText> Texts = new List<FloatingText>();
        readonly List<FlashFx> flashes = new List<FlashFx>();
        readonly List<SpriteRenderer> groundBBs = new List<SpriteRenderer>();
        int nextGroundBB;
        Transform container;

        void Awake()
        {
            Instance = this;
            container = new GameObject("Effects").transform;
            container.SetParent(transform, false);
        }

        public static void Text(Vector2 position, string text, Color color, float life = 1.3f, int size = 14)
        {
            if (Instance == null) return;
            Instance.Texts.Add(new FloatingText { position = position, text = text, color = color, born = Time.time, life = life, size = size });
        }

        public static void Flash(Vector2 position, Color color, float size, float life)
        {
            if (Instance == null) return;
            FlashFx fx = null;
            foreach (var f in Instance.flashes)
            {
                if (!f.sr.enabled) { fx = f; break; }
            }
            if (fx == null)
            {
                fx = new FlashFx { sr = Instance.MakeRenderer("Flash", SpriteFactory.SmallCircle, 25) };
                Instance.flashes.Add(fx);
            }
            fx.sr.enabled = true;
            fx.sr.transform.position = position;
            fx.sr.transform.localScale = Vector3.one * (size / 0.375f);
            fx.sr.color = color;
            fx.color = color;
            fx.born = Time.time;
            fx.life = life;
        }

        /// <summary>A spent BB lying in the grass. Old ones get recycled.</summary>
        public static void GroundDot(Vector2 position)
        {
            if (Instance == null) return;
            var list = Instance.groundBBs;
            SpriteRenderer sr;
            if (list.Count < MaxGroundBBs)
            {
                sr = Instance.MakeRenderer("Spent BB", SpriteFactory.BB, -5);
                sr.color = new Color(0.95f, 0.95f, 0.88f, 0.55f);
                list.Add(sr);
            }
            else
            {
                sr = list[Instance.nextGroundBB];
                Instance.nextGroundBB = (Instance.nextGroundBB + 1) % MaxGroundBBs;
            }
            sr.enabled = true;
            sr.transform.position = position;
        }

        public void ClearAll()
        {
            Texts.Clear();
            foreach (var f in flashes) f.sr.enabled = false;
            foreach (var g in groundBBs) g.enabled = false;
        }

        void Update()
        {
            float now = Time.time;
            for (int i = Texts.Count - 1; i >= 0; i--)
            {
                if (now - Texts[i].born > Texts[i].life) Texts.RemoveAt(i);
            }
            foreach (var f in flashes)
            {
                if (!f.sr.enabled) continue;
                float t = (now - f.born) / f.life;
                if (t >= 1f) { f.sr.enabled = false; continue; }
                var c = f.color;
                c.a = 1f - t;
                f.sr.color = c;
            }
        }

        SpriteRenderer MakeRenderer(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(container, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
