using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
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
        void IAttributeProcessor.OnDestory(AttributeBase attribute, IObject entity) => OnDestory(attribute, (IEntity)entity);
        void IAttributeProcessor.OnDestorySync(AttributeBase attribute, IObject entity) => OnDestorySync(attribute, (IEntity)entity);

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
        protected virtual void OnDestory(AttributeBase attribute, IEntity entity) { }
        /// <summary><inheritdoc cref="OnDestory(AttributeBase, IEntity)"/></summary>
        /// <remarks>
        /// 동기 작업입니다.
        /// </remarks>
        protected virtual void OnDestorySync(AttributeBase attribute, IEntity entity) { }
    }
    /// <inheritdoc cref="IAttributeProcessor"/>
    [Preserve]
    public abstract class AttributeProcessor<T> : ProcessorBase, IAttributeProcessor 
        where T : AttributeBase
    {
        Type IProcessor.Target => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, IObject entity) => OnCreated((T)attribute, (IEntity)entity);
        void IAttributeProcessor.OnCreatedSync(AttributeBase attribute, IObject entity) => OnCreatedSync((T)attribute, (IEntity)entity);
        void IAttributeProcessor.OnDestory(AttributeBase attribute, IObject entity) => OnDestory((T)attribute, (IEntity)entity);
        void IAttributeProcessor.OnDestorySync(AttributeBase attribute, IObject entity) => OnDestorySync((T)attribute, (IEntity)entity);

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
}
