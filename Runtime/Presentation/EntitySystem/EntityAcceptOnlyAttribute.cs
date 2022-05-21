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
    /// <see cref="Entities.EntityDataBase"/> 가 참조할 수 있는 <see cref="Attributes.AttributeBase"/> 를 선언 할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 이 어트리뷰트를 상속받는 <see cref="Entities.EntityDataBase"/> 의 참조 타입을 
    /// EntityWindow 에서 제한합니다.
    /// </remarks>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class EntityAcceptOnlyAttribute : Attribute
    {
        public Type[] AttributeTypes;

        public EntityAcceptOnlyAttribute(params Type[] attributeTypes)
        {
#if DEBUG_MODE
            for (int i = 0; i < attributeTypes.Length; i++)
            {
                if (!typeof(Attributes.AttributeBase).IsAssignableFrom(attributeTypes[i]))
                {
                    CoreSystem.Logger.LogError(LogChannel.Entity, 
                        $"Type({attributeTypes[i].Name}) is not a attribute type.");
                    throw new Exception();
                }
            }
#endif
            AttributeTypes = attributeTypes;
        }
    }
}
