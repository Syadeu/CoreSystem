namespace Syadeu.Presentation
{
    internal interface IEntityProcessor : IProcessor
    {
        void OnCreated(IObject entity);
        void OnCreatedSync(IObject entity);
        void OnDestory(IObject entity);
        void OnDestorySync(IObject entity);
    }
}
