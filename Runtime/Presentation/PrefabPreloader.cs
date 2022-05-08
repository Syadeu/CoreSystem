﻿// Copyright 2021 Seung Ha Kim
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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation
{
    public sealed class PrefabPreloader : IDisposable
    {
        private List<PrefabReference> m_Writer;

        internal PrefabPreloader(List<PrefabReference> wr)
        {
            m_Writer = wr;
        }

        public void Add(PrefabReference prefab)
        {
#if DEBUG_MODE
            if (prefab.IsNone() || !prefab.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Core,
                    $"Cannot add invalid prefab(index: {prefab.Index}, subasset: {prefab.SubAssetName}) to preload");
                return;
            }
#endif
            m_Writer.Add(prefab);
        }
        public void Add(params PrefabReference[] prefabs)
        {
            for (int i = 0; i < prefabs.Length; i++)
            {
                m_Writer.Add(prefabs[i]);
            }
        }

        void IDisposable.Dispose()
        {
            m_Writer = null;
        }
    }
}
