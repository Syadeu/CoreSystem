using Syadeu.Mono;
using Syadeu.Presentation.Render;
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Internal;

namespace Syadeu.Presentation.Map
{
    public sealed class LevelDesignSystem : PresentationSystemEntity<LevelDesignSystem>
    {
        private const string c_TerrainLayerName = "Terrain";
        public static readonly LayerMask TerrainLayer = LayerMask.GetMask(c_TerrainLayerName);

        public enum TerrainTool
        {
            None    =   0,
            
            Raise   =   1,
            Lower   =   2,
            Flatten =   3
        }

        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        private bool m_EnabledTerrainTool = false;
        private TerrainTool m_SelectedTool = TerrainTool.None;

        private SceneSystem m_SceneSystem;
        private RenderSystem m_RenderSystem;
        private MapSystem m_MapSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<SceneSystem>(Bind);
            RequestSystem<RenderSystem>(Bind);
            RequestSystem<MapSystem>(Bind);

            AddConsoleCommands();

            return base.OnInitialize();
        }

        #region Binds
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
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
        #endregion

        private void AddConsoleCommands()
        {
            ConsoleWindow.CreateCommand((cmd) =>
            {
                m_EnabledTerrainTool = true;
                m_SelectedTool = TerrainTool.Raise;

            }, "enable", "terrain", "raiseTool");
            ConsoleWindow.CreateCommand((cmd) =>
            {
                m_EnabledTerrainTool = false;
                m_SelectedTool = TerrainTool.None;

            }, "disable", "terrain");
        }

        protected override PresentationResult OnPresentation()
        {
            if (m_EnabledTerrainTool)
            {
                if (m_SelectedTool == TerrainTool.Raise)
                {
                    if (Mouse.current.press.isPressed)
                    {
                        Ray ray = m_RenderSystem.ScreenPointToRay(new float3(Mouse.current.position.ReadValue(), 0));
                        RaiseTerrain(ray, 10, .1f);
                    }
                }
            }

            return base.OnPresentation();
        }

        #region Tools

        private void ExecuteTool(TerrainTool tool, Ray ray, in int effectSize, in float effectIncrement = .1f)
        {
            switch (tool)
            {
                case TerrainTool.Raise:
                    RaiseTerrain(ray, in effectSize, in effectIncrement);
                    break;
                case TerrainTool.Lower:
                    LowerTerrain(ray, in effectSize, in effectIncrement);
                    break;
                case TerrainTool.Flatten:
                    FlattenTerrain(ray, in effectSize);
                    break;
            }
        }

        private void RaiseTerrain(Ray ray, in int effectSize, in float effectIncrement)
        {
            if (!GetTerrainLocation(ray, in effectSize, out Terrain terrain,
                out int terX, out int terZ))
            {
                return;
            }

            float[,] heights;
            try
            {
                heights = terrain.terrainData.GetHeights(terX, terZ, effectSize, effectSize);
            }
            catch (Exception)
            {
                return;
            }

            for (int xx = 0; xx < effectSize; xx++)
            {
                for (int yy = 0; yy < effectSize; yy++)
                {
                    heights[xx, yy] += (effectIncrement * Time.smoothDeltaTime);
                }
            }
            terrain.terrainData.SetHeights(terX, terZ, heights);
        }
        private void LowerTerrain(Ray ray, in int effectSize, in float effectIncrement)
        {
            if (!GetTerrainLocation(ray, in effectSize, out Terrain terrain,
            out int terX, out int terZ))
            {
                return;
            }

            float[,] heights;
            try
            {
                heights = terrain.terrainData.GetHeights(terX, terZ, effectSize, effectSize);
            }
            catch (Exception)
            {
                return;
            }

            for (int xx = 0; xx < effectSize; xx++)
            {
                for (int yy = 0; yy < effectSize; yy++)
                {
                    heights[xx, yy] -= (effectIncrement * Time.smoothDeltaTime);
                }
            }
            terrain.terrainData.SetHeights(terX, terZ, heights);
        }
        private void FlattenTerrain(Ray ray, in int effectSize)
        {
            if (!GetTerrainLocation(ray, 0, out Terrain terrain,
            out int terX, out int terZ))
            {
                return;
            }

            float[,] heights;
            try
            {
                heights = terrain.terrainData.GetHeights(terX, terZ, effectSize, effectSize);
            }
            catch (Exception)
            {
                return;
            }

            float sampledHeight = SampleHeight(ray);
            if (sampledHeight < 0) return;

            for (int xx = 0; xx < effectSize; xx++)
            {
                for (int yy = 0; yy < effectSize; yy++)
                {
                    if (heights[xx, yy] != sampledHeight)
                    {
                        heights[xx, yy] = sampledHeight;
                    }
                }
            }
            terrain.terrainData.SetHeights(terX, terZ, heights);
        }
        private float SampleHeight(Ray ray)
        {
            if (!GetTerrainLocation(ray, 0, out Terrain terrain,
            out int terX, out int terZ))
            {
                return -1;
            }

            float height;
            try
            {
                height = terrain.terrainData.GetHeight(terX, terZ);
            }
            catch (Exception)
            {
                return -1;
            }

            return Mathf.LerpUnclamped(0f, 1f, height / terrain.terrainData.size.y);
        }

        private bool GetTerrainLocation(Ray ray, in int effectSize, out Terrain terrain, 
            out int terX, out int terZ)
        {
            terX = 0; terZ = 0;
            if (!Raycast(ray, out var hit))
            {
                terrain = null;
                return false;
            }
            terrain = hit.transform.GetComponent<Terrain>();
            if (terrain == null)
            {
                return false;
            }

            "in".ToLog();
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

            int offset = effectSize / 2;

            terX = (int)locationInTerrain.x - offset;
            terZ = (int)locationInTerrain.z - offset;

            return true;
        }

        #endregion

        public bool Raycast(Ray ray, out RaycastHit hitInfo, 
            [DefaultValue("Mathf.Infinity")] float maxDistance = float.PositiveInfinity)
        {
            return m_SceneSystem.CurrentPhysicsScene.Raycast(ray.origin, ray.direction,
                out hitInfo,
                maxDistance: maxDistance,
                layerMask: TerrainLayer,
                queryTriggerInteraction: QueryTriggerInteraction.Collide);
        }
        public int RaycastAll(Ray ray, RaycastHit[] hitInfos, 
            [DefaultValue("Mathf.Infinity")] float maxDistance = float.PositiveInfinity)
        {
            return m_SceneSystem.CurrentPhysicsScene.Raycast(ray.origin, ray.direction,
                hitInfos,
                maxDistance: maxDistance,
                layerMask: TerrainLayer,
                queryTriggerInteraction: QueryTriggerInteraction.Collide);
        }
    }
}
