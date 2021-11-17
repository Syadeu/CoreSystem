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
using Syadeu.Internal;
using System;

using UnityEngine.AddressableAssets;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Converters
{
    [Preserve]
    internal sealed class AssetReferenceJsonConverter : JsonConverter
    {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType) => objectType.Equals(TypeHelper.TypeOf<AssetReference>.Type);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            AssetReference refAsset = value as AssetReference;
            writer.WriteValue(refAsset.AssetGUID);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken jo = JToken.Load(reader);
            string guid = jo.Value<string>();
            return Activator.CreateInstance(objectType, guid);
        }
    }
}
