using Syadeu.Database;
using Syadeu.Internal;
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    public struct CameraFrustum : IDisposable
    {
		public const int PlaneCount = 6;
		public const int CornerCount = 8;

		private JobHandle m_UpdateJob;

		private float3 m_Position;
		private NativeArray<Plane> m_Planes;
		private NativeArray<float3>
			m_Corners,
			m_AbsNormals,
			m_PlaneNormals;
		private NativeArray<float> m_PlaneDistances;

		public JobHandle JobHandle => m_UpdateJob;
		public float3 Position => m_Position;

		[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
		[NativeContainerIsReadOnly, NativeContainerSupportsDeallocateOnJobCompletion]
		public readonly struct ReadOnly : IDisposable
        {
			public readonly float3 position;
			public readonly NativeArray<Plane> planes;
			public readonly NativeArray<float3>
				corners,
				absNormals,
				planeNormals;
			public readonly NativeArray<float> planeDistances;

			internal ReadOnly(ref CameraFrustum data, Allocator allocator)
            {
				position = data.m_Position;
				planes = new NativeArray<Plane>(data.m_Planes, allocator);
				corners = new NativeArray<float3>(data.m_Corners, allocator);
				absNormals = new NativeArray<float3>(data.m_AbsNormals, allocator);
				planeNormals = new NativeArray<float3>(data.m_PlaneNormals, allocator);
				planeDistances = new NativeArray<float>(data.m_PlaneDistances, allocator);
			}
            public void Dispose()
            {
				planes.Dispose();
				corners.Dispose();
				absNormals.Dispose();
				planeNormals.Dispose();
				planeDistances.Dispose();
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool Contains(in float3 point)
			{
				for (int i = 0; i < PlaneCount; i++)
				{
					var normal = planeNormals[i];
					var distance = planeDistances[i];

					float dist = normal.x * point.x + normal.y * point.y + normal.z * point.z + distance;

					if (dist < 0f)
					{
						return false;
					}
				}

				return true;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool IntersectsBox(in AABB box, float frustumPadding = 0)
			{
				if (box.Contains(corners[CornerCount - 1]))
				{
					return true;
				}
				float3 center = box.center;
				float3 extents = box.extents;

				for (int i = 0; i < PlaneCount; i++)
				{
					float3 abs = absNormals[i];

					float3 planeNormal = planeNormals[i];
					float planeDistance = planeDistances[i];

					float r = extents.x * abs.x + extents.y * abs.y + extents.z * abs.z;
					float s = planeNormal.x * center.x + planeNormal.y * center.y + planeNormal.z * center.z;

					if (s + r < -planeDistance - frustumPadding)
					{
						return false;
					}
				}

				return true;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public IntersectionType IntersectsSphere(ref float3 center, float radius, float frustumPadding = 0)
			{
                bool intersecting = false;
				for (int i = 0; i < PlaneCount; i++)
				{
					var normal = planeNormals[i];
					var distance = planeDistances[i];

					float dist = normal.x * center.x + normal.y * center.y + normal.z * center.z + distance;

					if (dist < -radius - frustumPadding)
					{
						return IntersectionType.False;
					}

					intersecting |= (dist <= radius);
				}
				return intersecting ? IntersectionType.Intersects : IntersectionType.Contains;
			}
		}

        #region Constructor

		public CameraFrustum(Camera cam)
        {
			CoreSystem.Logger.ThreadBlock(nameof(CameraFrustum), Syadeu.Internal.ThreadInfo.Unity);

			this = default(CameraFrustum);

			m_Corners = new NativeArray<float3>(CornerCount, Allocator.Persistent);
			m_Planes = new NativeArray<Plane>(PlaneCount, Allocator.Persistent);
			m_AbsNormals = new NativeArray<float3>(PlaneCount, Allocator.Persistent);
			m_PlaneNormals = new NativeArray<float3>(PlaneCount, Allocator.Persistent);
			m_PlaneDistances = new NativeArray<float>(PlaneCount, Allocator.Persistent);

			Update(ref this, cam);
		}
		public CameraFrustum(CameraData cam)
        {
			this = default(CameraFrustum);

			m_Corners = new NativeArray<float3>(CornerCount, Allocator.Persistent);
			m_Planes = new NativeArray<Plane>(PlaneCount, Allocator.Persistent);
			m_AbsNormals = new NativeArray<float3>(PlaneCount, Allocator.Persistent);
			m_PlaneNormals = new NativeArray<float3>(PlaneCount, Allocator.Persistent);
			m_PlaneDistances = new NativeArray<float>(PlaneCount, Allocator.Persistent);

			Update(ref this, cam.position, cam.orientation, cam.fov, cam.nearClipPlane, cam.farClipPlane, cam.aspect);
		}

		public void Dispose()
        {
			m_UpdateJob.Complete();

			m_Planes.Dispose();
			m_Corners.Dispose();
			m_AbsNormals.Dispose();
			m_PlaneNormals.Dispose();
			m_PlaneDistances.Dispose();
		}

		public ReadOnly AsReadOnly(Allocator allocator)
        {
			CoreSystem.Logger.ThreadBlock(nameof(AsReadOnly), ThreadInfo.Unity);
			m_UpdateJob.Complete();

			return new ReadOnly(ref this, allocator);
		}
		public ReadOnly JobReadOnly() => new ReadOnly(ref this, Allocator.Temp);

		#endregion

		public void Update(Camera cam) => Update(ref this, cam);
		public void ScheduleUpdate(CameraData data)
        {
			CoreSystem.Logger.ThreadBlock(nameof(ScheduleUpdate), ThreadInfo.Unity);
			m_UpdateJob.Complete();

			UpdateJob update = new UpdateJob
			{
				frustum = this,
				data = data
			};
			JobHandle jobHandle = update.Schedule();
			m_UpdateJob = JobHandle.CombineDependencies(m_UpdateJob, jobHandle);
		}
        private struct UpdateJob : IJob
        {
			public CameraFrustum frustum;
			public CameraData data;

            public void Execute()
            {
				Update(ref frustum, data.position, data.orientation, data.fov, data.nearClipPlane, data.farClipPlane, data.aspect);
			}
        }

        #region Statics

        private static void Update(ref CameraFrustum other, Camera cam) => Update(ref other, cam, cam.farClipPlane);
		private static void Update(ref CameraFrustum other, Camera cam, float farClipPlane)
        {
			Transform tr = cam.transform;
			other.m_Position = tr.position;
			quaternion rot = tr.rotation;
			float3 forward = tr.forward;

			if (cam.orthographic)
			{
				CalculateFrustumCornersOrthographic(cam, ref other.m_Corners);
			}
			else
			{
				CalculateFrustumCornersPerspective(
					ref other.m_Corners,
					ref other.m_Position,
					ref rot,
					cam.fieldOfView,
					cam.nearClipPlane,
					cam.farClipPlane,
					cam.aspect
				);
			}

			// CORNERS:
			// [0] = Far Bottom Left,  [1] = Far Top Left,  [2] = Far Top Right,  [3] = Far Bottom Right, 
			// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right

			// PLANES:
			// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
			other.m_Planes[0] = new Plane(other.m_Corners[4], other.m_Corners[1], other.m_Corners[0]);
			other.m_Planes[1] = new Plane(other.m_Corners[6], other.m_Corners[3], other.m_Corners[2]);
			other.m_Planes[2] = new Plane(other.m_Corners[7], other.m_Corners[0], other.m_Corners[3]);
			other.m_Planes[3] = new Plane(other.m_Corners[5], other.m_Corners[2], other.m_Corners[1]);
			other.m_Planes[4] = new Plane(forward, other.m_Position + forward * cam.nearClipPlane);
			other.m_Planes[5] = new Plane(-forward, other.m_Position + forward * farClipPlane);

			for (int i = 0; i < PlaneCount; i++)
			{
				var plane = other.m_Planes[i];
				var normal = plane.normal;

				other.m_AbsNormals[i] = new Vector3(Math.Abs(normal.x), Math.Abs(normal.y), Math.Abs(normal.z));
				other.m_PlaneNormals[i] = normal;
				other.m_PlaneDistances[i] = plane.distance;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Update(ref CameraFrustum other, float3 position, quaternion orientation, float fov, float nearClipPlane, float farClipPlane, float aspect)
		{
			other.m_Position = position;

			CalculateFrustumCornersPerspective(
				ref other.m_Corners,
				ref position, 
				ref orientation, 
				fov, nearClipPlane, farClipPlane, aspect);

            float3 forward = math.mul(orientation, new float3(0, 0, 1));

			// CORNERS:
			// [0] = Far Bottom Left,  [1] = Far Top Left,  [2] = Far Top Right,  [3] = Far Bottom Right, 
			// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right

			// PLANES:
			// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far

			other.m_Planes[0] = new Plane(other.m_Corners[4], other.m_Corners[1], other.m_Corners[0]);
			other.m_Planes[1] = new Plane(other.m_Corners[6], other.m_Corners[3], other.m_Corners[2]);
			other.m_Planes[2] = new Plane(other.m_Corners[7], other.m_Corners[0], other.m_Corners[3]);
			other.m_Planes[3] = new Plane(other.m_Corners[5], other.m_Corners[2], other.m_Corners[1]);
			other.m_Planes[4] = new Plane(forward, position + forward * nearClipPlane);
			other.m_Planes[5] = new Plane(-forward, position + forward * farClipPlane);

			for (int i = 0; i < PlaneCount; i++)
			{
				var plane = other.m_Planes[i];
				var normal = plane.normal;

				other.m_AbsNormals[i] = math.abs(normal);
				other.m_PlaneNormals[i] = normal;
				other.m_PlaneDistances[i] = plane.distance;
			}
		}

		private static void CalculateFrustumCornersOrthographic(Camera camera, ref NativeArray<float3> _corners)
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

		private static void CalculateFrustumCornersPerspective(ref NativeArray<float3> _corners, ref float3 position, ref quaternion orientation, float fov, float nearClipPlane, float farClipPlane, float aspect)
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

			_corners[0] = position + math.mul(orientation, bottomLeft);
			_corners[1] = position + math.mul(orientation, topLeft);
			_corners[2] = position + math.mul(orientation, topRight);
			_corners[3] = position + math.mul(orientation, bottomRight);

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

			_corners[4] = position + math.mul(orientation, bottomLeft);
			_corners[5] = position + math.mul(orientation, topLeft);
			_corners[6] = position + math.mul(orientation, topRight);
			_corners[7] = position + math.mul(orientation, bottomRight);
		}
        #endregion
    }
}
