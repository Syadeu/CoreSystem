using Syadeu.Internal;
using System;

namespace Syadeu.Presentation
{
    public abstract class AttributeProcessor : IAttributeProcessor
    {
        Type IAttributeProcessor.TargetAttribute => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, DataGameObject dataObj) => OnCreated(attribute, dataObj);
        void IAttributeProcessor.OnDestory(AttributeBase attribute, DataGameObject dataObj) => OnDestory(attribute, dataObj);

        protected abstract Type TargetAttribute { get; }
        protected virtual void OnCreated(AttributeBase attribute, DataGameObject dataObj) { }
        protected virtual void OnDestory(AttributeBase attribute, DataGameObject dataObj) { }
    }
    public abstract class AttributeProcessor<T> : IAttributeProcessor where T : AttributeBase
    {
        Type IAttributeProcessor.TargetAttribute => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, DataGameObject dataObj) => OnCreated((T)attribute, dataObj);
        void IAttributeProcessor.OnDestory(AttributeBase attribute, DataGameObject dataObj) => OnDestory((T)attribute, dataObj);

        private Type TargetAttribute => TypeHelper.TypeOf<T>.Type;
        protected virtual void OnCreated(T attribute, DataGameObject dataObj) { }
        protected virtual void OnDestory(T attribute, DataGameObject dataObj) { }
    }
}
