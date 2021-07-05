using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Presentation
{
    public class CustomLoadingScene : MonoBehaviour
    {
        [SerializeField] private bool m_StartOnAwake = true;

        [SerializeField] protected Camera m_Camera;
        [SerializeField] protected Canvas m_Canvas;
        [SerializeField] protected CanvasGroup m_FadeGroup;
        [SerializeField] protected Image m_BackgroundImage;

        private void Start()
        {
            if (m_Camera == null) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                "Camera 는 null 이 될 수 없습니다.");
            if (m_Canvas == null) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                "Canvas 는 null 이 될 수 없습니다.");
            if (m_FadeGroup == null) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                "FadeGroup 은 null 이 될 수 없습니다.");

            CoreSystem.StartUnityUpdate(this, InternalInitialize());
        }
        private IEnumerator InternalInitialize()
        {
            if (m_StartOnAwake)
            {
                yield return PresentationSystem<SceneSystem>.GetAwaiter();
                Initialize();
            }
        }

        protected void Initialize()
        {
            SceneSystem sceneSystem = PresentationSystem<SceneSystem>.GetSystem();
            sceneSystem.SetLoadingScene(m_Camera, m_Canvas, m_FadeGroup, m_BackgroundImage);
        }
    }
}
