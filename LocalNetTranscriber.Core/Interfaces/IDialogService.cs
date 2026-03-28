namespace LocalNetTranscriber.Core.Interfaces;

public interface IDialogService
{
    Task ShowErrorAsync(string title, string message);
}
