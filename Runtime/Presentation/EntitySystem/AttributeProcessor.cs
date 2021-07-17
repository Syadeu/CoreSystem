using Syadeu.Internal;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    /// <inheritdoc cref="IAttributeProcessor"/>
    [Preserve]
    public abstract class AttributeProcessor : IAttributeProcessor
    {
        Type IAttributeProcessor.TargetAttribute => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, IEntity entity) => OnCreated(attribute, entity);
        void IAttributeProcessor.OnDestory(AttributeBase attribute, IEntity entity) => OnDestory(attribute, entity);

        /// <inheritdoc cref="IAttributeProcessor.TargetAttribute"/>
        protected abstract Type TargetAttribute { get; }
        /// <inheritdoc cref="IAttributeProcessor.OnCreated(AttributeBase, IEntity)"/>
        protected virtual void OnCreated(AttributeBase attribute, IEntity entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnDestory(AttributeBase, IEntity)"/>
        protected virtual void OnDestory(AttributeBase attribute, IEntity entity) { }
    }
    /// <inheritdoc cref="IAttributeProcessor"/>
    [Preserve]
    public abstract class AttributeProcessor<T> : IAttributeProcessor where T : AttributeBase
    {
        Type IAttributeProcessor.TargetAttribute => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, IEntity entity) => OnCreated((T)attribute, entity);
        void IAttributeProcessor.OnDestory(AttributeBase attribute, IEntity entity) => OnDestory((T)attribute, entity);

        private Type TargetAttribute => TypeHelper.TypeOf<T>.Type;
        /// <inheritdoc cref="IAttributeProcessor.OnCreated(AttributeBase, IEntity)"/>
        protected virtual void OnCreated(T attribute, IEntity entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnDestory(AttributeBase, IEntity)"/>
        protected virtual void OnDestory(T attribute, IEntity entity) { }
    }
}
