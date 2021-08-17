#undef UNITY_ADDRESSABLES

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
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    [RequireGlobalConfig("Graphics")]
    public sealed class RenderSystem : PresentationSystemEntity<RenderSystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => true;

        private ObClass<Camera> m_Camera;
        private Matrix4x4 m_Matrix4x4;

        [ConfigValue(Header = "Resolution", Name = "X")] private int m_ResolutionX;
        [ConfigValue(Header = "Resolution", Name = "Y")] private int m_ResolutionY;

        public Camera Camera => m_Camera.Value;
		public CameraFrustum.ReadOnly Frustum => GetFrustum(Allocator.TempJob);

        public event Action<Camera, Camera> OnCameraChanged;
        public event Action OnRender;

        internal List<CoreRoutine> m_PreRenderRoutines = new List<CoreRoutine>();
        internal List<CoreRoutine> m_PostRenderRoutines = new List<CoreRoutine>();

        private JobHandle m_FrustumJob;
		private CameraFrustum m_CameraFrustum;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_CameraFrustum = new CameraFrustum(new CameraData());

            m_Camera = new ObClass<Camera>(ObValueDetection.Changed);
            m_Camera.OnValueChange += OnCameraChangedHandler;

            CoreSystem.Instance.OnRender -= Instance_OnRender;
            CoreSystem.Instance.OnRender += Instance_OnRender;

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_Camera.OnValueChange -= OnCameraChangedHandler;
            CoreSystem.Instance.OnRender -= Instance_OnRender;

            m_CameraFrustum.Dispose();
        }
        private void OnCameraChangedHandler(Camera from, Camera to)
        {
            OnCameraChanged.Invoke(from, to);
            if (to == null) return;

            m_Matrix4x4 = GetCameraMatrix4X4(to);
        }
        private void Instance_OnRender()
        {
            OnRender?.Invoke();
        }

        protected override PresentationResult BeforePresentation()
        {
            if (m_Camera.Value == null)
            {
                m_Camera.Value = Camera.main;
                if (Camera == null) return PresentationResult.Warning("Cam not found");
            }
            m_Matrix4x4 = GetCameraMatrix4X4(m_Camera.Value);

			//m_CameraFrustum.ScheduleUpdate(new CameraData(Camera));
			//m_CameraFrustum.Update(Camera);

			return base.BeforePresentation();
        }
        protected override PresentationResult AfterPresentation()
        {
            if (Camera == null) return base.AfterPresentation();

            FrustumJob job = new FrustumJob
            {
                m_Frustum = m_CameraFrustum,
                m_Data = new CameraData(Camera)
            };
            m_FrustumJob = ScheduleAt(JobPosition.After, job);

			return base.AfterPresentation();
        }

        private struct FrustumJob : IJob
        {
            public CameraFrustum m_Frustum;
            public CameraData m_Data;

            public void Execute()
            {
                CameraFrustum.Update(ref m_Frustum, m_Data.position, m_Data.orientation, m_Data.fov, m_Data.nearClipPlane, m_Data.farClipPlane, m_Data.aspect);
            }
        }

        #endregion

        public CameraFrustum.ReadOnly GetFrustum(Allocator allocator)
        {
            m_FrustumJob.Complete();
            return m_CameraFrustum.AsReadOnly(allocator);
        }
		internal CameraFrustum GetRawFrustum() => m_CameraFrustum;

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
        public bool IsInCameraScreen(Vector3 worldPosition, float3 offset = default)
        {
            return IsInCameraScreen(worldPosition, m_Matrix4x4, offset);
        }
        public bool IsInCameraScreen(float3[] worldVertices, float3 offset = default)
        {
            return IsInCameraScreen(worldVertices, m_Matrix4x4, offset);
        }
        public bool IsInCameraScreen(NativeArray<float3> worldVertices, float3 offset = default)
        {
            return IsInCameraScreen(worldVertices, m_Matrix4x4, offset);
        }
        /// <summary>
        /// 해당 좌표가 입력한 카메라 내부에 위치하는지 반환합니다.
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public static bool IsInCameraScreen(Camera cam, float3 worldPosition)
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
        internal static bool IsInCameraScreen(float3 worldPosition, Matrix4x4 matrix, float3 offset)
        {
            Vector3 screenPoint = GetScreenPoint(matrix, worldPosition);
            
            return screenPoint.z > 0 - offset.z &&
                screenPoint.x > 0 - offset.x &&
                screenPoint.x < 1 + offset.x &&
                screenPoint.y > 0 - offset.y &&
                screenPoint.y < 1 + offset.y;
        }
        internal static bool IsInCameraScreen(float3[] vertices, float4x4 matrix, float3 offset)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                if (IsInCameraScreen(vertices[i], matrix, offset)) return true;
            }
            return false;
        }
        internal static bool IsInCameraScreen(NativeArray<float3> vertices, Matrix4x4 matrix, Vector3 offset)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                if (IsInCameraScreen(vertices[i], matrix, offset)) return true;
            }
            return false;
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
    }
}
