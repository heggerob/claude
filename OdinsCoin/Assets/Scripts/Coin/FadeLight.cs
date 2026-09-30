using UnityEngine;

namespace OdinsCoin
{
    /// <summary>A light that fades out over <see cref="Seconds"/> and is gone.</summary>
    public class FadeLight : MonoBehaviour
    {
        public float Seconds = 1.2f;
        Light light;
        float start, from;

        void Start() { light = GetComponent<Light>(); start = Time.time; from = light != null ? light.intensity : 0f; }

        void Update()
        {
            float t = (Time.time - start) / Seconds;
            if (light != null) light.intensity = Mathf.Lerp(from, 0f, t);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
