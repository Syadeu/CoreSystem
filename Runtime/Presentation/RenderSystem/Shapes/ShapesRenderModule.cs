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
#endif

using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using Syadeu.Collections;
using Syadeu.Presentation.Components;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Syadeu.Presentation.Render
{
    public sealed class ShapesRenderModule : PresentationSystemModule<RenderSystem>
    {
#if CORESYSTEM_SHAPES
        List<InstanceID> m_Shapes = new List<InstanceID>();

        private NativeQueue<InstanceID> m_BatchedShapeEntities;

        private EntitySystem m_EntitySystem;
        private GameObjectProxySystem m_ProxySystem;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);
            RequestSystem<DefaultPresentationGroup, GameObjectProxySystem>(Bind);

            m_BatchedShapeEntities = new NativeQueue<InstanceID>(AllocatorManager.Persistent);

            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
        }

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;

            m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
        }

        private void M_EntitySystem_OnEntityCreated(IObject obj)
        {
            Add(obj.Idx);
        }

        private void Bind(GameObjectProxySystem other)
        {
            m_ProxySystem = other;
        }
        
        protected override void OnDispose()
        {
            RenderPipelineManager.beginCameraRendering -= RenderPipelineManager_beginCameraRendering;
            m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;

            m_BatchedShapeEntities.Dispose();

            m_EntitySystem = null;
            m_ProxySystem = null;
        }

        #endregion

        private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
        {
            using (new CoreSystem.LogTimer("for batched", Channel.Debug))
            {
                for (int i = 0; i < m_Shapes.Count; i++)
                {
                    ref var component = ref m_Shapes[i].GetComponent<ShapesComponent>();
                    DrawShapes(in arg2, in component);
                }
            }
            
            using (new CoreSystem.LogTimer("queue batched", Channel.Debug))
            {
                int count = m_BatchedShapeEntities.Count;
                for (int i = 0; i < count; i++)
                {
                    DrawShapes(in arg2, m_BatchedShapeEntities.Dequeue().GetComponent<ShapesComponent>());
                }

                PrepareBatchShapesJob prepareJob = new PrepareBatchShapesJob()
                {
                    m_BatchedQueue = m_BatchedShapeEntities.AsParallelWriter()
                };

                System.ScheduleAt<PrepareBatchShapesJob, ShapesComponent>(
                    Internal.PresentationSystemEntity.JobPosition.Transform,
                    prepareJob);
            }
        }

        private static void DrawShapes(in Camera camera, in ShapesComponent shapes)
        {
            using (Draw.Command(camera))
            {
                Draw.Thickness = .02f;
                Draw.DiscGeometry = DiscGeometry.Flat2D;

                Draw.Arc(
                    pos: shapes.transform.position,
                    normal: shapes.transform.up,
                    angleRadStart: Mathf.Rad2Deg * 0,
                    angleRadEnd: Mathf.Rad2Deg * 351,
                    colors: DiscColors.Flat(Color.white));
            }
        }

        public void Add(InstanceID id)
        {
            m_Shapes.Add(id);

            id.AddComponent<ShapesComponent>();

            ref ShapesComponent com = ref id.GetComponent<ShapesComponent>();

            float3 pos;
            if (id.IsEntity<IEntity>())
            {
                pos = id.GetEntity<IEntity>().transform.position;
            }
            else pos = float3.zero;

            com.m_Transform =
                m_ProxySystem.CreateTransform(pos, quaternion.identity, 1);
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct PrepareBatchShapesJob : IJobParallelForEntities<ShapesComponent>
        {
            public NativeQueue<InstanceID>.ParallelWriter
                m_BatchedQueue;

            public void Execute(in InstanceID entity, in ShapesComponent component)
            {
                if (!component.transform.isVisible) return;

                m_BatchedQueue.Enqueue(entity);
            }
        }
        private struct ShapesJob : IJobParallelForEntities<ShapesComponent>
        {
            public void Execute(in InstanceID entity, in ShapesComponent component)
            {
                Draw.Disc(component.transform.position, component.transform.up);
            }
        }
#endif
    }
}
