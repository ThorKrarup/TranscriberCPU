namespace LocalNetTranscriber.Core.Interfaces;

public interface IFileSaverService
{
    Task<bool> SaveTranscriptAsync(string content, string suggestedExtension, string suggestedFileName = "transcript");
}
