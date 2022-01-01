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
using Syadeu.Collections.Buffer;
using Syadeu.Presentation.Proxy;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    internal sealed class EntityTransformModule : PresentationSystemModule<EntitySystem>
    {
        private UnsafeHashMap<InstanceID, ProxyTransform> m_TransformHashMap;
        private UnsafeHashMap<ProxyTransform, InstanceID> m_EntityHashMap;

        private GameObjectProxySystem m_ProxySystem;

        protected override void OnInitialize()
        {
            m_TransformHashMap = new UnsafeHashMap<InstanceID, ProxyTransform>(10240, AllocatorManager.Persistent);
            m_EntityHashMap = new UnsafeHashMap<ProxyTransform, InstanceID>(10240, AllocatorManager.Persistent);

            ConstructSharedStatic();

            RequestSystem<DefaultPresentationGroup, GameObjectProxySystem>(Bind);
        }
        private unsafe void ConstructSharedStatic()
        {
            SharedStatic<EntityTransformStatic> shared = EntityTransformStatic.GetValue();

            ref UntypedUnsafeHashMap trHashMap =
                ref UnsafeUtility.As<UnsafeHashMap<InstanceID, ProxyTransform>, UntypedUnsafeHashMap>(ref m_TransformHashMap);
            shared.Data.m_TransformHashMap = (UntypedUnsafeHashMap*)UnsafeUtility.AddressOf(ref trHashMap);

            ref UntypedUnsafeHashMap entityHashMap =
                ref UnsafeUtility.As<UnsafeHashMap<ProxyTransform, InstanceID>, UntypedUnsafeHashMap>(ref m_EntityHashMap);
            shared.Data.m_EntityHashMap = (UntypedUnsafeHashMap*)UnsafeUtility.AddressOf(ref entityHashMap);
        }
        private void Bind(GameObjectProxySystem other)
        {
            m_ProxySystem = other;
        }

        protected override void OnShutDown()
        {
            base.OnShutDown();
        }
        protected override void OnDispose()
        {
            m_TransformHashMap.Dispose();
            m_EntityHashMap.Dispose();

            m_ProxySystem = null;
        }

        public ProxyTransform CreateTransform(
            in InstanceID entity,
            in PrefabReference<UnityEngine.GameObject> prefab, 
            in float3 pos, in quaternion rot, in float3 scale,
            in bool enableCull,
            in float3 center, in float3 size, in bool staticBatching)
        {
#if DEBUG_MODE
            if (!prefab.IsNone() && !prefab.IsValid())
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    $"{entity.GetEntity().Name} has an invalid prefab. This is not allowed.");
            }
#endif
            ProxyTransform tr = m_ProxySystem.CreateNewPrefab(
                in prefab, in pos, in rot, in scale, 
                in enableCull, in center, in size, staticBatching);

            m_TransformHashMap.Add(entity, tr);
            m_EntityHashMap.Add(tr, entity);

            return tr;
        }

        public bool HasTransform(in InstanceID entity) => m_TransformHashMap.ContainsKey(entity);
        public ProxyTransform GetTransform(in InstanceID entity)
        {
            if (!m_TransformHashMap.ContainsKey(entity))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"");

                return ProxyTransform.Null;
            }

            return m_TransformHashMap[entity];
        }
        public void RemoveTransform(in InstanceID entity)
        {
            if (!m_TransformHashMap.ContainsKey(entity))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"");

                return;
            }

            ProxyTransform tr = m_TransformHashMap[entity];

            m_TransformHashMap.Remove(entity);
            m_EntityHashMap.Remove(tr);

            tr.Destroy();
        }

        public bool HasEntity(in ProxyTransform transform) => m_EntityHashMap.ContainsKey(transform);
        public InstanceID GetEntity(in ProxyTransform transform)
        {
            if (!m_EntityHashMap.ContainsKey(transform))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"");

                return InstanceID.Empty;
            }

            return m_EntityHashMap[transform];
        }
    }
    public static class EntityTransformExtensions
    {
        public static bool HasTransform(this ObjectBase entity)
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;
            return entitySystem.GetModule<EntityTransformModule>().HasTransform(entity.Idx);
        }
        public static ProxyTransform GetTransform(this ObjectBase entity)
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;
            return entitySystem.GetModule<EntityTransformModule>().GetTransform(entity.Idx);
        }
    }
}
