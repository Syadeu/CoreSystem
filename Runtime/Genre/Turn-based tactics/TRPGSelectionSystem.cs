#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Database;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Render;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGSelectionSystem : PresentationSystemEntity<TRPGSelectionSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        public override bool IsStartable => m_RenderSystem.CameraComponent != null;

        private NativeList<Entity<IEntity>> m_SelectedEntities;

        private UnityEngine.InputSystem.InputAction
            m_LeftMouseButtonAction,
            m_RightMouseButtonAction;

        private RenderSystem m_RenderSystem;
        private InputSystem m_InputSystem;
        private EntityRaycastSystem m_EntityRaycastSystem;
        private CoroutineSystem m_CoroutineSystem;

        // LevelDesignPresentationGroup
        private LevelDesignSystem m_LevelDesignSystem;

        private TRPGTurnTableSystem m_TurnTableSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EntityRaycastSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, CoroutineSystem>(Bind);
            RequestSystem<LevelDesignPresentationGroup, LevelDesignSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);

            m_SelectedEntities = new NativeList<Entity<IEntity>>(4, AllocatorManager.Persistent);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_SelectedEntities.Dispose();

            m_RenderSystem = null;
            m_InputSystem = null;
            m_EntityRaycastSystem = null;
            m_CoroutineSystem = null;
            m_LevelDesignSystem = null;
            m_TurnTableSystem = null;
        }

        #region Binds

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
        private void Bind(EntityRaycastSystem other)
        {
            m_EntityRaycastSystem = other;
        }
        private void Bind(InputSystem other)
        {
            m_InputSystem = other;

            m_LeftMouseButtonAction = m_InputSystem.GetMouseButtonBinding(
                UnityEngine.InputSystem.LowLevel.MouseButton.Left,
                UnityEngine.InputSystem.InputActionType.Button);

            m_LeftMouseButtonAction.performed += M_LeftMouseButtonAction_performed;

            m_RightMouseButtonAction = m_InputSystem.GetMouseButtonBinding(
                UnityEngine.InputSystem.LowLevel.MouseButton.Right,
                UnityEngine.InputSystem.InputActionType.Button
                );

            m_RightMouseButtonAction.performed += M_RightMouseButtonAction_performed;

            m_LeftMouseButtonAction.Enable();
            m_RightMouseButtonAction.Enable();
        }
        private void Bind(CoroutineSystem other)
        {
            m_CoroutineSystem = other;
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
                if (m_SelectedEntities.Length > 0)
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
            Ray ray = m_RenderSystem.ScreenPointToRay(new Unity.Mathematics.float3(m_InputSystem.MousePosition, 0));

            m_LevelDesignSystem.Raycast(ray, out var hit);
            for (int i = 0; i < m_SelectedEntities.Length; i++)
            {
                if (!m_SelectedEntities[i].HasComponent<TRPGActorMoveComponent>())
                {
                    "no move component".ToLog();
                    continue;
                }

                var move = m_SelectedEntities[i].GetComponent<TRPGActorMoveComponent>();
                move.MoveTo(
                    hit.point, 
                    new ActorMoveEvent<ActorPointMovePredicate>(
                        m_SelectedEntities[i].As<IEntity, IEntityData>(), 
                        0,
                        new ActorPointMovePredicate()));
            }
        }

        public void SelectEntity(Entity<IEntity> entity)
        {
            TRPGSelectionAttribute select = entity.GetAttribute<TRPGSelectionAttribute>();
            if (select == null) return;

            ClearSelectedEntities();
            m_SelectedEntities.Add(entity);

            var tr = entity.transform;
            AABB aabb = tr.aabb;
            float3 pos = aabb.center;
            pos.y -= aabb.extents.y;

            for (int i = 0; i < select.m_SelectedFloorUI.Length; i++)
            {
                select.m_SelectedFloorUI[i].Fire(m_CoroutineSystem.SystemID, tr);
            }

            $"select entity {entity.RawName}".ToLog();
        }
        public void DeSelectEntity(Entity<IEntity> entity)
        {
            var select = entity.GetAttribute<TRPGSelectionAttribute>();
            for (int i = 0; i < select.m_SelectedFloorUI.Length; i++)
            {
                select.m_SelectedFloorUI[i].Stop();
            }

            m_SelectedEntities.RemoveFor(entity);
        }
        public void ClearSelectedEntities()
        {
            for (int i = 0; i < m_SelectedEntities.Length; i++)
            {
                Entity<IEntity> entity = m_SelectedEntities[i];

                var select = entity.GetAttribute<TRPGSelectionAttribute>();
                for (int j = 0; j < select.m_SelectedFloorUI.Length; j++)
                {
                    select.m_SelectedFloorUI[j].Stop();
                }
            }

            m_SelectedEntities.Clear();
        }

        private struct ActorPointMovePredicate : IExecutable<Entity<ActorEntity>>
        {
            [BurstDiscard]
            private bool IsTurnTableStarted()
            {
                return PresentationSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>.System.Enabled;
            }

            public bool Predicate(in Entity<ActorEntity> t)
            {
                if (IsTurnTableStarted()) return false;

                return true;
            }
        }
    }
}