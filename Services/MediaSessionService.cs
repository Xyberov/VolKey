using Windows.Media.Control;

namespace VolKey.Services;

internal sealed record TrackInfo(string Title, string Artist, bool IsPlaying)
{
    public string Display => string.IsNullOrEmpty(Artist) ? Title : $"{Artist} — {Title}";

    public bool IsSameTrack(TrackInfo other) => Title == other.Title && Artist == other.Artist;
}

/// <summary>
/// Читает, что сейчас играет, через системные медиасессии Windows (то же, что показывает
/// всплывающее окно громкости). Плеерами по-прежнему управляют медиаклавиши — они работают и
/// с плеерами, которые не регистрируют медиасессию.
/// </summary>
internal sealed class MediaSessionService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;

    public async Task<TrackInfo?> GetCurrentTrackAsync()
    {
        try
        {
            var session = await GetSessionAsync();
            if (session is null) return null;
            var properties = await session.TryGetMediaPropertiesAsync();
            if (string.IsNullOrWhiteSpace(properties?.Title)) return null;
            var isPlaying = session.GetPlaybackInfo()?.PlaybackStatus ==
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            return new TrackInfo(properties.Title.Trim(), properties.Artist?.Trim() ?? "", isPlaying);
        }
        catch (Exception)
        {
            // Медиасессии отдают сторонние плееры, и ошибки бывают любые — трек просто не покажем.
            return null;
        }
    }

    public async Task<Image?> GetCoverAsync()
    {
        try
        {
            var session = await GetSessionAsync();
            if (session is null) return null;
            var properties = await session.TryGetMediaPropertiesAsync();
            if (properties?.Thumbnail is null) return null;

            using var stream = (await properties.Thumbnail.OpenReadAsync()).AsStreamForRead();
            var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            buffer.Position = 0;
            using var image = Image.FromStream(buffer);
            return new Bitmap(image);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Ждёт, пока плеер применит команду (сменит трек или паузу). По таймауту возвращает
    /// текущее состояние — например, если «предыдущий трек» просто перемотал текущий в начало.
    /// </summary>
    public async Task<TrackInfo?> WaitForChangeAsync(Func<TrackInfo, bool> isChanged, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        TrackInfo? track = null;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(150);
            track = await GetCurrentTrackAsync();
            if (track is not null && isChanged(track)) return track;
        }
        return track;
    }

    private async Task<GlobalSystemMediaTransportControlsSession?> GetSessionAsync()
    {
        _manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        return _manager.GetCurrentSession();
    }
}
