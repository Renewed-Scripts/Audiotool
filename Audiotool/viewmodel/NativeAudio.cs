using Microsoft.Win32;
using Audiotool.repository;
using System.Collections.ObjectModel;
using Audiotool.model;
using System.Windows;
using System.Text.Json;
using System.IO;

namespace Audiotool.viewmodel;

public class NativeAudio : ViewModelBase
{

    private ObservableCollection<Audio> _audioFiles;

    public ObservableCollection<Audio> AudioFiles
    {
        get { 
            return _audioFiles; 
        }
        set { 
            _audioFiles = value;
            OnPropertyChanged();
        }
    }

    private Audio _selectedAudio;

    public Audio SelectedAudio
    {
        get { return _selectedAudio; }
        set { _selectedAudio = value; }
    }

    private string _soundSetName;

    public string SoundSetName
    {
        get { 
            return _soundSetName; 
        }
        set {
            _soundSetName = value;
            OnPropertyChanged();
        }
    }

    private string _audioBankName;

    public string AudioBankName
    {
        get
        {
            return _audioBankName;
        }
        set
        {
            _audioBankName = value;
            OnPropertyChanged();
        }
    }

    private string _outputPath;

    public string OutputPath
    {
        get { return _outputPath; }
        set { 
            _outputPath = value;
            OnPropertyChanged();
        }
    }

    private string _audioDataFileName;

    public string AudioDataFileName
    {
        get
        {
            return _audioDataFileName;
        }
        set
        {
            _audioDataFileName = value;
            OnPropertyChanged();
        }
    }

    private string _outputAudioName;
    public string OutputAudioName
    {
        get { return _outputAudioName; }
        set
        {
            _outputAudioName = value;
            OnPropertyChanged();
        }
    }

    private bool _debugFiles;

    public bool DebugFiles
    {
        get { return _debugFiles; }
        set
        {
            _debugFiles = value;
            OnPropertyChanged();
        }
    }

    private string _lastLoadedSettingsPath;
    public string LastLoadedSettingsPath
    {
        get { return _lastLoadedSettingsPath; }
        set
        {
            _lastLoadedSettingsPath = value;
            OnPropertyChanged();
        }
    }

    private readonly NativeAudioRepo _repo;

    public RelayCommand AddFilesCommand => new(execute => SelectAudioFiles(), canExecute => true);
    public RelayCommand DeleteCommand => new(execute => RemoveAudioFile(), canExecute => SelectedAudio != null);
    public RelayCommand ExportCommand => new(execute => _repo.BuildAWC(SoundSetName, AudioBankName, OutputPath, AudioFiles, AudioDataFileName, DebugFiles, OutputAudioName), canExecute => AudioFiles != null && AudioFiles.Count > 0);
    public RelayCommand OutputFolderCommand => new(execute => SetOutputFolder(), canExecute => true);
    public RelayCommand SaveSettingsCommand => new(execute => SaveSettings(), canExecute => AudioFiles != null && AudioFiles.Count > 0);
    public RelayCommand LoadSettingsCommand => new(execute => LoadSettings(), canExecute => true);

    private void SetOutputFolder()
    {
        var dialog = new OpenFolderDialog();
        var result = dialog.ShowDialog();

        if (result == true)
        {
            OutputPath = dialog.FolderName;
        }
    }

    private void RemoveAudioFile() => AudioFiles = _repo.RemoveAudioFile(SelectedAudio.FileName);

