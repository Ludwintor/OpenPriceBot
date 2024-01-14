using System;
using Newtonsoft.Json;

namespace DedustNet.Api.Entities
{
    public struct Trade
    {
        public Trade() { }

        [JsonProperty("sender")]
        public string Sender { get; private set; } = string.Empty;

        [JsonProperty("assetIn")]
        public TradeAsset AssetIn { get; private set; }
        [JsonProperty("assetOut")]
        public TradeAsset AssetOut { get; private set; }

        [JsonProperty("amountIn")]
        public UInt128 AmountIn { get; private set; }

        [JsonProperty("amountOut")]
        public UInt128 AmountOut { get; private set; }

        [JsonProperty("lt")]
        public UInt128 Lt { get; private set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; private set; }
    }

    public struct TradeAsset
    {
        public TradeAsset() { }

        [JsonProperty("type")]
        public AssetType Type { get; private set; } = AssetType.Unknown;

        [JsonProperty("address")]
        public string Address { get; private set; } = string.Empty;
    }
}
