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

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Syadeu.Collections.ResourceControl;

namespace Syadeu.Collections.ResourceControl.LowLevel
{
    [Guid("a05b0346-54fa-44cf-975d-14082836aa61")]
    internal struct UnsafeAssetInfo : IEquatable<UnsafeAssetInfo>
    {
        public FixedString512Bytes key;
        public bool loaded;

        public Hash checkSum;
        public Timer lastUsage;

        public AssetHandleType assetHandleType;

        public bool Equals(UnsafeAssetInfo other) => key.Equals(other.key);
    }
}

#endif