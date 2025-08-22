using Audiotool.model;
using FFMpegCore;
using System.Collections.ObjectModel;
using System.IO;
using Audiotool.builders;
using Audiotool.Converters;
using System.Windows;

namespace Audiotool.repository;

public class NativeAudioRepo
{
    private readonly List<Audio> AudioFiles = [];

    public async Task AddAudioFile(string path)
    {
        IMediaAnalysis info = await FFProbe.AnalyseAsync(path);
        if (info.PrimaryAudioStream == null)
        {
            throw new Exception("Unable to retrieve primary audio stream");
        }

        string filename = Path.GetFileNameWithoutExtension(path);

        Audio currentAudioFile = AudioFiles.FirstOrDefault(a => a.FileName == filename);

        if (currentAudioFile != null) return;

        Audio audioFile = new()
        {
            Codec = "ADPCM",
            FilePath = path,
            FileName = filename,
            FileExtension = Path.GetExtension(path),
            Samples = (int)Math.Round(info.Duration.TotalSeconds * info.PrimaryAudioStream.SampleRateHz),
            SampleRate = info.PrimaryAudioStream.SampleRateHz,
            Duration = info.Duration,
            Channels = info.PrimaryAudioStream.Channels,
            FileSize = (ulong)new FileInfo(path).Length
        };


        AudioFiles.Add(audioFile);
    }

    public ObservableCollection<Audio> GetAudioFiles() => new(AudioFiles);

    public ObservableCollection<Audio> RemoveAudioFile(string fileName)
    {
        foreach (Audio audio in AudioFiles)
        {
            if (audio.FileName == fileName)
            {
                AudioFiles.Remove(audio);
                break;
            }
        }


        return new ObservableCollection<Audio>(AudioFiles);
    }

    public void ClearAudioFiles()
    {
        AudioFiles.Clear();
    }

    private static void CreateFolders(string path, string dataPath, string audioDirectoryPath, string wavPath)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }

        if (!Directory.Exists(audioDirectoryPath))
        {
            Directory.CreateDirectory(audioDirectoryPath);
        }

        if (!Directory.Exists(wavPath))
        {
            Directory.CreateDirectory(wavPath);
        }
    }

    public void BuildAWC(string SoundSet, string AudioBank, string? folderPath, ObservableCollection<Audio> _newList, string audioDataFileName = "audioexample_sounds", bool debugFiles = true)
    {
        try
        {
            string path = Path.Combine(folderPath ?? AppContext.BaseDirectory, "Renewed-Audio");
            string wavPath = Path.Combine(path, "wav");
            string dataPath = Path.Combine(path, "data");
            string audioDirectoryPath = Path.Combine(path, "audiodirectory");

            CreateFolders(path, dataPath, audioDirectoryPath, wavPath);

            if (debugFiles)
            {
                string clientPath = Path.Combine(path, "client");
                if (!Directory.Exists(clientPath))
                {
                    Directory.CreateDirectory(clientPath);
                }
            }

            WavConverter.ConvertToWav(AudioFiles, wavPath);
            Dat54Builder.ConstructDat54(AudioFiles, path, AudioBank, SoundSet, audioDataFileName);
            AWCBuilder.GenerateXML(AudioFiles, audioDirectoryPath, wavPath, AudioBank);

            LuaBuilder.AwcFileName = AudioBank;
            LuaBuilder.RelFileName = $"{audioDataFileName}.dat54.rel";

            LuaBuilder.GenerateManifest(path, AudioFiles, debugFiles, SoundSet, audioDataFileName);

            MessageBox.Show("Resource has been build!");

            if (!debugFiles)
            {
                CleanupDebugFiles(audioDirectoryPath, dataPath, wavPath, audioDataFileName);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Build failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Build Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void CleanupDebugFiles(string audioDirectoryPath, string dataPath, string wavPath, string audioDataFileName)
    {
        try
        {
            string outputAwcXml = Path.Combine(audioDirectoryPath, "output.awc.xml");
            if (File.Exists(outputAwcXml))
            {
                File.Delete(outputAwcXml);
            }

            string dat54RelXml = Path.Combine(dataPath, $"{audioDataFileName}.dat54.rel.xml");
            if (File.Exists(dat54RelXml))
            {
                File.Delete(dat54RelXml);
            }

            if (Directory.Exists(wavPath))
            {
                Directory.Delete(wavPath, true);
            }

            string awcNametable = Path.Combine(audioDirectoryPath, "awc.nametable");
            if (File.Exists(awcNametable))
            {
                File.Delete(awcNametable);
            }
        }
        catch (Exception)
        {

        }
    }

}
