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

using Syadeu.Mono;
using System;
using UnityEngine;

namespace Syadeu.Collections
{
    [Obsolete("", true)]
    public struct PrefabFactory
    {
        public static readonly Vector3 INIT_POSITION = new Vector3(-9999, -9999, -9999);

        public static PrefabReference<GameObject> MakePrefab(string name, params Type[] components)
        {
            CoreSystem.Logger.ThreadBlock(nameof(MakePrefab), Internal.ThreadInfo.Unity);

            GameObject obj = new GameObject(name, components);
            obj.SetActive(false);
            obj.hideFlags = HideFlags.HideInHierarchy;
            obj.transform.position = INIT_POSITION;

            PrefabList.Instance.ObjectSettings.Add(new PrefabList.ObjectSetting(name, null, false)
            {
                m_IsRuntimeObject = true,
                m_Prefab = obj
            });
            return new PrefabReference<GameObject>(PrefabList.Instance.ObjectSettings.Count - 1);
        }
    }
}
