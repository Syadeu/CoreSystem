using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;

namespace Syadeu.Presentation
{
    public abstract class ObjectBase
    {
        const string c_NameBase = "New {0}";

        [JsonProperty(Order = -20, PropertyName = "Name")] public string Name { get; set; }
        [JsonProperty(Order = -19, PropertyName = "Hash")] [ReflectionSealedView] public Hash Hash { get; set; }

        public ObjectBase()
        {
            Name = string.Format(c_NameBase, GetType().Name);
            Hash = Hash.NewHash();
        }
    }
}
