using DedustNet.Api.Serialization;
using Newtonsoft.Json;

namespace DedustNet.Api.Entities
{
    [JsonConverter(typeof(PoolTypeConverter))]
    public enum PoolType
    {
        Volatile,
        Stable,
        Unknown
    }
}