    private void SelectAudioFiles()
    {
        OpenFileDialog dialog = new()
        {
            Multiselect = true
        };

        if (dialog.ShowDialog() != true || dialog.FileNames.Length <= 0) return;
        
        
        foreach (string path in dialog.FileNames)
        {
            string extension = Path.GetExtension(path).ToLower();

            if (extension != ".wav" && extension != ".mp3")
            {
                MessageBox.Show($"Unsupported file format: {extension}. Only .wav and .mp3 files are allowed.", "Unsupported Format", MessageBoxButton.OK, MessageBoxImage.Warning);
                continue;
            }

            try
            {
                Task.Run(async () => await _repo.AddAudioFile(path)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding file '{Path.GetFileName(path)}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        AudioFiles = _repo.GetAudioFiles();
    }


    public NativeAudio()
    {
        SoundSetName = "special_soundset";
        AudioBankName = "custom_sounds";
        AudioDataFileName = "audioexample_sounds";
        OutputAudioName = "Renewed-Audio";
        DebugFiles = true;
        LastLoadedSettingsPath = "";
        _repo = new NativeAudioRepo();
        AudioFiles = _repo.GetAudioFiles();
    }

    private void SaveSettings()
    {
        string saveFilePath = null;

        if (!string.IsNullOrEmpty(LastLoadedSettingsPath) && File.Exists(LastLoadedSettingsPath))
        {
            var result = MessageBox.Show(
                $"Overwrite the original settings file?\n\n{Path.GetFileName(LastLoadedSettingsPath)}\n\nChoose 'No' to save to a new location.",
                "Save Settings",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
                saveFilePath = LastLoadedSettingsPath;
        }

        if (string.IsNullOrEmpty(saveFilePath))
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = "audiotool_settings.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                saveFilePath = saveDialog.FileName;
            }
        }

        if (!string.IsNullOrEmpty(saveFilePath))
        {
            try
            {
                var settings = new BuildSettings
                {
                    SoundSetName = SoundSetName,
                    AudioBankName = AudioBankName,
                    AudioDataFileName = AudioDataFileName,
                    OutputPath = OutputPath ?? "",
                    OutputAudioName = OutputAudioName,
                    AudioFiles = AudioFiles?.Select(audio => new AudioFileSettings
                    {
                        FilePath = audio.FilePath,
                        FileName = audio.FileName,
                        FileExtension = audio.FileExtension,
                        Volume = audio.Volume,
                        Headroom = audio.Headroom,
                        PlayBegin = audio.PlayBegin,
                        PlayEnd = audio.PlayEnd,
                        LoopBegin = audio.LoopBegin,
                        LoopEnd = audio.LoopEnd,
                        LoopPoint = audio.LoopPoint,
                        Peak = audio.Peak
                    }).ToList() ?? new List<AudioFileSettings>()
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var jsonString = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(saveFilePath, jsonString);

                LastLoadedSettingsPath = saveFilePath;

                MessageBox.Show($"Settings saved successfully!\n\n{Path.GetFileName(saveFilePath)}", "Save Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadSettings()
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json"
        };

        if (openDialog.ShowDialog() == true)
        {
            var result = MessageBox.Show(
                "Loading settings will clear all current audio files and settings. Do you want to continue?",
                "Confirm Load Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
                return;

            try
            {
                var jsonString = File.ReadAllText(openDialog.FileName);
                var settings = JsonSerializer.Deserialize<BuildSettings>(jsonString);

                if (settings != null)
                {
                    LastLoadedSettingsPath = openDialog.FileName;
                    LoadSettingsFromObject(settings);
                }
                else
                {
                    MessageBox.Show("Invalid settings file format.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadSettingsFromObject(BuildSettings settings)
    {
        try
        {
            _repo.ClearAudioFiles();
            AudioFiles = new ObservableCollection<Audio>();

            SoundSetName = settings.SoundSetName ?? "special_soundset";
            AudioBankName = settings.AudioBankName ?? "custom_sounds";
            AudioDataFileName = settings.AudioDataFileName ?? "audioexample_sounds";
            OutputPath = settings.OutputPath ?? "";
            OutputAudioName = settings.OutputAudioName ?? "Renewed-Audio";

            var loadedFiles = 0;
            var missingFiles = new List<string>();

            foreach (var audioFileSettings in settings.AudioFiles)
            {
                if (File.Exists(audioFileSettings.FilePath))
                {
                    try
                    {
                        Task.Run(async () => await _repo.AddAudioFile(audioFileSettings.FilePath)).GetAwaiter().GetResult();
                        loadedFiles++;
                    }
                    catch (Exception ex)
                    {
                        missingFiles.Add($"{audioFileSettings.FileName}: {ex.Message}");
                    }
                }
                else
                {
                    missingFiles.Add($"{audioFileSettings.FileName}: File not found at {audioFileSettings.FilePath}");
                }
            }

            AudioFiles = _repo.GetAudioFiles();

            foreach (var audio in AudioFiles)
            {
                var savedSettings = settings.AudioFiles.FirstOrDefault(s => s.FileName == audio.FileName);
                if (savedSettings != null)
                {
                    audio.Volume = savedSettings.Volume;
                    audio.Headroom = savedSettings.Headroom;
                    audio.PlayBegin = savedSettings.PlayBegin;
                    audio.PlayEnd = savedSettings.PlayEnd;
                    audio.LoopBegin = savedSettings.LoopBegin;
                    audio.LoopEnd = savedSettings.LoopEnd;
                    audio.LoopPoint = savedSettings.LoopPoint;
                    audio.Peak = savedSettings.Peak;
                }
            }

            var message = $"Settings loaded successfully!\nLoaded {loadedFiles} audio files.";

            if (missingFiles.Any())
            {
                message += $"\n\nWarning: {missingFiles.Count} files could not be loaded:\n" + string.Join("\n", missingFiles.Take(5));

                if (missingFiles.Count > 5)
                {
                    message += $"\n... and {missingFiles.Count - 5} more files.";
                }
            }

            MessageBox.Show(message, "Load Settings", MessageBoxButton.OK, missingFiles.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading settings: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
