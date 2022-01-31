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
using Syadeu.Collections.Buffer.LowLevel;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation.Components
{
    internal sealed class EntityComponentProcessorModule : PresentationSystemModule<EntityComponentSystem>
    {
        private Dictionary<Type, IComponentProcessor> m_Processors;

        protected override void OnInitialize()
        {
            Type[] processorTypes = TypeHelper.GetTypes(t => !t.IsAbstract && !t.IsInterface && TypeHelper.TypeOf<IComponentProcessor>.Type.IsAssignableFrom(t));
            m_Processors = new Dictionary<Type, IComponentProcessor>();

            for (int i = 0; i < processorTypes.Length; i++)
            {
                var ins = (IComponentProcessor)Activator.CreateInstance(processorTypes[i]);
                m_Processors.Add(ins.ComponentType, ins);
            }

            foreach (var item in m_Processors.Values)
            {
                item.OnInitialize();
            }
        }
        protected override void OnShutDown()
        {
            foreach (var item in m_Processors.Values)
            {
                item.Dispose();
            }
        }

        public void ProcessOnCreated(in InstanceID entity, Type componentType, UnsafeReference value)
        {
            if (!m_Processors.TryGetValue(componentType, out var processor)) return;

            processor.OnCreated(in entity, value);
        }
        public void ProcessOnDestroy(in InstanceID entity, Type componentType, UnsafeReference value)
        {
            if (!m_Processors.TryGetValue(componentType, out var processor)) return;

            processor.OnDestroy(in entity, value);
        }
    }
}
