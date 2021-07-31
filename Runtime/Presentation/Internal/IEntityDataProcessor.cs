namespace Syadeu.Presentation.Internal
{
    internal interface IEntityDataProcessor : IProcessor
    {
        void OnCreated(IEntityData entity);
        void OnCreatedSync(IEntityData entity);
        void OnDestory(IEntityData entity);
        void OnDestorySync(IEntityData entity);
    }
}
