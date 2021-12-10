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
using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

using TypeInfo = Syadeu.Collections.TypeInfo;

namespace Syadeu.Presentation.Components
{
    [Obsolete("Use TypeStatic", true)]
    public struct ComponentType
    {
        public static SharedStatic<TypeInfo> GetValue(Type componentType)
        {
            return SharedStatic<TypeInfo>.GetOrCreate(
                TypeHelper.TypeOf<EntityComponentSystem>.Type,
                componentType, (uint)UnsafeUtility.AlignOf<TypeInfo>());
        }
    }
    [Obsolete("Use TypeStatic", true)]
    public struct ComponentType<TComponent>
    {
        private static readonly SharedStatic<TypeInfo> Value
            = SharedStatic<TypeInfo>.GetOrCreate<EntityComponentSystem, TComponent>((uint)UnsafeUtility.AlignOf<TypeInfo>());

        public static Type Type => Value.Data.Type;
        public static TypeInfo TypeInfo => Value.Data;
        public static ComponentTypeQuery Query => ComponentTypeQuery.Create(TypeInfo);
    }
}
