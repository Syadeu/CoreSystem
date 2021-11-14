#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

#undef UNITY_ADDRESSABLES

using Syadeu.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
#endif

namespace Syadeu.Presentation
{
    /// <summary>
    /// Scene 에 수동으로 필요한 에셋을 등록할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 이 구현부를 상속받은 객체는 <see cref="SceneSystem.RegisterSceneAsset(SceneReference, INotifyAsset)"/> 을 통해 등록할 수 있습니다.
    /// <br/>
    /// 자동 등록은 <seealso cref="INotifySceneAsset"/> 을 참조하세요.
    /// </remarks>
    public interface INotifyAsset
    {
        IEnumerable<IPrefabReference> NotifyAssets { get; }
    }
}
