using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;

namespace Syadeu.Presentation.Internal
{
    internal interface IAttributeProcessor : IProcessor
    {
        void OnCreated(IAttribute attribute, EntityData<IEntityData> entity);
        void OnDestroy(IAttribute attribute, EntityData<IEntityData> entity);
    }
}
