namespace VolKey.Services;

internal sealed record CompiledGesture(VolumeKey[] Keys, GestureAction Action);

/// <param name="Match">Самая длинная комбинация, которой заканчиваются нажатия.</param>
/// <param name="CanContinue">
/// Нажатия вместе с найденной комбинацией — начало более длинной. Тогда срабатывание нужно
/// отложить до паузы, иначе длинную комбинацию невозможно было бы ввести.
/// </param>
internal readonly record struct GestureEvaluation(CompiledGesture? Match, bool CanContinue);

/// <summary>Чистая логика распознавания без таймеров — её удобно тестировать.</summary>
internal static class GestureMatcher
{
    public static IReadOnlyList<CompiledGesture> Compile(IEnumerable<GestureBinding> bindings) => bindings
        .Where(binding => GestureSequence.IsValid(binding.Sequence))
        .DistinctBy(binding => binding.Sequence)
        .Select(binding => new CompiledGesture(GestureSequence.ToKeys(binding.Sequence), binding.Action))
        .ToList();

    public static GestureEvaluation Evaluate(IReadOnlyList<VolumeKey> presses, IReadOnlyList<CompiledGesture> gestures)
    {
        var match = gestures
            .Where(gesture => EndsWith(presses, gesture.Keys, gesture.Keys.Length))
            .MaxBy(gesture => gesture.Keys.Length);
        if (match is null) return new GestureEvaluation(null, false);

        var matchLength = match.Keys.Length;
        var canContinue = gestures.Any(gesture =>
        {
            for (var length = matchLength; length < gesture.Keys.Length; length++)
                if (EndsWith(presses, gesture.Keys, length)) return true;
            return false;
        });
        return new GestureEvaluation(match, canContinue);
    }

    /// <summary>Заканчиваются ли нажатия первыми <paramref name="length"/> кнопками комбинации.</summary>
    private static bool EndsWith(IReadOnlyList<VolumeKey> presses, VolumeKey[] keys, int length)
    {
        if (length > presses.Count) return false;
        var offset = presses.Count - length;
        for (var i = 0; i < length; i++)
            if (presses[offset + i] != keys[i]) return false;
        return true;
    }
}
