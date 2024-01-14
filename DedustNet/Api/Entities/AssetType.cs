using DedustNet.Api.Serialization;
using Newtonsoft.Json;

namespace DedustNet.Api.Entities
{
    [JsonConverter(typeof(AssetTypeConverter))]
    public enum AssetType
    {
        /// <summary>
        /// Native asset is TON (Toncoin). No address will be provided (cuz TON doesn't have any smart contract)
        /// </summary>
        Native,
        /// <summary>
        /// Any token that follows jetton standart (simply every token but TON)
        /// </summary>
        Jetton,
        /// <summary>
        /// Something wrong happened
        /// </summary>
        Unknown
    }
}