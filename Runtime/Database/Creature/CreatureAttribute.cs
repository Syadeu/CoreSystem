using Syadeu.Database.Lua;
using Syadeu.Presentation;
using System.Runtime.Serialization;

namespace Syadeu.Database.CreatureData.Attributes
{
    [DataContract]
    public abstract class CreatureAttribute : DataComponentEntity, ICreatureAttribute
    {
        public string Name { get; set; } = "New Attribute";
        public Hash Hash { get; set; }
    }
}
