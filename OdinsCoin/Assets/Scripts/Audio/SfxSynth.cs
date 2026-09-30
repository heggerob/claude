using System;

namespace OdinsCoin
{
    public enum SfxId
    {
        CoinFlip, CoinLand, Blessing, Curse,
        AxeSwing, AxeHit, ShieldBlock, Hurt,
        ArrowWhoosh, ArrowThunk,
        Gold, Chest, Splash, Purchase,
        DiceRattle, DiceLand,
        Thunder, RamCrash, SerpentRoar, WarHorn, Raven,
        UiClick,
        // Loops
        SeaLoop, WindLoop, RainLoop,
    }

    /// <summary>
    /// Every sound in Odin's Coin is generated from code at startup: no audio files to import.
    /// Pure C# (no Unity types) so it can be tested and rendered to WAV outside Unity.
    /// </summary>
    public static class SfxSynth
    {
        public const int SampleRate = 22050;

        public static bool IsLoop(SfxId id) { return id == SfxId.SeaLoop || id == SfxId.WindLoop || id == SfxId.RainLoop; }

        public static float[] Generate(SfxId id)
        {
            var rng = new Random((int)id * 7919 + 101);
            switch (id)
            {
                // Odin's coin: a bright bell-like ring with inharmonic partials, then it tumbles and lands.
                case SfxId.CoinFlip: return Mix(Bell(0.9f, 2350f, 0.35f), Offset(Bell(0.5f, 2600f, 0.15f), 0.08f));
                case SfxId.CoinLand: return Mix(Click(rng, 0.01f, 0.5f), Offset(Bell(0.35f, 3100f, 0.25f), 0.005f), Offset(Click(rng, 0.008f, 0.3f), 0.07f), Offset(Click(rng, 0.006f, 0.2f), 0.12f));
                case SfxId.Blessing: return Arpeggio(new[] { 392f, 494f, 587f, 784f }, 0.12f, 0.35f);
                case SfxId.Curse: return Arpeggio(new[] { 294f, 277f, 233f, 147f }, 0.16f, 0.35f);

                case SfxId.AxeSwing: return Band(rng, 0.22f, 0.4f, 0.75f, true);
                case SfxId.AxeHit: return Mix(Tone(0.12f, 150f, 60f, 0.8f), Noise(rng, 0.09f, 40f, 0.5f, 0.4f));
                case SfxId.ShieldBlock: return Mix(Tone(0.15f, 240f, 200f, 0.6f), Bell(0.25f, 900f, 0.2f), Click(rng, 0.01f, 0.5f));
                case SfxId.Hurt: return Mix(Tone(0.2f, 170f, 110f, 0.5f), Noise(rng, 0.15f, 18f, 0.25f, 0.8f));

                case SfxId.ArrowWhoosh: return Band(rng, 0.35f, 0.15f, 0.5f, false);
                case SfxId.ArrowThunk: return Mix(Tone(0.06f, 420f, 180f, 0.5f), Click(rng, 0.008f, 0.4f));

                case SfxId.Gold: return Coins(rng, 9);
                case SfxId.Chest: return Mix(Tone(0.14f, 110f, 70f, 0.8f), Noise(rng, 0.1f, 30f, 0.3f, 0.7f), Offset(Coins(rng, 3), 0.03f));
                case SfxId.Splash: return Mix(Noise(rng, 0.45f, 7f, 0.55f, 0.55f), Offset(Noise(rng, 0.25f, 14f, 0.3f, 0.85f), 0.08f));
                case SfxId.Purchase: return Mix(Coins(rng, 5), Offset(Arpeggio(new[] { 523f, 784f }, 0.1f, 0.3f), 0.15f));

                case SfxId.DiceRattle: return Rattle(rng, 0.9f, 22);
                case SfxId.DiceLand: return Rattle(rng, 0.3f, 5);

                case SfxId.Thunder: return Thunder(rng);
                case SfxId.RamCrash: return Mix(Noise(rng, 0.5f, 6f, 0.9f, 0.3f), Tone(0.4f, 90f, 40f, 0.9f), Offset(Click(rng, 0.02f, 0.8f), 0.02f));
                case SfxId.SerpentRoar: return Roar(rng);
                case SfxId.WarHorn: return Horn();
                case SfxId.Raven: return Mix(Caw(rng, 0f), Caw(rng, 0.32f));
                case SfxId.UiClick: return Tone(0.03f, 700f, 520f, 0.25f);

                case SfxId.SeaLoop: return Loop(Sea(rng));
                case SfxId.WindLoop: return Loop(Wind(rng));
                case SfxId.RainLoop: return Loop(Rain(rng));
            }
            return new float[1];
        }

        // ---------------------------------------------------------------- building blocks

        static int Samples(float seconds) { return Math.Max(1, (int)(seconds * SampleRate)); }

