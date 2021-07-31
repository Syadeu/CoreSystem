namespace Syadeu.Presentation.Attributes
{
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
}
