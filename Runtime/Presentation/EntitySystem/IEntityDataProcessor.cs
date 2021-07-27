namespace Syadeu.Presentation
{
    internal interface IEntityDataProcessor : IProcessor
    {
        void OnCreated(IObject entity);
        void OnCreatedSync(IObject entity);
        void OnDestory(IObject entity);
        void OnDestorySync(IObject entity);
    }
}
