namespace Syadeu.Collections
{
    /// <summary>
    /// <see cref="EntityData{T}{T}"/>, <see cref="Entity{T}"/> 에 달릴 수 있는 컴포넌트를 만듭니다.
    /// </summary>
    /// <remarks>
    /// 사용자는 추가로 <seealso cref="System.IDisposable"/> 을 상속하여 엔티티가 파괴될 때 실행할 메모리 수집을
    /// 선언할 수 있습니다. <seealso cref="INotifyComponent{TComponent}"/> 를 참조하세요.
    /// <br/>
    /// 선언된 컴포넌트는 <seealso cref="EntityData{T}.AddComponent{TComponent}(TComponent)"/>, 
    /// <seealso cref="Entity{T}.AddComponent{TComponent}(TComponent)"/> 로 추가할 수 있고, 
    /// <seealso cref="EntityData{T}.GetComponent{TComponent}"/>, <seealso cref="Entity{T}.GetComponent{TComponent}"/>
    /// 를 통해 가져올 수 있습니다.
    /// </remarks>
    public interface IEntityComponent { }    
}
