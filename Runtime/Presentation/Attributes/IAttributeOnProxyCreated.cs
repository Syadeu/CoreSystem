using Syadeu.Mono;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Attributes
{
    /// <summary>
    /// <see cref="AttributeBase"/>에서 프록시 오브젝트가 생성되었을 때 실행될 동작을 선언할 수 있습니다.<br/>
    /// 이 인터페이스 단일로는 동작하지않습니다. <seealso cref="AttributeProcessor"/>를 참조하는 class에 상속받아 사용되어야됩니다.
    /// </summary>
    public interface IAttributeOnProxyCreated
    {
        /// <summary>
        /// <see cref="AttributeProcessor.Target"/>에 설정된 <see cref="AttributeBase"/>가 부착된 <see cref="IEntity"/>의
        /// 프록시 오브젝트가 생성되어 부착되었을 때 실행되는 메소드입니다.<br/>
        /// 이 메소드가 호출되었을 때에는 이미 프록시 오브젝트가 생성되어 받아올 수 있습니다.
        /// <seealso cref="IEntity.gameObject"/>에서 <seealso cref="DataGameObject.GetProxyObject"/>로 프록시 오브젝트를 받아올 수 있습니다.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="entity"></param>
        void OnProxyCreated(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj);
    }
}
