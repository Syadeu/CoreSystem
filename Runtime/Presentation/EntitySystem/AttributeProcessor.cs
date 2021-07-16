using System;

namespace Syadeu.Presentation
{
    public abstract class AttributeProcessor : IAttributeProcessor
    {
        Type IAttributeProcessor.TargetAttribute => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, DataGameObject dataObj) => OnCreated(attribute, dataObj);
        void IAttributeProcessor.OnPresentation(AttributeBase attribute, DataGameObject dataObj) => OnPresentation(attribute, dataObj);
        void IAttributeProcessor.OnDead(AttributeBase attribute, DataGameObject dataObj) => OnDead(attribute, dataObj);
        void IAttributeProcessor.OnDestory(AttributeBase attribute, DataGameObject dataObj) => OnDestory(attribute, dataObj);

        protected abstract Type TargetAttribute { get; }
        protected abstract void OnCreated(AttributeBase attribute, DataGameObject dataObj);
        protected virtual void OnPresentation(AttributeBase attribute, DataGameObject dataObj) { }
        protected virtual void OnDead(AttributeBase attribute, DataGameObject dataObj) { }
        protected abstract void OnDestory(AttributeBase attribute, DataGameObject dataObj);
    }
    public abstract class AttributeProcessor<T> : IAttributeProcessor where T : AttributeBase
    {
        Type IAttributeProcessor.TargetAttribute => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, DataGameObject dataObj) => OnCreated((T)attribute, dataObj);
        void IAttributeProcessor.OnPresentation(AttributeBase attribute, DataGameObject dataObj) => OnPresentation((T)attribute, dataObj);
        void IAttributeProcessor.OnDead(AttributeBase attribute, DataGameObject dataObj) => OnDead((T)attribute, dataObj);
        void IAttributeProcessor.OnDestory(AttributeBase attribute, DataGameObject dataObj) => OnDestory((T)attribute, dataObj);

        private Type TargetAttribute => TypeHelper.TypeOf<T>.Type;
        protected abstract void OnCreated(T attribute, DataGameObject dataObj);
        protected virtual void OnPresentation(T attribute, DataGameObject dataObj) { }
        protected virtual void OnDead(T attribute, DataGameObject dataObj) { }
        protected abstract void OnDestory(T attribute, DataGameObject dataObj);
    }
}
