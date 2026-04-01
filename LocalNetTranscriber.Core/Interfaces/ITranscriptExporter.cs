using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.Core.Interfaces;

public interface ITranscriptExporter
{
    string Render(TranscriptionResult result, ExportFormat format);
    string Render(TranscriptionResult result, ExportFormat format, IReadOnlyDictionary<string, string> speakerNames);
}
