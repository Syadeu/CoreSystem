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
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;

            m_RenderSystem.OnRender += RenderPipelineManager_beginCameraRendering;
        }
        private void Bind(GameObjectProxySystem other)
        {
            m_ProxySystem = other;
        }

        #endregion

        protected override void OnShutDown()
        {
            m_RenderSystem.OnRender -= RenderPipelineManager_beginCameraRendering;
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
            switch (shapes.shape)
            {
                case ShapesComponent.Shape.Disc:
                    DrawDiscShape(in shapes);
                    break;
                case ShapesComponent.Shape.Rectangle:
                    break;
                default:
                    break;
            }
        }
        public static void DrawDiscShape(in ShapesComponent shapes)
        {
            ProxyTransform tr = shapes.transform;
            Draw.Thickness = shapes.generals.thickness;
            Draw.DiscGeometry = shapes.discParameters.discGeometry;

            Draw.Arc(
                pos: tr.position + shapes.offsets.position,
                rot: shapes.offsets.rotation,
                angleRadStart: shapes.discParameters.angleStart.radian,
                angleRadEnd: shapes.discParameters.angleEnd.radian,
                colors: shapes.discParameters.colors);

            //$"{Draw.Thickness}, {shapes.arcParameters.angleStart.radian}, {shapes.arcParameters.angleEnd.radian}, {tr.eulerAngles}".ToLog();
        }
        public static void DrawRectangleShape(in ShapesComponent shapes)
        {
            ProxyTransform tr = shapes.transform;

            if (shapes.rectangleParameters.enableFill)
            {
                Draw.UseGradientFill = true;
                Draw.GradientFill = shapes.rectangleParameters.fill;
            }

            switch (shapes.rectangleParameters.type)
            {
                case Rectangle.RectangleType.HardSolid:
                    Draw.Rectangle(
                        pos: tr.position + shapes.offsets.position,
                        rot: shapes.offsets.rotation,
                        size: shapes.rectangleParameters.size,
                        pivot: shapes.rectangleParameters.pivot
                    );
                    break;
                case Rectangle.RectangleType.RoundedSolid:
                    Draw.Rectangle(
                        pos: tr.position + shapes.offsets.position,
                        rot: shapes.offsets.rotation,
                        size: shapes.rectangleParameters.size,
                        pivot: shapes.rectangleParameters.pivot
                    );
                    break;
                case Rectangle.RectangleType.HardBorder:
                    Draw.RectangleBorder(
                        pos: tr.position + shapes.offsets.position,
                        rot: shapes.offsets.rotation,
                        size: shapes.rectangleParameters.size,
                        pivot: shapes.rectangleParameters.pivot,
                        thickness: shapes.generals.thickness
                    );
                    break;
                case Rectangle.RectangleType.RoundedBorder:
                    Draw.RectangleBorder(
                        pos: tr.position + shapes.offsets.position,
                        rot: shapes.offsets.rotation,
                        size: shapes.rectangleParameters.size,
                        pivot: shapes.rectangleParameters.pivot,
                        thickness: shapes.generals.thickness
                    );
                    break;
                default:
                    break;
            }

            Draw.UseGradientFill = false;
            Draw.GradientFill = GradientFill.defaultFill;
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
