using Syadeu.Internal;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    [Preserve]
    public abstract class EntityProcessor : IEntityProcessor
    {
        Type IProcessor.Target => TargetEntity;
        void IEntityProcessor.OnCreated(IObject entity) => OnCreated((IEntity)entity);
        void IEntityProcessor.OnCreatedSync(IObject entity) => OnCreatedSync((IEntity)entity);
        void IEntityProcessor.OnDestory(IObject entity) => OnDestory((IEntity)entity);
        void IEntityProcessor.OnDestorySync(IObject entity) => OnDestorySync((IEntity)entity);

        /// <summary>
        /// 이 프로세서가 타겟으로 삼을 <see cref="EntityBase"/>입니다.
        /// </summary>
        protected abstract Type TargetEntity { get; }
        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="ObjectBase"/>가
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
        /// <see cref="Target"/>에 설정된 <see cref="EntityBase"/>가
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
    public abstract class EntityProcessor<T> : IEntityProcessor where T : EntityBase
    {
        Type IProcessor.Target => TargetEntity;
        void IEntityProcessor.OnCreated(IObject entity) => OnCreated((T)entity);
        void IEntityProcessor.OnCreatedSync(IObject entity) => OnCreatedSync((T)entity);
        void IEntityProcessor.OnDestory(IObject entity) => OnDestory((T)entity);
        void IEntityProcessor.OnDestorySync(IObject entity) => OnDestorySync((T)entity);

        private Type TargetEntity => TypeHelper.TypeOf<T>.Type;
        /// <inheritdoc cref="EntityProcessor.OnCreated(IEntity)"/>
        protected virtual void OnCreated(T entity) { }
        /// <inheritdoc cref="EntityProcessor.OnCreatedSync(IEntity)"/>
        protected virtual void OnCreatedSync(T entity) { }
        /// <inheritdoc cref="EntityProcessor.OnDestory(IEntity)"/>
        protected virtual void OnDestory(T entity) { }
        /// <inheritdoc cref="EntityProcessor.OnDestorySync(IEntity)"/>
        protected virtual void OnDestorySync(T entity) { }
    }
}
