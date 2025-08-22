namespace Audiotool.model;

public class BuildSettings
{
    public string SoundSetName { get; set; } = "special_soundset";
    public string AudioBankName { get; set; } = "custom_sounds";
    public string AudioDataFileName { get; set; } = "audioexample_sounds";
    public string OutputPath { get; set; } = "";
    public List<AudioFileSettings> AudioFiles { get; set; } = new List<AudioFileSettings>();
}

public class AudioFileSettings
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string FileExtension { get; set; } = "";
    public int Volume { get; set; } = 100;
    public int Headroom { get; set; } = 0;
    public int PlayBegin { get; set; } = 0;
    public int PlayEnd { get; set; } = 0;
    public int LoopBegin { get; set; } = 0;
    public int LoopEnd { get; set; } = 0;
    public int LoopPoint { get; set; } = 0;
    public int Peak { get; set; } = 0;
}
