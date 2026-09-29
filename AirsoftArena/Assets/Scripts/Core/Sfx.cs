using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// Plays the generated sound effects. Sounds in the world get quieter with distance from the camera
    /// and pan left/right by where they happen. No AudioListener setup needed beyond the camera's default.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        const int Voices = 24;
        const float HearingRange = 30f;
        const int MaxPerFramePerSound = 3;

        readonly Dictionary<SfxId, AudioClip> clips = new Dictionary<SfxId, AudioClip>();
        readonly Dictionary<SfxId, int> playedThisFrame = new Dictionary<SfxId, int>();
        readonly List<AudioSource> voices = new List<AudioSource>();
        int nextVoice;
        int frame = -1;
        Camera cam;

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
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                voices.Add(source);
            }
            if (FindListener() == null && Camera.main != null) Camera.main.gameObject.AddComponent<AudioListener>();
        }

        static AudioListener FindListener()
        {
            var cam = Camera.main;
            return cam != null ? cam.GetComponent<AudioListener>() : null;
        }

        /// <summary>A sound with no position (UI, jingles).</summary>
        public static void Play(SfxId id, float volume = 1f, float pitch = 1f)
        {
            if (Instance != null) Instance.PlayInternal(id, volume, pitch, 0f);
        }

        /// <summary>A sound in the world: fades with distance from the camera and pans by side.</summary>
        public static void PlayAt(SfxId id, Vector2 position, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (Instance == null) return;
            if (Instance.cam == null) Instance.cam = Camera.main;
            float pan = 0f;
            if (Instance.cam != null)
            {
                Vector2 listener = Instance.cam.transform.position;
                float distance = Vector2.Distance(listener, position);
                if (distance > HearingRange) return;
                volume *= 1f - distance / HearingRange;
                pan = Mathf.Clamp((position.x - listener.x) / 12f, -0.8f, 0.8f);
            }
            Instance.PlayInternal(id, volume, 1f + Random.Range(-pitchJitter, pitchJitter), pan);
        }

        public static SfxId ShotSound(PowerSystem power)
        {
            switch (power)
            {
                case PowerSystem.Gas: return SfxId.ShotGas;
                case PowerSystem.Spring: return SfxId.ShotSpring;
                case PowerSystem.CO2: return SfxId.ShotCO2;
                default: return SfxId.ShotAEG;
            }
        }

        public static SfxId Reveal(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare: return SfxId.RevealRare;
                case Rarity.Epic: return SfxId.RevealEpic;
                case Rarity.Legendary: return SfxId.RevealLegendary;
                default: return SfxId.RevealCommon;
            }
        }

        void PlayInternal(SfxId id, float volume, float pitch, float pan)
        {
            // Many BBs landing in the same frame shouldn't turn into a wall of noise.
            if (frame != Time.frameCount)
            {
                frame = Time.frameCount;
                playedThisFrame.Clear();
            }
            int count;
            playedThisFrame.TryGetValue(id, out count);
            if (count >= MaxPerFramePerSound) return;
            playedThisFrame[id] = count + 1;

            volume *= GameSettings.Volume;
            if (volume <= 0.01f) return;
            var source = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Count;
            source.clip = clips[id];
            source.volume = Mathf.Clamp01(volume);
            source.pitch = pitch;
            source.panStereo = pan;
            source.Play();
        }
    }
}
