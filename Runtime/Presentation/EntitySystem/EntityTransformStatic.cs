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
using Syadeu.Presentation.Proxy;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation
{
    public struct EntityTransformStatic
    {
        internal readonly static SharedStatic<EntityTransformStatic> Value;
        static EntityTransformStatic()
        {
            Value = SharedStatic<EntityTransformStatic>.GetOrCreate(
                TypeHelper.TypeOf<EntitySystem>.Type,
                TypeHelper.TypeOf<EntityTransformModule>.Type,
                (uint)UnsafeUtility.AlignOf<EntityTransformStatic>());
        }

        public static EntityTransformHashMap GetHashMap() => new EntityTransformHashMap(Value.Data);

        internal UnsafeReference<UntypedUnsafeHashMap> m_TransformHashMap, m_EntityHashMap;

        public bool HasTransform(in InstanceID entity)
        {
            ref UnsafeHashMap<InstanceID, ProxyTransform> map
                = ref UnsafeUtility.As<UntypedUnsafeHashMap, UnsafeHashMap<InstanceID, ProxyTransform>>(ref m_TransformHashMap.Value);

            return map.ContainsKey(entity);
        } 
        public ProxyTransform GetTransform(in InstanceID entity)
        {
            ref UnsafeHashMap<InstanceID, ProxyTransform> map
                = ref UnsafeUtility.As<UntypedUnsafeHashMap, UnsafeHashMap<InstanceID, ProxyTransform>>(ref m_TransformHashMap.Value);

            if (!map.ContainsKey(entity))
            {
                return ProxyTransform.Null;
            }

            return map[entity];
        }
    }

    [BurstCompatible]
    public readonly struct EntityTransformHashMap
    {
        private readonly UnsafeReference<UntypedUnsafeHashMap> 
            m_TransformHashMap, m_EntityHashMap;

        internal EntityTransformHashMap(EntityTransformStatic transformStatic)
        {
            m_TransformHashMap = transformStatic.m_TransformHashMap;
            m_EntityHashMap = transformStatic.m_EntityHashMap;
        }

        public bool HasTransform(in InstanceID entity)
        {
            ref UnsafeHashMap<InstanceID, ProxyTransform> map
                = ref UnsafeUtility.As<UntypedUnsafeHashMap, UnsafeHashMap<InstanceID, ProxyTransform>>(ref m_TransformHashMap.Value);

            return map.ContainsKey(entity);
        }
        public ProxyTransform GetTransform(in InstanceID entity)
        {
            ref UnsafeHashMap<InstanceID, ProxyTransform> map
                = ref UnsafeUtility.As<UntypedUnsafeHashMap, UnsafeHashMap<InstanceID, ProxyTransform>>(ref m_TransformHashMap.Value);

            if (!map.ContainsKey(entity))
            {
                return ProxyTransform.Null;
            }

            return map[entity];
        }
    }
}
