﻿using Syadeu.Database;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Internal;
using Syadeu.Presentation.Proxy;
using System;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    /// <summary>
    /// 직접 상속은 허용하지 않습니다.
    /// </summary>
    [RequireDerived]
    public abstract class ProcessorBase
    {
        internal EntitySystem m_EntitySystem;

        protected EntitySystem EntitySystem => m_EntitySystem;
        protected EventSystem EventSystem
        {
            get
            {
                if (m_EntitySystem.m_EventSystem == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                        $"{nameof(EventSystem)} is not initialized yet. Did you called from OnInitializeAsync?");
                }

                return m_EntitySystem.m_EventSystem;
            }
        }
        protected DataContainerSystem DataContainerSystem
        {
            get
            {
                if (m_EntitySystem.m_DataContainerSystem == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                        $"{nameof(DataContainerSystem)} is not initialized yet. Did you called from OnInitializeAsync?");
                }

                return m_EntitySystem.m_DataContainerSystem;
            }
        }
        private GameObjectProxySystem ProxySystem => EntitySystem.m_ProxySystem;

        [Obsolete]
        protected void RequestSystem<TSystem>(Action<TSystem> setter) where TSystem : PresentationSystemEntity
        {
            PresentationManager.RegisterRequest<DefaultPresentationGroup, TSystem>(setter);
        }
        protected void RequestSystem<TGroup, TSystem>(Action<TSystem> setter)
            where TGroup : PresentationGroupEntity
            where TSystem : PresentationSystemEntity
        {
            PresentationManager.RegisterRequest<TGroup, TSystem>(setter);
        }

        protected EntityData<IEntityData> CreateObject(IReference obj)
        {
            CoreSystem.Logger.NotNull(obj, "Target object cannot be null");
            return EntitySystem.CreateObject(obj.Hash);
        }

        protected Entity<T> CreateEntity<T>(Reference<T> entity, float3 position, quaternion rotation) where T : EntityBase
            => CreateEntity(entity, position, rotation, 1);
        protected Entity<T> CreateEntity<T>(Reference<T> entity, float3 position, quaternion rotation, float3 localSize) where T : EntityBase
        {
            CoreSystem.Logger.NotNull(entity, "Target entity cannot be null");

            Entity<IEntity> target = EntitySystem.CreateEntity(entity, position, rotation, localSize);
            if (!target.IsValid()) return Entity<T>.Empty;

            return target.As<IEntity, T>();
        }
    }
}
