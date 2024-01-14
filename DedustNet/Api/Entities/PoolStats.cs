using System;
using Newtonsoft.Json;

namespace DedustNet.Api.Entities
{
    public readonly struct PoolStats
    {
        [JsonProperty("fees")]
        private readonly UInt128[] _fees;

        [JsonProperty("volume")]
        private readonly UInt128[] _volume;

        [JsonIgnore]
        public UInt128 LeftFees => _fees?[0] ?? UInt128.Zero;

        [JsonIgnore]
        public UInt128 RightFees => _fees?[1] ?? UInt128.Zero;

        [JsonIgnore]
        public UInt128 LeftVolume => _volume?[0] ?? UInt128.Zero;

        [JsonIgnore]
        public UInt128 RightVolume => _volume?[1] ?? UInt128.Zero;
    }
}