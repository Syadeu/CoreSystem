#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Presentation.Internal;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <typeparamref name="TSystem"/> 에 추가될 서브 모듈을 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 모듈은 선언된 이후 <typeparamref name="TSystem"/> 가 <see cref="INotifySystemModule{TModule}"/> 을 상속받고
    /// 이 모듈을 선언하여야 합니다.
    /// </remarks>
    /// <typeparam name="TSystem"></typeparam>
    public abstract class PresentationSystemModule<TSystem> : PresentationSystemModuleBase
        where TSystem : PresentationSystemEntity
    {
        protected TSystem System => (TSystem)m_System;
    }
}
