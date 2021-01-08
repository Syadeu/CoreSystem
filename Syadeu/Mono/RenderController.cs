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

        internal Vector3 Position { get; private set; }
        internal bool Destroyed { get; private set; } = false;
        internal bool Listed { get; private set; } = false;

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

            CoreSystem.Instance.StartBackgroundUpdate(BackgroundUpdate());
        }

        private void Update()
        {
            Position = transform.position;
        }
        private void OnDestroy()
        {
            Destroyed = true;
        }

        private IEnumerator BackgroundUpdate()
        {
            while (!Destroyed)
            {
                if (RenderManager.Instance.IsInCameraScreen(Position))
                {
                    IsInvisible = false;

                    if (IsForcedOff && WhileVisible.Invoke())
                    {
                        RenderOn();
                    }

                    if (!Listed)
                    {
                        if (!IsStandalone) RenderManager.Instance.AddRenderControl(this);
                        OnVisible?.Invoke();
                        Listed = true;
                    }
                }
                else
                {
                    IsInvisible = true;
                    
                    if (Listed)
                    {
                        OnInvisible?.Invoke();
                        Listed = false;
                    }
                }

                yield return null;
            }
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
