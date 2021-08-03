using Syadeu.Internal;
using Syadeu.Presentation.Internal;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    [Preserve]
    public abstract class EntityDataProcessor : ProcessorBase, IEntityDataProcessor
    {
        Type IProcessor.Target => TargetEntity;
        void IEntityDataProcessor.OnCreated(EntityData<IEntityData> entity) => OnCreated(entity);
        void IEntityDataProcessor.OnCreatedSync(EntityData<IEntityData> entity) => OnCreatedSync(entity);
        void IEntityDataProcessor.OnDestroy(EntityData<IEntityData> entity) => OnDestory(entity);
        void IEntityDataProcessor.OnDestroySync(EntityData<IEntityData> entity) => OnDestorySync(entity);

        /// <summary>
        /// 이 프로세서가 타겟으로 삼을 <see cref="EntityDataBase"/>입니다.
        /// </summary>
        protected abstract Type TargetEntity { get; }
        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="EntityDataBase"/>가
        /// 생성되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        /// <param name="entity"></param>
        protected virtual void OnCreated(EntityData<IEntityData> entity) { }
        /// <summary><inheritdoc cref="OnCreated(IEntity)"/></summary>
        /// <remarks>
        /// 동기 작업입니다.
        /// </remarks>
        protected virtual void OnCreatedSync(EntityData<IEntityData> entity) { }
        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="EntityDataBase"/>가
        /// 파괴되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <param name="entity"></param>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        protected virtual void OnDestory(EntityData<IEntityData> entity) { }
        /// <summary><inheritdoc cref="OnDestory(IEntity)"/></summary>
        /// <remarks>
        /// 동기 작업입니다.
        /// </remarks>
        protected virtual void OnDestorySync(EntityData<IEntityData> entity) { }
    }
    [Preserve]
    public abstract class EntityDataProcessor<T> : ProcessorBase, IEntityDataProcessor where T : EntityDataBase
    {
        Type IProcessor.Target => TargetEntity;
        void IEntityDataProcessor.OnCreated(EntityData<IEntityData> entity) => OnCreated(entity);
        void IEntityDataProcessor.OnCreatedSync(EntityData<IEntityData> entity) => OnCreatedSync(entity);
        void IEntityDataProcessor.OnDestroy(EntityData<IEntityData> entity) => OnDestroy(entity);
        void IEntityDataProcessor.OnDestroySync(EntityData<IEntityData> entity) => OnDestorySync(entity);

        private Type TargetEntity => TypeHelper.TypeOf<T>.Type;
        /// <inheritdoc cref="EntityDataProcessor.OnCreated(EntityData{IEntityData})"/>
        protected virtual void OnCreated(EntityData<T> entity) { }
        /// <inheritdoc cref="EntityDataProcessor.OnCreatedSync(EntityData{IEntityData})"/>
        protected virtual void OnCreatedSync(EntityData<T> entity) { }
        /// <inheritdoc cref="EntityDataProcessor.OnDestory(EntityData{IEntityData})"/>
        protected virtual void OnDestroy(EntityData<T> entity) { }
        /// <inheritdoc cref="EntityDataProcessor.OnDestorySync(EntityData{IEntityData})"/>
        protected virtual void OnDestorySync(EntityData<T> entity) { }
    }
}
