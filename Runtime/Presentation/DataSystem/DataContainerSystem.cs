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
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Collections.Threading;
using Syadeu.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Unity.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Data
{
    /// <summary>
    /// 시스템 동기화를 위해 임시로 데이터를 저장할 수 있는 시스템입니다.
    /// </summary>
    public sealed class DataContainerSystem : PresentationSystemEntity<DataContainerSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly ConcurrentDictionary<Hash, object> m_DataContainer = new ConcurrentDictionary<Hash, object>();

        private NativeMultiHashMap<TypeInfo, InstanceID> m_ConstantEntities;
        private Dictionary<Guid, InstanceID> m_ConstantEntitiesGUID;
        private AtomicSafeInteger m_CreatedConstantEntityCount;

        private EntitySystem m_EntitySystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnShutDown()
        {
            foreach (var item in m_ConstantEntities)
            {
                m_EntitySystem.DestroyEntity(item.Value);
            }
        }
        protected override void OnDispose()
        {
            m_ConstantEntities.Dispose();

            m_EntitySystem = null;
        }

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
        }

        protected override PresentationResult OnStartPresentation()
        {
            ConstantData[] constantEntities = EntityDataList.Instance.GetData<ConstantData>();
            m_ConstantEntities = new NativeMultiHashMap<TypeInfo, InstanceID>(constantEntities.Length, Allocator.Persistent);
            m_ConstantEntitiesGUID = new Dictionary<Guid, InstanceID>();

            for (int i = 0; i < constantEntities.Length; i++)
            {
                Type t = constantEntities[i].GetType();
                Entity<ConstantData> instance = m_EntitySystem.CreateEntity(constantEntities[i].AsOriginal());

                m_ConstantEntities.Add(
                    t.ToTypeInfo(),
                    instance.Idx
                    );

                if (!t.GUID.Equals(Guid.Empty))
                {
                    m_ConstantEntitiesGUID.Add(t.GUID, instance.Idx);
                }
            }
            m_CreatedConstantEntityCount = constantEntities.Length;

            return base.OnStartPresentation();
        }

        #endregion

        public bool TryGetConstantEntities(TypeInfo type, out FixedList4096Bytes<InstanceID> entities)
        {
            entities = new FixedList4096Bytes<InstanceID>();

            if (!m_ConstantEntities.TryGetFirstValue(type, out InstanceID entity, out var iter))
            {
                return false;
            }

            do
            {
                entities.Add(entity);
            } while (m_ConstantEntities.TryGetNextValue(out entity, ref iter));

            return true;
        }

        #region Data Operations

        public bool HasValue(Hash key) => m_DataContainer.ContainsKey(key);
        public bool HasValue(string key) => HasValue(ToDataHash(key));

        public void Enqueue(Hash key, object value)
        {
            m_DataContainer.TryAdd(key, value);
        }
        public void Enqueue(string key, object value) => Enqueue(ToDataHash(key), value);

        public object Dequeue(Hash key)
        {
#if DEBUG_MODE
            if (!m_DataContainer.ContainsKey(key))
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"{key} is not in the {TypeHelper.TypeOf<DataContainerSystem>.Name}.");

                return null;
            }
#endif
            return m_DataContainer[key];
        }
        public object Dequeue(string key) => Dequeue(ToDataHash(key));
        public T Dequeue<T>(Hash key)
        {
            object value = Dequeue(key);
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<T>.Type.IsAssignableFrom(value.GetType()))
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"Type mismatch. Value is {TypeHelper.ToString(value.GetType())} but requested as {TypeHelper.TypeOf<T>.Name}");

                return default(T);
            }
#endif
            return (T)value;
        }
        public T Dequeue<T>(string key)
        {
            object value = Dequeue(key);
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<T>.Type.IsAssignableFrom(value.GetType()))
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"Type mismatch. Value is {TypeHelper.ToString(value.GetType())} but requested as {TypeHelper.TypeOf<T>.Name}");

                return default(T);
            }
#endif
            return (T)value;
        }

        #endregion

        public static Hash ToDataHash(string value) => Hash.NewHash(value, Hash.Algorithm.FNV1a64);
    }
}
