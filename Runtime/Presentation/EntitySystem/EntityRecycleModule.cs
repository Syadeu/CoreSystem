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
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    internal sealed class EntityRecycleModule : PresentationSystemModule<EntitySystem>
    {
        private readonly Dictionary<Hash, Stack<IObject>> m_ReservedObjects
            = new Dictionary<Hash, Stack<IObject>>();

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

        public void InsertReservedObject(IObject obj)
        {
            //if (obj is ConvertedEntity)
            //{
            //    return;
            //}

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
                temp.m_HashCode = System.CreateHashCode();
                temp.InternalInitialize();

                return temp;
            }

            var clone = (ObjectBase)original.Clone();
            clone.m_HashCode = System.CreateHashCode();

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
    }
}
