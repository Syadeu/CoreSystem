using Syadeu.Database.Lua;
using Syadeu.Presentation;
using System.Runtime.Serialization;

namespace Syadeu.Database.CreatureData
{
    [DataContract]
    public abstract class CreatureAttribute : DataComponentEntity, ICreatureAttribute
    {
        public string Name { get; set; } = "New Attribute";
        public Hash Hash { get; set; }
        public ValuePairContainer Values { get; set; }
        public LuaScript OnEntityStart { get; set; }
        public LuaScript OnEntityDestory { get; set; }
    }
}
