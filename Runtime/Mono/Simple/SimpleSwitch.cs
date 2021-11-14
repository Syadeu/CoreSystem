using UnityEngine;
using UnityEngine.Events;

namespace Syadeu.Mono
{
    [AddComponentMenu("CoreSystem/Simple/Switch")]
    public sealed class SimpleSwitch : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Content
        {
            public string m_Name;
            public Object m_Object;

            [Space]
            public UnityEvent m_OnSelected;
            public UnityEvent m_OnDeselected;

            [Space]
            public bool m_Default;
            internal bool m_Enabled = true;
        }

        [SerializeField] private Content[] m_Contents;

        private void Awake()
        {
            for (int i = 0; i < m_Contents.Length; i++)
            {
                if (m_Contents[i].m_Object == null) continue;

                if (m_Contents[i].m_Object is GameObject gameObj)
                {
                    gameObj.SetActive(m_Contents[i].m_Default);
                }
                else if (m_Contents[i].m_Object is Behaviour behaviour)
                {
                    behaviour.enabled = m_Contents[i].m_Default;
                }
                else throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"지원하지 않는 오브젝트 타입 ({m_Contents[i].m_Object.GetType().Name})");

                m_Contents[i].m_Enabled = m_Contents[i].m_Default;
            }
        }

        //public void Toogle(int idx)
        //{
        //    if (m_Contents[idx].m_Object is GameObject gameObj)
        //    {
        //        gameObj.SetActive(!gameObj.activeInHierarchy);
        //    }
        //    else if (m_Contents[idx].m_Object is Behaviour behaviour)
        //    {
        //        behaviour.enabled = !behaviour.enabled;
        //    }
        //    else throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
        //        $"지원하지 않는 오브젝트 타입 ({m_Contents[idx].m_Object.GetType().Name})");
        //}

        public void On(int idx)
        {
            if (!m_Contents[idx].m_Enabled && m_Contents[idx].m_Object != null)
            {
                if (m_Contents[idx].m_Object is GameObject gameObj)
                {
                    gameObj.SetActive(true);
                }
                else if (m_Contents[idx].m_Object is Behaviour behaviour)
                {
                    behaviour.enabled = true;
                }
                else throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"지원하지 않는 오브젝트 타입 ({m_Contents[idx].m_Object.GetType().Name})");

                m_Contents[idx].m_OnSelected?.Invoke();
                m_Contents[idx].m_Enabled = true;
            }
            
            for (int i = 0; i < m_Contents.Length; i++)
            {
                if (i == idx) continue;
                Off(i);
            }
        }
        public void Off(int idx)
        {
            if (m_Contents[idx].m_Enabled && m_Contents[idx].m_Object != null)
            {
                if (m_Contents[idx].m_Object is GameObject gameObj)
                {
                    gameObj.SetActive(false);
                }
                else if (m_Contents[idx].m_Object is Behaviour behaviour)
                {
                    behaviour.enabled = false;
                }
                else throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"지원하지 않는 오브젝트 타입 ({m_Contents[idx].m_Object.GetType().Name})");

                m_Contents[idx].m_OnDeselected?.Invoke();
                m_Contents[idx].m_Enabled = false;
            }
        }
    }
}