        static float[] Noise(Random rng, float seconds, float decay, float volume, float smoothing)
        {
            var s = new float[Samples(seconds)];
            float prev = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                prev = prev * smoothing + (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - smoothing);
                s[i] = prev * volume * (float)Math.Exp(-decay * t);
            }
            return s;
        }

        static float[] Tone(float seconds, float fromHz, float toHz, float volume)
        {
            var s = new float[Samples(seconds)];
            double phase = 0.0;
            for (int i = 0; i < s.Length; i++)
            {
                float k = i / (float)s.Length;
                phase += 2.0 * Math.PI * (fromHz + (toHz - fromHz) * k) / SampleRate;
                s[i] = (float)Math.Sin(phase) * volume * (1f - k) * (1f - k);
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

        /// <summary>A struck metal ring: inharmonic partials, each decaying at its own rate.</summary>
        static float[] Bell(float seconds, float hz, float volume)
        {
            float[] ratios = { 1f, 2.76f, 5.4f, 8.93f };
            float[] amps = { 1f, 0.5f, 0.25f, 0.12f };
            var s = new float[Samples(seconds)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float v = 0f;
                for (int p = 0; p < ratios.Length; p++)
                    v += amps[p] * (float)(Math.Sin(2.0 * Math.PI * hz * ratios[p] * t) * Math.Exp(-t * (4f + p * 3f) / seconds));
                s[i] = v * volume * 0.5f;
            }
            return s;
        }

        /// <summary>Band of noise swept up (or down) in brightness: whooshes.</summary>
        static float[] Band(Random rng, float seconds, float volume, float peakAt, bool rising)
        {
            var s = new float[Samples(seconds)];
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float k = i / (float)s.Length;
                float bright = rising ? 0.05f + 0.5f * k : 0.55f - 0.45f * k;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (n - lp) * bright;
                lp2 += (lp - lp2) * bright;
                float env = k < peakAt ? k / peakAt : (1f - k) / (1f - peakAt);
                s[i] = (lp - lp2) * 3f * volume * env * env;
            }
            return s;
        }

        static float[] Coins(Random rng, int count)
        {
            float[] mix = new float[Samples(0.5f)];
            for (int c = 0; c < count; c++)
            {
                var ting = Bell(0.18f, 2800f + (float)rng.NextDouble() * 1800f, 0.18f);
                mix = Mix(mix, Offset(ting, (float)rng.NextDouble() * 0.3f));
            }
            return mix;
        }

        static float[] Rattle(Random rng, float seconds, int hits)
        {
            float[] mix = new float[Samples(seconds)];
            for (int h = 0; h < hits; h++)
            {
                float at = (float)Math.Pow(rng.NextDouble(), 0.7) * seconds * 0.9f;
                var knock = Mix(Tone(0.025f, 900f + (float)rng.NextDouble() * 600f, 500f, 0.3f), Click(rng, 0.006f, 0.35f));
                mix = Mix(mix, Offset(knock, at));
            }
            return mix;
        }

        static float[] Thunder(Random rng)
        {
            // A sharp crack, then a long rolling rumble with random swells.
            var s = new float[Samples(3.2f)];
            float lp = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                lp += ((float)(rng.NextDouble() * 2.0 - 1.0) - lp) * 0.02f;
                float swell = 0.6f + 0.4f * (float)Math.Sin(t * 5.3f + Math.Sin(t * 1.7f) * 2.0);
                s[i] = lp * 6f * swell * (float)Math.Exp(-t * 1.1f);
            }
            return Mix(s, Noise(rng, 0.2f, 20f, 0.7f, 0.2f));
        }

