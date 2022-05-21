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
using Unity.Collections;

namespace Syadeu.Presentation
{
    internal sealed class EntityHierarchyModule : PresentationSystemModule<EntitySystem>
    {
        private NativeMultiHashMap<InstanceID, InstanceID> m_ChildHierarchyHashMap;
        private NativeHashMap<InstanceID, InstanceID> m_ParentHashMap;

        protected override void OnInitialize()
        {
            m_ChildHierarchyHashMap = new NativeMultiHashMap<InstanceID, InstanceID>(1024, AllocatorManager.Persistent);
            m_ParentHashMap = new NativeHashMap<InstanceID, InstanceID>(1024, AllocatorManager.Persistent);

            System.OnEntityDestroy += System_OnEntityDestroy;
        }
        protected override void OnShutDown()
        {
            System.OnEntityDestroy -= System_OnEntityDestroy;
        }
        protected override void OnDispose()
        {
            m_ChildHierarchyHashMap.Dispose();
            m_ParentHashMap.Dispose();
        }

        #region EventHandlers

        private void System_OnEntityDestroy(IObject obj)
        {
            if (m_ChildHierarchyHashMap.TryGetFirstValue(obj.Idx, out InstanceID child, out var iter))
            {
                FixedInstanceList64 list = new FixedInstanceList64();
                do
                {
                    System.InternalDestroyEntity(in child);
                    list.Add(child);
                } while (m_ChildHierarchyHashMap.TryGetNextValue(out child, ref iter));

                for (int i = 0; i < list.Length; i++)
                {
                    RemoveChild(obj.Idx, list[i]);
                }
            }

            if (HasParent(obj.Idx))
            {
                RemoveChild(GetParent(obj.Idx), obj.Idx);
            }
        }

        #endregion

        public void AddChild(in InstanceID parent, in InstanceID child)
        {
            m_ChildHierarchyHashMap.Add(parent, child);
            m_ParentHashMap.Add(child, parent);
        }
        public void RemoveChild(in InstanceID parent, in InstanceID child)
        {
            if (!m_ChildHierarchyHashMap.ContainsKey(parent))
            {
                return;
            }

            m_ParentHashMap.Remove(child);

            if (m_ChildHierarchyHashMap.CountValuesForKey(parent) == 1)
            {
                m_ChildHierarchyHashMap.Remove(parent);
                return;
            }

            m_ChildHierarchyHashMap.Remove(parent, child);
        }

        public int GetChildCount(in InstanceID parent)
        {
            if (!m_ChildHierarchyHashMap.ContainsKey(parent))
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Entity({parent.GetObject().Name}) doesn\'t have any childs.");

                return 0;
            }

            return m_ChildHierarchyHashMap.CountValuesForKey(parent);
        }
        public bool HasParent(in InstanceID child)
        {
            return m_ParentHashMap.ContainsKey(child);
        }
        public InstanceID GetParent(in InstanceID child)
        {
            if (!m_ParentHashMap.TryGetValue(child, out InstanceID parent)) return InstanceID.Empty;

            return parent;
        }
        public InstanceID GetChild(in InstanceID parent, in int index)
        {
            int childCount = GetChildCount(in parent);
            if (childCount <= index)
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Out of range, Entity({parent.GetObject().Name}) has {childCount} childs.");

                return InstanceID.Empty;
            }

            NativeMultiHashMap<InstanceID, InstanceID>.Enumerator temp = m_ChildHierarchyHashMap.GetValuesForKey(parent);
            temp.MoveNext();
            for (int i = 0; i < index; i++)
            {
                temp.MoveNext();
            }

            InstanceID output = temp.Current;
            temp.Dispose();

            return output;
        }
    }
}
