using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Internal
{
    internal interface IEntityDataProcessor : IProcessor
    {
        void OnCreated(IEntityData entity);
        void OnDestroy(IEntityData entity);
    }
}
