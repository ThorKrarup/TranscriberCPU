namespace LocalNetTranscriber.Core.Exceptions;

public class ModelLoadException : Exception
{
    public string ModelPath { get; }

    public ModelLoadException(string modelPath)
        : base($"Failed to load Whisper model from: '{modelPath}'")
    {
        ModelPath = modelPath;
    }

    public ModelLoadException(string modelPath, string message)
        : base(message)
    {
        ModelPath = modelPath;
    }

    public ModelLoadException(string modelPath, string message, Exception inner)
        : base(message, inner)
    {
        ModelPath = modelPath;
    }
}
