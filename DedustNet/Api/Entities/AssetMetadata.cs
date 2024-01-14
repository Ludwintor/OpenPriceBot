using Newtonsoft.Json;

namespace DedustNet.Api.Entities
{
    public struct AssetMetadata
    {
        public AssetMetadata() { }

        [JsonProperty("name")]
        public string Name { get; private set; } = string.Empty;

        [JsonProperty("symbol")]
        public string Symbol { get; private set; } = string.Empty;

        [JsonProperty("image")]
        public string Image { get; private set; } = string.Empty;

        [JsonProperty("decimals")]
        public int Decimals { get; private set; } = 9;
    }
}