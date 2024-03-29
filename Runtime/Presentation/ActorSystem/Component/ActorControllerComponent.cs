﻿// Copyright 2021 Seung Ha Kim
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
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public struct ActorControllerComponent : IEntityComponent
    {
        internal bool m_IsExecutingEvent;
        internal TypeInfo m_LastExecuteEventName;

        internal Entity<ActorEntity> m_Parent;
        internal FixedInstanceList64<IActorProvider> m_InstanceProviders;
        internal FixedReferenceList64<ParamAction<IActorEvent>> m_OnEventReceived;

        public bool IsBusy(out TypeInfo executingEvent)
        {
            executingEvent = m_LastExecuteEventName;

            return m_IsExecutingEvent;
        }

        public bool HasProvider<TProvider>() where TProvider : class, IActorProvider
        {
            if (TypeHelper.TypeOf<TProvider>.IsAbstract)
            {
                for (int i = 0; i < m_InstanceProviders.Length; i++)
                {
                    if (TypeHelper.TypeOf<TProvider>.Type.IsAssignableFrom(m_InstanceProviders[i].GetObject().GetType()))
                    {
                        return true;
                    }
                }

                return false;
            }

            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                if (m_InstanceProviders[i].GetObject() is TProvider)
                {
                    return true;
                }
            }
            return false;
        }
        public Entity<TProvider> GetProvider<TProvider>() where TProvider : class, IActorProvider
        {
            if (TypeHelper.TypeOf<TProvider>.IsAbstract)
            {
                for (int i = 0; i < m_InstanceProviders.Length; i++)
                {
                    if (TypeHelper.TypeOf<TProvider>.Type.IsAssignableFrom(m_InstanceProviders[i].GetObject().GetType()))
                    {
                        return m_InstanceProviders[i].GetEntity<TProvider>();
                    }
                }

                return Entity<TProvider>.Empty;
            }

            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                if (m_InstanceProviders[i].GetObject() is TProvider)
                {
                    return m_InstanceProviders[i].GetEntity<TProvider>();
                }
            }
            return Entity<TProvider>.Empty;
        }
    }
}
