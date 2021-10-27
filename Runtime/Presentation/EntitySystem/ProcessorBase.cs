#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
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
        internal GameObjectProxySystem ProxySystem => EntitySystem.m_ProxySystem;

        [Obsolete]
        protected void RequestSystem<TSystem>(Action<TSystem> setter
#if DEBUG_MODE
            , [System.Runtime.CompilerServices.CallerFilePath] string methodName = ""
#endif
            )
            where TSystem : PresentationSystemEntity
        {
            PresentationManager.RegisterRequest<DefaultPresentationGroup, TSystem>(setter
#if DEBUG_MODE
                , methodName
#endif
                );
        }
        /// <summary>
        /// <see cref="DefaultPresentationGroup"/> 은 즉시 등록되지만 나머지 그룹에 한하여,
        /// <typeparamref name="TGroup"/> 이 시작될 때 등록됩니다.
        /// </summary>
        /// <typeparam name="TGroup">요청할 <typeparamref name="TSystem"/> 이 위치한 그룹입니다.</typeparam>
        /// <typeparam name="TSystem">요청할 시스템입니다.</typeparam>
        /// <param name="setter"></param>
        /// <param name="methodName"></param>
        protected void RequestSystem<TGroup, TSystem>(Action<TSystem> setter
#if DEBUG_MODE
            , [System.Runtime.CompilerServices.CallerFilePath] string methodName = ""
#endif
            )
            where TGroup : PresentationGroupEntity
            where TSystem : PresentationSystemEntity
        {
            PresentationManager.RegisterRequest<TGroup, TSystem>(setter
#if DEBUG_MODE
                , methodName
#endif
                );
        }

        protected EntityData<IEntityData> CreateObject(IFixedReference obj)
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
