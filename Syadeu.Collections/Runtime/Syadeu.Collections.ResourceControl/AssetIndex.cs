﻿// Copyright 2021 Ikina Games
// Author : Seung Ha Kim (Syadeu)
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

#if UNITY_2019_1_OR_NEWER && UNITY_ADDRESSABLES
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE
#if UNITY_2019 && !UNITY_2020_1_OR_NEWER
#define UNITYENGINE_OLD
#else
#if UNITY_MATHEMATICS
#endif
#endif

using Newtonsoft.Json;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Collections.ResourceControl
{
    [Serializable]
    public struct AssetIndex : IEmpty, IValidation, IAssetIndex
    {
        public static AssetIndex Empty => new AssetIndex(-1);

        [SerializeField] internal int2 m_Index;
        [SerializeField] private bool m_IsCreated;

        [JsonIgnore]
        public AssetReference AssetReference
        {
            get
            {
                ResourceHashMap.Instance.TryGetAssetReference(this, out var asset);
                return asset;
            }
        }
        int2 IAssetIndex.Index => m_Index;

        public AssetIndex(int2 index)
        {
            m_Index = index;
            m_IsCreated = index.x >= 0 && index.y >= 0;
        }
        public AssetIndex(int x, int y)
        {
            m_Index = new int2(x, y);
            m_IsCreated = x >= 0 && y >= 0;
        }

        public bool IsEmpty() => !m_IsCreated;
        public bool IsValid() => ResourceHashMap.Instance.TryGetAssetReference(this, out _);

        public override string ToString()
        {
            AssetReference temp = AssetReference;
            if (temp.IsValid()) return temp.ToString();
            return $"INVALID({m_Index.x}:{m_Index.y})";
        }
    }
    [Serializable]
    public struct AssetIndex<TObject> : IEmpty, IValidation, IAssetIndex
        where TObject : UnityEngine.Object
    {
        public static AssetIndex<TObject> Empty => new AssetIndex<TObject>(-1);

        [SerializeField] internal int2 m_Index;
        [SerializeField] private bool m_IsCreated;

        [JsonIgnore]
        public AssetReference AssetReference
        {
            get
            {
                ResourceHashMap.Instance.TryGetAssetReference(this, out var asset);
                return asset;
            }
        }
        int2 IAssetIndex.Index => m_Index;

        public AssetIndex(int2 index)
        {
            m_Index = index;
            m_IsCreated = index.x >= 0 && index.y >= 0;
        }
        public AssetIndex(int x, int y)
        {
            m_Index = new int2(x, y);
            m_IsCreated = x >= 0 && y >= 0;
        }

        public bool IsEmpty() => !m_IsCreated;
        public bool IsValid() => ResourceHashMap.Instance.TryGetAssetReference(this, out _);

        public override string ToString()
        {
            AssetReference temp = AssetReference;
            if (temp.IsValid()) return temp.ToString();
            return $"INVALID({m_Index.x}:{m_Index.y})";
        }

        public static implicit operator AssetIndex(AssetIndex<TObject> t)
        {
            return new AssetIndex(t.m_Index);
        }
    }

    [JsonConverter(typeof(Converters.AssetIndexJsonConverter))]
    public interface IAssetIndex
    {
        int2 Index { get; }
    }
}

#endif