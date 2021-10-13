#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Presentation.Internal;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="PresentationSystemEntity{T}"/> 에 추가될 서브 모듈을 선언할 수 있습니다.
    /// </summary>
    /// <typeparam name="TSystem"></typeparam>
    public abstract class PresentationSystemModule<TSystem> : PresentationSystemModuleBase
        where TSystem : PresentationSystemEntity
    {
        protected TSystem System => (TSystem)m_System;
    }
}
