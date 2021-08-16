using Syadeu.Database;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace SyadeuEditor.Presentation.Map
{
    public sealed class MapData : IDisposable
    {
        public MapDataEntity MapDataEntity;
        private readonly Dictionary<MapDataEntity.Object, MapObject> Objects = new Dictionary<MapDataEntity.Object, MapObject>();
        private readonly Dictionary<GameObject, MapObject> Mapper = new Dictionary<GameObject, MapObject>();

        public MapData(Transform folder, MapDataEntity mapDataEntity)
        {
            MapDataEntity = mapDataEntity;

            for (int i = 0; i < MapDataEntity.m_Objects.Length; i++)
            {
                MapDataEntity.Object mapDataObj = MapDataEntity.m_Objects[i];
                MapObject data = new MapObject(this, folder, mapDataObj);

                Objects.Add(mapDataObj, data);
                Mapper.Add(data.GameObject, data);
            }
        }

        public MapObject GetData(GameObject obj)
        {
            if (obj == null) return null;
            if (Mapper.TryGetValue(obj, out MapObject value)) return value;
            return null;
        }
        public MapObject Add(Reference<EntityBase> entity, Transform folder, float3 pos)
        {
            var objData = new MapDataEntity.Object()
            {
                m_Object = entity,
                m_Translation = pos,
                m_Rotation = quaternion.identity,
                m_Scale = 1
            };

            MapObject data = new MapObject(this, folder, objData);

            Objects.Add(data.Data, data);
            Mapper.Add(data.GameObject, data);

            MapDataEntity.m_Objects = Objects.Keys.ToArray();

            return data;
        }
        public void Destroy(MapObject obj, bool withoutSave)
        {
            Objects.Remove(obj.Data);
            Mapper.Remove(obj.GameObject);

            UnityEngine.Object.DestroyImmediate(obj.GameObject);

            if (!withoutSave) MapDataEntity.m_Objects = Objects.Keys.ToArray();
        }

        public void Dispose()
        {
            MapObject[] temp = Objects.Values.ToArray();
            for (int i = 0; i < temp.Length; i++)
            {
                temp[i].Destroy(true);
            }

            EntityDataList.Instance.SaveData(MapDataEntity);

            Objects.Clear();
            Mapper.Clear();

            MapDataEntity = null;
        }
    }
}
