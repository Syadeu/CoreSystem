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
using Syadeu.Collections.ResourceControl;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;

namespace Syadeu.Collections.Converters
{
    internal sealed class AssetIndexJsonConverter : JsonConverter<IAssetIndex>
    {
        private const char c_Spliter = ':';

        public override bool CanRead => true;
        public override bool CanWrite => true;

        private static readonly Type[] s_CtorParams = new Type[] { TypeHelper.TypeOf<int2>.Type };
        private static Dictionary<Type, ConstructorInfo> s_Constructors = new Dictionary<Type, ConstructorInfo>();
        private static ConstructorInfo GetConstructor(Type type)
        {
            if (s_Constructors.TryGetValue(type, out var ctor)) return ctor;

            ctor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                CallingConventions.HasThis,
                s_CtorParams,
                null
                );

            s_Constructors.Add(type, ctor);
            return ctor;
        }

        private static object[] s_SharedParam = new object[1];

        public override IAssetIndex ReadJson(
            JsonReader reader, Type objectType, IAssetIndex existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken to = JToken.Load(reader);
            // Old PrefabReference
            //if (to.Type == JTokenType.Object && to["Index"] != null)
            //{
            //    int index = to["Index"].ToObject<int>();
            //    string subAssetName = to["SubAssetName"].ToObject<string>();
                

            //    //ResourceHashMap.Instance.FindAsset()

            //    return null;
            //}

            string value = to.Value<string>();
            if (value.IsNullOrEmpty())
            {
                s_SharedParam[0] = new int2(-1, -1);
                return (IAssetIndex)GetConstructor(objectType).Invoke(s_SharedParam);
            }

            string[] indices = value.Split(c_Spliter);
            if (indices.Length != 2 ||
                !int.TryParse(indices[0], out int x) ||
                !int.TryParse(indices[1], out int y))
            {
                s_SharedParam[0] = new int2(-1, -1);
                return (IAssetIndex)GetConstructor(objectType).Invoke(s_SharedParam);
            }

            s_SharedParam[0] = new int2(x, y);
            return (IAssetIndex)GetConstructor(objectType).Invoke(s_SharedParam);
        }
        public override void WriteJson(JsonWriter writer, IAssetIndex value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value.Index.x}:{value.Index.y}");
        }
    }
}
