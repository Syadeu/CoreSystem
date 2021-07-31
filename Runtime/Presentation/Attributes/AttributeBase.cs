using Newtonsoft.Json;
using Syadeu.Internal;

namespace Syadeu.Presentation.Attributes
{
    /// <inheritdoc cref="IAttribute"/>
    public abstract class AttributeBase : ObjectBase, IAttribute
    {
        [JsonIgnore] public IEntityData Parent { get; internal set; }

        public override sealed string ToString() => Name;
        public override sealed object Clone() => base.Clone();
    }
}
