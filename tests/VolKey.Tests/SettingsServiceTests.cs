using VolKey.Services;

namespace VolKey.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void UnknownActionDropsOnlyThatGesture()
    {
        var settings = SettingsService.Parse("""
            {
              "ShowNotifications": false,
              "Gestures": [
                { "Sequence": "+-+", "Action": "NextTrack" },
                { "Sequence": "-+-", "Action": "Stop" },
                { "Sequence": "+-", "Action": 7 }
              ]
            }
            """);

        Assert.False(settings.ShowNotifications);
        var gesture = Assert.Single(settings.Gestures);
        Assert.Equal(GestureAction.NextTrack, gesture.Action);
    }

    [Fact]
    public void SettingsWithoutGesturesGetDefaults()
    {
        var settings = SettingsService.Parse("""{ "GestureTimeoutMs": 700 }""");

        Assert.Equal(700, settings.GestureTimeoutMs);
        Assert.Equal(AppSettings.DefaultGestures().Count, settings.Gestures.Count);
    }

    [Theory]
    [InlineData("\"English\"", "English")]
    [InlineData("\"Russian\"", "Russian")]
    [InlineData("\"Klingon\"", "System")]
    [InlineData("5", "System")]
    public void ReadsLanguageLeniently(string json, string expected)
    {
        var settings = SettingsService.Parse($$"""{ "Language": {{json}}, "GestureTimeoutMs": 700 }""");

        Assert.Equal(expected, settings.Language.ToString());
        Assert.Equal(700, settings.GestureTimeoutMs);
    }

    [Fact]
    public void BrokenJsonFallsBackToDefaults()
    {
        var settings = SettingsService.Parse("{ not json");

        Assert.True(settings.Enabled);
        Assert.Equal(AppSettings.DefaultGestures().Count, settings.Gestures.Count);
    }
}
