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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation
{
    internal sealed class EntityRecycleModule : PresentationSystemModule<EntitySystem>
    {
        private readonly Dictionary<Hash, Stack<IObject>> m_ReservedObjects
            = new Dictionary<Hash, Stack<IObject>>();

        private readonly Dictionary<PrefabReference, Stack<UnityEngine.Object>> m_ReservedPrefabObjects
            = new Dictionary<PrefabReference, Stack<UnityEngine.Object>>();

        protected override void OnInitialize()
        {
            EntityExtensionMethods.s_EntityRecycleModule = this;
        }
        protected override void OnDispose()
        {
            EntityExtensionMethods.s_EntityRecycleModule = null;
        }

        public void ExecuteDisposeAll()
        {
            EntityProcessorModule processorModule = System.GetModule<EntityProcessorModule>();
            foreach (var item in m_ReservedObjects)
            {
                int count = item.Value.Count;
                for (int i = 0; i < count; i++)
                {
                    ObjectBase targetObject = (ObjectBase)item.Value.Pop();

                    processorModule.ProcessDisposal(targetObject);
                    //targetObject.InternalOnDestroy();
                    //((IDisposable)targetObject).Dispose();
                }
            }
        }

        #region Entity Recycle

        public void InsertReservedObject(IObject obj)
        {
            if (!m_ReservedObjects.TryGetValue(obj.Hash, out var list))
            {
                list = new Stack<IObject>();
                m_ReservedObjects.Add(obj.Hash, list);
            }
            list.Push(obj);
        }
        public T GetOrCreateInstance<T>(IObject original) 
            where T : class, IObject
            => (T)GetOrCreateInstance(original);
        public IObject GetOrCreateInstance(IObject original)
        {
            if (TryGetObject(original.Hash, out IObject obj))
            {
                ObjectBase temp = (ObjectBase)obj;
                temp.InternalInitialize();

                return temp;
            }

            var clone = (ObjectBase)original.Clone();

            return clone;
        }
        private bool TryGetObject(Hash hash, out IObject obj)
        {
            if (m_ReservedObjects.TryGetValue(hash, out var list) &&
                list.Count > 0)
            {
                obj = list.Pop();
                return true;
            }

            obj = null;
            return false;
        }

        #endregion

        #region Prefab Recycle

        public UnityEngine.Object GetOrCreatePrefab(PrefabReference prefab)
        {
#if DEBUG_MODE
            if (!prefab.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This prefab(at {prefab.Index}) is invalid.");

                return null;
            }
            else if (prefab.Asset == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This prefab({prefab.GetObjectSetting().Name}) is not loaded. You can preload prefab with {nameof(IPrefabPreloader)}.");

                return null;
            }
#endif
            if (!m_ReservedPrefabObjects.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<UnityEngine.Object>();
                m_ReservedPrefabObjects.Add(prefab, stack);
            }

            UnityEngine.Object obj;
            if (stack.Count == 0)
            {
                obj = UnityEngine.Object.Instantiate(prefab.Asset);
                if (obj is GameObject gameobj)
                {
                    PresentationSystemEntity.DontDestroyOnLoad(gameobj);
                }
            }

            obj = stack.Pop();
            return obj;
        }
        public void ReservePrefab(PrefabReference prefab, UnityEngine.Object obj)
        {
#if DEBUG_MODE
            if (!prefab.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This prefab(at {prefab.Index}) is invalid.");

                return;
            }
            else if (obj == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    "");

                return;
            }
#endif
            if (!m_ReservedPrefabObjects.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<UnityEngine.Object>();
                m_ReservedPrefabObjects.Add(prefab, stack);
            }

            stack.Push(obj);
        }

        #endregion
    }
}
