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
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Converters
{
    [Preserve]
    internal sealed class ConstActionReferenceJsonConverter : JsonConverter<IConstActionReference>
    {
        private readonly Type[] m_ConstructorParam;

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public ConstActionReferenceJsonConverter() : base()
        {
            m_ConstructorParam = new Type[] { TypeHelper.TypeOf<Guid>.Type };
        }

        public override IConstActionReference ReadJson(JsonReader reader, Type objectType, IConstActionReference existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jo = JToken.Load(reader);
            string temp = jo.ToString();

            ConstructorInfo constructor = objectType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                callConvention: CallingConventions.HasThis,
                m_ConstructorParam,
                null
                );

            if (Guid.TryParse(temp, out Guid guid))
            {
                return (IConstActionReference)constructor.Invoke(new object[] { guid });
            }

            return (IConstActionReference)constructor.Invoke(new object[] { Guid.Empty });
        }

        public override void WriteJson(JsonWriter writer, IConstActionReference value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Guid);
        }
    }
}
