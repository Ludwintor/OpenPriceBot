using Newtonsoft.Json;

namespace DedustNet.Api.Entities
{
    public struct Asset
    {
        public Asset() { }

        [JsonProperty("type")]
        public AssetType Type { get; private set; } = AssetType.Unknown;

        [JsonProperty("address")]
        public string Address { get; private set; } = string.Empty;

        [JsonProperty("metadata")]
        public AssetMetadata? Meta { get; private set; } = null;

        [JsonIgnore]
        public string Name => Meta?.Name ?? string.Empty;

        [JsonIgnore]
        public string Symbol => Meta?.Symbol ?? string.Empty;

        [JsonIgnore]
        public string Image => Meta?.Image ?? string.Empty;

        [JsonIgnore]
        public int Decimals => Meta?.Decimals ?? 9;
    }
}