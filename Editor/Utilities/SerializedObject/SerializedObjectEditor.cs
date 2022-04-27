// Copyright 2022 Seung Ha Kim
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

using System;

namespace SyadeuEditor.Utilities
{
    public abstract class SerializedObjectEditor<T> : InspectorEditor
    {
        private SerializedObject<T> m_SerializedObject = null;

        protected new T target
        {
            get
            {
                SerializeScriptableObject temp = (SerializeScriptableObject)base.target;
                return (T)temp.Object;
            }
        }
        protected Type type => target.GetType();
        protected new SerializedObject<T> serializedObject
        {
            get
            {
                if (m_SerializedObject == null)
                {
                    m_SerializedObject = new SerializedObject<T>((SerializeScriptableObject)base.target, base.serializedObject);
                }

                return m_SerializedObject;
            }
        }
    }
}
