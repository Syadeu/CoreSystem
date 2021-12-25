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
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

using TypeInfo = Syadeu.Collections.TypeInfo;

namespace Syadeu.Presentation.Components
{
    public struct ComponentType
    {
        public static SharedStatic<ComponentType> GetValue(Type componentType)
        {
            return SharedStatic<ComponentType>.GetOrCreate(
                TypeHelper.TypeOf<EntityComponentSystem>.Type,
                componentType, (uint)UnsafeUtility.AlignOf<ComponentType>());
        }

        internal unsafe ComponentBuffer* ComponentBuffer;

        public int Length
        {
            get
            {
                unsafe
                {
                    return ComponentBuffer->Length;
                }
            }
        }

        public ref TComponent ComponentAt<TComponent>(in int index) 
            where TComponent : unmanaged, IEntityComponent
        {
            unsafe
            {
                return ref ComponentBuffer->ElementAt<TComponent>(in index);
            }
        }
    }
    public struct ComponentType<TComponent> where TComponent : unmanaged, IEntityComponent
    {
        private static readonly SharedStatic<ComponentType> Value
            = SharedStatic<ComponentType>.GetOrCreate<EntityComponentSystem, TComponent>((uint)UnsafeUtility.AlignOf<ComponentType>());

        internal static unsafe ComponentBuffer* ComponentBuffer => Value.Data.ComponentBuffer;

        public static int Length => Value.Data.Length;
        public static ref TComponent ComponentAt(in int index) => ref Value.Data.ComponentAt<TComponent>(in index);
    }
}
