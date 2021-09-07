using System;

namespace Syadeu.Presentation.Map
{
    public sealed class LevelDesignSystem : PresentationSystemEntity<LevelDesignSystem>
    {
        public override bool EnableBeforePresentation => throw new NotImplementedException();
        public override bool EnableOnPresentation => throw new NotImplementedException();
        public override bool EnableAfterPresentation => throw new NotImplementedException();

        private MapSystem m_MapSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<MapSystem>(Bind);

            return base.OnInitialize();
        }
        private void Bind(MapSystem other)
        {
            m_MapSystem = other;
        }


    }
}
