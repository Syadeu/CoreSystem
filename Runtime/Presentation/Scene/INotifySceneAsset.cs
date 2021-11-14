#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="TargetScene"/> 에 자동으로 필요한 에셋을 등록합니다.
    /// </summary>
    /// <remarks>
    /// 사용자에 의해 수동으로 추가될 필요가 없습니다.
    /// </remarks>
    public interface INotifySceneAsset : INotifyAsset
    {
        SceneReference TargetScene { get; }
    }
}
