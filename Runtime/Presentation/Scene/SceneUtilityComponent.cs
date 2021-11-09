#undef UNITY_ADDRESSABLES

using UnityEngine;

#if UNITY_EDITOR
#endif

namespace Syadeu.Presentation
{
    public sealed class SceneUtilityComponent : MonoBehaviour
    {
        [SerializeField] private int m_SceneIndex;
        [SerializeField] private float m_PreDelay;
        [SerializeField] private float m_PostDelay;

        public void LoadStartScene(float preDelay, float postDelay)
        {
            PresentationSystem<DefaultPresentationGroup, SceneSystem>
                .System
                .LoadStartScene(preDelay, postDelay);
        }
        public void LoadScene(int index, float preDelay, float postDelay)
        {
            PresentationSystem<DefaultPresentationGroup, SceneSystem>
                .System
                .LoadScene(index, preDelay, postDelay);
        }

        public void LoadStartScene() => LoadStartScene(m_PreDelay, m_PostDelay);
        public void LoadScene() => LoadScene(m_SceneIndex, m_PreDelay, m_PostDelay);
    }
}
