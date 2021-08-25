#undef UNITY_ADDRESSABLES

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

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
		public float4x4 localToWorldMatrix
        {
            get
            {
				float3x3 rot = new float3x3(orientation);
				return new float4x4(rot, position);
			}
        }
		public float4x4 worldToLocalMatrix => math.fastinverse(localToWorldMatrix);
		public float4x4 cameraSpace
        {
            get
            {
				return math.mul(projectionMatrix, math.fastinverse(localToWorldMatrix));
			}
        }

		public float4x4 orthographicProjection
        {
            get => float4x4.Ortho(pixelWidth, pixelHeight, nearClipPlane, farClipPlane);
		}
		public float4x4 perspectiveProjection
        {
			get => float4x4.PerspectiveFov(fov, aspect, nearClipPlane, farClipPlane);
		}

		public CameraData(Camera cam)
        {
			this = default(CameraData);
			Update(cam);
		}
		public void Update(Camera cam)
        {
			CoreSystem.Logger.ThreadBlock(nameof(CameraData), Syadeu.Internal.ThreadInfo.Unity);
			if (cam == null)
            {
				CoreSystem.Logger.LogError(Channel.Data,
					$"{nameof(CameraData)} trying to update null");
				return;
			}
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

	public struct LightData
    {
		public LightType type;
		public LightShadowResolution shadowResolution;

		public float3 position;
		public quaternion orientation;

		public float4x4 projectionMatrix
        {
            get
            {
                float4
                    a0 = new float4(.5f, 0, 0, 0),
                    a1 = new float4(0, .5f, 0, 0),
                    a2 = new float4(0, 0, .5f, 0),
                    a3 = new float4(.5f, .5f, .5f, 1);
                //float4
                //    a0 = new float4(.5f, 0, 0, 0.5f),
                //    a1 = new float4(0, .5f, 0, 0.5f),
                //    a2 = new float4(0, 0, .5f, 0.5f),
                //    a3 = new float4(0, 0, 0, 1);
                return new float4x4(a0, a1, a2, a3);
			}
        }
        public float4x4 lightSpace
        {
            get
            {
                float3x3 rot = new float3x3(orientation);
                float4x4 tr = new float4x4(rot, position);
                return math.mul(projectionMatrix, math.fastinverse(tr));
            }
        }

        public LightData(Light light)
        {
			this = default(LightData);
			Update(light);
        }
		public void Update(Light light)
        {
			CoreSystem.Logger.ThreadBlock(nameof(LightData), Syadeu.Internal.ThreadInfo.Unity);
			if (light == null)
            {
				CoreSystem.Logger.LogError(Channel.Data,
					$"{nameof(LightData)} trying to update null");
				return;
            }

			type = light.type;
			shadowResolution = light.shadowResolution;

			Transform tr = light.transform;
			position = tr.position;
			orientation = tr.rotation;
		}

		private static float ToMultiplier(LightShadowResolution resolution)
        {
            switch (resolution)
            {
				default:
				case LightShadowResolution.Low:
					return .125f;
                case LightShadowResolution.Medium:
					return .25f;
                case LightShadowResolution.High:
					return .5f;
                case LightShadowResolution.VeryHigh:
					return 1;
				case LightShadowResolution.FromQualitySettings:
                    switch (QualitySettings.shadowResolution)
                    {
						default:
						case ShadowResolution.Low:
							return .125f;
						case ShadowResolution.Medium:
							return .25f;
						case ShadowResolution.High:
							return .5f;
						case ShadowResolution.VeryHigh:
							return 1;
					}
            }
        }
    }
}
