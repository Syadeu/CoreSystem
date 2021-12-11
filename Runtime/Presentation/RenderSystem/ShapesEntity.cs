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

#if CORESYSTEM_SHAPES
using Shapes;
using Syadeu.Collections;
using Syadeu.Presentation.Components;
#endif

using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Syadeu.Presentation.Render
{
#if CORESYSTEM_SHAPES
    public sealed class ShapesEntity : EntityDataBase,
        INotifyComponent<ShapesComponent>
    {

    }
    internal sealed class ShapesEntityProcessor : EntityProcessor<ShapesEntity>
    {
        protected override void OnCreated(ShapesEntity obj)
        {
            ref ShapesComponent com = ref obj.GetComponent<ShapesComponent>();

            com.m_Transform =
                ProxySystem.CreateTransform(float3.zero, quaternion.identity, 1);

            PresentationSystem<DefaultPresentationGroup, RenderSystem>.System.GetModule<ShapesRenderModule>().Add(obj.Idx);
        }
    }

    public struct ShapesComponent : IEntityComponent
    {
        internal ProxyTransform m_Transform;

        public ProxyTransform transform => m_Transform;
    }
    public sealed class ShapesRenderModule : PresentationSystemModule<RenderSystem>
    {
        List<InstanceID> m_Shapes = new List<InstanceID>();

        protected override void OnInitialize()
        {
            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
        }

        private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
        {
            for (int i = 0; i < m_Shapes.Count; i++)
            {
                ref var component = ref m_Shapes[i].GetComponent<ShapesComponent>();

                using (Draw.Command(arg2))
                {
                    Draw.Disc(component.transform.position, component.transform.up, 1, DiscColors.Flat(Color.white));
                }
            }
        }
        protected override void OnDispose()
        {
            RenderPipelineManager.beginCameraRendering -= RenderPipelineManager_beginCameraRendering;
        }

        protected override void TransformPresentation()
        {
            
        }

        public void Add(InstanceID id)
        {
            m_Shapes.Add(id);
        }

        private struct ShapesJob : IJobParallelForEntities<ShapesComponent>
        {
            public void Execute(in InstanceID entity, in ShapesComponent component)
            {
                Draw.Disc(component.transform.position, component.transform.up);
            }
        }
    }
#endif
}
