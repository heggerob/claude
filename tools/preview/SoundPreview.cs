// Writes every generated sound effect to a 16-bit mono WAV so they can be listened to without Unity.
using System;
using System.IO;
using AirsoftArena;

public static class SoundPreview
{
    public static void WriteAll(string dir)
    {
        Directory.CreateDirectory(dir);
        foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            Write(Path.Combine(dir, id + ".wav"), SfxSynth.Generate(id), SfxSynth.SampleRate);
    }

    static void Write(string path, float[] samples, int rate)
    {
        using (var w = new BinaryWriter(File.Create(path)))
        {
            int bytes = samples.Length * 2;
            w.Write("RIFF".ToCharArray()); w.Write(36 + bytes); w.Write("WAVE".ToCharArray());
            w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write("data".ToCharArray()); w.Write(bytes);
            foreach (var s in samples) w.Write((short)(Math.Max(-1f, Math.Min(1f, s)) * 32000));
        }
    }
}
