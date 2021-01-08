using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Syadeu.Mono
{
    public class RenderController : MonoBehaviour
    {
        public bool IsStandalone = false;

        [SerializeField] private Camera m_Camera;
        [SerializeField] private Vector3 m_Offset;

        [Space]
        public List<Behaviour> AdditionalRenders = new List<Behaviour>();

        [Space]
        [SerializeField] private UnityEvent OnVisible;
        [SerializeField] private UnityEvent OnInvisible;

        internal Renderer[] Renderers { get; private set; }
        public bool IsInvisible { get; set; } = true;
        public bool IsForcedOff { get; internal set; } = false;

        private Transform Transform { get; set; }
        internal Vector3 Position { get; private set; }
        internal bool Destroyed { get; private set; } = false;
        internal bool Listed { get; private set; } = false;

        // For standalone
        private Matrix4x4 Matrix { get; set; }

        private void Awake()
        {
            Transform = transform;
            Renderers = Transform.GetComponentsInChildren<Renderer>();

            if (!IsStandalone)
            {
                CoreSystem.StartBackgroundUpdate(Transform, ManagedUpdate(RenderManager.Instance));
            }
            else
            {
                if (m_Camera == null) throw new CoreSystemException(CoreSystemExceptionFlag.Render, "스탠드얼론으로 지정된 RenderController에서 카메라가 지정되지 않음");

                Matrix = RenderManager.GetCameraMatrix4X4(m_Camera);
                CoreSystem.StartBackgroundUpdate(Transform, StandaloneUpdate());
            }
        }

        private void Update()
        {
            Position = Transform.position;
        }
        private void OnDestroy()
        {
            Destroyed = true;
        }

        private IEnumerator StandaloneUpdate()
        {
            while (!Destroyed)
            {
                if (RenderManager.IsInCameraScreen(Position, Matrix, m_Offset))
                {
                    IsInvisible = false;

                    if (!Listed)
                    {
                        CoreSystem.AddForegroundJob(InvokeOnVisible);
                        Listed = true;
                    }
                }
                else
                {
                    IsInvisible = true;

                    if (Listed)
                    {
                        CoreSystem.AddForegroundJob(InvokeOnInvisible);
                        Listed = false;
                    }
                }

                yield return null;
            }
        }
        private IEnumerator ManagedUpdate(RenderManager mgr)
        {
            while (!Destroyed && mgr != null)
            {
                if (mgr.IsInCameraScreen(Position))
                {
                    IsInvisible = false;

                    if (!Listed)
                    {
                        mgr.AddRenderControl(this);

                        CoreSystem.AddForegroundJob(InvokeOnVisible);
                        Listed = true;
                    }
                }
                else
                {
                    IsInvisible = true;
                    
                    if (Listed)
                    {
                        CoreSystem.AddForegroundJob(InvokeOnInvisible);
                        Listed = false;
                    }
                }

                yield return null;
            }
        }

        private void InvokeOnVisible() => OnVisible?.Invoke();
        private void InvokeOnInvisible() => OnInvisible?.Invoke();

        public void RenderOff()
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
        public void RenderOn()
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
