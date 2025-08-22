using Microsoft.Win32;
using Audiotool.repository;
using System.Collections.ObjectModel;
using Audiotool.model;
using System.Windows;

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

    private readonly NativeAudioRepo _repo;

    public RelayCommand AddFilesCommand => new(execute => SelectAudioFiles(), canExecute => true);

    public RelayCommand DeleteCommand => new(execute => RemoveAudioFile(), canExecute => SelectedAudio != null);

    public RelayCommand ExportCommand => new(execute => _repo.BuildAWC(SoundSetName, AudioBankName, OutputPath, AudioFiles, AudioDataFileName), canExecute => AudioFiles != null && AudioFiles.Count > 0);

    public RelayCommand OutputFolderCommand => new(execute => SetOutputFolder(), canExecute => true);


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
            Task.Run(async () => await _repo.AddAudioFile(path)).GetAwaiter().GetResult();
        }

        AudioFiles = _repo.GetAudioFiles();
    }


    public NativeAudio()
    {
        SoundSetName = "special_soundset";
        AudioBankName = "custom_sounds";
        AudioDataFileName = "audioexample_sounds";
        _repo = new NativeAudioRepo();
        AudioFiles = _repo.GetAudioFiles();
    }
}
