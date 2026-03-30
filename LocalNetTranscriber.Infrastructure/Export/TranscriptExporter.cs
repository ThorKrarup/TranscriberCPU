using System.Text;
using LocalNetTranscriber.Core.Interfaces;
using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.Infrastructure.Export;

public class TranscriptExporter : ITranscriptExporter
{
    public string Render(TranscriptionResult result, ExportFormat format) => format switch
    {
        ExportFormat.PlainText => RenderPlainText(result),
        ExportFormat.Markdown  => RenderMarkdown(result),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    private static string RenderPlainText(TranscriptionResult result)
    {
        if (result.Segments is { Count: > 0 })
        {
            var sb = new StringBuilder();
            foreach (var seg in result.Segments)
            {
                sb.AppendLine($"{seg.SpeakerId}  {FormatTime(seg.Start)} – {FormatTime(seg.End)}");
                if (!string.IsNullOrEmpty(seg.Text))
                    sb.AppendLine(seg.Text);
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd();
        }

        return result.Text;
    }

    private static string RenderMarkdown(TranscriptionResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Transcript");
        sb.AppendLine();
        sb.AppendLine($"*Duration: {result.Duration:hh\\:mm\\:ss} · Language: {result.Language}*");
        sb.AppendLine();

        if (result.Segments is { Count: > 0 })
        {
            foreach (var seg in result.Segments)
            {
                sb.AppendLine($"## {seg.SpeakerId}");
                sb.AppendLine();
                sb.AppendLine($"*{FormatTime(seg.Start)} – {FormatTime(seg.End)}*");
                sb.AppendLine();
                if (!string.IsNullOrEmpty(seg.Text))
                {
                    sb.AppendLine(seg.Text);
                    sb.AppendLine();
                }
            }
        }
        else if (result.TimedSegments is { Count: > 0 })
        {
            foreach (var seg in result.TimedSegments)
            {
                if (!string.IsNullOrWhiteSpace(seg.Text))
                {
                    sb.AppendLine(seg.Text.Trim());
                    sb.AppendLine();
                }
            }
        }
        else
        {
            sb.AppendLine(result.Text);
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatTime(TimeSpan ts) =>
        $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
}
