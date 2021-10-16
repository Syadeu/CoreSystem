using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Attributes
{
    /// <inheritdoc cref="IAttribute"/>
    public abstract class AttributeBase : ObjectBase, IAttribute
    {
        [JsonIgnore] public IEntityData ParentEntity { get; internal set; }
        [JsonIgnore] public EntityData<IEntityData> Parent => EntityData<IEntityData>.GetEntityWithoutCheck(ParentEntity.Idx);

        public override sealed string ToString() => Name;
        public override sealed object Clone() => base.Clone();

        internal override void InternalReserve()
        {
            base.InternalReserve();

            ParentEntity = null;
        }
    }
}
