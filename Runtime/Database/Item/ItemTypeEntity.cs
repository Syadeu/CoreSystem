using Newtonsoft.Json;
using System;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    [Serializable][JsonConverter(typeof(Converters.ItemTypeJsonConverter))]
    public abstract class ItemTypeEntity : IAttribute
    {
        public string Name { get; set; }
        public Hash Hash { get; set; }
    }
}
