using Newtonsoft.Json;
using System;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    [Serializable][JsonConverter(typeof(Converters.ItemTypeJsonConverter))]
    public abstract class ItemTypeEntity
    {
        [JsonProperty(Order = 0, PropertyName = "Name")] public string m_Name;
        [JsonProperty(Order = 1, PropertyName = "Hash")] public Hash m_Hash;
    }
}
