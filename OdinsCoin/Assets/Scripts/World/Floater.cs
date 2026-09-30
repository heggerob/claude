using UnityEngine;

namespace OdinsCoin
{
    /// <summary>Simple kinematic bobbing for small things on the sea: barrels, crates, wreckage.</summary>
    public class Floater : MonoBehaviour
    {
        public float sink = 0.2f;
        public float tiltAmount = 0.8f;
        public float drift = 0.3f;
        Vector3 driftDir;

        void Start()
        {
            driftDir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
        }

        void Update()
        {
            var p = transform.position + driftDir * drift * Time.deltaTime;
            p.y = Waves.Height(p.x, p.z) - sink;
            transform.position = p;
            Vector3 n = Vector3.Slerp(Vector3.up, Waves.Normal(p.x, p.z), tiltAmount);
            transform.rotation = Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(0f, Time.time * 5f, 0f);
        }
    }
}
