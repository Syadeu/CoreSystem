// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using Unity.Burst;
using Unity.Jobs;

namespace Syadeu.Presentation.Actor
{
    public struct ActorControllerComponent : IEntityComponent, IDisposable
    {
        internal PresentationSystemID<EntitySystem> m_EntitySystem;

        internal Entity<ActorEntity> m_Parent;
        internal FixedInstanceList64<ActorProviderBase> m_InstanceProviders;
        internal FixedReferenceList64<ParamAction<IActorEvent>> m_OnEventReceived;

        public bool IsBusy()
        {
            ActorSystem system = PresentationSystem<DefaultPresentationGroup, ActorSystem>.System;
            if (!system.CurrentEventActor.IsEmpty())
            {
                if (system.CurrentEventActor.Idx.Equals(m_Parent.Idx))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasProvider<T>() where T : ActorProviderBase
        {
            if (TypeHelper.TypeOf<T>.IsAbstract)
            {
                for (int i = 0; i < m_InstanceProviders.Length; i++)
                {
                    if (TypeHelper.TypeOf<T>.Type.IsAssignableFrom(m_InstanceProviders[i].GetObject().GetType()))
                    {
                        return true;
                    }
                }

                return false;
            }

            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                if (m_InstanceProviders[i].GetObject() is T)
                {
                    return true;
                }
            }
            return false;
        }
        public Instance<T> GetProvider<T>() where T : ActorProviderBase
        {
            if (TypeHelper.TypeOf<T>.IsAbstract)
            {
                for (int i = 0; i < m_InstanceProviders.Length; i++)
                {
                    if (TypeHelper.TypeOf<T>.Type.IsAssignableFrom(m_InstanceProviders[i].GetObject().GetType()))
                    {
                        return m_InstanceProviders[i].Cast<ActorProviderBase, T>();
                    }
                }

                return Instance<T>.Empty;
            }

            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                if (m_InstanceProviders[i].GetObject() is T)
                {
                    return m_InstanceProviders[i].Cast<ActorProviderBase, T>();
                }
            }
            return Instance<T>.Empty;
        }

        void IDisposable.Dispose()
        {
            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                ExecuteOnDestroy(m_InstanceProviders[i].GetObject(), m_Parent);
                m_EntitySystem.System.DestroyObject(m_InstanceProviders[i]);
            }
        }
        private static void ExecuteOnDestroy(IActorProvider provider, Entity<ActorEntity> entity)
        {
            provider.OnDestroy(entity);
        }
    }
}
