using VolKey.Services;

namespace VolKey.Tests;

public class GestureRecorderTests
{
    [Fact]
    public void FinishesOnMaxLengthWithVolumeBeforeFirstPress()
    {
        using var recorder = new GestureRecorder();
        GestureRecordedEventArgs? result = null;
        recorder.Recorded += (_, e) => result = e;

        recorder.Start(500);
        recorder.Register(VolumeKey.Up, new VolumeState(0.3f, false));
        recorder.Register(VolumeKey.Down, new VolumeState(0.32f, false));
        recorder.Register(VolumeKey.Up, new VolumeState(0.3f, false));
        recorder.Register(VolumeKey.Down, new VolumeState(0.32f, false));

        Assert.NotNull(result);
        Assert.Equal("+-+-", result.Sequence);
        Assert.Equal(new VolumeState(0.3f, false), result.VolumeBefore);
        Assert.False(recorder.IsRecording);
    }

    [Fact]
    public void IgnoresPressesWhenNotRecording()
    {
        using var recorder = new GestureRecorder();
        var raised = false;
        recorder.Recorded += (_, _) => raised = true;

        for (var i = 0; i < GestureSequence.MaxLength; i++)
            recorder.Register(VolumeKey.Up, null);

        Assert.False(raised);
    }

    [Fact]
    public void CancelDiscardsPresses()
    {
        using var recorder = new GestureRecorder();
        GestureRecordedEventArgs? result = null;
        recorder.Recorded += (_, e) => result = e;

        recorder.Start(500);
        recorder.Register(VolumeKey.Up, null);
        recorder.Register(VolumeKey.Up, null);
        recorder.Cancel();
        recorder.Start(500);
        foreach (var key in new[] { VolumeKey.Down, VolumeKey.Up, VolumeKey.Down, VolumeKey.Up })
            recorder.Register(key, null);

        Assert.Equal("-+-+", result?.Sequence);
    }
}
