using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Syadeu.Mono
{
    [Obsolete("Will be deprecated and replaced to based on IRender interface MonoBehaviour", true)]
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
        public bool IsInvisible { get; private set; } = true;
        public bool IsForcedOff { get; internal set; } = false;

        private Transform Transform { get; set; }
        internal Vector3 Position { get; set; }
        internal bool Destroyed { get; private set; } = false;
        internal bool Listed { get; private set; } = false;

        // For standalone
        private Matrix4x4 Matrix { get; set; }

        private void Awake()
        {
            throw new Exception();

            Transform = transform;
            Renderers = Transform.GetComponentsInChildren<Renderer>();

            if (!IsStandalone)
            {
                RenderManager.Instance.m_ManagedObjects.Add(new RenderManager.ManagedObject(this));
                CoreSystem.OnBackgroundAsyncUpdate += ManagedUpdate;
                //CoreSystem.StartBackgroundUpdate(Transform, ManagedUpdate(RenderManager.Instance));
            }
            else
            {
                if (m_Camera == null) throw new CoreSystemException(CoreSystemExceptionFlag.Render, "스탠드얼론으로 지정된 RenderController에서 카메라가 지정되지 않음");

                CoreSystem.OnUnityUpdate += OnStandaloneUnityUpdate;
                CoreSystem.StartBackgroundUpdate(Transform, StandaloneUpdate());
            }
        }

        //private void Update()
        //{
        //    Position = Transform.position;
        //    if (IsStandalone) Matrix = RenderManager.GetCameraMatrix4X4(m_Camera);
        //}
        private void OnDestroy()
        {
            if (IsStandalone) CoreSystem.OnUnityUpdate -= OnStandaloneUnityUpdate;
            else CoreSystem.OnBackgroundAsyncUpdate -= ManagedUpdate;

            Destroyed = true;
        }
        private void OnStandaloneUnityUpdate()
        {
            Position = Transform.position;
            Matrix = RenderManager.GetCameraMatrix4X4(m_Camera);
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

                //Matrix4x4 matrixVP = m_Camera.projectionMatrix * m_Camera.worldToCameraMatrix; //multipication order matters
                //Vector4 clipCoord = matrixVP.MultiplyPoint(Position);
                //Vector3 screenPos = new Vector3(clipCoord.x + 1f, clipCoord.y + 1f, clipCoord.z + 1f) / 2f;

                //Matrix4x4 p = Matrix;
                //Vector4 p4 = Position;
                //p4.w = 1;
                //Vector4 result4 = p * p4;
                //Vector3 screenPoint = result4;
                //screenPoint /= -result4.w;
                //screenPoint.x = screenPoint.x / 2 + 0.5f;
                //screenPoint.y = screenPoint.y / 2 + 0.5f;
                //screenPoint.z = -result4.w;

                //$"{screenPoint}".ToLog();
                yield return null;
            }
        }
        private void ManagedUpdate(CoreSystem.Awaiter awaiter)
        {
            if (RenderManager.IsInCameraScreen(Position))
            {
                IsInvisible = false;

                if (!Listed)
                {
                    //mgr.AddRenderControl(this);

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
        }
        //private IEnumerator ManagedUpdate(RenderManager mgr)
        //{
        //    while (!Destroyed && mgr != null)
        //    {
        //        if (RenderManager.IsInCameraScreen(Position))
        //        {
        //            IsInvisible = false;

        //            if (!Listed)
        //            {
        //                //mgr.AddRenderControl(this);

        //                CoreSystem.AddForegroundJob(InvokeOnVisible);
        //                Listed = true;
        //            }
        //        }
        //        else
        //        {
        //            IsInvisible = true;
                    
        //            if (Listed)
        //            {
        //                CoreSystem.AddForegroundJob(InvokeOnInvisible);
        //                Listed = false;
        //            }
        //        }

        //        yield return null;
        //    }
        //}

        private void InvokeOnVisible() => OnVisible?.Invoke();
        private void InvokeOnInvisible() => OnInvisible?.Invoke();

        public Vector3 GetScreenPoint()
        {
            if (IsStandalone) return RenderManager.GetScreenPoint(Matrix, Position);
            else
            {
                return RenderManager.GetScreenPoint(RenderManager.Instance.CamMatrix4x4, Position);
            }
        }

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
