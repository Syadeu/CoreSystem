using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="AttributeBase"/>의 동작부를 선언할 수 있습니다.
    /// </summary>
    internal interface IAttributeProcessor
    {
        /// <summary>
        /// 이 프로세서가 타겟으로 삼을 <see cref="AttributeBase"/>입니다.
        /// </summary>
        Type TargetAttribute { get; }
        /// <summary>
        /// <see cref="TargetAttribute"/>에 설정된 <see cref="AttributeBase"/>가 부착된 <see cref="IEntity"/>가
        /// 생성되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <param name="attribute"><see cref="TargetAttribute"/></param>
        /// <param name="entity"></param>
        void OnCreated(AttributeBase attribute, IEntity entity);
        /// <summary>
        /// <see cref="TargetAttribute"/>에 설정된 <see cref="AttributeBase"/>가 부착된 <see cref="IEntity"/>가
        /// 파괴되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="entity"></param>
        void OnDestory(AttributeBase attribute, IEntity entity);
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
        /// <see cref="IAttributeProcessor.TargetAttribute"/>에 설정된 <see cref="AttributeBase"/>가 부착된 <see cref="IEntity"/>가
        /// 매 프레임마다 동작할 메소드입니다.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="entity"></param>
        void OnPresentation(AttributeBase attribute, IEntity entity);
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
    /// <see cref="AttributeBase"/>에서 프록시 오브젝트가 생성되었을 때 실행될 동작을 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 이 인터페이스 단일로는 동작하지않습니다. <seealso cref="AttributeProcessor"/>를 참조하는 class에 상속받아 사용되어야됩니다.
    /// </remarks>
    public interface IAttributeOnProxyCreated
    {
        /// <summary>
        /// <see cref="IAttributeProcessor.TargetAttribute"/>에 설정된 <see cref="AttributeBase"/>가 부착된 <see cref="IEntity"/>의
        /// 프록시 오브젝트가 생성되어 부착되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// 이 메소드가 호출되었을 때에는 이미 프록시 오브젝트가 생성되어 받아올 수 있습니다.
        /// <seealso cref="IEntity.gameObject"/>에서 <seealso cref="DataGameObject.GetProxyObject"/>로 프록시 오브젝트를 받아올 수 있습니다.
        /// </remarks>
        /// <param name="attribute"></param>
        /// <param name="entity"></param>
        void OnProxyCreated(AttributeBase attribute, IEntity entity);
    }
    /// <summary>
    /// <see cref="AttributeBase"/>에서 프록시 오브젝트가 제거되었을 때 실행될 동작을 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 이 인터페이스 단일로는 동작하지않습니다. <seealso cref="AttributeProcessor"/>를 참조하는 class에 상속받아 사용되어야됩니다.
    /// </remarks>
    public interface IAttributeOnProxyRemoved
    {
        /// <summary>
        /// <see cref="IAttributeProcessor.TargetAttribute"/>에 설정된 <see cref="AttributeBase"/>가 부착된 <see cref="IEntity"/>의
        /// 프록시 오브젝트가 제거될 때 실행되는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// 이 메소드가 호출되었을 때에는 아직 프록시 오브젝트가 존재하여 받아올 수 있습니다.
        /// <seealso cref="IEntity.gameObject"/>에서 <seealso cref="DataGameObject.GetProxyObject"/>로 프록시 오브젝트를 받아올 수 있습니다.
        /// </remarks>
        /// <param name="attribute"></param>
        /// <param name="entity"></param>
        void OnProxyRemoved(AttributeBase attribute, IEntity entity);
    }
}
