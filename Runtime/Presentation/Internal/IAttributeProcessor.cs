using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;

namespace Syadeu.Presentation.Internal
{
    internal interface IAttributeProcessor : IProcessor
    {
        void OnCreated(AttributeBase attribute, EntityData<IEntityData> entity);
        void OnDestroy(AttributeBase attribute, EntityData<IEntityData> entity);
    }
}
