// Copyright 2021 Ikina Games
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

using Syadeu.Collections.Buffer;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Syadeu.Collections.ResourceControl.LowLevel
{
    internal sealed class FindResourceLocationOperation : AsyncOperationBase<IResourceLocation>
    {
        private object RuntimeKey { get; set; }
        private Type Type { get; set; }

        public static FindResourceLocationOperation Get(object runtimeKey, Type type)
        {
            var ins = ObjectPool<FindResourceLocationOperation>.Shared.Get();

            ins.RuntimeKey = runtimeKey;
            ins.Type = type;

            return ins;
        }

        protected override void Execute()
        {
            IResourceLocation location = ExecuteOperation(RuntimeKey, Type);
            if (location != null)
            {
                Complete(location, true, string.Empty);
            }
            else
            {
                Complete(location, false, new InvalidKeyException(RuntimeKey, Type), true);
                ObjectPool<FindResourceLocationOperation>.Shared.Reserve(this);
            }
        }
        public static IResourceLocation ExecuteOperation(object runtimeKey, Type type)
        {
            foreach (var resourceLocator in Addressables.ResourceLocators)
            {
                if (!resourceLocator.Locate(runtimeKey, type, out IList<IResourceLocation> locations)) continue;

                foreach (IResourceLocation item in locations)
                {
                    if (Addressables.ResourceManager.GetResourceProvider(type, item) == null)
                    {
                        continue;
                    }

                    return item;
                }
            }

            return null;
        }
    }
}

#endif