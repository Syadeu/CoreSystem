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

using Syadeu.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace SyadeuEditor.Utilities
{
    public sealed class SerializedObject<T> : IDisposable
    {
        private static Dictionary<T, SerializedObject<T>> s_Shared = new Dictionary<T, SerializedObject<T>>();
        public static SerializedObject<T> GetSharedObject(T obj)
        {
            if (!s_Shared.TryGetValue(obj, out var s))
            {
                s = new SerializedObject<T>(obj);
                s_Shared.Add(obj, s);
            }

            return s;

            //if (s_Shared.m_Object != null)
            //{
            //    UnityEngine.Object.DestroyImmediate(s_Shared.m_Object);
            //}

            //s_Shared.m_Object = SerializeScriptableObject.Deserialize(obj);
            //s_Shared.m_SerializedObject = new SerializedObject(s_Shared.m_Object);

            //return s_Shared;
        }
        public static float GetPropertyHeight(T obj)
        {
            SerializedObject<T> temp = GetSharedObject(obj);
            return temp.PropertyHeight;
        }

        private SerializeScriptableObject m_Object;
        private SerializedObject m_SerializedObject;
        private AnimFloat m_PropertyHeight;

        public SerializedProperty SerializedProperty
        {
            get
            {
                const string c_Str = "m_Object";

                return m_SerializedObject.FindProperty(c_Str);
            }
        }
        public float PropertyHeight
        {
            get
            {
                float target = EditorGUI.GetPropertyHeight(this, true) + 20;

                if (m_PropertyHeight == null)
                {
                    m_PropertyHeight = new AnimFloat(target);
                }

                //if (SerializedProperty.isExpanded)
                {
                    m_PropertyHeight.target = target;
                }
                //else
                //{
                //    m_PropertyHeight.target = EditorGUI.GetPropertyHeight(this, false);
                //}

                return m_PropertyHeight.value;
            }
        }

        private SerializedObject() { }
        internal SerializedObject(SerializeScriptableObject obj, SerializedObject serializedObject)
        {
            m_Object = obj;
            m_SerializedObject = serializedObject;
        }
        public SerializedObject(string json)
        {
            m_Object = SerializeScriptableObject.Deserialize(json);
            m_SerializedObject = new SerializedObject(m_Object);
        }
        public SerializedObject(T obj)
        {
            m_Object = SerializeScriptableObject.Deserialize(obj);
            m_SerializedObject = new SerializedObject(m_Object);
        }
        
        public void Dispose()
        {
            if (m_Object == null) throw new Exception("already disposed.");

            UnityEngine.Object.DestroyImmediate(m_Object);
            
            m_Object = null;
            m_SerializedObject = null;
        }

        public void GetCachedEditor(ref Editor editor)
        {
            var iter = TypeHelper.GetTypesIter((t) => !t.IsAbstract && !t.IsInterface && TypeHelper.TypeOf<SerializedObjectEditor<T>>.Type.IsAssignableFrom(t));
            if (iter.Any())
            {
                Editor.CreateCachedEditor(m_Object, iter.First(), ref editor);
                return;
            }

            Editor.CreateCachedEditor(m_Object, TypeHelper.TypeOf<DefaultSerializedObjectEditor>.Type, ref editor);
        }
        public Editor GetEditor()
        {
            var iter = TypeHelper.GetTypesIter((t) => !t.IsAbstract && !t.IsInterface && TypeHelper.TypeOf<SerializedObjectEditor<T>>.Type.IsAssignableFrom(t));
            if (iter.Any())
            {
                return (SerializedObjectEditor<T>)Editor.CreateEditor(m_Object, iter.First());
            }
            
            return Editor.CreateEditor(m_Object, TypeHelper.TypeOf<DefaultSerializedObjectEditor>.Type);
        }

        #region Property Utils

        public SerializedProperty FindProperty(string path)
        {
            return SerializedProperty.FindPropertyRelative(path);
        }

        public void ApplyModifiedProperties() => m_SerializedObject.ApplyModifiedProperties();
        public void Update() => m_SerializedObject.Update();

        #endregion

        public static implicit operator SerializedObject(SerializedObject<T> t) => t.m_SerializedObject;
        public static implicit operator SerializedProperty(SerializedObject<T> t) => t.SerializedProperty;
    }
}
