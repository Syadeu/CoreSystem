// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Syadeu.Collections;
using Syadeu.Internal;
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using AABB = Syadeu.Collections.AABB;
using Plane = Syadeu.Collections.Plane;

namespace Syadeu.Presentation.Render
{
	[NativeContainer]
    public struct CameraFrustum : IDisposable
    {
		public const int PlaneCount = 6;
		public const int CornerCount = 8;

#if UNITY_EDITOR
		public AtomicSafetyHandle m_Safety;
		[NativeSetClassTypeToNullOnSchedule] public DisposeSentinel m_DisposeSentinel;
#endif
		private readonly bool m_IsCreated;

		private float3 m_Position;
		private NativeArray<Plane> m_Planes;
		private NativeArray<float3>
			m_Corners,
			m_AbsNormals,
			m_PlaneNormals;
		private NativeArray<float> m_PlaneDistances;

		public float3 Position => m_Position;

		[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
		[NativeContainer, NativeContainerIsReadOnly]
		public struct ReadOnly
        {
			public readonly float3 position;
			//public readonly NativeArray<Plane> planes;
			public readonly NativeArray<float3>
				corners,
				absNormals,
				planeNormals;
			public readonly NativeArray<float> planeDistances;

#if UNITY_EDITOR
			internal AtomicSafetyHandle m_Safety;
#endif

			internal ReadOnly(ref CameraFrustum data)
            {
				position = data.m_Position;
				//planes = data.m_Planes;
				corners = data.m_Corners;
				absNormals = data.m_AbsNormals;
				planeNormals = data.m_PlaneNormals;
				planeDistances = data.m_PlaneDistances;

#if UNITY_EDITOR
				m_Safety = data.m_Safety;
				AtomicSafetyHandle.UseSecondaryVersion(ref m_Safety);
#endif
			}

			public bool Contains(in float3 point)
            {
#if UNITY_EDITOR
				AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
				AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
				return CameraFrustum.Contains(in planeNormals, in planeDistances, in point);
			}
			public bool IntersectsBox(in AABB box, float frustumPadding = 0)
            {
#if UNITY_EDITOR
				AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
				AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
				IntersectionType type = CameraFrustum.IntersectsBox(in corners, in absNormals, in planeNormals, in planeDistances,
					in box, frustumPadding);

				if ((type & IntersectionType.Intersects) == IntersectionType.Intersects ||
				(type & IntersectionType.Contains) == IntersectionType.Contains) return true;
				return false;
			}
			public IntersectionType IntersectsSphere(in float3 center, in float radius, float frustumPadding = 0)
            {
#if UNITY_EDITOR
				AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
				AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
				return CameraFrustum.IntersectsSphere(in planeNormals, in planeDistances, in center, in radius, frustumPadding);
			}
		}

        #region Constructor

		public CameraFrustum(Camera cam)
        {
			CoreSystem.Logger.ThreadBlock(nameof(CameraFrustum), Syadeu.Internal.ThreadInfo.Unity);

			this = default(CameraFrustum);

			Allocate(ref this);

			Update(ref this, cam);
			m_IsCreated = true;
		}
		public CameraFrustum(CameraData cam)
        {
			this = default(CameraFrustum);

			Allocate(ref this);

			UpdatePers(ref this, cam.position, cam.orientation, cam.fov, cam.nearClipPlane, cam.farClipPlane, cam.aspect);
			m_IsCreated = true;
		}
		private static void Allocate(ref CameraFrustum cameraFrustum)
        {
			cameraFrustum.m_Corners = new NativeArray<float3>(CornerCount, Allocator.Persistent);
			cameraFrustum.m_Planes = new NativeArray<Plane>(PlaneCount, Allocator.Persistent);
			cameraFrustum.m_AbsNormals = new NativeArray<float3>(PlaneCount, Allocator.Persistent);
			cameraFrustum.m_PlaneNormals = new NativeArray<float3>(PlaneCount, Allocator.Persistent);
			cameraFrustum.m_PlaneDistances = new NativeArray<float>(PlaneCount, Allocator.Persistent);

#if UNITY_EDITOR
			DisposeSentinel.Create(out cameraFrustum.m_Safety, out cameraFrustum.m_DisposeSentinel, 1, Allocator.Persistent);
#endif
		}

		public void Dispose()
        {
#if UNITY_EDITOR
			DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

			m_Planes.Dispose();
			m_Corners.Dispose();
			m_AbsNormals.Dispose();
			m_PlaneNormals.Dispose();
			m_PlaneDistances.Dispose();
		}

		public ReadOnly AsReadOnly()
        {
			CoreSystem.Logger.ThreadBlock(nameof(AsReadOnly), ThreadInfo.Unity);
			return new ReadOnly(ref this);
		}

		public void Copy(in CameraFrustum from)
        {
#if UNITY_EDITOR
			AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
			AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

			CoreSystem.Logger.ThreadBlock(nameof(Copy), ThreadInfo.Unity);
#endif

			m_Position = from.m_Position;
            for (int i = 0; i < CornerCount; i++)
            {
				m_Corners[i] = from.m_Corners[i];
            }
            for (int i = 0; i < PlaneCount; i++)
            {
				m_Planes[i] = from.m_Planes[i];
				m_AbsNormals[i] = from.m_AbsNormals[i];
				m_PlaneNormals[i] = from.m_PlaneNormals[i];
				m_PlaneDistances[i] = from.m_PlaneDistances[i];
			}
		}

		#endregion

		public void Update(Camera cam)
        {
#if UNITY_EDITOR
			AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
			AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

			CoreSystem.Logger.ThreadBlock(nameof(Update), ThreadInfo.Unity);
#endif
			Update(ref this, cam);
		}

		public bool Contains(in float3 point)
        {
#if UNITY_EDITOR
			AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
			AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
			return Contains(in m_PlaneNormals, in m_PlaneDistances, in point);
		}
        public bool IntersectsRay(in Ray ray, out float distance)
        {
            distance = float.MaxValue;
            bool interect = false;
            for (int i = 0; i < m_Planes.Length; i++)
            {
                interect |= m_Planes[i].Raycast(ray, out float dis);
                if (interect && dis < distance) distance = dis;
            }
            return interect;
        }
        public bool IntersectsBox(in AABB box, float frustumPadding = 0)
        {
#if UNITY_EDITOR
			AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
			AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
			IntersectionType type = IntersectsBox(in m_Corners, in m_AbsNormals, in m_PlaneNormals, in m_PlaneDistances,
				in box, frustumPadding);

			if ((type & IntersectionType.Intersects) == IntersectionType.Intersects ||
				(type & IntersectionType.Contains) == IntersectionType.Contains) return true;
			return false;
		}
		public IntersectionType IntersectsSphere(in float3 center, in float radius, float frustumPadding = 0)
        {
#if UNITY_EDITOR
			AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
			AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
			return IntersectsSphere(in m_PlaneNormals, in m_PlaneDistances, in center, in radius, frustumPadding);
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
				CalculateFrustumCornersOrthographic(
					ref other.m_Corners,
					other.m_Position,
					rot,
					cam.orthographicSize,
					cam.nearClipPlane,
					cam.farClipPlane,
					cam.aspect);
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

				other.m_AbsNormals[i] = math.abs(normal);
				other.m_PlaneNormals[i] = normal;
				other.m_PlaneDistances[i] = plane.distance;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void UpdatePers(ref CameraFrustum other, float3 position, quaternion orientation, float fov, float nearClipPlane, float farClipPlane, float aspect)
		{
			other.m_Position = position;

			CalculateFrustumCornersPerspective(
				ref other.m_Corners,
				ref position, 
				ref orientation, 
				fov, nearClipPlane, farClipPlane, aspect);

			float3 forward = math.mul(orientation, math.forward());

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
		internal static void UpdateOrtho(ref CameraFrustum other, float3 position, quaternion orientation,
			float orthographicSize, float nearClipPlane, float farClipPlane, float aspect)
        {
			other.m_Position = position;

			CalculateFrustumCornersOrthographic(
					ref other.m_Corners,
					other.m_Position,
					orientation,
					nearClipPlane,
					farClipPlane,
					orthographicSize,
					aspect);

			float3 forward = math.mul(orientation, math.forward());

			// CORNERS:
			// [0] = Far Bottom Left,  [1] = Far Top Left,  [2] = Far Top Right,  [3] = Far Bottom Right, 
			// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right

			// PLANES:
			// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
			other.m_Planes[0] = new Plane(other.m_Corners[4], other.m_Corners[1], other.m_Corners[0]);
			other.m_Planes[1] = new Plane(other.m_Corners[6], other.m_Corners[3], other.m_Corners[2]);
			other.m_Planes[2] = new Plane(other.m_Corners[7], other.m_Corners[0], other.m_Corners[3]);
			other.m_Planes[3] = new Plane(other.m_Corners[5], other.m_Corners[2], other.m_Corners[1]);
			other.m_Planes[4] = new Plane(forward, other.m_Position + forward * nearClipPlane);
			other.m_Planes[5] = new Plane(-forward, other.m_Position + forward * farClipPlane);

			for (int i = 0; i < PlaneCount; i++)
			{
				var plane = other.m_Planes[i];
				var normal = plane.normal;

				other.m_AbsNormals[i] = math.abs(normal);
				other.m_PlaneNormals[i] = normal;
				other.m_PlaneDistances[i] = plane.distance;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Contains(
			in NativeArray<float3> planeNormals, 
			in NativeArray<float> planeDistances, 
			in float3 point)
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
		private static IntersectionType IntersectsBox(
			in NativeArray<float3> corners,
			in NativeArray<float3> absNormals,
			in NativeArray<float3> planeNormals,
			in NativeArray<float> planeDistances,
			in AABB box,
			float frustumPadding = 0)
		{
			if (box.Contains(corners[CornerCount - 1]))
			{
				return IntersectionType.True;
			}
			float3 center = box.center;
			float3 extents = box.extents;

			bool intersecting = false;
			for (int i = 0; i < PlaneCount; i++)
			{
				float3 abs = absNormals[i];

				float3 planeNormal = planeNormals[i];
				float planeDistance = planeDistances[i];

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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static IntersectionType IntersectsSphere(
			in NativeArray<float3> planeNormals,
			in NativeArray<float> planeDistances,
			in float3 center, in float radius, float frustumPadding = 0)
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

		private static void CalculateFrustumCornersOrthographic(ref NativeArray<float3> _corners, float3 position, quaternion orientation, float nearClipPlane, float farClipPlane, float orthographicSize, float aspect)
		{
			var forward = math.mul(orientation, math.forward());
			var right = math.mul(orientation, math.right()) * orthographicSize * aspect;
			var up = math.mul(orientation, math.up()) * orthographicSize;

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

			Vector3 topLeft = (Vector3.forward - toRight + toTop);
			float camScale = topLeft.magnitude * farClipPlane;

			topLeft.Normalize();
			topLeft *= camScale;

			Vector3 topRight = (Vector3.forward + toRight + toTop);
			topRight.Normalize();
			topRight *= camScale;

			Vector3 bottomRight = (Vector3.forward + toRight - toTop);
			bottomRight.Normalize();
			bottomRight *= camScale;

			Vector3 bottomLeft = (Vector3.forward - toRight - toTop);
			bottomLeft.Normalize();
			bottomLeft *= camScale;

			_corners[0] = position + math.mul(orientation, bottomLeft);
			_corners[1] = position + math.mul(orientation, topLeft);
			_corners[2] = position + math.mul(orientation, topRight);
			_corners[3] = position + math.mul(orientation, bottomRight);

			topLeft = (Vector3.forward - toRight + toTop);
			camScale = topLeft.magnitude * nearClipPlane;

			topLeft.Normalize();
			topLeft *= camScale;

			topRight = (Vector3.forward + toRight + toTop);
			topRight.Normalize();
			topRight *= camScale;

			bottomRight = (Vector3.forward + toRight - toTop);
			bottomRight.Normalize();
			bottomRight *= camScale;

			bottomLeft = (Vector3.forward - toRight - toTop);
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
