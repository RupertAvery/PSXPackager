using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace PSXPackagerGUI.Models.Resource;

public class FontFamilyConverter : JsonConverter<FontFamily>
{
    public override FontFamily Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return FontManager.GetFontFamily(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, FontFamily value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Source);
    }

}