namespace LocalNetTranscriber.Core.Exceptions;

public class UnsupportedAudioFormatException : Exception
{
    public string FilePath { get; }

    public UnsupportedAudioFormatException(string filePath)
        : base($"Unsupported audio format: '{filePath}'")
    {
        FilePath = filePath;
    }

    public UnsupportedAudioFormatException(string filePath, string message)
        : base(message)
    {
        FilePath = filePath;
    }
}
