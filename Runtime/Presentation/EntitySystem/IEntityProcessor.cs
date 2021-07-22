namespace Syadeu.Presentation
{
    internal interface IEntityProcessor : IProcessor
    {
        void OnCreated(IEntity entity);
        void OnCreatedSync(IEntity entity);
        void OnDestory(IEntity entity);
        void OnDestorySync(IEntity entity);
    }
}
