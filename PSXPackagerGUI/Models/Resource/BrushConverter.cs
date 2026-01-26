using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace PSXPackagerGUI.Models.Resource;

public class BrushConverter : JsonConverter<Brush>
{
    public override Brush Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(reader.GetString()!));
    }

    public override void Write(Utf8JsonWriter writer, Brush value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

}