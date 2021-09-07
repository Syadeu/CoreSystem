using System;
using UnityEngine;

namespace Syadeu.Presentation.Map
{
    public sealed class LevelDesignSystem : PresentationSystemEntity<LevelDesignSystem>
    {
        public const string c_TerrainLayerName = "Terrain";

        public override bool EnableBeforePresentation => throw new NotImplementedException();
        public override bool EnableOnPresentation => throw new NotImplementedException();
        public override bool EnableAfterPresentation => throw new NotImplementedException();

        private SceneSystem m_SceneSystem;
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

        public void Test()
        {
        }

        public void RaycastTerrain()
        {
            //PhysicsScene physicsScene = new PhysicsScene();
            //physicsScene.
        }
    }
}
