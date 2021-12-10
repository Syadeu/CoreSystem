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
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

using TypeInfo = Syadeu.Collections.TypeInfo;

namespace Syadeu.Presentation.Components
{
    internal sealed class EntityNotifiedComponentModule : PresentationSystemModule<EntityComponentSystem>
    {
        private NativeHashSet<Hash> m_ZeroNotifiedObjects;
        private NativeMultiHashMap<Hash, TypeInfo> m_NotifiedObjects;

        private EntitySystem m_EntitySystem;

        protected override void OnInitialize()
        {
            m_ZeroNotifiedObjects = new NativeHashSet<Hash>(EntityDataList.Instance.m_Objects.Count,
                 AllocatorManager.Persistent);
            m_NotifiedObjects = new NativeMultiHashMap<Hash, TypeInfo>(System.BufferLength, AllocatorManager.Persistent);

            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_ZeroNotifiedObjects.Dispose();
            m_NotifiedObjects.Dispose();

            m_EntitySystem = null;
        }
        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
        }

        public void TryAddComponent(IObject rawObject, Action<InstanceID, Type> onAdd)
        {
            Hash rawID = rawObject.Hash;
            InstanceID instanceID = (rawObject is AttributeBase att) ? att.ParentEntity.Idx : rawObject.Idx;

            if (m_ZeroNotifiedObjects.Contains(rawID)) return;
            else if (m_NotifiedObjects.TryGetFirstValue(rawID, out TypeInfo typeInfo, out var parsedIter))
            {
                do
                {
                    System.AddComponent(instanceID, typeInfo);
                    onAdd?.Invoke(instanceID, typeInfo.Type);

                } while (m_NotifiedObjects.TryGetNextValue(out typeInfo, ref parsedIter));

                return;
            }

            var iter = Select(rawObject.GetType());
            if (!iter.Any())
            {
                m_ZeroNotifiedObjects.Add(rawID);
                return;
            }

            var select = iter.Select(i => i.GenericTypeArguments[0]);
            foreach (var componentType in select)
            {
                System.AddComponent(instanceID, componentType);
                onAdd?.Invoke(instanceID, componentType);

                m_NotifiedObjects.Add(rawID, TypeStatic.GetValue(componentType).Data);
            }
        }
        public void TryRemoveComponent(IObject rawObject, Action<InstanceID, Type> onRemove)
        {
            Hash rawID = rawObject.Hash;
            InstanceID instanceID = (rawObject is AttributeBase att) ? att.ParentEntity.Idx : rawObject.Idx;

            if (m_ZeroNotifiedObjects.Contains(rawID)) return;
            else if (m_NotifiedObjects.TryGetFirstValue(rawID, out TypeInfo typeInfo, out var parsedIter))
            {
                do
                {
                    onRemove?.Invoke(instanceID, typeInfo.Type);
                    System.RemoveComponent(instanceID, typeInfo);

                } while (m_NotifiedObjects.TryGetNextValue(out typeInfo, ref parsedIter));

                return;
            }

            var iter = Select(rawObject.GetType());
            if (!iter.Any())
            {
                m_ZeroNotifiedObjects.Add(rawID);
                return;
            }

            var select = iter.Select(i => i.GenericTypeArguments[0]);
            foreach (var componentType in select)
            {
                onRemove?.Invoke(instanceID, componentType);
                System.RemoveComponent(instanceID, componentType);

                m_NotifiedObjects.Add(rawID, TypeStatic.GetValue(componentType).Data);
            }
        }

        private static bool CollectTypes(Type t)
        {
            return t.GetInterfaces()
                .Where(i => i.IsGenericType)
                .Where(i => i.GetGenericTypeDefinition() == typeof(INotifyComponent<>))
                .Any();
        }
        private static IEnumerable<Type> Select(Type t)
        {
            return t.GetInterfaces()
                .Where(i => i.IsGenericType)
                .Where(i => i.GetGenericTypeDefinition() == typeof(INotifyComponent<>));
                //.Select(i => i.GenericTypeArguments[0]);
        }
    }
}
