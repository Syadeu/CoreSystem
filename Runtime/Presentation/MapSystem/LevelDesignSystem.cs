using System;
using Unity.Mathematics;
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

        private void RaiseTerrain(Ray ray, in int effectSize, in float effectIncrement)
        {
            if (!Raycast(ray, out var hit)) return;
            Terrain terrain = hit.transform.GetComponent<Terrain>();

            float3
                tempCoord = (hit.point - terrain.GetPosition()),
                coord = new float3(
                    tempCoord.x / terrain.terrainData.size.x,
                    tempCoord.y / terrain.terrainData.size.y,
                    tempCoord.z / terrain.terrainData.size.z
                    ),
                locationInTerrain = new float3(
                    coord.x * terrain.terrainData.heightmapResolution,
                    0,
                    coord.z * terrain.terrainData.heightmapResolution
                    );

            int 
                offset = effectSize / 2,
                terX = (int)locationInTerrain.x - offset,
                terZ = (int)locationInTerrain.z - offset;

            float[,] heights = terrain.terrainData.GetHeights(terX, terZ, effectSize, effectSize);
            for (int xx = 0; xx < effectSize; xx++)
            {
                for (int yy = 0; yy < effectSize; yy++)
                {
                    heights[xx, yy] += (effectIncrement * Time.smoothDeltaTime);
                }
            }

            terrain.terrainData.SetHeights(terX, terZ, heights);
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
