using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Presentation
{
    public class CustomLoadingScene : LoadingSceneSetupEntity
    {
        [SerializeField] protected Camera m_Camera;
        [SerializeField] protected CanvasGroup m_FadeGroup;

        protected override void Start()
        {
            //if (m_Camera == null) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
            //    "Camera 는 null 이 될 수 없습니다.");
            if (m_FadeGroup == null) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                "FadeGroup 은 null 이 될 수 없습니다.");

            m_OnLoadingEnter.AddListener(() =>
            {
                m_FadeGroup.interactable = true;
                m_FadeGroup.blocksRaycasts = true;
                m_FadeGroup.Lerp(1, Time.deltaTime * 2);
            });
            m_OnLoadingExit.AddListener(() =>
            {
                m_FadeGroup.interactable = false;
                m_FadeGroup.blocksRaycasts = false;
                m_FadeGroup.Lerp(0, Time.deltaTime * 2);
            });

            Initialize();
        }
    }
}
