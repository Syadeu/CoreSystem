using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Attributes
{
    /// <summary>
    /// <see cref="AttributeBase"/>의 동작부를 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 만약 상속받은 class가 private, internal 혹은 그에 준하는 레벨로 노출에 제한이 있다면<br/>
    /// AOT 문제를 방지하기 위해 <seealso cref="UnityEngine.Scripting.PreserveAttribute"/> 어트리뷰트를 해당 class에 선언하여야합니다.<br/>
    /// 프로세서는 순수 내부 Reflection 을 통해 작동되므로 해당 사항이 필수입니다.
    /// <br/><br/>
    /// 참조: <seealso cref="IAttributeOnPresentation"/>, <seealso cref="IAttributeOnProxy"/>,
    /// </remarks>
    [Preserve]
    public abstract class AttributeProcessor : ProcessorBase, IAttributeProcessor
    {
        Type IProcessor.Target => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, IObject entity) => OnCreated(attribute, (IEntity)entity);
        void IAttributeProcessor.OnCreatedSync(AttributeBase attribute, IObject entity) => OnCreatedSync(attribute, (IEntity)entity);
        void IAttributeProcessor.OnDestroy(AttributeBase attribute, IObject entity) => OnDestroy(attribute, (IEntity)entity);
        void IAttributeProcessor.OnDestroySync(AttributeBase attribute, IObject entity) => OnDestroySync(attribute, (IEntity)entity);

        /// <summary>
        /// 이 프로세서가 타겟으로 삼을 <see cref="AttributeBase"/>입니다.
        /// </summary>
        protected abstract Type TargetAttribute { get; }
        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="ObjectBase"/>가 부착된 <see cref="IEntity"/>가
        /// 생성되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        /// <param name="attribute"><see cref="Target"/></param>
        /// <param name="entity"></param>
        protected virtual void OnCreated(AttributeBase attribute, IEntity entity) { }
        /// <summary><inheritdoc cref="OnCreated(AttributeBase, IEntity)"/></summary>
        /// <remarks>
        /// 동기 작업입니다.
        /// </remarks>
        protected virtual void OnCreatedSync(AttributeBase attribute, IEntity entity) { }
        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="AttributeBase"/>가 부착된 <see cref="IEntity"/>가
        /// 파괴되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="entity"></param>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        protected virtual void OnDestroy(AttributeBase attribute, IEntity entity) { }
        /// <summary><inheritdoc cref="OnDestroy(AttributeBase, IEntity)"/></summary>
        /// <remarks>
        /// 동기 작업입니다.
        /// </remarks>
        protected virtual void OnDestroySync(AttributeBase attribute, IEntity entity) { }
    }
    /// <inheritdoc cref="IAttributeProcessor"/>
    [Preserve]
    public abstract class AttributeProcessor<T> : ProcessorBase, IAttributeProcessor 
        where T : AttributeBase
    {
        Type IProcessor.Target => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, IObject entity) => OnCreated((T)attribute, entity);
        void IAttributeProcessor.OnCreatedSync(AttributeBase attribute, IObject entity) => OnCreatedSync((T)attribute, entity);
        void IAttributeProcessor.OnDestroy(AttributeBase attribute, IObject entity) => OnDestroy((T)attribute, entity);
        void IAttributeProcessor.OnDestroySync(AttributeBase attribute, IObject entity) => OnDestroySync((T)attribute, entity);

        private Type TargetAttribute => TypeHelper.TypeOf<T>.Type;
        /// <inheritdoc cref="IAttributeProcessor.OnCreated(AttributeBase, IObject)"/>
        protected virtual void OnCreated(T attribute, IObject entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnCreatedSync(AttributeBase, IObject)"/>
        protected virtual void OnCreatedSync(T attribute, IObject entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnDestroy(AttributeBase, IObject)"/>
        protected virtual void OnDestroy(T attribute, IObject entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnDestroySync(AttributeBase, IObject)"/>
        protected virtual void OnDestroySync(T attribute, IObject entity) { }
    }
}
