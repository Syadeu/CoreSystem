﻿#undef UNITY_ADDRESSABLES

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation
{
    [RequireGlobalConfig("General")]
    public sealed class RenderSystem : PresentationSystemEntity<RenderSystem>
    {
        private ObClass<Camera> m_Camera;
        private Matrix4x4 m_Matrix4x4;

        private readonly List<ObserverObject> m_ObserverList = new List<ObserverObject>();

        [ConfigValue(Header = "Screen", Name = "ResolutionX")] private int m_ResolutionX;
        [ConfigValue(Header = "Screen", Name = "ResolutionY")] private int m_ResolutionY;

        private class ObserverObject
        {
            public IRender m_Object;
            public bool m_IsVisible = false;
        }

        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        public Camera Camera => m_Camera.Value;
        public event Action OnRender;

        internal List<CoreRoutine> m_PreRenderRoutines = new List<CoreRoutine>();
        internal List<CoreRoutine> m_PostRenderRoutines = new List<CoreRoutine>();

        private Vector3 m_ScreenOffset;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_Camera = new ObClass<Camera>(ObValueDetection.Changed);
            m_Camera.OnValueChange += (from, to) =>
            {
                if (to == null) return;
                //if (m_TopdownCamera != null)
                //{
                //    m_TopdownCamera.transform.SetParent(to.transform);
                //}
                //else
                //{
                //    GameObject obj = new GameObject("RenderSystem.Camera");
                //    m_TopdownCamera = obj.AddComponent<Camera>();
                //    //m_TopdownCamera.enabled = false;
                //    m_TopdownCamera.targetDisplay = 1;

                //    Vector3 pos = to.transform.position;
                //    pos.y += 50;
                //    m_TopdownCamera.transform.position = pos;

                //    m_TopdownCamera.transform.SetParent(to.transform);
                //    m_TopdownCamera.transform.eulerAngles = new Vector3(90, 0, 0);
                //}
                m_Matrix4x4 = GetCameraMatrix4X4(to);
                if (to.GetComponent<CameraComponent>() == null)
                {
                    to.gameObject.AddComponent<CameraComponent>().Initialize(this);
                }
                else to.GetComponent<CameraComponent>().Initialize(this);
            };
            m_ScreenOffset = SyadeuSettings.Instance.m_ScreenOffset;

            CoreSystem.Instance.OnRender -= Instance_OnRender;
            CoreSystem.Instance.OnRender += Instance_OnRender;

            return base.OnInitialize();
        }

        private void Instance_OnRender()
        {
            OnRender?.Invoke();
        }

        protected override PresentationResult BeforePresentation()
        {
            m_ScreenOffset = SyadeuSettings.Instance.m_ScreenOffset;
            if (m_Camera.Value == null)
            {
                m_Camera.Value = Camera.main;
                if (Camera == null) return PresentationResult.Warning("Cam not found");
            }
            m_Matrix4x4 = GetCameraMatrix4X4(m_Camera.Value);

            return base.BeforePresentation();
        }
        protected override PresentationResult OnPresentation()
        {
            if (Camera == null) return PresentationResult.Warning("Cam not found");

            for (int i = 0; i < m_ObserverList.Count; i++)
            {
                if (m_ObserverList[i].m_Object == null ||
                    m_ObserverList[i].m_Object.transform == null)
                {
                    m_ObserverList.RemoveAt(i);
                    i--;
                    continue;
                }

                if (!m_ObserverList[i].m_IsVisible &&
                    IsInCameraScreen(m_Camera.Value, m_ObserverList[i].m_Object.transform.position))
                {
                    m_ObserverList[i].m_Object.OnVisible();
                    m_ObserverList[i].m_IsVisible = true;
                }

                if (m_ObserverList[i].m_IsVisible &&
                    !IsInCameraScreen(m_Camera.Value, m_ObserverList[i].m_Object.transform.position))
                {
                    m_ObserverList[i].m_Object.OnInvisible();
                    m_ObserverList[i].m_IsVisible = false;
                }
            }

            return base.OnPresentation();
        }
        public override void Dispose()
        {
            CoreSystem.Instance.OnRender -= Instance_OnRender;

            base.Dispose();
        }

        #endregion

        public void AddObserver(IRender render)
        {
            bool visible = IsInCameraScreen(m_Camera.Value, render.transform.position);

            m_ObserverList.Add(new ObserverObject
            {
                m_Object = render,
                m_IsVisible = visible
            });

            if (visible) render.OnVisible();
            else render.OnInvisible();
        }
        public void RemoveObserver(IRender render)
        {
            for (int i = 0; i < m_ObserverList.Count; i++)
            {
                if (m_ObserverList[i].m_Object.Equals(render))
                {
                    m_ObserverList.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// 해당 월드 좌표를 입력한 Matrix 기반으로 2D 좌표값을 반환합니다.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public static Vector3 GetScreenPoint(Matrix4x4 matrix, Vector3 worldPosition)
        {
            Vector4 p4 = worldPosition;
            p4.w = 1;
            Vector4 result4 = matrix * p4;
            Vector3 screenPoint = result4;
            screenPoint /= -result4.w;
            screenPoint.x = screenPoint.x / 2 + 0.5f;
            screenPoint.y = screenPoint.y / 2 + 0.5f;
            screenPoint.z = -result4.w;

            return screenPoint;
        }
        internal static Matrix4x4 GetCameraMatrix4X4(Camera cam) => cam.projectionMatrix * cam.transform.worldToLocalMatrix;
        /// <inheritdoc cref="IsInCameraScreen(Camera, Vector3)"/>
        public bool IsInCameraScreen(Vector3 worldPosition)
        {
            //if (CoreSystem.IsThisMainthread())
            //{
            //    return IsInCameraScreen(m_Camera.Value, worldPosition);
            //}
            //Unity.Mathematics.float4x4 matrix4x4 = Camera.projectionMatrix;
            //math.
            return IsInCameraScreen(worldPosition, m_Matrix4x4, m_ScreenOffset) 
                /*|| IsInCameraScreen(worldPosition, m_TopMatrix4x4, m_ScreenOffset)*/;
        }
        /// <summary>
        /// 해당 좌표가 입력한 카메라 내부에 위치하는지 반환합니다.
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public static bool IsInCameraScreen(Camera cam, Vector3 worldPosition)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
            screenPos.y = Screen.height - screenPos.y;

            if (screenPos.y < 0 || screenPos.y > Screen.height ||
                screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
            {
                return false;
            }
            return true;
        }
        internal static bool IsInCameraScreen(Vector3 worldPosition, Matrix4x4 matrix, Vector3 offset)
        {
            Vector3 screenPoint = GetScreenPoint(matrix, worldPosition);

            return screenPoint.z > 0 - offset.z &&
                screenPoint.x > 0 - offset.x &&
                screenPoint.x < 1 + offset.x &&
                screenPoint.y > 0 - offset.y &&
                screenPoint.y < 1 + offset.y;
        }

        public void StartPreRender(IEnumerator iter)
        {
            CoreRoutine routine = new CoreRoutine(iter, false);
            m_PreRenderRoutines.Add(routine);
        }
        public void StartPostRender(IEnumerator iter)
        {
            CoreRoutine routine = new CoreRoutine(iter, false);
            m_PostRenderRoutines.Add(routine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 TRS(float3 translation, quaternion rotation, float3 scale)
        {
            float3x3 r = new float3x3(rotation);
            return
                new float4x4(
                    new float4(r.c0 * scale.x, 0),
                    new float4(r.c1 * scale.y, 0),
                    new float4(r.c2 * scale.z, 0),
                    new float4(translation, 1)
                    );
        }
        public static float4x4 LocalToWorldMatrix(float3 translation, quaternion rotation)
        {
            float3x3 r = new float3x3(rotation);
            return new float4x4(r, translation);
        }
        public static float4x4 WorldToLocalMatrix(float3 translation, quaternion rotation) => math.fastinverse(LocalToWorldMatrix(translation, rotation));
    }

    public sealed class CameraComponent : MonoBehaviour
    {
        private RenderSystem m_System;

        public void Initialize(RenderSystem system)
        {
            m_System = system;
        }

        private IEnumerator OnPreRender()
        {
            while (true)
            {
                for (int i = m_System.m_PreRenderRoutines.Count - 1; i >= 0; i--)
                {
                    if (m_System.m_PreRenderRoutines[i].Iterator.Current == null)
                    {
                        if (!m_System.m_PreRenderRoutines[i].Iterator.MoveNext())
                        {
                            m_System.m_PreRenderRoutines.RemoveAt(i);
                            continue;
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                yield return null;
            }
        }
        private IEnumerator OnPostRender()
        {
            while (true)
            {
                for (int i = m_System.m_PostRenderRoutines.Count - 1; i >= 0; i--)
                {
                    if (m_System.m_PostRenderRoutines[i].Iterator.Current == null)
                    {
                        if (!m_System.m_PostRenderRoutines[i].Iterator.MoveNext())
                        {
                            m_System.m_PostRenderRoutines.RemoveAt(i);
                            continue;
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                yield return null;
            }
        }
    }
}
