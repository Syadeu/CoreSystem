using Syadeu.Mono;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Attributes
{
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
