using System;
using DedustNet.Api.Entities;
using Newtonsoft.Json;

namespace DedustNet.Api.Serialization
{
    internal sealed class AssetTypeConverter : JsonConverter<AssetType>
    {
        public override AssetType ReadJson(JsonReader reader, Type objectType, AssetType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return AssetType.Unknown;

            return (string?)reader.Value switch
            {
                "native" => AssetType.Native,
                "jetton" => AssetType.Jetton,
                _ => AssetType.Unknown
            };
        }

        public override void WriteJson(JsonWriter writer, AssetType value, JsonSerializer serializer)
        {
            string? result = value switch
            {
                AssetType.Native => "native",
                AssetType.Jetton => "jetton",
                _ => null,
            };
            writer.WriteValue(result);
        }
    }
}