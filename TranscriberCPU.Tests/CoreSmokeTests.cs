namespace LocalNetTranscriber.Tests;

public class CoreSmokeTests
{
    [Fact]
    public void Core_Assembly_Loads()
    {
        var assembly = typeof(LocalNetTranscriber.Core.Models.TranscriptionResult).Assembly;
        Assert.NotNull(assembly);
    }
}