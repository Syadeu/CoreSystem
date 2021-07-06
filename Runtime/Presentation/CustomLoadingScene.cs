using System.Collections;
using UnityEngine;
using UnityEngine.Events;
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

        [Space]
        [SerializeField] protected UnityEvent m_OnLoadingEnter;
        [SerializeField] protected UnityEvent<float> m_OnLoading;
        [SerializeField] protected UnityEvent m_OnLoadingExit;

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

                Destroy(gameObject);
            }
        }

        protected void Initialize()
        {
            SceneSystem sceneSystem = PresentationSystem<SceneSystem>.GetSystem();
            sceneSystem.SetLoadingScene(m_Camera, m_FadeGroup,
                () => m_FadeGroup.Lerp(1, Time.deltaTime * 2),
                m_OnLoading.Invoke,
                () => m_FadeGroup.Lerp(0, Time.deltaTime * 2)
                );
        }
    }
}
