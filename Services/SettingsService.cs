using System.Text.Json;
using System.Text.Json.Serialization;

namespace VolKey.Services;

internal sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new LenientEnumConverter<GestureAction>((GestureAction)(-1)),
            new LenientEnumConverter<UiLanguage>(UiLanguage.System),
            new LenientEnumConverter<UiTheme>(UiTheme.System)
        }
    };

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VolKey", "settings.json");

    public AppSettings Load()
    {
        try
        {
            return File.Exists(_settingsPath) ? Parse(File.ReadAllText(_settingsPath)) : new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public static AppSettings Parse(string json)
    {
        try
        {
            return (JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings()).Normalize();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            // Пишем во временный файл и подменяем, чтобы сбой посреди записи не испортил настройки.
            var tempPath = _settingsPath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(tempPath, _settingsPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Не удалось сохранить — настройки продолжат действовать до перезапуска.
        }
    }

    /// <summary>
    /// Неизвестное значение (удалённое или из более новой версии) не должно сбрасывать все настройки:
    /// оно заменяется на <paramref name="fallback"/>. Для действий это недопустимое значение,
    /// и <see cref="AppSettings.Normalize"/> отбрасывает только этот жест.
    /// </summary>
    private sealed class LenientEnumConverter<T>(T fallback) : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                reader.Skip();
                return fallback;
            }
            return Enum.TryParse<T>(reader.GetString(), ignoreCase: true, out var value) && Enum.IsDefined(value)
                ? value
                : fallback;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
