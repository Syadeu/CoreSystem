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
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    [Serializable, JsonArray]
    public sealed class ConstActionReferenceArray : ArrayWrapper<ConstActionReference>, IConstActionReferenceArray
    {
        public ConstActionReferenceArray() : base() { }
        public ConstActionReferenceArray(IEnumerable<ConstActionReference> constActions) : base(constActions) { }

        public static implicit operator ConstActionReference[](ConstActionReferenceArray t) => t.p_Array;
        public static implicit operator ConstActionReferenceArray(ConstActionReference[] t) => new ConstActionReferenceArray(t);
    }
    [Serializable, JsonArray]
    public sealed class ConstActionReferenceArray<TValue> : ArrayWrapper<ConstActionReference<TValue>>, IConstActionReferenceArray
    {
        public ConstActionReferenceArray() { }
        [JsonConstructor]
        public ConstActionReferenceArray(IEnumerable<ConstActionReference<TValue>> constActions) : base(constActions) { }

        public static implicit operator ConstActionReference<TValue>[](ConstActionReferenceArray<TValue> t) => t.p_Array;
        public static implicit operator ConstActionReferenceArray<TValue>(ConstActionReference<TValue>[] t) => new ConstActionReferenceArray<TValue>(t);
    }
}
