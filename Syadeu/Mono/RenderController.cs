using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Collections;
using UnityEngine.Events;

namespace Syadeu.Mono
{
    public class RenderController : MonoBehaviour
    {
        public Camera Camera;
        public bool IsStandalone = false;

        [Space]
        public List<MonoBehaviour> AdditionalRenders = new List<MonoBehaviour>();

        [Space]
        [SerializeField] private UnityEvent OnVisible;
        [SerializeField] private UnityEvent OnInvisible;

        /// <summary>
        /// IsStandalone = true 인 상태면 이 delegate는 작동하지않음
        /// True를 반환하면 RenderOn, False면 RenderOFf
        /// </summary>
        public RenderManager.RenderCondition WhileVisible;

        internal Renderer[] Renderers { get; private set; }
        internal bool IsInvisible { get; set; } = true;
        internal bool IsForcedOff { get; set; } = false;

        private void Awake()
        {
            Renderers = transform.GetComponentsInChildren<Renderer>();

            if (Camera == null && !IsStandalone)
            {
                if (RenderManager.Instance.MainCamera == null)
                {
                    RenderManager.Instance.MainCamera = Camera.main;
                }

                Camera = RenderManager.Instance.MainCamera;
            }
        }

        private void OnBecameVisible()
        {
            IsInvisible = false;

            OnVisible?.Invoke();
            if (IsForcedOff && WhileVisible.Invoke())
            {
                RenderOn();
            }

            if (!IsStandalone) RenderManager.Instance.AddRenderControl(this);
        }
        private void OnBecameInvisible()
        {
            IsInvisible = true;

            OnInvisible?.Invoke();
        }

        internal void RenderOff()
        {
            IsForcedOff = true;
            foreach (var item in Renderers)
            {
                item.enabled = false;
            }
            for (int i = 0; i < AdditionalRenders.Count; i++)
            {
                AdditionalRenders[i].enabled = false;
            }
        }
        internal void RenderOn()
        {
            IsForcedOff = false;
            foreach (var item in Renderers)
            {
                item.enabled = true;
            }
            for (int i = 0; i < AdditionalRenders.Count; i++)
            {
                AdditionalRenders[i].enabled = true;
            }
        }
    }
}
