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

using Syadeu.Collections.Graphs;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Syadeu.Collections.Graphs.Editor
{
    [CustomPropertyDrawer(typeof(VisualExposedParameter), true)]
    public class VisualExposedParameterView : PropertyDrawer<VisualExposedParameter>
    {
        private Type m_Type;
        private IEnumerable<FieldInfo> m_Fields;

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            m_Type = property.GetTargetObject().GetType();
            m_Fields = m_Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(t =>
                {
                    if (t.IsPrivate)
                    {
                        if (t.GetCustomAttribute<SerializeField>() != null) return true;
                    }

                    if (t.GetCustomAttribute<NonSerializedAttribute>() != null) return false;
                    return true;
                });
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            foreach (FieldInfo field in m_Fields)
            {
                SerializedProperty element = property.FindPropertyRelative(field.Name);
                PropertyField(ref rect, element, new GUIContent(element.displayName), element.isExpanded);
            }
        }
    }
}
