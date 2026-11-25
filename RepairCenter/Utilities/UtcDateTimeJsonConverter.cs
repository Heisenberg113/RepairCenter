using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RepairCenter.Utilities;

public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("A timestamp must be a string.");

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException("A timestamp cannot be empty.");

        var timeSeparator = value.IndexOf('T');
        if (timeSeparator < 0)
            timeSeparator = value.IndexOf('t');
        if (timeSeparator < 0)
            timeSeparator = value.IndexOf(' ');
        var hasExplicitOffset = value.EndsWith("Z", StringComparison.OrdinalIgnoreCase) ||
            timeSeparator >= 0 &&
            (value.LastIndexOf('+') > timeSeparator || value.LastIndexOf('-') > timeSeparator);

        if (hasExplicitOffset)
        {
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffset))
                return dateTimeOffset.UtcDateTime;
            throw new JsonException("The timestamp is invalid.");
        }

        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            throw new JsonException("The timestamp is invalid.");
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        writer.WriteStringValue(utc);
    }
}
