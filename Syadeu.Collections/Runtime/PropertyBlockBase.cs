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

using Newtonsoft.Json;
using System;

namespace Syadeu.Collections
{
    [Serializable]
    public abstract class PropertyBlockBase
    {
        [JsonProperty(Order = -100, PropertyName = "Name")]
        protected string m_Name;
        [JsonProperty(Order = -99, PropertyName = "UseInstance")]
        protected bool m_UseInstance;

        internal abstract PropertyBlockBase InternalGetProperty();
    }
    public abstract class PropertyBlock<T> : PropertyBlockBase
        where T : PropertyBlockBase, new()
    {
        internal override PropertyBlockBase InternalGetProperty()
        {
            return GetProperty();
        }
        public T GetProperty()
        {
            if (m_UseInstance)
            {
                T t = new T();
                OnCreateInstance(t);
                return t;
            }
            else
            {
                return this as T;
            }
        }
        protected virtual void OnCreateInstance(T instance) { }

    }
}
