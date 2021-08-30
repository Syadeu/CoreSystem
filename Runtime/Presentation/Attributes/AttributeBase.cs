using Newtonsoft.Json;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Attributes
{
    /// <inheritdoc cref="IAttribute"/>
    public abstract class AttributeBase : ObjectBase, IAttribute
    {
        [JsonIgnore] public EntityData<IEntityData> Parent { get; internal set; }

        public override sealed string ToString() => Name;
        public override sealed object Clone() => base.Clone();
    }
}
