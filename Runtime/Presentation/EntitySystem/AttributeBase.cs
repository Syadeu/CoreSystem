using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using System;

namespace Syadeu.Presentation
{
    /// <inheritdoc cref="IAttribute"/>
    public abstract class AttributeBase : IAttribute, ICloneable
    {
        public string Name { get; set; } = "New Attribute";
        [ReflectionSealedView] public Hash Hash { get; set; }

        [JsonIgnore] public IEntity Parent { get; internal set; }

        public virtual object Clone()
        {
            AttributeBase att = (AttributeBase)MemberwiseClone();
            att.Name = string.Copy(Name);

            return att;
        }

        public override string ToString() => Name;
    }
}
