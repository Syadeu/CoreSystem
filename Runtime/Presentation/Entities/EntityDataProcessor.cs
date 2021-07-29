using Syadeu.Internal;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    [Preserve]
    public abstract class EntityDataProcessor : ProcessorBase, IEntityDataProcessor
    {
        Type IProcessor.Target => TargetEntity;
        void IEntityDataProcessor.OnCreated(IObject entity) => OnCreated((IEntity)entity);
        void IEntityDataProcessor.OnCreatedSync(IObject entity) => OnCreatedSync((IEntity)entity);
        void IEntityDataProcessor.OnDestory(IObject entity) => OnDestory((IEntity)entity);
        void IEntityDataProcessor.OnDestorySync(IObject entity) => OnDestorySync((IEntity)entity);

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
        protected virtual void OnCreated(IEntity entity) { }
        /// <summary><inheritdoc cref="OnCreated(IEntity)"/></summary>
        /// <remarks>
        /// 동기 작업입니다.
        /// </remarks>
        protected virtual void OnCreatedSync(IEntity entity) { }
        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="EntityDataBase"/>가
        /// 파괴되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <param name="entity"></param>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        protected virtual void OnDestory(IEntity entity) { }
        /// <summary><inheritdoc cref="OnDestory(IEntity)"/></summary>
        /// <remarks>
        /// 동기 작업입니다.
        /// </remarks>
        protected virtual void OnDestorySync(IEntity entity) { }
    }
    [Preserve]
    public abstract class EntityDataProcessor<T> : ProcessorBase, IEntityDataProcessor where T : EntityDataBase
    {
        Type IProcessor.Target => TargetEntity;
        void IEntityDataProcessor.OnCreated(IObject entity) => OnCreated((T)entity);
        void IEntityDataProcessor.OnCreatedSync(IObject entity) => OnCreatedSync((T)entity);
        void IEntityDataProcessor.OnDestory(IObject entity) => OnDestroy((T)entity);
        void IEntityDataProcessor.OnDestorySync(IObject entity) => OnDestorySync((T)entity);

        private Type TargetEntity => TypeHelper.TypeOf<T>.Type;
        /// <inheritdoc cref="EntityDataProcessor.OnCreated(IEntity)"/>
        protected virtual void OnCreated(T entity) { }
        /// <inheritdoc cref="EntityDataProcessor.OnCreatedSync(IEntity)"/>
        protected virtual void OnCreatedSync(T entity) { }
        /// <inheritdoc cref="EntityDataProcessor.OnDestory(IEntity)"/>
        protected virtual void OnDestroy(T entity) { }
        /// <inheritdoc cref="EntityDataProcessor.OnDestorySync(IEntity)"/>
        protected virtual void OnDestorySync(T entity) { }
    }
}
