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
using UnityEngine.Experimental.GlobalIllumination;

namespace Syadeu.Presentation.Render
{
    [RequireGlobalConfig("Graphics")]
    public sealed class RenderSystem : PresentationSystemEntity<RenderSystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => true;

        private ObClass<Camera> m_Camera;
        private ObClass<Light> m_DirectionalLight;
        private Matrix4x4 m_Matrix4x4;

        [ConfigValue(Header = "Resolution", Name = "X")] private int m_ResolutionX;
        [ConfigValue(Header = "Resolution", Name = "Y")] private int m_ResolutionY;

        public Camera Camera
        {
            get => m_Camera.Value;
            set
            {
                m_Camera.Value = value;
            }
        }
        public Light DirectionalLight
        {
            get => m_DirectionalLight.Value;
            set
            {
                if (value.type != UnityEngine.LightType.Directional)
                {
                    CoreSystem.Logger.LogError(Channel.Render,
                        $"{value.name} is not a Directional Light.");
                    return;
                }

                m_DirectionalLight.Value = value;
            }
        }
		public CameraFrustum.ReadOnly Frustum => GetFrustum();

        public event Action<Camera, Camera> OnCameraChanged;
        public event Action OnRender;

        private JobHandle m_FrustumJob;
		private CameraFrustum m_CameraFrustum;
        private CameraData m_LastCameraData;

        private LightData m_LastDirectionalLightData;

        public CameraData LastCameraData => m_LastCameraData;
        public LightData LastDirectionalLightData => m_LastDirectionalLightData;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_CameraFrustum = new CameraFrustum(new CameraData());
            m_LastCameraData = new CameraData() { orientation = quaternion.identity };

            m_Camera = new ObClass<Camera>(ObValueDetection.Changed);
            m_Camera.OnValueChange += OnCameraChangedHandler;

            CoreSystem.Instance.OnRender -= Instance_OnRender;
            CoreSystem.Instance.OnRender += Instance_OnRender;

            m_DirectionalLight = new ObClass<Light>(ObValueDetection.Changed);
            m_LastDirectionalLightData = new LightData() { orientation = quaternion.identity };

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
                //if (Camera == null) return PresentationResult.Warning("Cam not found");
                if (Camera == null) return base.BeforePresentation();
            }
            m_Matrix4x4 = GetCameraMatrix4X4(m_Camera.Value);

			//m_CameraFrustum.ScheduleUpdate(new CameraData(Camera));
			//m_CameraFrustum.Update(Camera);

			return base.BeforePresentation();
        }
        protected override PresentationResult AfterPresentation()
        {
            if (DirectionalLight != null)
            {
                m_LastDirectionalLightData.Update(DirectionalLight);

                //float4x4 matrix = m_LastDirectionalLightData.lightSpace;
                
            }

            if (Camera == null) return base.AfterPresentation();

            m_LastCameraData.Update(Camera);
            FrustumJob job = new FrustumJob
            {
                m_Frustum = m_CameraFrustum,
                m_Data = m_LastCameraData
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

        public CameraFrustum.ReadOnly GetFrustum()
        {
            m_FrustumJob.Complete();
            return m_CameraFrustum.AsReadOnly();
        }
		internal CameraFrustum GetRawFrustum() => m_CameraFrustum;

        public float4 WorldToScreenPoint(float3 worldPoint)
        {
            float4x4 projection = float4x4.PerspectiveFov(m_LastCameraData.fov, m_LastCameraData.aspect, m_LastCameraData.nearClipPlane, m_LastCameraData.farClipPlane);
            float4x4 tr = new float4x4(new float3x3(m_LastCameraData.orientation), m_LastCameraData.position);
            
            float4x4 matrix = math.mul(projection, math.fastinverse(tr));
            // if w == -1 not seen by the cam
            return math.mul(matrix, new float4(worldPoint, 1));
        }
        public float3 ScreenToWorldPoint(float3 screenPoint)
        {
            float4x4 projection = m_LastCameraData.projectionMatrix;
            float4x4 tr = new float4x4(new float3x3(m_LastCameraData.orientation), m_LastCameraData.position);

            float4x4 matrix = math.inverse(math.mul(projection, math.fastinverse(tr)));
            float4 temp = new float4
            {
                x = 2 * (screenPoint.x / m_LastCameraData.pixelWidth) - 1,
                y = 2 * (screenPoint.y / m_LastCameraData.pixelHeight) - 1,
                z = m_LastCameraData.nearClipPlane,
                w = 1
            };

            float4 pos = math.mul(matrix, temp);

            pos.w = 1 / pos.w;
            pos.x *= pos.w; pos.y *= pos.w; pos.z *= pos.w;
            return pos.xyz;
        }
        
        public Ray ScreenToRay(float3 screenPoint)
        {
            float4x4 projection = m_LastCameraData.projectionMatrix;
            float4x4 tr = new float4x4(new float3x3(m_LastCameraData.orientation), m_LastCameraData.position);

            float4x4 matrix = math.inverse(math.mul(projection, math.fastinverse(tr)));
            float4 temp = new float4
            {
                x = 2 * (screenPoint.x / m_LastCameraData.pixelWidth) - 1,
                y = 2 * (screenPoint.y / m_LastCameraData.pixelHeight) - 1,
                z = m_LastCameraData.nearClipPlane,
                w = 1
            };

            float4 pos = math.mul(matrix, temp);
            return new Ray(m_LastCameraData.position, pos.xyz - m_LastCameraData.position);
        }
        public Ray PointToLightRay(float3 worldPoint)
        {
            float3 forward = math.mul(LastDirectionalLightData.orientation, new float3(0, 0, 1));
            return new Ray(worldPoint, forward);
        }

        public void SetResolution()
        {
            //Screen.SetResolution(100,100, FullScreenMode.ExclusiveFullScreen, )
        }

        #region Legacy

        /// <summary>
        /// 해당 월드 좌표를 입력한 Matrix 기반으로 2D 좌표값을 반환합니다.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public static float3 GetScreenPoint(float4x4 matrix, float3 worldPosition)
        {
            float4 p4 = new float4(worldPosition, 1);
            float4 result4 = math.mul(matrix, p4);
            float3 screenPoint = result4.xyz;
            screenPoint /= -result4.w;
            screenPoint.x = screenPoint.x / 2 + 0.5f;
            screenPoint.y = screenPoint.y / 2 + 0.5f;
            screenPoint.z = -result4.w;

            return screenPoint;
        }
        internal static float4x4 GetCameraMatrix4X4(Camera cam) => cam.projectionMatrix * cam.transform.worldToLocalMatrix;
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
        internal static bool IsInCameraScreen(float3 worldPosition, float4x4 matrix, float3 offset)
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
        internal static bool IsInCameraScreen(NativeArray<float3> vertices, float4x4 matrix, Vector3 offset)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                if (IsInCameraScreen(vertices[i], matrix, offset)) return true;
            }
            return false;
        }

        #endregion
    }
}
