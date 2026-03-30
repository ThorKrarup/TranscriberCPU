using System.Text.Json;
using LocalNetTranscriber.Core.Interfaces;
using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.UI.Services;

public class JsonSettingsService : ISettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LocalNetTranscriber",
        "settings.json");

    private record SettingsData(
        WhisperModelSize SelectedModelSize = WhisperModelSize.Base,
        bool DiarizationEnabled = false,
        int? KnownSpeakerCount = null,
        ExportFormat SelectedExportFormat = ExportFormat.PlainText);

    public WhisperModelSize SelectedModelSize { get; private set; } = WhisperModelSize.Base;
    public bool DiarizationEnabled { get; private set; } = false;
    public int? KnownSpeakerCount { get; private set; } = null;
    public ExportFormat SelectedExportFormat { get; private set; } = ExportFormat.PlainText;

    public JsonSettingsService()
    {
        Load();
    }

    public void SaveSelectedModelSize(WhisperModelSize size)
    {
        SelectedModelSize = size;
        Save();
    }

    public void SaveDiarizationSettings(bool enabled, int? speakerCount)
    {
        DiarizationEnabled = enabled;
        KnownSpeakerCount = speakerCount;
        Save();
    }

    public void SaveExportFormat(ExportFormat format)
    {
        SelectedExportFormat = format;
        Save();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            var data = JsonSerializer.Deserialize<SettingsData>(json);
            if (data is not null)
            {
                SelectedModelSize = data.SelectedModelSize;
                DiarizationEnabled = data.DiarizationEnabled;
                KnownSpeakerCount = data.KnownSpeakerCount;
                SelectedExportFormat = data.SelectedExportFormat;
            }
        }
        catch { /* ignore corrupt/missing settings */ }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(new SettingsData(SelectedModelSize, DiarizationEnabled, KnownSpeakerCount, SelectedExportFormat));
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* ignore save errors */ }
    }
}