        static float[] Roar(Random rng)
        {
            var s = new float[Samples(2.2f)];
            double phase = 0.0;
            float lp = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float k = t / 2.2f;
                double hz = 55.0 + 25.0 * Math.Sin(Math.PI * k) + 6.0 * Math.Sin(t * 31.0);
                phase += 2.0 * Math.PI * hz / SampleRate;
                // Growl: a buzzy wave, roughed up with noise.
                double buzz = Math.Sin(phase) + 0.5 * Math.Sin(phase * 2.0 + Math.Sin(phase * 0.5) * 3.0) + 0.3 * Math.Sin(phase * 3.0);
                lp += ((float)(rng.NextDouble() * 2.0 - 1.0) - lp) * 0.1f;
                float env = (float)Math.Sin(Math.PI * Math.Min(1.0, k * 1.15));
                s[i] = (float)(buzz * 0.3 + lp * 0.8) * env * 0.7f;
            }
            return s;
        }

        static float[] Horn()
        {
            var s = new float[Samples(1.8f)];
            double phase = 0.0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                double hz = 110.0 * (1.0 + 0.012 * Math.Sin(t * 30.0)) * (t < 0.15 ? 0.94 + t * 0.4 : 1.0);
                phase += 2.0 * Math.PI * hz / SampleRate;
                double saw = 0.0;
                for (int h = 1; h <= 6; h++) saw += Math.Sin(phase * h) / h;
                float env = Math.Min(1f, t / 0.15f) * Math.Min(1f, (1.8f - t) / 0.4f);
                s[i] = (float)saw * 0.35f * env;
            }
            return s;
        }

        static float[] Caw(Random rng, float at)
        {
            var s = new float[Samples(0.26f)];
            double phase = 0.0;
            for (int i = 0; i < s.Length; i++)
            {
                float k = i / (float)s.Length;
                double hz = 780.0 - 260.0 * k;
                phase += 2.0 * Math.PI * hz / SampleRate;
                double nasal = Math.Sin(phase) * 0.5 + Math.Sin(phase * 3.0) * 0.35 + Math.Sin(phase * 5.0) * 0.2;
                float rasp = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.25f;
                s[i] = (float)(nasal + rasp) * (float)Math.Sin(Math.PI * k) * 0.45f;
            }
            return Offset(s, at);
        }

        static float[] Sea(Random rng)
        {
            // Brown-ish noise with slow swells: waves washing along the hull. 8 s, two swells.
            var s = new float[Samples(8f)];
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                lp += ((float)(rng.NextDouble() * 2.0 - 1.0) - lp) * 0.06f;
                lp2 += (lp - lp2) * 0.3f;
                float swell = 0.45f + 0.55f * (float)Math.Pow(0.5 + 0.5 * Math.Sin(t * Math.PI * 2.0 / 4.0), 2.0);
                s[i] = lp2 * 2.2f * swell;
            }
            return s;
        }

        static float[] Wind(Random rng)
        {
            // Band-passed noise whose pitch drifts: a howl. 6 s.
            var s = new float[Samples(6f)];
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float bright = 0.035f + 0.02f * (float)Math.Sin(t * Math.PI * 2.0 / 3.0);
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (n - lp) * bright;
                lp2 += (lp - lp2) * bright;
                float gust = 0.6f + 0.4f * (float)Math.Sin(t * Math.PI * 2.0 / 6.0 + 1.0);
                s[i] = (lp - lp2) * 9f * gust;
            }
            return s;
        }

        static float[] Rain(Random rng)
        {
            // Hiss plus scattered drops. 4 s.
            var s = Noise(rng, 4f, 0f, 0.18f, 0.35f);
            for (int d = 0; d < 260; d++)
            {
                int at = rng.Next(s.Length - 200);
                float v = 0.08f + (float)rng.NextDouble() * 0.15f;
                for (int i = 0; i < 120; i++) s[at + i] += (float)(rng.NextDouble() * 2.0 - 1.0) * v * (1f - i / 120f);
            }
            return s;
        }

        /// <summary>Crossfade a loop's tail into its head so it repeats without a click.</summary>
        static float[] Loop(float[] s)
        {
            int fade = Math.Min(s.Length / 4, Samples(0.4f));
            var o = new float[s.Length - fade];
            for (int i = 0; i < o.Length; i++) o[i] = s[i];
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                o[i] = s[i] * k + s[o.Length + i] * (1f - k);
            }
            // Loops play under everything: normalise them to a steady level.
            float peak = 0f;
            foreach (var v in o) peak = Math.Max(peak, Math.Abs(v));
            if (peak > 0f) for (int i = 0; i < o.Length; i++) o[i] *= 0.7f / peak;
            return o;
        }

        static float[] Arpeggio(float[] notes, float step, float volume)
        {
            var s = new float[Samples(step * notes.Length + 0.35f)];
            for (int n = 0; n < notes.Length; n++)
            {
                int start = Samples(step * n);
                var note = Pluck(notes[n], 0.4f, volume);
                for (int i = 0; i < note.Length && start + i < s.Length; i++) s[start + i] += note[i];
            }
            return s;
        }

        /// <summary>A plucked-lyre note: fundamental plus a couple of harmonics, fast attack, slow decay.</summary>
        static float[] Pluck(float hz, float seconds, float volume)
        {
            var s = new float[Samples(seconds)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                double v = Math.Sin(2 * Math.PI * hz * t) + 0.4 * Math.Sin(4 * Math.PI * hz * t) + 0.15 * Math.Sin(6 * Math.PI * hz * t);
                s[i] = (float)v * volume * 0.6f * (float)Math.Exp(-t * 7f) * Math.Min(1f, t * 400f);
            }
            return s;
        }

        static float[] Offset(float[] s, float seconds)
        {
            int n = Samples(seconds);
            var o = new float[s.Length + n];
            Array.Copy(s, 0, o, n, s.Length);
            return o;
        }

        static float[] Mix(params float[][] parts)
        {
            int len = 0;
            foreach (var p in parts) len = Math.Max(len, p.Length);
            var o = new float[len];
            foreach (var p in parts) for (int i = 0; i < p.Length; i++) o[i] += p[i];
            // Soft clip so stacked sounds never crackle.
            for (int i = 0; i < o.Length; i++) o[i] = (float)Math.Tanh(o[i]);
            return o;
        }
    }
}
