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

using Cinemachine;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    [RequireComponent(typeof(CameraComponent))]
    public abstract class AdditionalCameraComponent : MonoBehaviour
    {
        public RenderSystem RenderSystem { get; internal set; }
        public CameraComponent CameraComponent { get; internal set; }

        internal void InternalInitialize(Camera camera, CinemachineBrain brain, CinemachineStateDrivenCamera stateDrivenCamera, CinemachineTargetGroup targetGroup) => OnInitialize(camera, brain, stateDrivenCamera, targetGroup);
        internal void InternalOnRenderStart() => OnRenderStart();

        protected virtual void OnInitialize(Camera camera, CinemachineBrain brain, CinemachineStateDrivenCamera stateDrivenCamera, CinemachineTargetGroup targetGroup) { }
        protected virtual void OnRenderStart() { }
    }
}
