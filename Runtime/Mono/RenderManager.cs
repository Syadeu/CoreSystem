using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Syadeu.Database;
using Syadeu.Extensions.Logs;

using UnityEngine;

namespace Syadeu.Mono
{
    public sealed class RenderManager : StaticManager<RenderManager>
    {
        public delegate bool RenderCondition();

        public override bool DontDestroy => false;
        public override bool HideInHierarchy => false;

        public List<ManagedObject> ManagedObjects = new List<ManagedObject>();

        internal readonly ObClass<Camera> m_MainCamera = new ObClass<Camera>(ObValueDetection.Changed);

        internal Matrix4x4 CamMatrix4x4 { get; private set; }

        [Serializable]
        public class ManagedObject
        {
            public RenderController Controller { get; }
            public ManagedObject(RenderController controller)
            {
                Controller = controller;
            }
        }

        private void Awake()
        {
            m_MainCamera.OnValueChange += MainCamera_OnValueChange;
            m_MainCamera.Value = Camera.main;

            //StartUnityUpdate(UnityUpdate());
        }

        private void MainCamera_OnValueChange(Camera current, Camera target)
        {
            if (target == null) return;

            CamMatrix4x4 = GetCameraMatrix4X4(target);
        }

        private void Update()
        {
            CamMatrix4x4 = GetCameraMatrix4X4(m_MainCamera.Value);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < ManagedObjects.Count; i++)
            {
                if (ManagedObjects[i].Controller == null) continue;
                ManagedObjects[i].Controller.StopAllCoroutines();
            }
            ManagedObjects.Clear();
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

        internal bool IsInCameraScreen(Vector3 worldPosition)
            => IsInCameraScreen(worldPosition, CamMatrix4x4, SyadeuSettings.Instance.m_ScreenOffset);

        internal static Matrix4x4 GetCameraMatrix4X4(Camera cam)
            => cam.projectionMatrix * cam.transform.worldToLocalMatrix;
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
        internal static bool IsInCameraScreen(Vector3 worldPosition, Matrix4x4 matrix, Vector3 offset)
        {
            Vector3 screenPoint = GetScreenPoint(matrix, worldPosition);

            return screenPoint.z > 0 - offset.z &&
                screenPoint.x > 0 - offset.x &&
                screenPoint.x < 1 + offset.x &&
                screenPoint.y > 0 - offset.y &&
                screenPoint.y < 1 + offset.y;
        }
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
    }
}