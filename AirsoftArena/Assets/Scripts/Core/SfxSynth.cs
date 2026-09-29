using System;

namespace AirsoftArena
{
    public enum SfxId
    {
        ShotAEG, ShotGas, ShotSpring, ShotCO2,
        Tak,          // BB hitting a player
        Tick,         // BB hitting cover
        Whistle,      // referee
        MagOut, MagIn,
        DryFire,
        Knife,
        UiClick,
        ReelTick,
        RevealCommon, RevealRare, RevealEpic, RevealLegendary,
        Capture,
        RankUp,
    }

    /// <summary>
    /// Tiny software synthesizer: every sound in the game is generated from code at startup,
    /// so there are no audio files to import. Pure C# (no Unity types) so it can be unit tested.
    /// </summary>
    public static class SfxSynth
    {
        public const int SampleRate = 22050;

        public static float[] Generate(SfxId id)
        {
            var rng = new Random((int)id * 7919 + 17);
            switch (id)
            {
                // AEG: short airy "pff" plus the gearbox clack.
                case SfxId.ShotAEG: return Mix(Noise(rng, 0.07f, 55f, 0.55f, 0.3f), Tone(0.03f, 180f, 60f, 0.35f), Click(rng, 0.012f, 0.3f));
                // Gas blowback: sharper pop and the slide slapping back.
                case SfxId.ShotGas: return Mix(Noise(rng, 0.06f, 70f, 0.8f, 0.15f), Offset(Click(rng, 0.015f, 0.45f), 0.03f));
                // Spring: a deep "thunk" as the piston slams forward.
                case SfxId.ShotSpring: return Mix(Tone(0.09f, 140f, 55f, 0.7f), Noise(rng, 0.05f, 45f, 0.35f, 0.6f));
                case SfxId.ShotCO2: return Mix(Noise(rng, 0.08f, 50f, 0.75f, 0.1f), Tone(0.04f, 260f, 120f, 0.25f));

                // The "tak" of a BB on goggles / plastic: high and very short.
                case SfxId.Tak: return Mix(Tone(0.03f, 3200f, 2600f, 0.6f), Click(rng, 0.006f, 0.5f));
                case SfxId.Tick: return Mix(Tone(0.02f, 1400f, 1100f, 0.25f), Click(rng, 0.005f, 0.2f));

                case SfxId.Whistle: return WhistleSound();
                case SfxId.MagOut: return Mix(Click(rng, 0.01f, 0.5f), Offset(Tone(0.04f, 500f, 300f, 0.2f), 0.01f));
                case SfxId.MagIn: return Mix(Click(rng, 0.012f, 0.7f), Offset(Click(rng, 0.01f, 0.5f), 0.05f));
                case SfxId.DryFire: return Click(rng, 0.008f, 0.35f);
                case SfxId.Knife: return Noise(rng, 0.12f, 30f, 0.35f, 0.9f);

                case SfxId.UiClick: return Tone(0.03f, 900f, 700f, 0.25f);
                case SfxId.ReelTick: return Mix(Tone(0.02f, 1800f, 1500f, 0.2f), Click(rng, 0.004f, 0.15f));
                case SfxId.RevealCommon: return Arpeggio(new[] { 523f, 659f }, 0.09f, 0.3f);
                case SfxId.RevealRare: return Arpeggio(new[] { 523f, 659f, 784f }, 0.09f, 0.35f);
                case SfxId.RevealEpic: return Arpeggio(new[] { 523f, 659f, 784f, 1047f }, 0.08f, 0.4f);
                case SfxId.RevealLegendary: return Arpeggio(new[] { 523f, 659f, 784f, 1047f, 1319f, 1568f }, 0.07f, 0.45f);
                case SfxId.Capture: return Arpeggio(new[] { 392f, 523f, 659f, 784f }, 0.11f, 0.4f);
                case SfxId.RankUp: return Arpeggio(new[] { 392f, 494f, 587f, 784f, 988f }, 0.1f, 0.4f);
            }
            return new float[1];
        }

