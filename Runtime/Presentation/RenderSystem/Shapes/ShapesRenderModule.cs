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
using Unity.Profiling;

namespace Syadeu.Presentation.Render
{
    public sealed class ShapesRenderModule : PresentationSystemModule<RenderSystem>
    {
#if CORESYSTEM_SHAPES
        private NativeQueue<InstanceID> m_BatchedShapeEntities;

        private static ProfilerMarker
            s_RenderShapesMarker = new ProfilerMarker($"{nameof(RenderSystem)}.{nameof(ShapesRenderModule)}.RenderShapes");

        private EntitySystem m_EntitySystem;
        private EntityComponentSystem m_ComponentSystem;
        private RenderSystem m_RenderSystem;
        private GameObjectProxySystem m_ProxySystem;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EntityComponentSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, GameObjectProxySystem>(Bind);

            m_BatchedShapeEntities = new NativeQueue<InstanceID>(AllocatorManager.Persistent);
        }

        #region Binds

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
        }
        private void Bind(EntityComponentSystem other)
        {
            m_ComponentSystem = other;

            m_ComponentSystem.OnComponentAdded += M_ComponentSystem_OnComponentAdded;
            m_ComponentSystem.OnComponentRemove += M_ComponentSystem_OnComponentRemove;
        }
        private void M_ComponentSystem_OnComponentAdded(InstanceID id, System.Type arg2)
        {
            if (!arg2.Equals(TypeHelper.TypeOf<ShapesComponent>.Type)) return;

            ref ShapesComponent com = ref id.GetComponent<ShapesComponent>();
            float3 pos;
            if (id.IsEntity<IEntity>())
            {
                Entity<IEntity> entity = id.GetEntity<IEntity>();
                ProxyTransform parent = (ProxyTransform)entity.transform;
                pos = parent.position;
                com.m_Transform = m_ProxySystem.CreateTransform(pos, quaternion.identity, 1);

                com.m_Transform.SetParent(parent);
                com.m_Transform.localPosition = 0;
                //com.m_Transform.localEulerAngles = new float3(90, 0, 0);
            }
            else
            {
                pos = float3.zero;
                com.m_Transform = m_ProxySystem.CreateTransform(pos, quaternion.EulerZXY(90, 0, 0), 1);
            }

            //com.m_Transform.localPosition = com.offsets.position;
            //com.m_Transform.localRotation = com.offsets.rotation;
        }
        private void M_ComponentSystem_OnComponentRemove(InstanceID id, System.Type arg2)
        {
            if (!arg2.Equals(TypeHelper.TypeOf<ShapesComponent>.Type)) return;

            ref ShapesComponent com = ref m_ComponentSystem.GetComponent<ShapesComponent>(id);
            com.m_Transform.Destroy();
        }

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;

            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
        }
        private void Bind(GameObjectProxySystem other)
        {
            m_ProxySystem = other;
        }

        #endregion

        protected override void OnShutDown()
        {
            m_ComponentSystem.OnComponentAdded -= M_ComponentSystem_OnComponentAdded;
            m_ComponentSystem.OnComponentRemove -= M_ComponentSystem_OnComponentRemove;

            RenderPipelineManager.beginCameraRendering -= RenderPipelineManager_beginCameraRendering;
        }
        protected override void OnDispose()
        {
            m_BatchedShapeEntities.Dispose();

            m_EntitySystem = null;
            m_ComponentSystem = null;
            m_RenderSystem = null;
            m_ProxySystem = null;
        }

        #endregion

        private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
        {
            using (s_RenderShapesMarker.Auto())
            using (Draw.Command(arg2))
            {
                int count = m_BatchedShapeEntities.Count;
                for (int i = 0; i < count; i++)
                {
                    var entity = m_BatchedShapeEntities.Dequeue();
                    if (!m_ComponentSystem.HasComponent<ShapesComponent>(entity)) continue;

                    DrawShapes(in arg2, m_ComponentSystem.GetComponent<ShapesComponent>(entity));
                }

                PrepareBatchShapesJob prepareJob = new PrepareBatchShapesJob()
                {
                    m_BatchedQueue = m_BatchedShapeEntities.AsParallelWriter()
                };

                m_RenderSystem.ScheduleAtRender<PrepareBatchShapesJob, ShapesComponent>(prepareJob);
            }
        }

        private static void DrawShapes(in Camera camera, in ShapesComponent shapes)
        {
            Draw.Thickness = shapes.generals.thickness;
            Draw.DiscGeometry = shapes.generals.discGeometry;

            switch (shapes.shape)
            {
                case ShapesComponent.Shape.Arc:
                    DrawArcShape(in shapes);
                    break;
                default:
                    break;
            }
        }
        private static void DrawArcShape(in ShapesComponent shapes)
        {
            ProxyTransform tr = shapes.transform;

            Draw.Arc(
                pos: tr.position + shapes.offsets.position,
                rot: shapes.offsets.rotation,
                angleRadStart: shapes.arcParameters.angleStart.radian,
                //angleRadStart: 0,
                //angleRadEnd: 350 * Mathf.Deg2Rad,
                angleRadEnd: shapes.arcParameters.angleEnd.radian,
                colors: shapes.generals.colors);

            //$"{Draw.Thickness}, {shapes.arcParameters.angleStart.radian}, {shapes.arcParameters.angleEnd.radian}, {tr.eulerAngles}".ToLog();
        }

        //public void Add(InstanceID id)
        //{
        //    m_Shapes.Add(id);

        //    id.AddComponent<ShapesComponent>();

        //    ref ShapesComponent com = ref id.GetComponent<ShapesComponent>();

        //    float3 pos;
        //    if (id.IsEntity<IEntity>())
        //    {
        //        pos = id.GetEntity<IEntity>().transform.position;
        //    }
        //    else pos = float3.zero;

        //    com.m_Transform =
        //        m_ProxySystem.CreateTransform(pos, quaternion.identity, 1);
        //}

        [BurstCompile(CompileSynchronously = true)]
        private struct PrepareBatchShapesJob : IJobParallelForEntities<ShapesComponent>
        {
            public NativeQueue<InstanceID>.ParallelWriter
                m_BatchedQueue;

            public void Execute(in InstanceID entity, ref ShapesComponent component)
            {
                //if (!component.transform.isVisible) return;

                m_BatchedQueue.Enqueue(entity);
            }
        }
#endif
    }
}
