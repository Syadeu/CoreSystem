namespace Syadeu.Presentation
{
    /// <summary>
    /// Presentation Manager에 유저 시스템을 등록하기 위한 인터페이스입니다.<br/>
    /// <see cref="PresentationRegisterEntity.RegisterSystem(System.Type[])"/> 을 <see cref="Register"/>에서 호출하여 등록하세요.
    /// </summary>
    public interface IPresentationRegister
    {
        /// <summary>
        /// <see cref="PresentationManager"/>에서 호출되는 메소드입니다.<br/>
        /// <see cref="PresentationRegisterEntity.RegisterSystem(System.Type[])"/> 을 여기서 호출하여 등록하세요.
        /// </summary>
        void Register();
    }
}
