namespace Syadeu.Presentation.Attributes
{
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
}
