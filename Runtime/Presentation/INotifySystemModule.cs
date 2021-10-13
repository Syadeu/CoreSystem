#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif


namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="PresentationSystemEntity{T}"/> 에 서브 모듈(<typeparamref name="TModule"/>)을 추가합니다.
    /// </summary>
    /// <remarks>
    /// 서브 모듈은 항상 메인 시스템이 동작한 이후에 수행됩니다.
    /// </remarks>
    /// <typeparam name="TModule"></typeparam>
    public interface INotifySystemModule<TModule> where TModule : PresentationSystemModuleBase
    {
    }
}
