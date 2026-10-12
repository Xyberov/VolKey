namespace VolKey.Services;

internal enum VolumeKey { Up, Down }

internal enum GestureAction { NextTrack, PreviousTrack, PlayPause, ToggleMute }

internal static class GestureActions
{
    public static readonly GestureAction[] All = Enum.GetValues<GestureAction>();

    public static string DisplayName(GestureAction action) => action switch
    {
        GestureAction.NextTrack => "Следующий трек",
        GestureAction.PreviousTrack => "Предыдущий трек",
        GestureAction.PlayPause => "Пауза / воспроизведение",
        GestureAction.ToggleMute => "Выключить / включить звук",
        _ => action.ToString()
    };
}

/// <summary>Комбинация кнопок в виде строки из «+» и «-» (например, "+-+") и назначенное ей действие.</summary>
internal sealed class GestureBinding
{
    public string Sequence { get; set; } = "";
    public GestureAction Action { get; set; }

    public GestureBinding() { }

    public GestureBinding(string sequence, GestureAction action)
    {
        Sequence = sequence;
        Action = action;
    }
}

internal static class GestureSequence
{
    public const int MinLength = 2;
    public const int MaxLength = 4;

    /// <summary>Все допустимые комбинации: сначала из трёх нажатий, затем из четырёх и двух.</summary>
    public static readonly IReadOnlyList<string> All = Generate();

    /// <summary>
    /// Допустимы комбинации из 2–4 нажатий, где есть обе кнопки: иначе обычная регулировка
    /// громкости (несколько нажатий «+» подряд) превращалась бы в жест.
    /// </summary>
    public static bool IsValid(string? sequence) =>
        sequence is { Length: >= MinLength and <= MaxLength } &&
        sequence.All(c => c is '+' or '-') &&
        sequence.Contains('+') && sequence.Contains('-');

    public static VolumeKey[] ToKeys(string sequence) =>
        sequence.Select(c => c == '+' ? VolumeKey.Up : VolumeKey.Down).ToArray();

    public static string Display(string sequence) =>
        string.Join("  ", sequence.Select(c => c == '+' ? '+' : '−'));

    private static List<string> Generate()
    {
        var result = new List<string>();
        foreach (var length in new[] { 3, 4, 2 })
        {
            for (var mask = 0; mask < 1 << length; mask++)
            {
                var sequence = new string(Enumerable.Range(0, length)
                    .Select(i => (mask >> (length - 1 - i) & 1) == 0 ? '+' : '-').ToArray());
                if (IsValid(sequence)) result.Add(sequence);
            }
        }
        return result;
    }
}
