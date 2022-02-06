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
using Syadeu.Collections.Converters;
using System;
using Unity.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Collections
{
    [JsonConverter(typeof(PrefabReferenceJsonConverter))]
    public interface IPrefabReference : IEquatable<IPrefabReference>, IValidation
    {
        long Index { get; }
        UnityEngine.Object Asset { get; }
        bool IsSubAsset { get; }
        FixedString128Bytes SubAssetName { get; }

        IPrefabResource GetObjectSetting();

        AsyncOperationHandle LoadAssetAsync();
        AsyncOperationHandle<T> LoadAssetAsync<T>() where T : UnityEngine.Object;
        void UnloadAsset();
        void ReleaseInstance(UnityEngine.GameObject obj);

        bool IsNone();
    }
    public interface IPrefabReference<T> : IPrefabReference, IEquatable<IPrefabReference<T>>
    {
        new T Asset { get; }
    }
}
