using Newtonsoft.Json;
using Syadeu.Database.Lua;
using System.Runtime.Serialization;

namespace Syadeu.Database.CreatureData
{
    internal interface ICreatureAttribute : IAttribute
    {
        //[DataMember] [JsonProperty(Order = -3)] LuaScript OnEntityStart { get; }
        //[DataMember] [JsonProperty(Order = -2)] LuaScript OnEntityDestory { get; }
        [DataMember] [JsonProperty(Order = -1)] ValuePairContainer Values { get; }
    }
}
