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

		private CameraFrustum m_CameraFrustum;
        //private CameraFrustum m_CopiedFrustum;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
			m_CameraFrustum = new CameraFrustum(Camera.main);
            //m_CopiedFrustum = new CameraFrustum(Camera.main);

            m_Camera = new ObClass<Camera>(ObValueDetection.Changed);
            m_Camera.OnValueChange += (from, to) =>
            {
                OnCameraChanged.Invoke(from, to);
                if (to == null) return;

                m_Matrix4x4 = GetCameraMatrix4X4(to);
            };

            CoreSystem.Instance.OnRender -= Instance_OnRender;
            CoreSystem.Instance.OnRender += Instance_OnRender;

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            CoreSystem.Instance.OnRender -= Instance_OnRender;
            m_CameraFrustum.Dispose();
            //m_CopiedFrustum.Dispose();
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

            //m_CopiedFrustum.Copy(in m_CameraFrustum);

            FrustumJob job = new FrustumJob
            {
                m_Frustum = m_CameraFrustum,
                m_Data = new CameraData(Camera)
            };
            ScheduleAt(JobPosition.After, job);

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

        public CameraFrustum.ReadOnly GetFrustum(Allocator allocator) => m_CameraFrustum.AsReadOnly(allocator);
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
        //internal static bool IsInCameraScreenWithPlane(NativeArray<float3> vertices, Matrix4x4 matrix, Vector3 offset)
        //{
        //    Plane[] planes = GeometryUtility.CalculateFrustumPlanes(matrix);
        //    Plane plane = new Plane();
        //    GeometryUtility.

        //    for (int i = 0; i < vertices.Length; i++)
        //    {
        //        if (IsInCameraScreen(vertices[i], matrix, offset)) return true;
        //    }
        //    return false;
        //}
        private static void CalculateFrustumPlanes(Matrix4x4 mat, Plane[] planes)
        {
            // left
            planes[0].normal = new Vector3(mat.m30 + mat.m00, mat.m31 + mat.m01, mat.m32 + mat.m02);
            planes[0].distance = mat.m33 + mat.m03;

            // right
            planes[1].normal = new Vector3(mat.m30 - mat.m00, mat.m31 - mat.m01, mat.m32 - mat.m02);
            planes[1].distance = mat.m33 - mat.m03;

            // bottom
            planes[2].normal = new Vector3(mat.m30 + mat.m10, mat.m31 + mat.m11, mat.m32 + mat.m12);
            planes[2].distance = mat.m33 + mat.m13;

            // top
            planes[3].normal = new Vector3(mat.m30 - mat.m10, mat.m31 - mat.m11, mat.m32 - mat.m12);
            planes[3].distance = mat.m33 - mat.m13;

            // near
            planes[4].normal = new Vector3(mat.m30 + mat.m20, mat.m31 + mat.m21, mat.m32 + mat.m22);
            planes[4].distance = mat.m33 + mat.m23;

            // far
            planes[5].normal = new Vector3(mat.m30 - mat.m20, mat.m31 - mat.m21, mat.m32 - mat.m22);
            planes[5].distance = mat.m33 - mat.m23;

            // normalize
            for (uint i = 0; i < 6; i++)
            {
                float length = planes[i].normal.magnitude;
                planes[i].normal /= length;
                planes[i].distance /= length;
            }
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

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static float4x4 TRS(float3 translation, quaternion rotation, float3 scale)
        //{
        //    float3x3 r = new float3x3(rotation);
        //    return
        //        new float4x4(
        //            new float4(r.c0 * scale.x, 0),
        //            new float4(r.c1 * scale.y, 0),
        //            new float4(r.c2 * scale.z, 0),
        //            new float4(translation, 1)
        //            );
        //}
        public static float4x4 LocalToWorldMatrix(float3 translation, quaternion rotation)
        {
            float3x3 r = new float3x3(rotation);
            return new float4x4(r, translation);
        }
        public static float4x4 WorldToLocalMatrix(float3 translation, quaternion rotation) => math.inverse(LocalToWorldMatrix(translation, rotation));
    }

	public enum IntersectionType
    {
		False		=	0b001,

		Intersects	=	0b010,
		Contains	=	0b100,

		True		=	0b110
	}
}
