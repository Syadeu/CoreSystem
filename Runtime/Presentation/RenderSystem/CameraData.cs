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
		public float3 position;
		public quaternion orientation;
		public float fov;
		public float nearClipPlane;
		public float farClipPlane; 
		public float aspect;

		public float
			pixelWidth, pixelHeigth;

		public CameraData(Camera cam)
        {
			CoreSystem.Logger.ThreadBlock(nameof(CameraData), Syadeu.Internal.ThreadInfo.Unity);
			Transform tr = cam.transform;

			position = tr.position;
			orientation = tr.rotation;
			fov = cam.fieldOfView;
			nearClipPlane = cam.nearClipPlane;
			farClipPlane = cam.farClipPlane;
			aspect = cam.aspect;

			pixelWidth = cam.pixelWidth;
			pixelHeigth = cam.pixelHeight;
        }
	}
}
