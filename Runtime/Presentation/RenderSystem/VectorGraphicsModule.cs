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

using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VectorGraphics;
using static Unity.VectorGraphics.VectorUtils;

namespace Syadeu.Presentation.Render
{
    public sealed class VectorGraphicsModule : PresentationSystemModule<RenderSystem>
    {
        private Scene m_VectorScene;
        private List<Geometry> m_Geometries;

        private SceneSystem m_SceneSystem;

        protected override void OnInitialize()
        {
            m_VectorScene = new Scene();
            m_VectorScene.Root = new SceneNode()
            {
                Transform = Matrix2D.identity
            };

            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_SceneSystem = null;

            m_SceneSystem.OnLoadingExit -= M_SceneSystem_OnSceneChanged;
        }

        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
            m_SceneSystem.OnLoadingExit += M_SceneSystem_OnSceneChanged;
        }

        private void M_SceneSystem_OnSceneChanged()
        {
            //m_Geometries = VectorUtils.TessellateScene(m_VectorScene, new TessellationOptions()
            //{
            //    StepDistance = 10,
            //    MaxCordDeviation = .5f,
            //    MaxTanAngleDeviation = .1f,
            //    SamplingStepSize = 100
            //});
        }
    }
}
