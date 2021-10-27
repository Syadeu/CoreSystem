using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using Syadeu.ThreadSafe;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [DisplayName("MapData: Map Data Entity")]
    [EntityAcceptOnly(typeof(MapDataAttributeBase))]
    public sealed class MapDataEntity : MapDataEntityBase
    {
        [JsonIgnore] public Entity<EntityBase>[] CreatedEntities { get; internal set; }
        [JsonIgnore] public ProxyTransform[] CreatedRawObjects { get; internal set; }
        [JsonIgnore] public bool DestroyChildOnDestroy { get; set; } = true;

        public override bool IsValid() => true;
        protected override ObjectBase Copy()
        {
            MapDataEntity clone = (MapDataEntity)base.Copy();
            Object[] temp = new Object[m_Objects.Length];
            for (int i = 0; i < temp.Length; i++)
            {
                temp[i] = (Object)m_Objects[i].Clone();
            }
            clone.m_Objects = temp;
            clone.CreatedEntities = null;
            clone.DestroyChildOnDestroy = true;

            return clone;
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<MapDataEntity>>();
            AotHelper.EnsureList<Reference<MapDataEntity>>();
            AotHelper.EnsureType<EntityData<MapDataEntity>>();
            AotHelper.EnsureList<EntityData<MapDataEntity>>();
            AotHelper.EnsureType<MapDataEntity>();
            AotHelper.EnsureList<MapDataEntity>();
        }
    }
    public sealed class MapDataProcessor : EntityDataProcessor<MapDataEntity>
    {
        protected override void OnCreated(MapDataEntity entity)
        {
            entity.CreatedEntities = new Entity<EntityBase>[entity.m_Objects.Length];
            for (int i = 0; i < entity.m_Objects.Length; i++)
            {
                if (entity.m_Objects[i].m_Object.IsEmpty() || 
                    !entity.m_Objects[i].m_Object.IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Cannot spawn map object in [{entity.Name}] element at {i} is not valid.");

                    entity.CreatedEntities[i] = Entity<EntityBase>.Empty;
                    continue;
                }

                entity.CreatedEntities[i] = CreateEntity(entity.m_Objects[i].m_Object, entity.m_Objects[i].m_Translation, entity.m_Objects[i].m_Rotation, entity.m_Objects[i].m_Scale);
            }

            entity.CreatedRawObjects = new ProxyTransform[entity.m_RawObjects.Length];
            for (int i = 0; i < entity.m_RawObjects.Length; i++)
            {
                if (entity.m_RawObjects[i].m_Object.IsNone() ||
                    !entity.m_RawObjects[i].m_Object.IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Cannot spawn map object in [{entity.Name}] element at {i} raw object is not valid.");

                    continue;
                }

                entity.CreatedRawObjects[i] = ProxySystem.CreateNewPrefab(entity.m_RawObjects[i].m_Object, entity.m_RawObjects[i].m_Translation, entity.m_RawObjects[i].m_Rotation, entity.m_RawObjects[i].m_Scale, true,
                    entity.m_RawObjects[i].m_Center, entity.m_RawObjects[i].m_Size, true);
            }
        }
        protected override void OnDestroy(MapDataEntity entity)
        {
            if (entity == null || !entity.DestroyChildOnDestroy) return;
            for (int i = 0; i < entity.CreatedEntities.Length; i++)
            {
                if (entity.CreatedEntities[i].IsValid())
                {
                    entity.CreatedEntities[i].Destroy();
                }
            }
            entity.CreatedEntities = null;

            for (int i = 0; i < entity.CreatedRawObjects.Length; i++)
            {
                if (!entity.CreatedRawObjects[i].isDestroyed)
                {
                    entity.CreatedRawObjects[i].Destroy();
                }
            }
        }
    }
}
