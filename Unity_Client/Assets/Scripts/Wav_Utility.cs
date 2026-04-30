using System.IO;
using UnityEngine;

// Bu sýnýf AudioClip'i alýp Python'un anlayacaðý .WAV formatýna (byte[]) çevirir.
public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            WriteWavHeader(stream, clip);
            WriteWavData(stream, clip);
            return stream.ToArray();
        }
    }

    private static void WriteWavHeader(MemoryStream stream, AudioClip clip)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        stream.Seek(0, SeekOrigin.Begin);
        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        stream.Write(riff, 0, 4);

        byte[] chunkSize = System.BitConverter.GetBytes(stream.Length - 8);
        stream.Write(chunkSize, 0, 4);

        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        stream.Write(wave, 0, 4);

        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        stream.Write(fmt, 0, 4);

        byte[] subChunk1 = System.BitConverter.GetBytes(16);
        stream.Write(subChunk1, 0, 4);

        ushort one = 1;
        byte[] audioFormat = System.BitConverter.GetBytes(one);
        stream.Write(audioFormat, 0, 2);

        byte[] numChannels = System.BitConverter.GetBytes((ushort)channels);
        stream.Write(numChannels, 0, 2);

        byte[] sampleRate = System.BitConverter.GetBytes(hz);
        stream.Write(sampleRate, 0, 4);

        byte[] byteRate = System.BitConverter.GetBytes(hz * channels * 2);
        stream.Write(byteRate, 0, 4);

        ushort blockAlign = (ushort)(channels * 2);
        stream.Write(System.BitConverter.GetBytes(blockAlign), 0, 2);

        ushort bps = 16;
        byte[] bitsPerSample = System.BitConverter.GetBytes(bps);
        stream.Write(bitsPerSample, 0, 2);

        byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        stream.Write(datastring, 0, 4);

        byte[] subChunk2 = System.BitConverter.GetBytes(samples * channels * 2);
        stream.Write(subChunk2, 0, 4);
    }

    private static void WriteWavData(MemoryStream stream, AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        ushort intData;
        byte[] bytesData;

        for (int i = 0; i < samples.Length; i++)
        {
            // Sesi 16-bit formatýna sýkýþtýr
            intData = (ushort)(samples[i] * 32767);
            bytesData = System.BitConverter.GetBytes(intData);
            stream.Write(bytesData, 0, 2);
        }
    }
}