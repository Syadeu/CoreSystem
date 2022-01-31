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
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation.Components
{
    internal sealed class EntityComponentDebugModule : PresentationSystemModule<EntityComponentSystem>
    {
#if DEBUG_MODE
        private const string
            c_ComponentIsNotFullDisposed
                = "Entity({0}) has number of {1} components that didn\'t disposed. {2}",
            c_ComponentFullDiposed
                = "Entity({0}) component all checked.";

        private Dictionary<InstanceID, List<Type>> m_AddedComponents;

        private EntitySystem m_EntitySystem;

        protected override void OnInitialize()
        {
            m_AddedComponents = new Dictionary<InstanceID, List<Type>>();
            PoolContainer<ComponentAtomicSafetyHandler>.Initialize(SafetyFactory, 32);

            System.OnComponentAdded += System_OnComponentAdded;
            System.OnComponentRemove += System_OnComponentRemove;

            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);
        }
        private ComponentAtomicSafetyHandler SafetyFactory()
        {
            return new ComponentAtomicSafetyHandler(this);
        }

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
            m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
        }
        protected override void OnShutDown()
        {
            m_EntitySystem.OnEntityDestroy -= M_EntitySystem_OnEntityDestroy;
        }
        protected override void OnDispose()
        {
            System.OnComponentAdded -= System_OnComponentAdded;
            System.OnComponentRemove -= System_OnComponentRemove;

            m_EntitySystem = null;
        }

        #region EventHandlers

        private void System_OnComponentAdded(InstanceID arg1, Type arg2)
        {
            if (!m_AddedComponents.TryGetValue(arg1, out var list))
            {
                list = new List<Type>();
                m_AddedComponents.Add(arg1, list);
            }

            if (list.Contains(arg2)) return;

            list.Add(arg2);
        }
        private void System_OnComponentRemove(InstanceID arg1, Type arg2)
        {
            if (!m_AddedComponents.TryGetValue(arg1, out var list))
            {
                return;
            }

            if (list.Count == 1)
            {
                m_AddedComponents.Remove(arg1);
            }
            else list.Remove(arg2);
        }
        private void M_EntitySystem_OnEntityDestroy(IObject obj)
        {
            if (!m_AddedComponents.ContainsKey(obj.Idx)) return;

            var handler = PoolContainer<ComponentAtomicSafetyHandler>.Dequeue();
            handler.Initialize(obj);
            CoreSystem.WaitInvokeBackground(1, handler.CheckAllComponentIsDisposed);
        }

        #endregion

        private sealed class ComponentAtomicSafetyHandler
        {
            private EntityComponentDebugModule m_DebugModule;

            private string m_Name;
            private InstanceID m_InstanceID;

            public ComponentAtomicSafetyHandler(EntityComponentDebugModule debugModule)
            {
                m_DebugModule = debugModule;
            }

            public void Initialize(IObject obj)
            {
                m_Name = obj.Name;
                m_InstanceID = obj.Idx;
            }

            public void CheckAllComponentIsDisposed()
            {
                if (Debug_HasComponent(m_InstanceID, out int count, out string names))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        string.Format(c_ComponentIsNotFullDisposed, m_Name, count, names));
                }
                else
                {
                    CoreSystem.Logger.Log(Channel.Entity,
                        string.Format(c_ComponentFullDiposed, m_Name));
                }

                m_Name = null;
                PoolContainer<ComponentAtomicSafetyHandler>.Enqueue(this);
            }

            private bool Debug_HasComponent(InstanceID entity, out int count, out string names)
            {
                if (m_DebugModule.m_AddedComponents.TryGetValue(entity, out var list))
                {
                    count = list.Count;
                    names = list[0].Name;
                    for (int i = 1; i < list.Count; i++)
                    {
                        names += $", {list[i].Name}";
                    }

                    return true;
                }

                count = 0;
                names = string.Empty;
                return false;
            }
        }
#endif
    }
}
