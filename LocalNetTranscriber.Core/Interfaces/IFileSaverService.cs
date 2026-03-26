namespace LocalNetTranscriber.Core.Interfaces;

public interface IFileSaverService
{
    Task<bool> SaveTranscriptAsync(string text);
}
