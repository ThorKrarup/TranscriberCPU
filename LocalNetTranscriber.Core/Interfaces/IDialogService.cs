namespace LocalNetTranscriber.Core.Interfaces;

public interface IDialogService
{
    Task ShowErrorAsync(string title, string message);

    /// <summary>Returns true if the user chooses to proceed, false if they cancel.</summary>
    Task<bool> ShowConfirmAsync(string title, string message);
}
