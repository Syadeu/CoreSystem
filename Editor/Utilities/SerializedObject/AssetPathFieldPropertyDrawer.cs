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

using Syadeu.Collections;
using System;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    [CustomPropertyDrawer(typeof(AssetPathField), true)]
    internal sealed class AssetPathFieldPropertyDrawer : PropertyDrawer<AssetPathField>
    {
        private const string c_AssetPathField = "p_AssetPath";
        private static Type s_GenericType = typeof(AssetPathField<>);

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty pathProperty = property.FindPropertyRelative(c_AssetPathField);

            string assetPath = pathProperty.stringValue;
            UnityEngine.Object asset = GetObjectAtPath(in assetPath);

            Type fieldType;
            if (fieldInfo.FieldType.IsArray) fieldType = fieldInfo.FieldType.GetElementType();
            else fieldType = fieldInfo.FieldType;

            Type targetType;
            if (TypeHelper.InheritsFrom(fieldType, s_GenericType))
            {
                if (fieldType.IsGenericType &&
                    s_GenericType.Equals(fieldType.GetGenericTypeDefinition()))
                {
                    targetType = fieldType.GenericTypeArguments[0];
                }
                else
                {
                    Type genericDef = TypeHelper.GetGenericBaseType(fieldType, s_GenericType);
                    targetType = genericDef.GenericTypeArguments[0];
                }
            }
            else
            {
                targetType = TypeHelper.TypeOf<UnityEngine.Object>.Type;
            }

            Rect[] pos = AutoRect.DivideWithRatio(rect.Pop(), .9f, .1f);

            if (property.IsInArray() && property.GetParent().ChildCount() == 1)
            {
                label = GUIContent.none;
            }
            //label = property.IsInArray() ? GUIContent.none : label;
            if (!property.isExpanded)
            {
                using (var changeCheck = new EditorGUI.ChangeCheckScope())
                {
                    UnityEngine.Object obj
                        = EditorGUI.ObjectField(pos[0], label, asset, targetType, false);

                    if (changeCheck.changed)
                    {
                        pathProperty.stringValue
                            = AssetDatabase.GetAssetPath(obj);
                    }
                }
            }
            else
            {
                pathProperty.stringValue = EditorGUI.TextField(pos[0], label, pathProperty.stringValue);
            }

            if (GUI.Button(pos[1], "Raw"))
            {
                property.isExpanded = !property.isExpanded;
            }
        }

        private static UnityEngine.Object GetObjectAtPath(in string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }
    }
}
