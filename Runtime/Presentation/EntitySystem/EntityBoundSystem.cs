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

using Syadeu.Collections;
using Syadeu.Mono;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using AABB = Syadeu.Collections.AABB;

namespace Syadeu.Presentation
{
    /// <summary>
    /// TODO : 최적화 대상
    /// </summary>
    internal sealed class EntityBoundSystem : PresentationSystemEntity<EntityBoundSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private Cluster<TriggerBoundAttribute> m_TriggerBoundCluster;
        private Entity<IEntity>[] m_TriggerBoundArray;

        private bool m_DrawBounds = false;
        private NativeArray<float3> m_Vertices;

        internal Cluster<TriggerBoundAttribute> BoundCluster => m_TriggerBoundCluster;
        internal IReadOnlyList<Entity<IEntity>> TriggerBoundArray => m_TriggerBoundArray;

        private EntitySystem m_EntitySystem;
        private EventSystem m_EventSystem;
        private RenderSystem m_RenderSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_TriggerBoundCluster = new Cluster<TriggerBoundAttribute>(1024);
            m_TriggerBoundArray = new Entity<IEntity>[1024];

            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnShutDown()
        {
            m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy -= M_EntitySystem_OnEntityDestroy;
        }
        public override void OnDispose()
        {
            m_TriggerBoundArray = Array.Empty<Entity<IEntity>>();
            m_TriggerBoundCluster.Dispose();

            if (m_Vertices.IsCreated) m_Vertices.Dispose();

            m_EntitySystem = null;
            m_EventSystem = null;
        }

        protected override PresentationResult OnStartPresentation()
        {
            ConsoleWindow.CreateCommand(EnableDrawTriggerBoundsCmd, "draw", "triggerbounds");

            return base.OnStartPresentation();
        }
        private void EnableDrawTriggerBoundsCmd(string cmd)
        {
            m_DrawBounds = !m_DrawBounds;
        }

        #region Bind

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;

