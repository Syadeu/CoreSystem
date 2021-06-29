using Newtonsoft.Json;
using System;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    [Serializable][JsonConverter(typeof(ItemTypeJsonConverter))]
    public abstract class ItemTypeEntity
    {
        [JsonProperty(Order = 0)] public string m_Name;
        [JsonProperty(Order = 1)] public string m_Guid;
    }
}
