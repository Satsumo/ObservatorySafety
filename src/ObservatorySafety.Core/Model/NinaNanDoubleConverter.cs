using System.Text.Json;
using System.Text.Json.Serialization;

namespace ObservatorySafety.Core.Model
{

  public class NinaNanDoubleConverter : JsonConverter<double?>
  {
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      // Handle numeric values normally
      if (reader.TokenType == JsonTokenType.Number)
      {
        return reader.GetDouble();
      }

      // Handle "NaN" string
      if (reader.TokenType == JsonTokenType.String)
      {
        var s = reader.GetString();
        if (string.Equals(s, "NaN", StringComparison.OrdinalIgnoreCase))
          return double.NaN;

        // Try parsing other numeric strings
        if (double.TryParse(s, out var parsed))
          return parsed;

        return null; // fallback
      }

      // Null → null
      if (reader.TokenType == JsonTokenType.Null)
        return null;

      throw new JsonException($"Unexpected token {reader.TokenType} when parsing double?");
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
      if (value == null)
      {
        writer.WriteNullValue();
        return;
      }

      if (double.IsNaN(value.Value))
      {
        writer.WriteStringValue("NaN");
        return;
      }

      writer.WriteNumberValue(value.Value);
    }
  }

}