            m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
        }
        private void M_EntitySystem_OnEntityCreated(IObject obj)
        {
            if (!(obj is EntityBase entity)) return;
            TriggerBoundAttribute att = entity.GetAttribute<TriggerBoundAttribute>();
            if (att == null) return;

            int arrayIndex = FindOrIncrementTriggerBoundArrayIndex();
            ClusterID id = m_TriggerBoundCluster.Add(entity.transform.position, arrayIndex);

            Entity<IEntity> target = Entity<IEntity>.GetEntityWithoutCheck(obj.Idx);
            m_TriggerBoundArray[arrayIndex] = target;

            att.m_ClusterID = id;
        }
        private void M_EntitySystem_OnEntityDestroy(IObject obj)
        {
            if (!(obj is EntityBase entity)) return;
            TriggerBoundAttribute att = entity.GetAttribute<TriggerBoundAttribute>();
            if (att == null) return;

            int arrayIndex = m_TriggerBoundCluster.Remove(att.m_ClusterID);
            m_TriggerBoundArray[arrayIndex] = Entity<IEntity>.Empty;

            att.m_ClusterID = ClusterID.Empty;
        }

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        }

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;

            m_RenderSystem.OnRender += M_RenderSystem_OnRender;
        }
        private void M_RenderSystem_OnRender(ScriptableRenderContext ctx, Camera cam)
        {
            if (!m_DrawBounds || m_RenderSystem.Camera == null) return;

            if (!m_Vertices.IsCreated)
            {
                m_Vertices = new NativeArray<float3>(8, Allocator.Persistent);
            }

            GL.PushMatrix();

            float3x3 rotmat = new float3x3(quaternion.identity);
            float4x4 mat = new float4x4(rotmat, float3.zero);
            GL.MultMatrix(mat);
            GL.LoadProjectionMatrix(m_RenderSystem.Camera.projectionMatrix);
            GridExtensions.DefaultMaterial.SetPass(0);

            GL.Begin(GL.LINES);
            GL.Color(Color.red);
            for (int i = 0; i < m_TriggerBoundArray.Length; i++)
            {
                if (!m_TriggerBoundArray[i].IsValid()) continue;

                var temp = m_TriggerBoundArray[i].transform.aabb;
                temp.GetVertices(m_Vertices);
                for (int a = 0; a < m_Vertices.Length; a++)
                {
                    GL.Vertex(m_Vertices[a]);
                }
            }
            GL.End();

            GL.PopMatrix();
        }

        #endregion

        #endregion

        #region Events

        private struct TriggerBoundJob : IJobParallelForEntities<TriggerBoundComponent>
        {
            public void Execute(in InstanceID entity, in TriggerBoundComponent component)
            {
                throw new NotImplementedException();
            }
        }
        private void OnTransformChangedEventHandler(OnTransformChangedEvent ev)
        {
            if (!ev.entity.IsValid()) return;

            TriggerBoundAttribute att = ev.entity.GetAttribute<TriggerBoundAttribute>();
            if (att == null) return;

            FindAndPostEvent(att);

            void FindAndPostEvent(TriggerBoundAttribute att)
            {
                ClusterID updatedID = m_TriggerBoundCluster.Update(att.m_ClusterID, ev.entity.transform.position);

                att.m_ClusterID = updatedID;

                for (int i = 0; i < m_TriggerBoundArray.Length; i++)
                {
                    if (!m_TriggerBoundArray[i].IsValid() || m_TriggerBoundArray[i].Hash.Equals(att.ParentEntity.Hash)) continue;

                    TryTrigger(in m_EventSystem, ev.entity, in m_TriggerBoundArray[i]);
                }
            }
            static void TryTrigger(in EventSystem eventSystem, in Entity<IEntity> from, in Entity<IEntity> to)
            {
                TriggerBoundAttribute fromAtt = from.GetAttribute<TriggerBoundAttribute>();
                TriggerBoundAttribute toAtt = to.GetAttribute<TriggerBoundAttribute>();

                AABB fromAABB = fromAtt.m_MatchWithAABB ? from.transform.aabb : new AABB(fromAtt.m_Center + from.transform.position, fromAtt.m_Center);
                AABB toAABB = toAtt.m_MatchWithAABB ? to.transform.aabb : new AABB(toAtt.m_Center + to.transform.position, toAtt.m_Center);

                CoreSystem.Logger.False(fromAtt.m_Triggered == toAtt.m_Triggered, "??");

                if (fromAABB.Intersect(toAABB))
                {
                    //"1".ToLog();
                    if (CanTriggerable(in fromAtt, in to) && !fromAtt.m_Triggered.Contains(to))
                    {
                        fromAtt.m_Triggered.Add(to);
                        eventSystem.PostEvent(EntityTriggerBoundEvent.GetEvent(from, to, true));
                    }
                    if (CanTriggerable(in toAtt, in from) && !toAtt.m_Triggered.Contains(from))
                    {
                        toAtt.m_Triggered.Add(from);
                        eventSystem.PostEvent(EntityTriggerBoundEvent.GetEvent(to, from, true));
                    }
                }
                else
                {
                    if (/*CanTriggerable(in fromAtt, in to) &&*/ fromAtt.m_Triggered.Contains(to))
                    {
                        fromAtt.m_Triggered.Remove(to);
                        eventSystem.PostEvent(EntityTriggerBoundEvent.GetEvent(from, to, false));
                    }
                    if (/*CanTriggerable(in toAtt, in from) && */toAtt.m_Triggered.Contains(from))
                    {
                        toAtt.m_Triggered.Remove(from);
                        eventSystem.PostEvent(EntityTriggerBoundEvent.GetEvent(to, from, false));
                    }
                }
            }
            
        }

        #endregion

        private static bool CanTriggerable(in TriggerBoundAttribute att, in Entity<IEntity> target)
        {
            if (!att.Enabled) return false;
            else if (att.m_TriggerOnly.Length == 0) return true;

            if (att.m_Inverse)
            {
                for (int i = 0; i < att.m_TriggerOnly.Length; i++)
                {
                    if (att.m_TriggerOnly[i].Hash.Equals(target.Hash))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                for (int i = 0; i < att.m_TriggerOnly.Length; i++)
                {
                    if (att.m_TriggerOnly[i].Hash.Equals(target.Hash))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private int FindOrIncrementTriggerBoundArrayIndex()
        {
            for (int i = 0; i < m_TriggerBoundArray.Length; i++)
            {
                if (m_TriggerBoundArray[i].Equals(Entity<IEntity>.Empty)) return i;
            }

            Entity<IEntity>[] newArr = new Entity<IEntity>[m_TriggerBoundArray.Length * 2];
            Array.Copy(m_TriggerBoundArray, newArr, m_TriggerBoundArray.Length);
            m_TriggerBoundArray = newArr;

            return FindOrIncrementTriggerBoundArrayIndex();
        }
    }
}
