using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Plays the generated sounds: 3D one-shots in the world (they fade with distance and pan with the
    /// camera), flat UI sounds, and the ambience of sea, wind and rain, mixed by the weather.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        const int Voices = 20;

        readonly Dictionary<SfxId, AudioClip> clips = new Dictionary<SfxId, AudioClip>();
        readonly List<AudioSource> voices = new List<AudioSource>();
        AudioSource flat, sea, wind, rain;
        int nextVoice;

        void Awake()
        {
            Instance = this;
            foreach (SfxId id in System.Enum.GetValues(typeof(SfxId)))
            {
                var data = SfxSynth.Generate(id);
                var clip = AudioClip.Create(id.ToString(), data.Length, 1, SfxSynth.SampleRate, false);
                clip.SetData(data, 0);
                clips[id] = clip;
            }
            for (int i = 0; i < Voices; i++)
            {
                var go = new GameObject("Voice " + i);
                go.transform.SetParent(transform, false);
                var v = go.AddComponent<AudioSource>();
                v.playOnAwake = false;
                v.spatialBlend = 1f;
                v.rolloffMode = AudioRolloffMode.Linear;
                v.minDistance = 4f;
                v.maxDistance = 70f;
                v.dopplerLevel = 0f;
                voices.Add(v);
            }
            flat = gameObject.AddComponent<AudioSource>();
            flat.playOnAwake = false;
            flat.spatialBlend = 0f;
            sea = Ambience(SfxId.SeaLoop);
            wind = Ambience(SfxId.WindLoop);
            rain = Ambience(SfxId.RainLoop);
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        }

        AudioSource Ambience(SfxId id)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.clip = clips[id];
            a.loop = true;
            a.spatialBlend = 0f;
            a.volume = 0f;
            a.playOnAwake = false;
            a.Play();
            return a;
        }

        /// <summary>A sound in the world.</summary>
        public static void At(SfxId id, Vector3 position, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (Instance == null) return;
            var v = Instance.voices[Instance.nextVoice];
            Instance.nextVoice = (Instance.nextVoice + 1) % Voices;
            v.transform.position = position;
            v.clip = Instance.clips[id];
            v.volume = volume;
            v.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            v.Play();
        }

        /// <summary>A sound with no place: UI, jingles, the coin's verdict.</summary>
        public static void Play(SfxId id, float volume = 1f)
        {
            if (Instance == null) return;
            Instance.flat.PlayOneShot(Instance.clips[id], volume);
        }

        void Update()
        {
            // The weather sets the mix: the sea is always there, wind with its strength, rain in storms.
            float storm = Storm.Instance != null ? Storm.Instance.Intensity : 0f;
            float dt = Time.unscaledDeltaTime;
            sea.volume = Mathf.MoveTowards(sea.volume, 0.28f + 0.3f * storm, dt * 0.5f);
            sea.pitch = 1f - 0.12f * storm;
            wind.volume = Mathf.MoveTowards(wind.volume, 0.06f + 0.14f * Wind.Strength + 0.4f * storm, dt * 0.5f);
            wind.pitch = 0.9f + 0.25f * Wind.Strength + 0.2f * storm;
            rain.volume = Mathf.MoveTowards(rain.volume, Mathf.Clamp01((storm - 0.15f) * 1.4f) * 0.5f, dt * 0.5f);
        }
    }
}
