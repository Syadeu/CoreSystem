#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Internal;
using Syadeu.Presentation.Events;
using System;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGIngameSystemGroup : PresentationGroupEntity
    {
        public override Type DependenceGroup => TypeHelper.TypeOf<DefaultPresentationGroup>.Type;

        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<TRPGPlayerSystem>.Type,
                TypeHelper.TypeOf<TRPGTurnTableSystem>.Type,
                TypeHelper.TypeOf<TRPGSelectionSystem>.Type,
                TypeHelper.TypeOf<TRPGGridSystem>.Type,
                TypeHelper.TypeOf<UI.TRPGCanvasUISystem>.Type
                );
        }
    }
    public sealed class TRPGAppCommonSystemGroup : PresentationGroupEntity
    {
        public override Type DependenceGroup => TypeHelper.TypeOf<DefaultPresentationGroup>.Type;

        public override void Register()
        {
            RegisterSystem(
                TypeHelper.TypeOf<TRPGAppIntiailzeSystem>.Type
                );
        }
    }

    public sealed class TRPGAppIntiailzeSystem : PresentationSystemEntity<TRPGAppIntiailzeSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private SceneSystem m_SceneSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_SceneSystem = null;
        }

        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;

            //m_SceneSystem.OnLoadingEnter
        }
    }
}