using System;
using DedustNet.Api.Entities;
using Newtonsoft.Json;

namespace DedustNet.Api.Serialization
{
    internal sealed class PoolTypeConverter : JsonConverter<PoolType>
    {
        public override PoolType ReadJson(JsonReader reader, Type objectType, PoolType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String)
                return PoolType.Unknown;

            return (string?)reader.Value switch
            {
                "volatile" => PoolType.Volatile,
                "stable" => PoolType.Stable,
                _ => PoolType.Unknown
            };
        }

        public override void WriteJson(JsonWriter writer, PoolType value, JsonSerializer serializer)
        {
            string? result = value switch
            {
                PoolType.Volatile => "volatile",
                PoolType.Stable => "stable",
                _ => null,
            };
            writer.WriteValue(result);
        }
    }
}