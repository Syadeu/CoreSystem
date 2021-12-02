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
        private NativeMultiHashMap<InstanceID, InstanceID> m_Hierarchy;

        protected override void OnInitialize()
        {
            m_Hierarchy = new NativeMultiHashMap<InstanceID, InstanceID>(1024, AllocatorManager.Persistent);
        }
        protected override void OnDispose()
        {
            m_Hierarchy.Dispose();
        }

        public void AddChild(in InstanceID parent, in InstanceID child)
        {
            m_Hierarchy.Add(parent, child);
        }
        public void RemoveChild(in InstanceID parent, in InstanceID child)
        {
            if (!m_Hierarchy.ContainsKey(parent))
            {
                return;
            }

            if (m_Hierarchy.CountValuesForKey(parent) == 1)
            {
                m_Hierarchy.Remove(parent);
                return;
            }

            m_Hierarchy.Remove(parent, child);
        }

        public int GetChildCount(in InstanceID parent)
        {
            if (!m_Hierarchy.ContainsKey(parent))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({parent.GetObject().Name}) doesn\'t have any childs.");

                return 0;
            }

            return m_Hierarchy.CountValuesForKey(parent);
        }
        public InstanceID GetChild(in InstanceID parent, in int index)
        {
            int childCount = GetChildCount(in parent);
            if (childCount <= index)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Out of range, Entity({parent.GetObject().Name}) has {childCount} childs.");

                return InstanceID.Empty;
            }

            NativeMultiHashMap<InstanceID, InstanceID>.Enumerator temp = m_Hierarchy.GetValuesForKey(parent);
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
