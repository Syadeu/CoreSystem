//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif


using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="PresentationManager"/>에게 시스템을 등록하기 위한 레지스터 클래스입니다.
    /// </summary>
    public abstract class PresentationRegisterEntity : IPresentationRegister
    {
        public abstract void Register();

        /// <summary>
        /// 해당 시스템을 등록합니다.
        /// </summary>
        /// <remarks>
        /// <see cref="Register"/>에서만 실행되어야 됩니다.
        /// </remarks>
        /// <param name="systems"></param>
        protected void RegisterSystem(params Type[] systems)
            => PresentationManager.RegisterSystem(GetType(), systems);
    }
}
