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
using Syadeu.Collections.Buffer;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGSelectionSystem : PresentationSystemEntity<TRPGSelectionSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        public override bool IsStartable => m_RenderSystem.CameraComponent != null;

        private ObjectPool<Selection> m_SelectionPool;
        private List<Selection> m_SelectedEntities;

        private UnityEngine.InputSystem.InputAction
            m_LeftMouseButtonAction,
            m_RightMouseButtonAction;

        private RenderSystem m_RenderSystem;
        private InputSystem m_InputSystem;
        private EntityRaycastSystem m_EntityRaycastSystem;
        private CoroutineSystem m_CoroutineSystem;
        private NavMeshSystem m_NavMeshSystem;

        // LevelDesignPresentationGroup
        private LevelDesignSystem m_LevelDesignSystem;

        private TRPGTurnTableSystem m_TurnTableSystem;

        protected override PresentationResult OnInitialize()
        {
            m_SelectionPool = new ObjectPool<Selection>(Selection.Factory, null, Selection.OnReserve, null);
            m_SelectedEntities = new List<Selection>();

            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EntityRaycastSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, CoroutineSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, NavMeshSystem>(Bind);
            RequestSystem<LevelDesignPresentationGroup, LevelDesignSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnShutDown()
        {
            m_RenderSystem.OnRenderShapes -= M_RenderSystem_OnRenderShapes;

            for (int i = 0; i < m_SelectedEntities.Count; i++)
            {
                m_SelectionPool.Reserve(m_SelectedEntities[i]);
            }
        }
        protected override void OnDispose()
        {
            m_SelectionPool.Dispose();

            m_RenderSystem = null;
            m_InputSystem = null;
            m_EntityRaycastSystem = null;
            m_CoroutineSystem = null;
            m_NavMeshSystem = null;
            m_LevelDesignSystem = null;
            m_TurnTableSystem = null;
        }

        #region Binds

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;

            m_RenderSystem.OnRenderShapes += M_RenderSystem_OnRenderShapes;
        }
        private void Bind(EntityRaycastSystem other)
        {
            m_EntityRaycastSystem = other;
        }

        private ActionWrapper<UnityEngine.InputSystem.InputAction.CallbackContext> 
            m_LeftMouseButtonActionWrapper,
            m_RightMouseButtonActionWrapper;

        private void Bind(InputSystem other)
        {
            m_InputSystem = other;

            m_LeftMouseButtonAction = m_InputSystem.GetMouseButtonBinding(
                UnityEngine.InputSystem.LowLevel.MouseButton.Left,
                UnityEngine.InputSystem.InputActionType.Button);
            {
                m_LeftMouseButtonActionWrapper = ActionWrapper<UnityEngine.InputSystem.InputAction.CallbackContext>.GetWrapper();
                m_LeftMouseButtonActionWrapper.SetAction(M_LeftMouseButtonAction_performed);
                m_LeftMouseButtonActionWrapper.SetProfiler($"{nameof(TRPGSelectionSystem)}.M_LeftMouseButtonAction_performed");

                m_LeftMouseButtonAction.performed += m_LeftMouseButtonActionWrapper.Invoke;
            }
            
            m_RightMouseButtonAction = m_InputSystem.GetMouseButtonBinding(
                UnityEngine.InputSystem.LowLevel.MouseButton.Right,
                UnityEngine.InputSystem.InputActionType.Button
                );

            {
                m_RightMouseButtonActionWrapper = ActionWrapper<UnityEngine.InputSystem.InputAction.CallbackContext>.GetWrapper();
                m_RightMouseButtonActionWrapper.SetAction(M_RightMouseButtonAction_performed);
                m_RightMouseButtonActionWrapper.SetProfiler($"{nameof(TRPGSelectionSystem)}.M_RightMouseButtonAction_performed");

                m_RightMouseButtonAction.performed += m_RightMouseButtonActionWrapper.Invoke;
            }

            m_LeftMouseButtonAction.Enable();
            m_RightMouseButtonAction.Enable();
        }
        private void Bind(CoroutineSystem other)
        {
            m_CoroutineSystem = other;
        }
        private void Bind(NavMeshSystem other)
        {
            m_NavMeshSystem = other;
        }

        private void Bind(LevelDesignSystem other)
        {
            m_LevelDesignSystem = other;
        }

        private void Bind(TRPGTurnTableSystem other)
        {
            m_TurnTableSystem = other;
        }

        #endregion

        #region EventHandlers

        private void M_LeftMouseButtonAction_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (m_EntityRaycastSystem == null || m_RenderSystem == null) return;

            Ray ray = m_RenderSystem.ScreenPointToRay(new Unity.Mathematics.float3(m_InputSystem.MousePosition, 0));
            m_EntityRaycastSystem.Raycast(in ray, out RaycastInfo info);

            if (info.hit)
            {
                $"hit: {info.hit}".ToLog();

                SelectEntity(info.entity);
            }
            else
            {
                ClearSelectedEntities();
            }
        }
        private void M_RightMouseButtonAction_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (!m_TurnTableSystem.Enabled)
            {
                if (m_SelectedEntities.Count > 0)
                {
                    EntityMoveHandler();
                }

            }
            else
            {

            }
        }
        private void EntityMoveHandler()
        {
            Ray ray = m_InputSystem.CursorRay;

            m_LevelDesignSystem.Raycast(ray, out var hit);
            for (int i = 0; i < m_SelectedEntities.Count; i++)
            {
                //var move = m_SelectedEntities[i].GetComponent<TRPGActorMoveComponent>();
                m_NavMeshSystem.MoveTo(
                    m_SelectedEntities[i].Entity,
                    hit.point, 
                    new ActorMoveEvent<ActorPointMovePredicate>(
                        m_SelectedEntities[i].Entity.ToEntity<IEntityData>(), 
                        0,
                        new ActorPointMovePredicate()));

                m_SelectedEntities[i].ResetModifiers();
            }
        }

        private void M_RenderSystem_OnRenderShapes(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera cam)
        {
            Shapes.Draw.Push();

            Shapes.Draw.ZOffsetFactor = -1;
            for (int i = 0; i < m_SelectedEntities.Count; i++)
            {
                ProxyTransform tr = m_SelectedEntities[i].Entity.transform;

                DrawHeadCircle(m_SelectedEntities[i], in tr);

                DrawPathline(m_SelectedEntities[i], in tr);

                m_SelectedEntities[i].FadeModifier = math.lerp(m_SelectedEntities[i].FadeModifier, 1, CoreSystem.deltaTime * 8);
            }

            Shapes.Draw.Pop();
        }
        private void DrawHeadCircle(Selection selection, in ProxyTransform tr)
        {
            Entity<IEntity> entity = selection.Entity;

            AABB aabb = tr.aabb;
            float3 upperCenter = aabb.upperCenter + (aabb.extents.y * .5f);

            using (Shapes.Draw.StyleScope)
            {
                Shapes.Draw.DiscGeometry = Shapes.DiscGeometry.Billboard;

                Shapes.Draw.Arc(
                    upperCenter,
                    radius: .15f,
                    thickness: .03f,
                    angleRadStart: 0,
                    angleRadEnd: math.PI * 2);
            }
        }
        private void DrawPathline(Selection selection, in ProxyTransform tr)
        {
            Entity<IEntity> entity = selection.Entity;
            if (!entity.HasComponent<NavAgentComponent>()) return;

            NavAgentComponent nav = entity.GetComponentReadOnly<NavAgentComponent>();
            if (!nav.IsMoving) return;

            float3 dir = math.normalize(nav.Destination - tr.position);

            Shapes.Draw.Line(
                tr.position, 
                math.lerp(tr.position, nav.Destination - (dir * .25f), selection.FadeModifier));
            //for (int i = 1; i + 1 < nav.PathPoints.Length; i++)
            //{
            //    Shapes.Draw.Line(nav.PathPoints[i], nav.PathPoints[i + 1]);
            //}

            Shapes.Draw.Arc(
                nav.Destination, Vector3.up, 
                radius: .25f, 
                thickness: .03f,
                angleRadStart: 0, 
                angleRadEnd: math.lerp(0, math.PI * 2, selection.FadeModifier));
        }

        #endregion

        private void SelectEntity(Entity<IEntity> entity)
        {
            TRPGSelectionAttribute select = entity.GetAttribute<TRPGSelectionAttribute>();
            if (select == null) return;

            ClearSelectedEntities();
            TRPGSelectionComponent selectionComponent = entity.GetComponent<TRPGSelectionComponent>();

            if (selectionComponent.m_Holdable)
            {
                Selection selection = m_SelectionPool.Get();
                selection.Initialize(entity);

                m_SelectedEntities.Add(selection);

                Proxy.ProxyTransform tr = entity.transform;
                for (int i = 0; i < select.m_SelectedFloorUI.Length; i++)
                {
                    select.m_SelectedFloorUI[i].Fire(m_CoroutineSystem, tr);
                }
            }

            selectionComponent.m_OnSelect.Schedule(entity);

#if CORESYSTEM_SHAPES
            if (select.m_Shapes.EnableShapes)
            {
                var shapes = new ShapesComponent();
                
                //entity.AddComponent<ShapesComponent>();
                //ref ShapesComponent shapes = ref entity.GetComponent<ShapesComponent>();

                shapes.Apply(select.m_Shapes);

                var wr = ComponentType<ShapesComponent>.ECB.Begin();
                ComponentType<ShapesComponent>.ECB.Add(ref wr, entity.Idx, ref shapes);
                ComponentType<ShapesComponent>.ECB.End(ref wr);
            }
#endif
            $"select entity {entity.RawName}".ToLog();
        }
        public void ClearSelectedEntities()
        {
            for (int i = 0; i < m_SelectedEntities.Count; i++)
            {
                Selection selection = m_SelectedEntities[i];

                InternalRemoveSelection(selection);
            }

            m_SelectedEntities.Clear();
        }
        private void InternalRemoveSelection(Selection selection)
        {
            var entity = selection.Entity;

            var select = entity.GetAttribute<TRPGSelectionAttribute>();
            TRPGSelectionComponent selectionComponent = entity.GetComponent<TRPGSelectionComponent>();

            for (int j = 0; j < select.m_SelectedFloorUI.Length; j++)
            {
                select.m_SelectedFloorUI[j].Stop();
            }

            selectionComponent.m_OnDeselect.Schedule(entity);

#if CORESYSTEM_SHAPES
            if (select.m_Shapes.EnableShapes)
            {
                //entity.RemoveComponent<ShapesComponent>();

                var wr = ComponentType<ShapesComponent>.ECB.Begin();
                ComponentType<ShapesComponent>.ECB.Remove(ref wr, entity.Idx);
                ComponentType<ShapesComponent>.ECB.End(ref wr);
            }
#endif

            m_SelectionPool.Reserve(selection);
        }

        [BurstCompatible]
        private struct ActorPointMovePredicate : IExecutable<Entity<ActorEntity>>
        {
            [NotBurstCompatible]
            private bool IsTurnTableStarted()
            {
                return PresentationSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>.System.Enabled;
            }

            public bool Predicate(in Entity<ActorEntity> t)
            {
                if (IsTurnTableStarted()) return false;

                return true;
            }

            [Preserve]
            private static void AOTCodeGenerator()
            {
                ActorSystem.AOTCodeGenerator<ActorMoveEvent<ActorPointMovePredicate>>();
            }
        }

        private sealed class Selection
        {
            public Entity<IEntity> Entity;
            public float FadeModifier = 0;

            public void Initialize(Entity<IEntity> entity)
            {
                Entity = entity;
            }
            public void ResetModifiers()
            {
                FadeModifier = 0;
            }

            public static Selection Factory() => new Selection();
            public static void OnReserve(Selection other)
            {
                other.Entity = Entity<IEntity>.Empty;
                other.FadeModifier = 0;
            }
        }
    }
}