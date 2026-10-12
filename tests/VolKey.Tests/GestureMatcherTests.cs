using VolKey.Services;

namespace VolKey.Tests;

public class GestureMatcherTests
{
    private static GestureEvaluation Evaluate(string presses, params GestureBinding[] bindings) =>
        GestureMatcher.Evaluate(GestureSequence.ToKeys(presses), GestureMatcher.Compile(bindings));

    private static readonly GestureBinding Next = new("+-+", GestureAction.NextTrack);
    private static readonly GestureBinding Previous = new("-+-", GestureAction.PreviousTrack);

    [Theory]
    [InlineData("+-+", nameof(GestureAction.NextTrack))]
    [InlineData("-+-", nameof(GestureAction.PreviousTrack))]
    [InlineData("++-+", nameof(GestureAction.NextTrack))]
    [InlineData("-+-+", nameof(GestureAction.NextTrack))]
    public void MatchesGestureAtEndOfPresses(string presses, string expected)
    {
        var result = Evaluate(presses, Next, Previous);

        Assert.Equal(Enum.Parse<GestureAction>(expected), result.Match?.Action);
        Assert.False(result.CanContinue);
    }

    [Theory]
    [InlineData("+")]
    [InlineData("+-")]
    [InlineData("++")]
    [InlineData("+-++")]
    public void NoMatchForIncompleteOrOtherSequences(string presses)
    {
        Assert.Null(Evaluate(presses, Next, Previous).Match);
    }

    [Fact]
    public void LongestGestureWins()
    {
        var result = Evaluate("+-+", new GestureBinding("-+", GestureAction.ToggleMute), Next);

        Assert.Equal(GestureAction.NextTrack, result.Match?.Action);
    }

    [Fact]
    public void ShortGestureWaitsWhenItStartsLongerOne()
    {
        var result = Evaluate("+-", new GestureBinding("+-", GestureAction.PlayPause), Next);

        Assert.Equal(GestureAction.PlayPause, result.Match?.Action);
        Assert.True(result.CanContinue);
    }

    [Fact]
    public void LongerGestureCompletesAfterWaitingShortOne()
    {
        var result = Evaluate("+-+", new GestureBinding("+-", GestureAction.PlayPause), Next);

        Assert.Equal(GestureAction.NextTrack, result.Match?.Action);
        Assert.False(result.CanContinue);
    }

    [Fact]
    public void UnrelatedLongerGestureDoesNotDelayMatch()
    {
        // «+ − +» не является началом «− + − +», поэтому срабатывает сразу.
        var result = Evaluate("+-+", Next, new GestureBinding("-+-+", GestureAction.ToggleMute));

        Assert.Equal(GestureAction.NextTrack, result.Match?.Action);
        Assert.False(result.CanContinue);
    }

    [Fact]
    public void CompileSkipsInvalidAndDuplicateSequences()
    {
        var compiled = GestureMatcher.Compile(
        [
            Next,
            new GestureBinding("+-+", GestureAction.ToggleMute),
            new GestureBinding("+++", GestureAction.ToggleMute),
            new GestureBinding("+x", GestureAction.ToggleMute)
        ]);

        var gesture = Assert.Single(compiled);
        Assert.Equal(GestureAction.NextTrack, gesture.Action);
    }
}
