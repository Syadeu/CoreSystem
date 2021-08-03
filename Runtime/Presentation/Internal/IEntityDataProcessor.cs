namespace Syadeu.Presentation.Internal
{
    internal interface IEntityDataProcessor : IProcessor
    {
        void OnCreated(IEntityData entity);
        void OnCreatedSync(IEntityData entity);
        void OnDestroy(IEntityData entity);
        void OnDestroySync(IEntityData entity);
    }
}
