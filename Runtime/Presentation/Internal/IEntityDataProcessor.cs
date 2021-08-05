using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Internal
{
    internal interface IEntityDataProcessor : IProcessor
    {
        void OnCreated(EntityData<IEntityData> entity);
        void OnCreatedSync(EntityData<IEntityData> entity);
        void OnDestroy(EntityData<IEntityData> entity);
        void OnDestroySync(EntityData<IEntityData> entity);
    }
}
