using Syadeu.Mono;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Attributes
{
    internal interface IAttributeProcessor : IProcessor
    {
        void OnCreated(AttributeBase attribute, IObject entity);
        void OnCreatedSync(AttributeBase attribute, IObject entity);
        void OnDestroy(AttributeBase attribute, IObject entity);
        void OnDestroySync(AttributeBase attribute, IObject entity);
    }
    /// <summary>
    /// <see cref="AttributeBase"/>에서 매 프레임마다 실행될 동작을 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 이 인터페이스 단일로는 동작하지않습니다. <seealso cref="AttributeProcessor"/>를 참조하는 class에 상속받아 사용되어야됩니다.
    /// </remarks>
    public interface IAttributeOnPresentation
    {
        /// <summary>
        /// <see cref="IAttributeProcessor.Target"/>에 설정된 <see cref="AttributeBase"/>가 부착된 <see cref="IObject"/>가
        /// 매 프레임마다 동작할 메소드입니다.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="entity"></param>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        void OnPresentation(AttributeBase attribute, IObject entity);
    }
    /// <summary>
    /// <see cref="AttributeBase"/>에서 프록시 오브젝트에 대한 동작을 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 이 인터페이스는 <seealso cref="IAttributeOnProxyCreated"/>, <seealso cref="IAttributeOnProxyRemoved"/>의 묶음 인터페이스입니다.
    /// </remarks>
    /// <remarks>
    /// 이 인터페이스 단일로는 동작하지않습니다. <seealso cref="AttributeProcessor"/>를 참조하는 class에 상속받아 사용되어야됩니다.
    /// </remarks>
    public interface IAttributeOnProxy : IAttributeOnProxyCreated, IAttributeOnProxyRemoved { }
    /// <summary>
    /// <see cref="AttributeBase"/>에서 프록시 오브젝트가 생성되었을 때 실행될 동작을 선언할 수 있습니다.<br/>
    /// 이 인터페이스 단일로는 동작하지않습니다. <seealso cref="AttributeProcessor"/>를 참조하는 class에 상속받아 사용되어야됩니다.
    /// </summary>
    /// <remarks>
    /// 이 인터페이스는 비동기 작업입니다. 동기 작업은 <seealso cref="IAttributeOnProxyCreatedSync"/>을 참조하세요.
    /// </remarks>
    public interface IAttributeOnProxyCreated
    {
        /// <summary>
        /// <see cref="IAttributeProcessor.Target"/>에 설정된 <see cref="AttributeBase"/>가 부착된 <see cref="IEntity"/>의
        /// 프록시 오브젝트가 생성되어 부착되었을 때 실행되는 메소드입니다.<br/>
        /// 이 메소드가 호출되었을 때에는 이미 프록시 오브젝트가 생성되어 받아올 수 있습니다.
        /// <seealso cref="IEntity.gameObject"/>에서 <seealso cref="DataGameObject.GetProxyObject"/>로 프록시 오브젝트를 받아올 수 있습니다.
        /// </summary>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        /// <param name="attribute"></param>
        /// <param name="entity"></param>
        void OnProxyCreated(AttributeBase attribute, IEntity entity, RecycleableMonobehaviour monoObj);
    }
    /// <summary><inheritdoc cref="IAttributeOnProxyCreated"/></summary>
    /// <remarks>
    /// 이 인터페이스는 동기 작업입니다. 비동기 작업은 <seealso cref="IAttributeOnProxyCreated"/>을 참조하세요.
    /// </remarks>
    public interface IAttributeOnProxyCreatedSync
    {
        /// <summary><inheritdoc cref="IAttributeOnProxyCreatedSync.OnProxyCreated(AttributeBase, IEntity)"/></summary>
        void OnProxyCreatedSync(AttributeBase attribute, IEntity entity, RecycleableMonobehaviour monoObj);
    }
    /// <summary>
    /// <see cref="AttributeBase"/>에서 프록시 오브젝트가 제거되었을 때 실행될 동작을 선언할 수 있습니다.<br/>
    /// 이 인터페이스 단일로는 동작하지않습니다. <seealso cref="AttributeProcessor"/>를 참조하는 class에 상속받아 사용되어야됩니다.
    /// </summary>
    /// <remarks>
    /// 이 인터페이스는 비동기 작업입니다. 동기 작업은 <seealso cref="IAttributeOnProxyRemovedSync"/>을 참조하세요.
    /// </remarks>
    public interface IAttributeOnProxyRemoved
    {
        /// <summary>
        /// <see cref="IAttributeProcessor.Target"/>에 설정된 <see cref="AttributeBase"/>가 부착된 <see cref="IEntity"/>의
        /// 프록시 오브젝트가 제거될 때 실행되는 메소드입니다.<br/>
        /// 이 메소드가 호출되었을 때에는 아직 프록시 오브젝트가 존재하여 받아올 수 있습니다.
        /// <seealso cref="IEntity.gameObject"/>에서 <seealso cref="DataGameObject.GetProxyObject"/>로 프록시 오브젝트를 받아올 수 있습니다.
        /// </summary>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        /// <param name="attribute"></param>
        /// <param name="entity"></param>
        void OnProxyRemoved(AttributeBase attribute, IEntity entity, RecycleableMonobehaviour monoObj);
    }
    /// <summary><inheritdoc cref="IAttributeOnProxyRemoved"/></summary>
    /// <remarks>
    /// 이 인터페이스는 동기 작업입니다. 비동기 작업은 <seealso cref="IAttributeOnProxyRemoved"/>을 참조하세요.
    /// </remarks>
    public interface IAttributeOnProxyRemovedSync
    {
        /// <summary><inheritdoc cref="IAttributeOnProxyRemoved.OnProxyRemoved(AttributeBase, IEntity)"/></summary>
        /// <remarks>
        /// 비동기 작업에서 오브젝트가 파괴됨으로, 동기 메소드에서 프록시 오브젝트를 호출할때에는 이미 파괴되었을 수 있습니다.
        /// </remarks>
        void OnProxyRemovedSync(AttributeBase attribute, IEntity entity, RecycleableMonobehaviour monoObj);
    }
}
