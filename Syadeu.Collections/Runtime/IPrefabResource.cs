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

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Collections
{
    // PrefabList.ObjectSetting
    //[System.Obsolete("Deprecated Use AssetIndex", true)]
    public interface IPrefabResource
    {
        string Name { get; }
        //UnityEngine.Object LoadedObject { get; }

        AsyncOperationHandle LoadAssetAsync(FixedString128Bytes name);
        AsyncOperationHandle<T> LoadAssetAsync<T>(FixedString128Bytes name) where T : UnityEngine.Object;
        void UnloadAsset();

        AsyncOperationHandle<GameObject> InstantiateAsync(in float3 pos, in quaternion rot, in Transform parent);
        void ReleaseInstance(GameObject obj);

        string ToString();
    }
}
