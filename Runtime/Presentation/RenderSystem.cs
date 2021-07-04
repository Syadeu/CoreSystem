#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

using Syadeu.Database;
using Syadeu.Mono;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class RenderSystem : PresentationSystemEntity<RenderSystem>
    {
        private ObClass<Camera> m_Camera;
        private Matrix4x4 m_Matrix4x4;

        private readonly List<ObserverObject> m_ObserverList = new List<ObserverObject>();

        private class ObserverObject
        {
            public IRender m_Object;
            public bool m_IsVisible = false;
        }

        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        public Camera Camera => m_Camera.Value;

        public override PresentationResult OnInitialize()
        {
            m_Camera = new ObClass<Camera>(ObValueDetection.Changed);
            m_Camera.OnValueChange += (from, to) =>
            {
                m_Matrix4x4 = GetCameraMatrix4X4(to);
            };

            return base.OnInitialize();
        }
        public override PresentationResult BeforePresentation()
        {
            if (m_Camera.Value == null)
            {
                m_Camera.Value = Camera.main;
            }

            return base.BeforePresentation();
        }
        public override PresentationResult OnPresentation()
        {
            for (int i = 0; i < m_ObserverList.Count; i++)
            {
                if (m_ObserverList[i].m_Object.transform == null)
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


            base.Dispose();
        }

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
        public bool IsInCameraScreen(Vector3 worldPosition) => IsInCameraScreen(m_Camera.Value, worldPosition);
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
    }
}
