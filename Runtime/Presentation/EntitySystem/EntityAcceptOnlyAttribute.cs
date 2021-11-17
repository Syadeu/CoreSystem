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

using System;

namespace Syadeu.Presentation
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class EntityAcceptOnlyAttribute : Attribute
    {
        public Type[] AttributeTypes;

        public EntityAcceptOnlyAttribute(params Type[] attributeTypes)
        {
            //for (int i = 0; i < attributeTypes.Length; i++)
            //{
            //    if (!TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(attributeTypes[i]))
            //    {
            //        CoreSystem.Logger.LogError(Channel.Entity, $"Type({attributeTypes[i].Name}) is not a attribute type.");
            //        throw new Exception();
            //    }
            //}
            
            AttributeTypes = attributeTypes;
        }
    }
}
