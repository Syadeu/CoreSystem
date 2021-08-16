using Syadeu.Presentation;
using Syadeu.Presentation.Map;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SyadeuEditor.Presentation.Map
{
    public sealed class SceneData : IDisposable
    {
        public SceneDataEntity SceneDataEntity;
        public readonly List<MapData> MapData = new List<MapData>();

        public SceneData(Transform folder, SceneDataEntity sceneDataEntity)
        {
            SceneDataEntity = sceneDataEntity;
            for (int i = 0; i < SceneDataEntity.MapData.Count; i++)
            {
                Reference<MapDataEntity> mapDataRef = SceneDataEntity.MapData[i];
                if (!mapDataRef.IsValid()) continue;

                MapDataEntity entity = mapDataRef.GetObject();
                MapData mapData = new MapData(folder, entity);

                MapData.Add(mapData);
            }
        }
        public void Dispose()
        {
            for (int i = 0; i < MapData.Count; i++)
            {
                MapData[i].Dispose();
            }
            MapData.Clear();
            SceneDataEntity = null;
        }
    }
}
