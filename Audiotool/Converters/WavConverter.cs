using System.IO;
using Audiotool.model;
using FFMpegCore;

namespace Audiotool.Converters;

public static class WavConverter
{
    // CodeWalker's ADPCMCodec.EncodeADPCM sizes its output buffer as data.Length / 4
    // and processes in 8192-byte blocks. A partial tail block writes a 4-byte header
    // past the end of the output buffer → IndexOutOfRangeException, swallowed by
    // AwcFile.ReadXml's catch and surfacing later as a null-ref NRE in BuildPeakChunks.
    // Padding the PCM data chunk to a multiple of 8192 bytes prevents the overflow.
    private const int AdpcmBlockBytes = 8192;

    public static void ConvertToWav(List<Audio> audioFiles, string outputFolder)
    {
        foreach (Audio audio in audioFiles)
        {
            string outputPath = Path.Combine(outputFolder, $"{audio.FileName}.wav");

            if (audio.FileExtension.TrimStart('.').ToLowerInvariant() != "wav")
            {
                FFMpegArguments ff = FFMpegArguments
                    .FromFileInput(audio.FilePath);
                _ = ff.OutputToFile(outputPath, true, opt =>
                {
                    opt.WithAudioSamplingRate(audio.SampleRate)
                        .WithoutMetadata()
                        .WithCustomArgument("-fflags +bitexact -flags:v +bitexact -flags:a +bitexact")
                        .WithAudioCodec("pcm_s16le")
                        .ForceFormat("wav")
                        .UsingMultithreading(true);
                    if (audio.Channels != 1)
                        opt.WithCustomArgument("-ac 1");
                }).ProcessSynchronously();
            }
            else
            {
                File.Copy(audio.FilePath, outputPath, true);
            }

            PadPcmToBlockBoundary(outputPath, AdpcmBlockBytes);

            audio.FileSize = (ulong)new FileInfo(outputPath).Length;

            int actualSamples = ComputeSampleCount(outputPath);
            if (actualSamples > 0)
                audio.Samples = actualSamples;
        }
    }

    private static int ComputeSampleCount(string wavPath)
    {
        byte[] wav = File.ReadAllBytes(wavPath);
        (int dataOffset, int dataLen) = FindDataChunk(wav);
        if (dataOffset < 0) return 0;
        int bitsPerSample = wav.Length >= 36 ? BitConverter.ToInt16(wav, 34) : 16;
        int channels = wav.Length >= 24 ? BitConverter.ToInt16(wav, 22) : 1;
        int bytesPerSample = (bitsPerSample / 8) * Math.Max(channels, 1);
        return bytesPerSample == 0 ? 0 : dataLen / bytesPerSample;
    }

    private static void PadPcmToBlockBoundary(string wavPath, int blockBytes)
    {
        byte[] wav = File.ReadAllBytes(wavPath);
        (int dataTag, int dataLen) = FindDataChunk(wav);
        if (dataTag < 0) return;

        int pad = (blockBytes - (dataLen % blockBytes)) % blockBytes;
        if (pad == 0) return;

        int dataStart = dataTag + 8;
        byte[] padded = new byte[wav.Length + pad];
        Buffer.BlockCopy(wav, 0, padded, 0, dataStart + dataLen);
        // trailing bytes stay zero-initialized (silence)

        int newDataLen = dataLen + pad;
        Buffer.BlockCopy(BitConverter.GetBytes(newDataLen), 0, padded, dataTag + 4, 4);

        int riffLen = BitConverter.ToInt32(wav, 4) + pad;
        Buffer.BlockCopy(BitConverter.GetBytes(riffLen), 0, padded, 4, 4);

        File.WriteAllBytes(wavPath, padded);
    }

    private static (int tagOffset, int dataLength) FindDataChunk(byte[] wav)
    {
        for (int i = 12; i < wav.Length - 8; i++)
        {
            if (wav[i] == 'd' && wav[i + 1] == 'a' && wav[i + 2] == 't' && wav[i + 3] == 'a')
                return (i, BitConverter.ToInt32(wav, i + 4));
        }
        return (-1, 0);
    }
}
