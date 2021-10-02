using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Render;
using Syadeu.Presentation.TurnTable.UI;
using System.Collections;

namespace Syadeu.Presentation.TurnTable
{
    [SubSystem(typeof(RenderSystem))]
    public sealed class TRPGPlayerSystem : PresentationSystemEntity<TRPGPlayerSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        public override bool IsStartable => m_RenderSystem.CameraComponent != null;

        private ShortcutType m_CurrentShortcut = ShortcutType.None;
        private GridPath32 m_LastPath;

        private RenderSystem m_RenderSystem;
        private CoroutineSystem m_CoroutineSystem;

        private EventSystem m_EventSystem;
        private TRPGTurnTableSystem m_TurnTableSystem;
        private TRPGCameraMovement m_TRPGCameraMovement;
        private TRPGGridSystem m_TRPGGridSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, CoroutineSystem>(Bind);
            RequestSystem<TRPGSystemGroup, EventSystem>(Bind);
            RequestSystem<TRPGSystemGroup, TRPGTurnTableSystem>(Bind);
            RequestSystem<TRPGSystemGroup, TRPGGridSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_EventSystem.RemoveEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
            m_EventSystem.RemoveEvent<TRPGGridCellUIPressedEvent>(TRPGGridCellUIPressedEventHandler);

            m_RenderSystem = null;
            m_EventSystem = null;
            m_TurnTableSystem = null;
            m_TRPGCameraMovement = null;
            m_TRPGGridSystem = null;
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
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
            m_EventSystem.AddEvent<TRPGGridCellUIPressedEvent>(TRPGGridCellUIPressedEventHandler);
        }
        private void Bind(TRPGTurnTableSystem other)
        {
            m_TurnTableSystem = other;
        }
        private void Bind(TRPGGridSystem other)
        {
            m_TRPGGridSystem = other;
        }

        #endregion

        protected override PresentationResult OnStartPresentation()
        {
            m_TRPGCameraMovement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

            return base.OnStartPresentation();
        }

        private void TRPGShortcutUIPressedEventHandler(TRPGShortcutUIPressedEvent ev)
        {
            "ev shortcut".ToLog();

            if (m_CurrentShortcut != ev.Shortcut)
            {
                switch (m_CurrentShortcut)
                {
                    default:
                    case ShortcutType.None:
                    case ShortcutType.Move:
                        m_TRPGGridSystem.ClearUICell();

                        break;
                    case ShortcutType.Attack:
                        break;
                }
            }

            switch (ev.Shortcut)
            {
                default:
                case ShortcutType.None:
                case ShortcutType.Move:
                    if (m_CurrentShortcut == ShortcutType.Move)
                    {
                        m_TRPGGridSystem.ClearUICell();

                        m_CurrentShortcut = ShortcutType.None;
                        return;
                    }

                    NavAgentAttribute navAgent = m_TurnTableSystem.CurrentTurn.GetAttribute<NavAgentAttribute>();
                    if (navAgent == null)
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            $"Entity({m_TurnTableSystem.CurrentTurn.Name}) doesn\'t have {nameof(NavAgentAttribute)} attribute.");
                        return;
                    }
                    else if (navAgent.IsMoving)
                    {
                        return;
                    }

                    m_TRPGCameraMovement.SetNormal();

                    m_TRPGGridSystem.DrawUICell(m_TurnTableSystem.CurrentTurn);
                    m_CurrentShortcut = ShortcutType.Move;

                    break;
                case ShortcutType.Attack:
                    if (m_CurrentShortcut == ShortcutType.Attack)
                    {
                        m_TRPGCameraMovement.SetNormal();

                        m_CurrentShortcut = ShortcutType.None;
                        return;
                    }

                    Instance<TRPGActorAttackProvider> attProvider = m_TurnTableSystem.CurrentTurn.GetComponent<ActorControllerComponent>().GetProvider<TRPGActorAttackProvider>();
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
            m_TRPGGridSystem.ClearUICell();

            MoveToCell(m_TurnTableSystem.CurrentTurn, ev.Position);
            //var move = m_TurnTableSystem.CurrentTurn.GetComponent<TRPGActorMoveComponent>();
            //move.movet
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

            // TODO : 타일대로 이동안하니 나중에 수정할 것
            navAgent.MoveTo(move.TileToPosition(m_LastPath[m_LastPath.Length - 1]));

            ref TurnPlayerComponent turnPlayer = ref entity.GetComponent<TurnPlayerComponent>();
            int requireAp = m_LastPath.Length;

            turnPlayer.ActionPoint -= requireAp;
        }
    }
}