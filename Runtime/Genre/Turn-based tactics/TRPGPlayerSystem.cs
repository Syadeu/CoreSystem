#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Render;
using Syadeu.Presentation.TurnTable.UI;
using System.Collections;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    [SubSystem(typeof(DefaultPresentationGroup), typeof(RenderSystem))]
    public sealed class TRPGPlayerSystem : PresentationSystemEntity<TRPGPlayerSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        public override bool IsStartable => m_RenderSystem.CameraComponent != null;

        private ShortcutType m_CurrentShortcut = ShortcutType.None;
        private GridPath64 m_LastPath;

        private NativeList<Entity<IEntity>> m_SelectedEntities;

        private UnityEngine.InputSystem.InputAction
            m_LeftMouseButtonAction,
            m_RightMouseButtonAction;

        private RenderSystem m_RenderSystem;
        private CoroutineSystem m_CoroutineSystem;
        private NavMeshSystem m_NavMeshSystem;
        private EventSystem m_EventSystem;
        private InputSystem m_InputSystem;
        private EntityRaycastSystem m_EntityRaycastSystem;

        private TRPGTurnTableSystem m_TurnTableSystem;
        private TRPGCameraMovement m_TRPGCameraMovement;
        private TRPGGridSystem m_TRPGGridSystem;
        private TRPGCanvasUISystem m_TRPGCanvasUISystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, CoroutineSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, NavMeshSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EntityRaycastSystem>(Bind);
            RequestSystem<TRPGSystemGroup, TRPGTurnTableSystem>(Bind);
            RequestSystem<TRPGSystemGroup, TRPGGridSystem>(Bind);
            RequestSystem<TRPGSystemGroup, TRPGCanvasUISystem>(Bind);

            m_SelectedEntities = new NativeList<Entity<IEntity>>(4, AllocatorManager.Persistent);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_EventSystem.RemoveEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
            m_EventSystem.RemoveEvent<TRPGGridCellUIPressedEvent>(TRPGGridCellUIPressedEventHandler);
            m_EventSystem.RemoveEvent<TRPGEndTurnUIPressedEvent>(TRPGEndTurnUIPressedEventHandler);
            m_EventSystem.RemoveEvent<TRPGEndTurnEvent>(TRPGEndTurnEventHandler);
            m_EventSystem.RemoveEvent<OnTurnStateChangedEvent>(OnTurnStateChangedEventHandler);
            m_EventSystem.RemoveEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);

            m_SelectedEntities.Dispose();

            m_RenderSystem = null;
            m_CoroutineSystem = null;
            m_NavMeshSystem = null;
            m_EventSystem = null;
            m_InputSystem = null;
            m_EntityRaycastSystem = null;

            m_TurnTableSystem = null;
            m_TRPGCameraMovement = null;
            m_TRPGGridSystem = null;
            m_TRPGCanvasUISystem = null;
        }

        #region Binds

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
        private void Bind(CoroutineSystem other)
        {
            m_CoroutineSystem = other;
        }
        private void Bind(NavMeshSystem other)
        {
            m_NavMeshSystem = other;
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
            m_EventSystem.AddEvent<TRPGGridCellUIPressedEvent>(TRPGGridCellUIPressedEventHandler);
            m_EventSystem.AddEvent<TRPGEndTurnUIPressedEvent>(TRPGEndTurnUIPressedEventHandler);
            m_EventSystem.AddEvent<TRPGEndTurnEvent>(TRPGEndTurnEventHandler);
            m_EventSystem.AddEvent<OnTurnStateChangedEvent>(OnTurnStateChangedEventHandler);
            m_EventSystem.AddEvent<OnTurnTableStateChangedEvent>(OnTurnTableStateChangedEventHandler);
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
            
        }

        private void Bind(EntityRaycastSystem other)
        {
            m_EntityRaycastSystem = other;
        }

        private void Bind(TRPGTurnTableSystem other)
        {
            m_TurnTableSystem = other;
        }
        private void Bind(TRPGGridSystem other)
        {
            m_TRPGGridSystem = other;
        }
        private void Bind(TRPGCanvasUISystem other)
        {
            m_TRPGCanvasUISystem = other;
        }

        #endregion

        protected override PresentationResult OnStartPresentation()
        {
            m_TRPGCameraMovement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

            m_TRPGCanvasUISystem.SetPlayerUI(false);

            return base.OnStartPresentation();
        }

        private void DisableCurrentShortcut()
        {
            switch (m_CurrentShortcut)
            {
                default:
                case ShortcutType.None:
                case ShortcutType.Move:
                    m_TRPGGridSystem.ClearUICell();
                    m_TRPGGridSystem.ClearUIPath();

                    break;
                case ShortcutType.Attack:
                    m_TRPGCameraMovement.SetNormal();
                    m_TRPGCanvasUISystem.SetFire(true);
                    break;
            }

            m_CurrentShortcut = ShortcutType.None;
        }

        #region Event Handlers

        private void TRPGShortcutUIPressedEventHandler(TRPGShortcutUIPressedEvent ev)
        {
            if (ev.Shortcut == m_CurrentShortcut)
            {
                "same return".ToLog();
                DisableCurrentShortcut();
                return;
            }
            else if (!m_TurnTableSystem.CurrentTurn.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({m_TurnTableSystem.CurrentTurn.RawName}) doesn\'t have {nameof(ActorControllerComponent)}.");
                return;
            }

            ActorControllerComponent ctr = m_TurnTableSystem.CurrentTurn.GetComponent<ActorControllerComponent>();
            if (ctr.IsBusy())
            {
                "busy out".ToLog();
                return;
            }

            DisableCurrentShortcut();

            switch (ev.Shortcut)
            {
                default:
                case ShortcutType.None:
                case ShortcutType.Move:
                    m_TRPGCameraMovement.SetNormal();

                    m_TRPGGridSystem.DrawUICell(m_TurnTableSystem.CurrentTurn);
                    m_CurrentShortcut = ShortcutType.Move;

                    break;
                case ShortcutType.Attack:
                    if (!ctr.HasProvider<TRPGActorAttackProvider>())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            $"Entity({m_TurnTableSystem.CurrentTurn.RawName}) doesn\'t have {nameof(TRPGActorAttackProvider)}.");

                        return;
                    }

                    m_TRPGCanvasUISystem.SetFire(false);

                    Instance<TRPGActorAttackProvider> attProvider = ctr.GetProvider<TRPGActorAttackProvider>();
                    var targets = attProvider.Object.GetTargetsInRange();
                    var tr = m_TurnTableSystem.CurrentTurn.As<IEntityData, IEntity>().transform;

                    $"{targets.Count} found".ToLog();
                    for (int i = 0; i < targets.Count; i++)
                    {
                        $"{targets[i].Name} found".ToLog();
                        m_TRPGCameraMovement.SetAim(tr, targets[i].transform);
                    }

                    m_CurrentShortcut = ShortcutType.Attack;

                    break;
            }
        }
        private void TRPGGridCellUIPressedEventHandler(TRPGGridCellUIPressedEvent ev)
        {
            DisableCurrentShortcut();
            m_CurrentShortcut = ShortcutType.None;

            MoveToCell(m_TurnTableSystem.CurrentTurn, ev.Position);
            //var move = m_TurnTableSystem.CurrentTurn.GetComponent<TRPGActorMoveComponent>();
            //move.movet
        }
        private void TRPGEndTurnUIPressedEventHandler(TRPGEndTurnUIPressedEvent ev)
        {
            DisableCurrentShortcut();

            m_TRPGCanvasUISystem.SetPlayerUI(false);

            m_EventSystem.ScheduleEvent(TRPGEndTurnEvent.GetEvent());
        }
        private void TRPGEndTurnEventHandler(TRPGEndTurnEvent ev)
        {
            m_TurnTableSystem.NextTurn();
        }
        private void OnTurnStateChangedEventHandler(OnTurnStateChangedEvent ev)
        {
            ActorFactionComponent faction = ev.Entity.GetComponent<ActorFactionComponent>();
            if (faction.FactionType != FactionType.Player || ev.State != OnTurnStateChangedEvent.TurnState.Start) return;

            m_TRPGCanvasUISystem.SetPlayerUI(true);
        }
        private void OnTurnTableStateChangedEventHandler(OnTurnTableStateChangedEvent ev)
        {
            if (!ev.Enabled)
            {
                m_TRPGCanvasUISystem.SetPlayerUI(false);
            }
        }

        #endregion

        public void SelectEntity(Entity<IEntity> entity)
        {
            ClearSelectedEntities();
            m_SelectedEntities.Add(entity);

            $"select entity {entity.RawName}".ToLog();
        }
        public void DeSelectEntity(Entity<IEntity> entity)
        {
            m_SelectedEntities.RemoveFor(entity);
        }
        public void ClearSelectedEntities()
        {
            m_SelectedEntities.Clear();
        }

        public void MoveToCell(EntityData<IEntityData> entity, GridPosition position)
        {
            if (!entity.HasComponent<TRPGActorMoveComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have {nameof(TRPGActorMoveComponent)}." +
                    $"Maybe didn\'t added {nameof(TRPGActorMoveProvider)} in {nameof(ActorControllerAttribute)}?");
                return;
            }
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have {nameof(NavAgentAttribute)} attribute.");
                return;
            }

            TRPGActorMoveComponent move = entity.GetComponent<TRPGActorMoveComponent>();
            if (!move.GetPath(in position, ref m_LastPath))
            {
                "path error not found".ToLogError();
                return;
            }

            m_NavMeshSystem.MoveTo(entity.As<IEntityData, IEntity>(),
                m_LastPath, new ActorMoveEvent(entity, 1));

            ref TurnPlayerComponent turnPlayer = ref entity.GetComponent<TurnPlayerComponent>();
            int requireAp = m_LastPath.Length;

            turnPlayer.ActionPoint -= requireAp;
        }
    }
}