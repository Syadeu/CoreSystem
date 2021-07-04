namespace Syadeu.Presentation
{
    /// <summary>
    /// Presentation Manager에 유저 시스템을 등록하기 위한 인터페이스입니다.<br/>
    /// <see cref="PresentationRegisterEntity.RegisterSystem(System.Type[])"/> 을 <see cref="Register"/>에서 호출하여 등록하세요.
    /// </summary>
    /// <remarks>
    /// 클래스에 직접 참조하여 사용하게끔 만들지 않았습니다.<br/>
    /// <seealso cref="PresentationRegisterEntity"/>을 참조하여 사용하세요.
    /// </remarks>
    public interface IPresentationRegister
    {
        /// <summary>
        /// <see langword="null"/> 이 아닐 경우, 해당 씬이 로드되거나 언로드 되면, 자동으로 활성화되고 비활성화 됩니다.
        /// </summary>
        SceneReference DependenceScene { get; }
        /// <summary>
        /// <see cref="PresentationManager"/>에서 호출되는 메소드입니다.<br/>
        /// <see cref="PresentationRegisterEntity.RegisterSystem(System.Type[])"/> 을 여기서 호출하여 등록하세요.
        /// </summary>
        void Register();
    }
}
