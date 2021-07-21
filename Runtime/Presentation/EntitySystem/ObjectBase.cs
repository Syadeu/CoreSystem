using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Syadeu.Database;
using Syadeu.Internal;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public abstract class ObjectBase : ICloneable
    {
        const string c_NameBase = "New {0}";

        [JsonProperty(Order = -20, PropertyName = "Name")] public string Name { get; set; }
        [JsonProperty(Order = -19, PropertyName = "Hash")] [ReflectionSealedView] public Hash Hash { get; set; }

        public ObjectBase()
        {
            Name = string.Format(c_NameBase, GetType().Name);
            Hash = Hash.NewHash();
        }

        public virtual ObjectBase Copy()
        {
            ObjectBase entity = (ObjectBase)MemberwiseClone();
            entity.Name = string.Copy(Name);

            return entity;
        }
        public virtual object Clone()
        {
            return Copy();
        }
    }
}