        // ---------------------------------------------------------------- building blocks

        static int Samples(float seconds) { return Math.Max(1, (int)(seconds * SampleRate)); }

        /// <summary>White noise with an exponential decay, lightly low-passed (smoothing 0..1).</summary>
        static float[] Noise(Random rng, float seconds, float decay, float volume, float smoothing)
        {
            var s = new float[Samples(seconds)];
            float prev = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                prev = prev * smoothing + n * (1f - smoothing);
                s[i] = prev * volume * (float)Math.Exp(-decay * t);
            }
            return s;
        }

        /// <summary>Sine sweep from one pitch to another with a fast decay.</summary>
        static float[] Tone(float seconds, float fromHz, float toHz, float volume)
        {
            var s = new float[Samples(seconds)];
            double phase = 0.0;
            for (int i = 0; i < s.Length; i++)
            {
                float k = i / (float)s.Length;
                double hz = fromHz + (toHz - fromHz) * k;
                phase += 2.0 * Math.PI * hz / SampleRate;
                float env = (1f - k) * (1f - k);
                s[i] = (float)Math.Sin(phase) * volume * env;
            }
            return s;
        }

        static float[] Click(Random rng, float seconds, float volume)
        {
            var s = new float[Samples(seconds)];
            for (int i = 0; i < s.Length; i++)
            {
                float k = i / (float)s.Length;
                s[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * volume * (1f - k) * (1f - k) * (1f - k);
            }
            return s;
        }

        /// <summary>Pea whistle: a high tone with the rattling trill of the pea inside.</summary>
        static float[] WhistleSound()
        {
            float seconds = 0.55f;
            var s = new float[Samples(seconds)];
            double phase = 0.0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                double hz = 2900.0 + 120.0 * Math.Sin(2.0 * Math.PI * 34.0 * t);
                phase += 2.0 * Math.PI * hz / SampleRate;
                float attack = Math.Min(1f, t / 0.02f);
                float release = Math.Min(1f, (seconds - t) / 0.06f);
                float trill = 0.7f + 0.3f * (float)Math.Sin(2.0 * Math.PI * 34.0 * t);
                s[i] = (float)Math.Sin(phase) * 0.4f * attack * release * trill;
            }
            return s;
        }

        /// <summary>Little chiptune jingle: square-ish notes one after another.</summary>
        static float[] Arpeggio(float[] notes, float noteSeconds, float volume)
        {
            int per = Samples(noteSeconds);
            var s = new float[per * notes.Length + Samples(0.15f)];
            for (int n = 0; n < notes.Length; n++)
            {
                bool last = n == notes.Length - 1;
                int length = last ? s.Length - per * n : per;
                double phase = 0.0;
                for (int i = 0; i < length; i++)
                {
                    phase += 2.0 * Math.PI * notes[n] / SampleRate;
                    float k = i / (float)length;
                    float env = last ? (1f - k) : (1f - 0.5f * k);
                    // Soft square: sine plus a bit of its third harmonic.
                    float v = (float)(Math.Sin(phase) + 0.3 * Math.Sin(3.0 * phase));
                    s[per * n + i] += v * volume * 0.75f * env;
                }
            }
            return s;
        }

        static float[] Offset(float[] sound, float seconds)
        {
            var s = new float[sound.Length + Samples(seconds)];
            Array.Copy(sound, 0, s, Samples(seconds), sound.Length);
            return s;
        }

        /// <summary>Adds sounds together and keeps the result inside -1..1.</summary>
        static float[] Mix(params float[][] sounds)
        {
            int length = 0;
            foreach (var x in sounds) length = Math.Max(length, x.Length);
            var s = new float[length];
            foreach (var x in sounds)
                for (int i = 0; i < x.Length; i++) s[i] += x[i];
            float peak = 0f;
            foreach (var v in s) peak = Math.Max(peak, Math.Abs(v));
            if (peak > 0.95f)
                for (int i = 0; i < s.Length; i++) s[i] *= 0.95f / peak;
            return s;
        }
    }
}
