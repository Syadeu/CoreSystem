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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

#if !CORESYSTEM_URP && !CORESYSTEM_HDRP
#define CORESYSTEM_SRP
#endif

using System;
using UnityEngine;

namespace Syadeu.Presentation.Render.LowLevel
{
    public abstract class GlobalMaterialProcessor : IMaterialProcessor, IDisposable
    {
        internal Material m_CurrentProcessingMaterial;

        InstancedMaterial IMaterialProcessor.Material => default(InstancedMaterial);
        void IMaterialProcessor.OnInitialize() => OnInitialize();
        void IMaterialProcessor.OnProcess(MaterialPropertyBlock mpb) => OnProcess(mpb);
        void IDisposable.Dispose()
        {
            OnDispose();
        }

        protected Material Material => m_CurrentProcessingMaterial;

        protected virtual void OnInitialize() { }
        protected virtual void OnDispose() { }

        protected virtual void OnProcess(MaterialPropertyBlock mpb) { }

        protected int PropertyToID(in string name) => Shader.PropertyToID(name);
        protected void EnableKeyword(in string name, bool enable)
        {
            if (enable) Material.EnableKeyword(name);
            else Material.DisableKeyword(name);
        }
        protected void EnableGlobalKeyword(in string name, bool enable)
        {
            if (enable) Shader.EnableKeyword(name);
            else Shader.DisableKeyword(name);
        }
    }

    //class test : MaterialProcessor
    //{
    //    protected override Material Material => throw new System.NotImplementedException();

    //    protected override void OnProcess(MaterialPropertyBlock mpb)
    //    {
    //        Material.shader.
    //    }
    //}
}
