// Copyright 2022 Ikina Games
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

#if UNITY_2020_1_OR_NEWER
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE

using Syadeu.Collections.Buffer.LowLevel;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Syadeu.Collections.ResourceControl.LowLevel
{
    [BurstCompatible]
    [Guid("14ad9ef9-3c7c-4e60-96a3-2f3d602f9846")]
    internal struct UnsafeAssetBundleInfo : IValidation, IEquatable<UnsafeAssetBundleInfo>, IDisposable
    {
        public static UnsafeAssetBundleInfo Invalid => new UnsafeAssetBundleInfo(-1);

        public readonly int index;
        public uint m_Generation;
        public bool m_Using;

        public FixedString512Bytes uri;
        public uint crc;

        public bool loaded;
        public JobHandle m_JobHandle;

        [NativeDisableUnsafePtrRestriction]
        public UnsafeList<UnsafeAssetInfo> assets;

        public UnsafeAssetBundleInfo(int index)
        {
            this.index = index;
            m_Generation = 0;
            m_Using = false;

            uri = string.Empty;
            crc = 0;

            loaded = false;
            m_JobHandle = default;

            assets = default;
        }
        public void Dispose()
        {
            if (assets.IsCreated)
            {
                assets.Dispose();
            }
        }

        public ref UnsafeAssetInfo GetAssetInfo(in int assetIndex)
        {
            return ref assets.ElementAt(assetIndex);
        }
        public unsafe UnsafeReference<UnsafeAssetInfo> GetAssetInfoPointer(in int assetIndex)
        {
            return assets.Ptr + assetIndex;
        }
        public bool HasAnyReferences()
        {
            for (int i = 0; i < assets.Length; i++)
            {
                if (!assets[i].checkSum.IsEmpty()) return true;
            }
            return false;
        }

        public bool IsValid() => index >= 0 && m_Using;
        public bool Equals(UnsafeAssetBundleInfo other) => index.Equals(other.index);
    }
}

#endif