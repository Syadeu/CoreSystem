// Copyright 2022 Seung Ha Kim
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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Presentation.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Presentation
{
    [Serializable, JsonArray]
    public sealed class AttributeArray : ArrayWrapper<Reference<AttributeBase>>
    {
        public AttributeArray() { }
        [JsonConstructor]
        public AttributeArray(IEnumerable<Reference<AttributeBase>> attributes)
        {
            m_Array = attributes.ToArray();
        }

        public static implicit operator Reference<AttributeBase>[](AttributeArray t) => t.m_Array;
        public static implicit operator AttributeArray(Reference<AttributeBase>[] t)=> new AttributeArray(t);
    }
}
