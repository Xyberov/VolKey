using VolKey.Services;

namespace VolKey.Tests;

public class GestureSequenceTests
{
    [Theory]
    [InlineData("+-", true)]
    [InlineData("+-+", true)]
    [InlineData("--+-", true)]
    [InlineData("++", false)]
    [InlineData("---", false)]
    [InlineData("+", false)]
    [InlineData("+-+-+", false)]
    [InlineData("+ -", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ValidatesSequences(string? sequence, bool expected)
    {
        Assert.Equal(expected, GestureSequence.IsValid(sequence));
    }

    [Fact]
    public void AllContainsEveryValidSequenceOnce()
    {
        // 2 из двух нажатий, 6 из трёх и 14 из четырёх: все варианты, кроме одинаковых нажатий подряд.
        Assert.Equal(22, GestureSequence.All.Count);
        Assert.Equal(GestureSequence.All.Count, GestureSequence.All.Distinct().Count());
        Assert.All(GestureSequence.All, sequence => Assert.True(GestureSequence.IsValid(sequence)));
        Assert.Equal(3, GestureSequence.All[0].Length);
    }

    [Fact]
    public void DisplayUsesTypographicMinus()
    {
        Assert.Equal("+  −  +", GestureSequence.Display("+-+"));
    }

    [Fact]
    public void NormalizeDropsInvalidGesturesAndClampsTimeout()
    {
        var settings = new AppSettings
        {
            GestureTimeoutMs = 0,
            Gestures =
            [
                new("+-+", GestureAction.NextTrack),
                new("+-+", GestureAction.ToggleMute),
                new("++", GestureAction.ToggleMute),
                new("-+", (GestureAction)42)
            ]
        }.Normalize();

        Assert.Equal(AppSettings.MinGestureTimeoutMs, settings.GestureTimeoutMs);
        var gesture = Assert.Single(settings.Gestures);
        Assert.Equal(GestureAction.NextTrack, gesture.Action);
    }

    [Fact]
    public void NormalizeRestoresDefaultsWhenGesturesMissing()
    {
        var settings = new AppSettings { Gestures = null! }.Normalize();

        Assert.Equal(AppSettings.DefaultGestures().Select(g => g.Sequence), settings.Gestures.Select(g => g.Sequence));
    }
}
