using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Internal;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    /// <summary>
    /// <see cref="EntityDataBase"/>의 동작부를 선언할 수 있습니다.
    /// </summary>
    [Preserve]
    public abstract class EntityDataProcessor : ProcessorBase, IEntityDataProcessor
    {
        Type IProcessor.Target => TargetEntity;
        void IProcessor.OnInitialize() => OnInitialize();
        void IProcessor.OnInitializeAsync() => OnInitializeAsync();
        void IEntityDataProcessor.OnCreated(IEntityData entity) => OnCreated(entity);
        void IEntityDataProcessor.OnDestroy(IEntityData entity) => OnDestory(entity);
        void IDisposable.Dispose() => OnDispose();

        ~EntityDataProcessor()
        {
            CoreSystem.Logger.Log(Channel.GC, $"Disposing EntityProcessor({TargetEntity.Name})");
            ((IDisposable)this).Dispose();
        }

        /// <summary>
        /// 이 프로세서가 타겟으로 삼을 <see cref="EntityDataBase"/>입니다.
        /// </summary>
        protected abstract Type TargetEntity { get; }
        protected virtual void OnInitialize() { }
        protected virtual void OnInitializeAsync() { }
        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="EntityDataBase"/>가
        /// 생성되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        /// <param name="entity"></param>
        protected virtual void OnCreated(IEntityData entity) { }
        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="EntityDataBase"/>가
        /// 파괴되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <param name="entity"></param>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        protected virtual void OnDestory(IEntityData entity) { }

        protected virtual void OnDispose() { }
    }
    /// <inheritdoc cref="EntityDataProcessor"/>
    /// <typeparam name="T"></typeparam>
    [Preserve]
    public abstract class EntityDataProcessor<T> : ProcessorBase, IEntityDataProcessor where T : EntityDataBase
    {
        Type IProcessor.Target => TargetEntity;
        void IProcessor.OnInitialize() => OnInitialize();
        void IProcessor.OnInitializeAsync() => OnInitializeAsync();
        void IEntityDataProcessor.OnCreated(IEntityData entity) => OnCreated((T)entity);
        void IEntityDataProcessor.OnDestroy(IEntityData entity) => OnDestroy((T)entity);
        void IDisposable.Dispose() => OnDispose();

        ~EntityDataProcessor()
        {
            CoreSystem.Logger.Log(Channel.GC, $"Disposing EntityProcessor({TypeHelper.TypeOf<T>.Name})");
            ((IDisposable)this).Dispose();
        }

        private Type TargetEntity => TypeHelper.TypeOf<T>.Type;
        protected virtual void OnInitialize() { }
        protected virtual void OnInitializeAsync() { }
        /// <inheritdoc cref="EntityDataProcessor.OnCreated(EntityData{IEntityData})"/>
        protected virtual void OnCreated(T entity) { }
        /// <inheritdoc cref="EntityDataProcessor.OnDestory(EntityData{IEntityData})"/>
        protected virtual void OnDestroy(T entity) { }

        protected virtual void OnDispose() { }
    }
}
