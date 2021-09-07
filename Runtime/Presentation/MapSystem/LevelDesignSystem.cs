using System;
using UnityEngine;
using UnityEngine.Internal;

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
            RequestSystem<SceneSystem>(Bind);
            RequestSystem<MapSystem>(Bind);

            return base.OnInitialize();
        }
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
        }
        private void Bind(MapSystem other)
        {
            m_MapSystem = other;
        }
        public override void OnDispose()
        {
            m_SceneSystem = null;
            m_MapSystem = null;
        }

        public void Test()
        {
        }

        public bool Raycast(Ray ray, out RaycastHit hitInfo, 
            [DefaultValue("Mathf.Infinity")] float maxDistance = float.PositiveInfinity)
        {
            return m_SceneSystem.CurrentPhysicsScene.Raycast(ray.origin, ray.direction, 
                out hitInfo,
                maxDistance: maxDistance,
                layerMask: LayerMask.NameToLayer(c_TerrainLayerName),
                queryTriggerInteraction: QueryTriggerInteraction.Ignore);
        }
        public int RaycastAll(Ray ray, RaycastHit[] hitInfos, 
            [DefaultValue("Mathf.Infinity")] float maxDistance = float.PositiveInfinity)
        {
            return m_SceneSystem.CurrentPhysicsScene.Raycast(ray.origin, ray.direction,
                hitInfos,
                maxDistance: maxDistance,
                layerMask: LayerMask.NameToLayer(c_TerrainLayerName),
                queryTriggerInteraction: QueryTriggerInteraction.Ignore);
        }
    }
}
