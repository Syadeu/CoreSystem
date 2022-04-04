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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    public abstract class PropertyDrawer<T> : PropertyDrawer
    {
        private static bool s_Initialized = false;

        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!s_Initialized)
            {
                OnInitialize(property);
                s_Initialized = true;
            }

            AutoRect rect = new AutoRect(position);
            BeforePropertyGUI(ref rect, property, label);

            ReflectionSealedViewAttribute sealedViewAttribute
                = fieldInfo.GetCustomAttribute<ReflectionSealedViewAttribute>();

            using (new EditorGUI.DisabledGroupScope(sealedViewAttribute != null))
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                OnPropertyGUI(ref rect, property, label);
            }
        }
        protected virtual void OnInitialize(SerializedProperty property) { }

        protected virtual void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label) { }
        protected virtual void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label) { }

        #region Cache

        protected void SaveCache<TObject>(SerializedProperty property, string name, TObject obj)
        {
            Hash hash = Hash.NewHash(property.propertyPath);
            string cacheName = hash.ToString() + name;

            EditorPrefs.SetString(cacheName, JsonConvert.SerializeObject(obj));
        }
        protected TObject LoadCache<TObject>(SerializedProperty property, string name) => LoadCache(property, name, default(TObject));
        protected TObject LoadCache<TObject>(SerializedProperty property, string name, TObject defaultValue)
        {
            Hash hash = Hash.NewHash(property.propertyPath);
            string cacheName = hash.ToString() + name;

            string json = EditorPrefs.GetString(cacheName, JsonConvert.SerializeObject(defaultValue));
            return JsonConvert.DeserializeObject<TObject>(json);
        }

        #endregion

        protected List<Rect> GetValueRect(Rect rawRect, GUIStyle style, params string[] names)
        {
            List<Rect> rects = EditorGUIUtility.GetFlowLayoutedRects(rawRect, style, EditorGUIUtility.standardVerticalSpacing, EditorGUIUtility.standardVerticalSpacing, names.ToList());

            return rects;
        }
    }
}
