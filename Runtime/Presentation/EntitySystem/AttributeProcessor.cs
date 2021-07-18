using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.ThreadSafe;
using System;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    /// <inheritdoc cref="IAttributeProcessor"/>
    [Preserve]
    public abstract class AttributeProcessor : AttributeProcessorBase, IAttributeProcessor
    {
        Type IAttributeProcessor.TargetAttribute => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, IEntity entity) => OnCreated(attribute, entity);
        void IAttributeProcessor.OnCreatedSync(AttributeBase attribute, IEntity entity) => OnCreatedSync(attribute, entity);
        void IAttributeProcessor.OnDestory(AttributeBase attribute, IEntity entity) => OnDestory(attribute, entity);
        void IAttributeProcessor.OnDestorySync(AttributeBase attribute, IEntity entity) => OnDestorySync(attribute, entity);

        /// <inheritdoc cref="IAttributeProcessor.TargetAttribute"/>
        protected abstract Type TargetAttribute { get; }
        /// <inheritdoc cref="IAttributeProcessor.OnCreated(AttributeBase, IEntity)"/>
        protected virtual void OnCreated(AttributeBase attribute, IEntity entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnCreatedSync(AttributeBase, IEntity)"/>
        protected virtual void OnCreatedSync(AttributeBase attribute, IEntity entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnDestory(AttributeBase, IEntity)"/>
        protected virtual void OnDestory(AttributeBase attribute, IEntity entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnDestorySync(AttributeBase, IEntity)"/>
        protected virtual void OnDestorySync(AttributeBase attribute, IEntity entity) { }
    }
    /// <inheritdoc cref="IAttributeProcessor"/>
    [Preserve]
    public abstract class AttributeProcessor<T> : AttributeProcessorBase, IAttributeProcessor 
        where T : AttributeBase
    {
        Type IAttributeProcessor.TargetAttribute => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, IEntity entity) => OnCreated((T)attribute, entity);
        void IAttributeProcessor.OnCreatedSync(AttributeBase attribute, IEntity entity) => OnCreatedSync((T)attribute, entity);
        void IAttributeProcessor.OnDestory(AttributeBase attribute, IEntity entity) => OnDestory((T)attribute, entity);
        void IAttributeProcessor.OnDestorySync(AttributeBase attribute, IEntity entity) => OnDestorySync((T)attribute, entity);

        private Type TargetAttribute => TypeHelper.TypeOf<T>.Type;
        /// <inheritdoc cref="IAttributeProcessor.OnCreated(AttributeBase, IEntity)"/>
        protected virtual void OnCreated(T attribute, IEntity entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnCreatedSync(AttributeBase, IEntity)"/>
        protected virtual void OnCreatedSync(T attribute, IEntity entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnDestory(AttributeBase, IEntity)"/>
        protected virtual void OnDestory(T attribute, IEntity entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnDestorySync(AttributeBase, IEntity)"/>
        protected virtual void OnDestorySync(T attribute, IEntity entity) { }
    }

    public abstract class AttributeProcessorBase
    {
        protected DataGameObject CreatePrefab(PrefabReference prefab, Vector3 position, quaternion rotation)
            => CreatePrefab(prefab, position, rotation, Vector3.One, true);
        protected DataGameObject CreatePrefab(PrefabReference prefab, Vector3 position, quaternion rotation, Vector3 localSize, bool enableCull)
        {
            GameObjectProxySystem system = PresentationSystem<GameObjectProxySystem>.System;
            CoreSystem.Logger.NotNull(system, "GameObjectProxySystem is not initialized");

            return system.CreateNewPrefab(prefab, position, rotation, localSize, enableCull);
        }

        protected IEntity CreateEntity(EntityReference entity, Vector3 position, quaternion rotation)
            => CreateEntity(entity, position, rotation, Vector3.One, true);
        protected IEntity CreateEntity(EntityReference entity, Vector3 position, quaternion rotation, Vector3 localSize, bool enableCull)
        {
            EntitySystem system = PresentationSystem<EntitySystem>.System;
            CoreSystem.Logger.NotNull(system, "GameObjectProxySystem is not initialized");

            return system.CreateEntity(entity, position, rotation, localSize, enableCull);
        }
    }
}
