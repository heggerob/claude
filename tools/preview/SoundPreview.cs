// Writes every generated Odin's Coin sound to a 16-bit mono WAV so it can be listened to without Unity.
using System;
using System.IO;
using OdinsCoin;

public static class SoundPreview
{
    public static void Main(string[] args)
    {
        Directory.CreateDirectory(args[0]);
        foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
        {
            var data = SfxSynth.Generate(id);
            using (var w = new BinaryWriter(File.Create(Path.Combine(args[0], id + ".wav"))))
            {
                int bytes = data.Length * 2;
                w.Write("RIFF".ToCharArray()); w.Write(36 + bytes); w.Write("WAVE".ToCharArray());
                w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(SfxSynth.SampleRate); w.Write(SfxSynth.SampleRate * 2); w.Write((short)2); w.Write((short)16);
                w.Write("data".ToCharArray()); w.Write(bytes);
                foreach (var v in data) w.Write((short)(Math.Max(-1f, Math.Min(1f, v)) * 32000));
            }
        }
        Console.WriteLine("wrote sounds to " + args[0]);
    }
}
