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
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    [RequireGlobalConfig("Graphics")]
    public sealed class RenderSystem : PresentationSystemEntity<RenderSystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

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

        public Camera Camera => m_Camera.Value;
        public Matrix4x4 Matrix4X4 => m_Matrix4x4;
		public CameraFrustum.ReadOnly Frustum => GetFrustum(Allocator.TempJob);

        public event Action<Camera, Camera> OnCameraChanged;
        public event Action OnRender;

        internal List<CoreRoutine> m_PreRenderRoutines = new List<CoreRoutine>();
        internal List<CoreRoutine> m_PostRenderRoutines = new List<CoreRoutine>();

		private CameraFrustum m_CameraFrustum;
		private Vector3 m_ScreenOffset;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
			m_CameraFrustum = new CameraFrustum(Camera.main);

			m_Camera = new ObClass<Camera>(ObValueDetection.Changed);
            m_Camera.OnValueChange += (from, to) =>
            {
                OnCameraChanged.Invoke(from, to);
                if (to == null) return;

                m_Matrix4x4 = GetCameraMatrix4X4(to);
                //if (to.GetComponent<CameraComponent>() == null)
                //{
                //    to.gameObject.AddComponent<CameraComponent>().Initialize(this);
                //}
                //else to.GetComponent<CameraComponent>().Initialize(this);
            };
            m_ScreenOffset = CoreSystemSettings.Instance.m_ScreenOffset;

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
            m_ScreenOffset = CoreSystemSettings.Instance.m_ScreenOffset;
            if (m_Camera.Value == null)
            {
                m_Camera.Value = Camera.main;
                if (Camera == null) return PresentationResult.Warning("Cam not found");
            }
            m_Matrix4x4 = GetCameraMatrix4X4(m_Camera.Value);

			m_CameraFrustum.ScheduleUpdate(new CameraData(Camera));
			//m_CameraFrustum.Update(Camera);

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
        public override void OnDispose()
        {
            CoreSystem.Instance.OnRender -= Instance_OnRender;
			m_CameraFrustum.Dispose();
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
        public bool IsInCameraScreen(float3[] worldVertices)
        {
            return IsInCameraScreen(worldVertices, m_Matrix4x4, m_ScreenOffset);
        }
        public bool IsInCameraScreen(NativeArray<float3> worldVertices)
        {
            return IsInCameraScreen(worldVertices, m_Matrix4x4, m_ScreenOffset);
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
        internal static bool IsInCameraScreen(float3[] vertices, Matrix4x4 matrix, Vector3 offset)
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

	[Obsolete("", true)]
    public class BoundingFrustum
	{
		#region Public fields 

		/// <summary>
		/// The number of planes in the frustum.
		/// </summary>
		public const int PlaneCount = 6;

		/// <summary>
		/// The number of corner points in the frustum.
		/// </summary>
		public const int CornerCount = 8;

		/// <summary>
		/// Returns the current position of the frustum
		/// </summary>
		public Vector3 Position { get; private set; }

		#endregion

		#region Private variables

		/// <summary>
		/// Ordering: [0] = Far Bottom Left, [1] = Far Top Left, [2] = Far Top Right, [3] = Far Bottom Right, 
		/// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right
		/// </summary>
		private Vector3[] _corners = new Vector3[CornerCount];

		/// <summary>
		/// Defines the set of planes that bound the camera's frustum. All plane normals point to the inside of the 
		/// frustum.
		/// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
		/// </summary>
		private Plane[] _planes = new Plane[PlaneCount];

		/// <summary>
		/// Caches the absolute values of plane normals for re-use during frustum culling of multiple AABB instances
		/// </summary>
		private Vector3[] _absNormals = new Vector3[PlaneCount];

		/// <summary>
		/// Caching the plane normals allows the culling code to avoid calling property getters on the Plane instances
		/// </summary>
		private Vector3[] _planeNormal = new Vector3[PlaneCount];

		/// <summary>
		/// Caching the plane distances allows the culling code to avoid calling property getters on the Plane instances
		/// </summary>
		private float[] _planeDistance = new float[PlaneCount];

		#endregion

		#region Public functions

		/// <summary>
		/// Extracts the frustum corners. The destination array must contain space for no less than CornerCount elements.
		/// Ordering: [0] = Far Bottom Left, [1] = Far Top Left, [2] = Far Top Right, [3] = Far Bottom Right, [4] = Camera Position
		/// </summary>
		public void GetCorners(Vector3[] outCorners)
		{
			if (outCorners == null || outCorners.Length < CornerCount)
			{
				throw new InvalidOperationException("Destination array is null or too small");
			}

			Array.Copy(_corners, outCorners, CornerCount);
		}

		/// <summary>
		/// Extracts the frustum planes. The destination array must contain space for no less than PlaneCount elements.
		/// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
		/// </summary>
		public void GetPlanes(Plane[] outPlanes)
		{
			if (outPlanes == null || outPlanes.Length < PlaneCount)
			{
				throw new InvalidOperationException("Destination array is null or too small");
			}

			Array.Copy(_planes, outPlanes, PlaneCount);
		}

		/// <summary>
		/// Update the bounding frustum from the current camera settings
		/// </summary>
		public void Update(Camera camera)
		{
			Update(camera, camera.farClipPlane);
		}

		/// <summary>
		/// Update the bounding frustum from the current camera settings
		/// </summary>
		public void Update(Camera camera, float farClipPlane)
		{
			var camTransform = camera.transform;
			var position = camTransform.position;
			var orientation = camTransform.rotation;

			this.Position = position;
			var forward = orientation * Vector3.forward;

			if (camera.orthographic)
			{
				calculateFrustumCornersOrthographic(camera);
			}
			else
			{
				calculateFrustumCornersPerspective(
					ref position,
					ref orientation,
					camera.fieldOfView,
					camera.nearClipPlane,
					camera.farClipPlane,
					camera.aspect
				);
			}

			// CORNERS:
			// [0] = Far Bottom Left,  [1] = Far Top Left,  [2] = Far Top Right,  [3] = Far Bottom Right, 
			// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right

			// PLANES:
			// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far

			_planes[0] = new Plane(_corners[4], _corners[1], _corners[0]);
			_planes[1] = new Plane(_corners[6], _corners[3], _corners[2]);
			_planes[2] = new Plane(_corners[7], _corners[0], _corners[3]);
			_planes[3] = new Plane(_corners[5], _corners[2], _corners[1]);
			_planes[4] = new Plane(forward, position + forward * camera.nearClipPlane);
			_planes[5] = new Plane(-forward, position + forward * farClipPlane);

			for (int i = 0; i < PlaneCount; i++)
			{
				var plane = _planes[i];
				var normal = plane.normal;

				_absNormals[i] = new Vector3(Math.Abs(normal.x), Math.Abs(normal.y), Math.Abs(normal.z));
				_planeNormal[i] = normal;
				_planeDistance[i] = plane.distance;
			}
		}

		/// <summary>
		/// Update the bounding frustum
		/// </summary>
		public void Update(Vector3 position, Quaternion orientation, float fov, float nearClipPlane, float farClipPlane, float aspect)
		{
			this.Position = position;

			calculateFrustumCornersPerspective(ref position, ref orientation, fov, nearClipPlane, farClipPlane, aspect);

			var forward = orientation * Vector3.forward;

			// CORNERS:
			// [0] = Far Bottom Left,  [1] = Far Top Left,  [2] = Far Top Right,  [3] = Far Bottom Right, 
			// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right

			// PLANES:
			// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far

			_planes[0] = new Plane(_corners[4], _corners[1], _corners[0]);
			_planes[1] = new Plane(_corners[6], _corners[3], _corners[2]);
			_planes[2] = new Plane(_corners[7], _corners[0], _corners[3]);
			_planes[3] = new Plane(_corners[5], _corners[2], _corners[1]);
			_planes[4] = new Plane(forward, position + forward * nearClipPlane);
			_planes[5] = new Plane(-forward, position + forward * farClipPlane);

			for (int i = 0; i < PlaneCount; i++)
			{
				var plane = _planes[i];
				var normal = plane.normal;

				_absNormals[i] = new Vector3(Math.Abs(normal.x), Math.Abs(normal.y), Math.Abs(normal.z));
				_planeNormal[i] = normal;
				_planeDistance[i] = plane.distance;
			}
		}

		/// <summary>
		/// Returns true if the frustum contains the specified point
		/// </summary>
		public bool Contains(ref Vector3 point)
		{
			for (int i = 0; i < PlaneCount; i++)
			{
				var normal = _planeNormal[i];
				var distance = _planeDistance[i];

				float dist = normal.x * point.x + normal.y * point.y + normal.z * point.z + distance;

				if (dist < 0f)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the type of intersection (if any) between the frustum and the sphere
		/// </summary>
		/// <param name="center">The world position of the sphere</param>
		/// <param name="radius">The radius of the sphere</param>
		public IntersectionType GetSphereIntersection(ref Vector3 center, float radius, float frustumPadding = 0)
		{
			var intersecting = false;

			for (int i = 0; i < PlaneCount; i++)
			{
				var normal = _planeNormal[i];
				var distance = _planeDistance[i];

				float dist = normal.x * center.x + normal.y * center.y + normal.z * center.z + distance;

				if (dist < -radius - frustumPadding)
				{
					return IntersectionType.False;
				}

				intersecting |= (dist <= radius);
			}

			return intersecting ? IntersectionType.Intersects : IntersectionType.Contains;
		}

		/// <summary>
		/// Returns the type of intersection (if any) between the frustum and the sphere
		/// </summary>
		public IntersectionType GetSphereIntersection(ref BoundingSphere sphere, float frustumPadding = 0)
		{
			var intersecting = false;

			var center = sphere.position;
			var radius = sphere.radius;

			for (int i = 0; i < PlaneCount; i++)
			{
				var normal = _planeNormal[i];
				var distance = _planeDistance[i];

				float dist = normal.x * center.x + normal.y * center.y + normal.z * center.z + distance;

				if (dist < -radius - frustumPadding)
				{
					return IntersectionType.False;
				}

				intersecting |= (dist <= radius);
			}

			return intersecting ? IntersectionType.Intersects : IntersectionType.Contains;
		}

		/// <summary>
		/// Iterates through each sphere in the array and sets the Result field to the result of the sphere/frustum intersection test
		/// This function is intended primarily for use with static geometry (or quadtrees, etc) where the bounding volumes will not 
		/// be updated frequently, but the frustum will. 
		/// </summary>
		public void CullSpheres(CullingSphere[] spheres, int sphereCount)
		{
			Vector3 planeNormal = Vector3.zero;

			var planeNormal0 = _planeNormal[0];
			var planeNormal1 = _planeNormal[1];
			var planeNormal2 = _planeNormal[2];
			var planeNormal3 = _planeNormal[3];
			var planeNormal4 = _planeNormal[4];
			var planeNormal5 = _planeNormal[5];

			var planeDistance0 = _planeDistance[0];
			var planeDistance1 = _planeDistance[1];
			var planeDistance2 = _planeDistance[2];
			var planeDistance3 = _planeDistance[3];
			var planeDistance4 = _planeDistance[4];
			var planeDistance5 = _planeDistance[5];

			for (int si = 0; si < sphereCount; si++)
			{
				var sphere = spheres[si];
				var center = sphere.SphereCenter;
				var radius = sphere.SphereRadius;

				bool outOfFrustum = false;

				outOfFrustum = outOfFrustum || (planeNormal0.x * center.x + planeNormal0.y * center.y + planeNormal0.z * center.z + planeDistance0) < -radius;
				outOfFrustum = outOfFrustum || (planeNormal1.x * center.x + planeNormal1.y * center.y + planeNormal1.z * center.z + planeDistance1) < -radius;
				outOfFrustum = outOfFrustum || (planeNormal2.x * center.x + planeNormal2.y * center.y + planeNormal2.z * center.z + planeDistance2) < -radius;
				outOfFrustum = outOfFrustum || (planeNormal3.x * center.x + planeNormal3.y * center.y + planeNormal3.z * center.z + planeDistance3) < -radius;
				outOfFrustum = outOfFrustum || (planeNormal4.x * center.x + planeNormal4.y * center.y + planeNormal4.z * center.z + planeDistance4) < -radius;
				outOfFrustum = outOfFrustum || (planeNormal5.x * center.x + planeNormal5.y * center.y + planeNormal5.z * center.z + planeDistance5) < -radius;

				spheres[si].IsInFrustum = !outOfFrustum;
			}
		}

		/// <summary>
		/// Returns the type of intersection (if any) between the frustum and the sphere
		/// </summary>
		/// <param name="center">The world position of the sphere</param>
		/// <param name="radius">The radius of the sphere</param>
		public bool IntersectsSphere(ref Vector3 center, float radius, float frustumPadding = 0)
		{
			for (int i = 0; i < PlaneCount; i++)
			{
				var normal = _planeNormal[i];
				var distance = _planeDistance[i];

				float dist = normal.x * center.x + normal.y * center.y + normal.z * center.z + distance;

				if (dist < -radius - frustumPadding)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the type of intersection (if any) between the frustum and the sphere
		/// </summary>
		/// <param name="sphere">The sphere to check</param>
		public bool IntersectsSphere(ref BoundingSphere sphere, float frustumPadding = 0)
		{
			var center = sphere.position;

			for (int i = 0; i < PlaneCount; i++)
			{
				var normal = _planeNormal[i];
				var distance = _planeDistance[i];

				float dist = normal.x * center.x + normal.y * center.y + normal.z * center.z + distance;

				if (dist < -sphere.radius - frustumPadding)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns TRUE if the box and frustum intersect
		/// </summary>
		public bool IntersectsBox(ref Bounds box, float frustumPadding = 0)
		{
			// Exit early if the box contains the frustum origin
			if (box.Contains(_corners[CornerCount - 1]))
			{
				return true;
			}

			var center = box.center;
			var extents = box.extents;

			for (int i = 0; i < PlaneCount; i++)
			{
				var abs = _absNormals[i];

				var planeNormal = _planeNormal[i];
				var planeDistance = _planeDistance[i];

				float r = extents.x * abs.x + extents.y * abs.y + extents.z * abs.z;
				float s = planeNormal.x * center.x + planeNormal.y * center.y + planeNormal.z * center.z;

				if (s + r < -planeDistance - frustumPadding)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the type of intersection (if any) between the bounding box and the frustum
		/// </summary>
		public IntersectionType GetBoxIntersection(ref Bounds box, float frustumPadding = 0)
		{
			var center = box.center;
			var extents = box.extents;

			var intersecting = false;

			for (int i = 0; i < PlaneCount; i++)
			{
				var abs = _absNormals[i];

				var planeNormal = _planeNormal[i];
				var planeDistance = _planeDistance[i];

				float r = extents.x * abs.x + extents.y * abs.y + extents.z * abs.z;
				float s = planeNormal.x * center.x + planeNormal.y * center.y + planeNormal.z * center.z;

				if (s + r < -planeDistance - frustumPadding)
				{
					return IntersectionType.False;
				}

				intersecting |= (s - r <= -planeDistance);
			}

			return intersecting ? IntersectionType.Intersects : IntersectionType.Contains;
		}

		/// <summary>
		/// Iterates through each box in the boxes array and sets the Result field to the result of the box/frustum intersection test.
		/// This function is intended primarily for use with static geometry (or quadtrees, etc) where the bounding volumes will not 
		/// be updated frequently, but the frustum will. 
		/// </summary>
		public void CullBoxes(CullingBox[] boxes, int boxCount)
		{
			var abs0 = _absNormals[0];
			var abs1 = _absNormals[1];
			var abs2 = _absNormals[2];
			var abs3 = _absNormals[3];
			var abs4 = _absNormals[4];
			var abs5 = _absNormals[5];

			var planeNormal0 = _planeNormal[0];
			var planeNormal1 = _planeNormal[1];
			var planeNormal2 = _planeNormal[2];
			var planeNormal3 = _planeNormal[3];
			var planeNormal4 = _planeNormal[4];
			var planeNormal5 = _planeNormal[5];

			var planeDistance0 = _planeDistance[0];
			var planeDistance1 = _planeDistance[1];
			var planeDistance2 = _planeDistance[2];
			var planeDistance3 = _planeDistance[3];
			var planeDistance4 = _planeDistance[4];
			var planeDistance5 = _planeDistance[5];

			for (int bi = 0; bi < boxCount; bi++)
			{
				var box = boxes[bi];
				var center = box.BoxCenter;
				var extents = box.BoxExtents;

				bool outOfFrustum = false;

				outOfFrustum = outOfFrustum || (
					(extents.x * abs0.x + extents.y * abs0.y + extents.z * abs0.z) +
					(planeNormal0.x * center.x + planeNormal0.y * center.y + planeNormal0.z * center.z)) < -planeDistance0;

				outOfFrustum = outOfFrustum || (
					(extents.x * abs1.x + extents.y * abs1.y + extents.z * abs1.z) +
					(planeNormal1.x * center.x + planeNormal1.y * center.y + planeNormal1.z * center.z)) < -planeDistance1;

				outOfFrustum = outOfFrustum || (
					(extents.x * abs2.x + extents.y * abs2.y + extents.z * abs2.z) +
					(planeNormal2.x * center.x + planeNormal2.y * center.y + planeNormal2.z * center.z)) < -planeDistance2;

				outOfFrustum = outOfFrustum || (
					(extents.x * abs3.x + extents.y * abs3.y + extents.z * abs3.z) +
					(planeNormal3.x * center.x + planeNormal3.y * center.y + planeNormal3.z * center.z)) < -planeDistance3;

				outOfFrustum = outOfFrustum || (
					(extents.x * abs4.x + extents.y * abs4.y + extents.z * abs4.z) +
					(planeNormal4.x * center.x + planeNormal4.y * center.y + planeNormal4.z * center.z)) < -planeDistance4;

				outOfFrustum = outOfFrustum || (
					(extents.x * abs5.x + extents.y * abs5.y + extents.z * abs5.z) +
					(planeNormal5.x * center.x + planeNormal5.y * center.y + planeNormal5.z * center.z)) < -planeDistance5;

				boxes[bi].IsInFrustum = !outOfFrustum;
			}
		}

		/// <summary>
		/// Returns TRUE if the oriented bounding box and frustum intersect
		/// </summary>
		/// <param name="box">The bounding box to test. Note: box.center is expected to be in world coordinates</param>
		/// <param name="right">The horizontal local coordinate axis (equivalent to Transform.right)</param>
		/// <param name="up">The vertical local coordinate axis (equivalent to Transform.up)</param>
		/// <param name="forward">The forward local coordinate axis (equivalent to Transform.forward)</param>
		/// <returns></returns>
		public bool IntersectsOrientedBox(ref Bounds box, ref Vector3 right, ref Vector3 up, ref Vector3 forward, float frustumPadding = 0)
		{
			var center = box.center;
			var extents = box.extents;

			for (int i = 0; i < PlaneCount; i++)
			{
				var planeNormal = _planeNormal[i];
				var planeDistance = _planeDistance[i];

				float r =
					extents.x * Math.Abs(Vector3.Dot(planeNormal, right)) +
					extents.y * Math.Abs(Vector3.Dot(planeNormal, up)) +
					extents.z * Math.Abs(Vector3.Dot(planeNormal, forward));

				float s = planeNormal.x * center.x + planeNormal.y * center.y + planeNormal.z * center.z;

				if (s + r < -planeDistance - frustumPadding)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the type of intersection (if any) of an oriented bounding box and the frustum.
		/// </summary>
		/// <param name="box">The bounding box to test. Note: box.center is expected to be in world coordinates</param>
		/// <param name="right">The horizontal local coordinate axis (equivalent to Transform.right)</param>
		/// <param name="up">The vertical local coordinate axis (equivalent to Transform.up)</param>
		/// <param name="forward">The forward local coordinate axis (equivalent to Transform.forward)</param>
		/// <returns></returns>
		public IntersectionType GetOrientedBoxIntersection(ref Bounds box, ref Vector3 right, ref Vector3 up, ref Vector3 forward, float frustumPadding = 0)
		{
			var center = box.center;
			var extents = box.extents;

			var intersecting = false;

			for (int i = 0; i < PlaneCount; i++)
			{
				var planeNormal = _planeNormal[i];
				var planeDistance = _planeDistance[i];

				float r =
					extents.x * Math.Abs(Vector3.Dot(planeNormal, right)) +
					extents.y * Math.Abs(Vector3.Dot(planeNormal, up)) +
					extents.z * Math.Abs(Vector3.Dot(planeNormal, forward));

				float s = planeNormal.x * center.x + planeNormal.y * center.y + planeNormal.z * center.z;

				if (s + r < -planeDistance - frustumPadding)
				{
					return IntersectionType.False;
				}

				intersecting |= (s - r <= -planeDistance);
			}

			return intersecting ? IntersectionType.Intersects : IntersectionType.Contains;
		}

		#endregion

		#region Private functions 

		private void calculateFrustumCornersOrthographic(Camera camera)
		{
			var camTransform = camera.transform;
			var position = camTransform.position;
			var orientation = camTransform.rotation;
			var farClipPlane = camera.farClipPlane;
			var nearClipPlane = camera.nearClipPlane;

			var forward = orientation * Vector3.forward;
			var right = orientation * Vector3.right * camera.orthographicSize * camera.aspect;
			var up = orientation * Vector3.up * camera.orthographicSize;

			// CORNERS:
			// [0] = Far Bottom Left,  [1] = Far Top Left,  [2] = Far Top Right,  [3] = Far Bottom Right, 
			// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right

			_corners[0] = position + forward * farClipPlane - up - right;
			_corners[1] = position + forward * farClipPlane + up - right;
			_corners[2] = position + forward * farClipPlane + up + right;
			_corners[3] = position + forward * farClipPlane - up + right;
			_corners[4] = position + forward * nearClipPlane - up - right;
			_corners[5] = position + forward * nearClipPlane + up - right;
			_corners[6] = position + forward * nearClipPlane + up + right;
			_corners[7] = position + forward * nearClipPlane - up + right;
		}

		private void calculateFrustumCornersPerspective(ref Vector3 position, ref Quaternion orientation, float fov, float nearClipPlane, float farClipPlane, float aspect)
		{
			float fovWHalf = fov * 0.5f;

			Vector3 toRight = Vector3.right * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * aspect;
			Vector3 toTop = Vector3.up * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);
			var forward = Vector3.forward;

			Vector3 topLeft = (forward - toRight + toTop);
			float camScale = topLeft.magnitude * farClipPlane;

			topLeft.Normalize();
			topLeft *= camScale;

			Vector3 topRight = (forward + toRight + toTop);
			topRight.Normalize();
			topRight *= camScale;

			Vector3 bottomRight = (forward + toRight - toTop);
			bottomRight.Normalize();
			bottomRight *= camScale;

			Vector3 bottomLeft = (forward - toRight - toTop);
			bottomLeft.Normalize();
			bottomLeft *= camScale;

			_corners[0] = position + orientation * bottomLeft;
			_corners[1] = position + orientation * topLeft;
			_corners[2] = position + orientation * topRight;
			_corners[3] = position + orientation * bottomRight;

			topLeft = (forward - toRight + toTop);
			camScale = topLeft.magnitude * nearClipPlane;

			topLeft.Normalize();
			topLeft *= camScale;

			topRight = (forward + toRight + toTop);
			topRight.Normalize();
			topRight *= camScale;

			bottomRight = (forward + toRight - toTop);
			bottomRight.Normalize();
			bottomRight *= camScale;

			bottomLeft = (forward - toRight - toTop);
			bottomLeft.Normalize();
			bottomLeft *= camScale;

			_corners[4] = position + orientation * bottomLeft;
			_corners[5] = position + orientation * topLeft;
			_corners[6] = position + orientation * topRight;
			_corners[7] = position + orientation * bottomRight;
		}

		#endregion

		#region Nested types

		// When culling large numbers of static volumes per frame, it can be faster and more efficient to store just their 
		// bounding volume representations in a single indexed array, together with the culling results. This allows for 
		// extremely fast brute-force culling of large numbers of objects without the need to recursively traverse hierarchical 
		// spatial partition structures. This can in some particular cases actually be significantly faster.
		// This was implemented for a specific use case in my own code and YMMV, so profile rigorously and make no assumptions.

		public struct CullingBox
		{
			public Vector3 BoxCenter;
			public Vector3 BoxExtents;
			public bool IsInFrustum;
		}

		public struct CullingSphere
		{
			public Vector3 SphereCenter;
			public float SphereRadius;
			public bool IsInFrustum;
		}

		#endregion
	}

	public enum IntersectionType
    {
		False		=	0b001,

		Intersects	=	0b010,
		Contains	=	0b100,

		True		=	0b110
	}
}
