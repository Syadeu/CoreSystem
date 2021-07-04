using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Syadeu.Database;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Mono
{
    /// <summary>
    /// 퇴역합니다, 
    /// <see cref="Presentation.PresentationSystem{T}"/><see cref="Presentation.RenderSystem"/>을 사용하세요
    /// </summary>
    [Obsolete("", true)]
    public sealed class RenderManager : StaticManager<RenderManager>
    {
        #region Initialize

        public override bool DontDestroy => false;
        public override bool HideInHierarchy => false;

        internal readonly List<ManagedObject> m_ManagedObjects = new List<ManagedObject>();
        internal readonly ObClass<Camera> m_MainCamera = new ObClass<Camera>(ObValueDetection.Changed);
        internal readonly List<ObserverObject> m_ObserverList = new List<ObserverObject>();

        public static Camera MainCamera => Instance.m_MainCamera.Value;
        internal Matrix4x4 CamMatrix4x4 { get; private set; }

        [Serializable]
        public class ManagedObject
        {
            public RenderController Controller { get; }
            public Transform Transform { get; }
            public ManagedObject(RenderController controller)
            {
                Controller = controller;
                Transform = controller.transform;
            }
        }
        internal class ObserverObject
        {
            public IRender render;
            public bool visible = false;
        }

        private void Awake()
        {
            m_MainCamera.OnValueChange += MainCamera_OnValueChange;
            m_MainCamera.Value = Camera.main;

            StartCoroutine(ComponentUpdate());
            StartCoroutine(ObserverUpdate());
        }

        private void MainCamera_OnValueChange(Camera current, Camera target)
        {
            if (target != null)
            {
                CamMatrix4x4 = GetCameraMatrix4X4(target);
            }
        }

        private IEnumerator ComponentUpdate()
        {
            while (true)
            {
                if (m_MainCamera.Value != null)
                {
                    CamMatrix4x4 = GetCameraMatrix4X4(m_MainCamera.Value);
                }
                else m_MainCamera.Value = Camera.main;

                for (int i = 0; i < m_ManagedObjects.Count; i++)
                {
                    m_ManagedObjects[i].Controller.Position = m_ManagedObjects[i].Transform.position;

                    if (i != 0 && i % 150 == 0) yield return null;
                }

                yield return null;
            }
        }
        private IEnumerator ObserverUpdate()
        {
            while (true)
            {
                for (int i = 0; i < m_ObserverList.Count; i++)
                {
                    if (m_ObserverList[i].render.transform == null)
                    {
                        m_ObserverList.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (!m_ObserverList[i].visible &&
                        IsInCameraScreen(m_ObserverList[i].render.transform.position))
                    {
                        m_ObserverList[i].render.OnVisible();
                        m_ObserverList[i].visible = true;
                    }

                    if (m_ObserverList[i].visible &&
                        !IsInCameraScreen(m_ObserverList[i].render.transform.position))
                    {
                        m_ObserverList[i].render.OnInvisible();
                        m_ObserverList[i].visible = false;
                    }

                    if (i != 0 && i % 150 == 0) yield return null;
                }

                yield return null;
            }
        }
        protected override void OnDestroy()
        {
            for (int i = 0; i < m_ManagedObjects.Count; i++)
            {
                if (m_ManagedObjects[i].Controller == null) continue;
                m_ManagedObjects[i].Controller.StopAllCoroutines();
            }
            m_ManagedObjects.Clear();

            StopAllCoroutines();
            base.OnDestroy();
        }

        #endregion

        public static void AddObserver(IRender render)
        {
            bool visible = IsInCameraScreen(render.transform.position);

            Instance.m_ObserverList.Add(new ObserverObject
            {
                render = render,
                visible = visible
            });

            if (visible) render.OnVisible();
            else render.OnInvisible();
        }
        public static void RemoveObserver(IRender render)
        {
            for (int i = 0; i < Instance.m_ObserverList.Count; i++)
            {
                if (Instance.m_ObserverList[i].render.Equals(render))
                {
                    Instance.m_ObserverList.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// 렌더링 규칙을 적용할 카메라를 설정합니다.
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="offset"></param>
        public static void SetCamera(Camera cam)
        {
            Instance.m_MainCamera.Value = cam;
        }
        /// <summary>
        /// 해당 좌표가 RenderManager가 감시하는 카메라의 Matrix 내 위치하는지 반환합니다.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public static bool IsInCameraScreen(Vector3 worldPosition)
        {
#if UNITY_EDITOR
            if (IsMainthread())
            {
                try
                {
                    if (!Application.isPlaying)
                    {
                        return IsInCameraScreen(worldPosition, GetCameraMatrix4X4(SceneView.lastActiveSceneView.camera), SyadeuSettings.Instance.m_ScreenOffset);
                    }
                    else
                    {
                        return IsInCameraScreen(worldPosition, Instance.CamMatrix4x4, SyadeuSettings.Instance.m_ScreenOffset);
                    }
                }
                catch (UnityException)
                {
                    return IsInCameraScreen(worldPosition, Instance.CamMatrix4x4, SyadeuSettings.Instance.m_ScreenOffset);
                }
            }
            else
#endif
            {
                return IsInCameraScreen(worldPosition, Instance.CamMatrix4x4, SyadeuSettings.Instance.m_ScreenOffset);
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

        internal static Matrix4x4 GetCameraMatrix4X4(Camera cam)
            => cam.projectionMatrix * cam.transform.worldToLocalMatrix;
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