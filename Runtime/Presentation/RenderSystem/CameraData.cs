#undef UNITY_ADDRESSABLES

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    public struct CameraData
    {
		public bool orthographic;

		public float3 position;
		public quaternion orientation;
		public float fov;
		public float nearClipPlane;
		public float farClipPlane; 
		public float aspect;

		public float
			pixelWidth, pixelHeight;

		public float4x4 projectionMatrix;

		public CameraData(Camera cam)
        {
			this = default(CameraData);
			Update(cam);
		}
		public void Update(Camera cam)
        {
			CoreSystem.Logger.ThreadBlock(nameof(CameraData), Syadeu.Internal.ThreadInfo.Unity);
			Transform tr = cam.transform;

			orthographic = cam.orthographic;

			position = tr.position;
			orientation = tr.rotation;
			fov = cam.fieldOfView;
			nearClipPlane = cam.nearClipPlane;
			farClipPlane = cam.farClipPlane;
			aspect = cam.aspect;

			pixelWidth = cam.pixelWidth;
			pixelHeight = cam.pixelHeight;

			if (orthographic) projectionMatrix = float4x4.Ortho(pixelWidth, pixelHeight, nearClipPlane, farClipPlane);
			else projectionMatrix = float4x4.PerspectiveFov(fov, aspect, nearClipPlane, farClipPlane);
		}
	}
}
