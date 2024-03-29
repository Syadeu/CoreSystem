﻿// Copyright 2021 Seung Ha Kim
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
		public float orthographicSize;
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
				CoreSystem.Logger.LogError(LogChannel.Data,
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

			//if (orthographic) projectionMatrix = float4x4.Ortho(pixelWidth, pixelHeight, nearClipPlane, farClipPlane);
			//else projectionMatrix = float4x4.PerspectiveFov(fov, aspect, nearClipPlane, farClipPlane);
			projectionMatrix = cam.projectionMatrix;
			orthographicSize = cam.orthographicSize;
		}
	}

	public struct LightData
    {
		public bool isCreated;
		public LightType type;
		public LightShadowResolution shadowResolution;

		public float3 position;
		public quaternion orientation;

		public float3 forward
        {
			get => math.mul(orientation, new float3(0, 0, 1));
        }
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
				CoreSystem.Logger.LogError(LogChannel.Data,
					$"{nameof(LightData)} trying to update null");
				return;
            }
			isCreated = true;

			type = light.type;
			shadowResolution = light.shadowResolution;

			Transform tr = light.transform;
			position = tr.position;
			orientation = tr.rotation;
		}
		public Ray PointToRay(float3 point)
        {
			float3 forward = math.mul(orientation, new float3(0, 0, 1));
			return new Ray(point, forward);
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
