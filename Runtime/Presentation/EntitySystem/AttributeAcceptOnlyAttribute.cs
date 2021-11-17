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

using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class AttributeAcceptOnlyAttribute : Attribute
    {
        public Type[] Types;
        public AttributeAcceptOnlyAttribute(params Type[] types)
        {
            //for (int i = 0; i < types.Length; i++)
            //{
            //    if (!TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(types[i]))
            //    {
            //        CoreSystem.Logger.LogError(Channel.Entity, $"Type({types[i].Name}) is not a entity type.");
            //        throw new Exception();
            //    }
            //}
            
            Types = types;
        }
    }
}
