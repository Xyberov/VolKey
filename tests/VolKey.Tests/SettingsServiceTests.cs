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

    [Fact]
    public void BrokenJsonFallsBackToDefaults()
    {
        var settings = SettingsService.Parse("{ not json");

        Assert.True(settings.Enabled);
        Assert.Equal(AppSettings.DefaultGestures().Count, settings.Gestures.Count);
    }
}
