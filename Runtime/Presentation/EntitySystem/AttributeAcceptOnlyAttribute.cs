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

using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="Attributes.AttributeBase"/> 를 참조할 수 있는 <see cref="Entities.EntityDataBase"/> 를 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 타입은 <see cref="Entities.EntityDataBase"/> 를 상속받는 클래스 이어야 합니다.
    /// </remarks>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class AttributeAcceptOnlyAttribute : Attribute
    {
        public Type[] Types;
        public AttributeAcceptOnlyAttribute(params Type[] types)
        {
#if DEBUG_MODE
            for (int i = 0; i < types.Length; i++)
            {
                if (!typeof(Entities.EntityDataBase).IsAssignableFrom(types[i]))
                {
                    CoreSystem.Logger.LogError(Channel.Entity, 
                        $"Type({types[i].Name}) is not a entity type.");
                    throw new Exception();
                }
            }
#endif
            Types = types;
        }
    }
}
