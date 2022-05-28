using UnityEditor;
using SyadeuEditor.Utilities;
using Syadeu.Presentation.Map;
using UnityEngine;
using NUnit.Framework;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation;
using System.Collections.Generic;
using JetBrains.Annotations;
using Syadeu.Collections.Editor;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(MapDataEntity))]
    internal sealed class MapDataEntityPropertyDrawer : EntityDataBasePropertyDrawer
    {
        private bool 
            m_IsInvalidObjectList = false,
            m_IsInvalidRawObjectList = false;
        private readonly List<int>
            m_InvalidObjectIndices = new List<int>(),
            m_InvalidRawObjectIndices = new List<int>();

        private SerializedProperty GetCenterProperty(SerializedProperty property)
        {
            const string c_Str = "m_Center";
            return property.FindPropertyRelative(c_Str);
        }
        private SerializedProperty GetObjectArrayProperty(SerializedProperty property)
        {
            const string c_Str = "m_Objects";
            return property.FindPropertyRelative(c_Str).FindPropertyRelative("p_Array");
        }
        private SerializedProperty GetRawObjectArrayProperty(SerializedProperty property)
        {
            const string c_Str = "m_RawObjects";
            return property.FindPropertyRelative(c_Str).FindPropertyRelative("p_Array");
        }

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            height += EntityDataBaseHeight(property, label);
            height += GetHeightFrom(GetAttributesProperty(property));

            return DefaultHeight(property, label) + height;
        }
        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            base.BeforePropertyGUI(ref rect, property, label);

            bool isInvalid;
            m_IsInvalidObjectList = false;
            m_InvalidObjectIndices.Clear();
            var objectArrayProp = GetObjectArrayProperty(property);
            for (int i = 0; i < objectArrayProp.arraySize; i++)
            {
                var element = objectArrayProp.GetArrayElementAtIndex(i);
                isInvalid = !IsValidObject(element);

                if (isInvalid)
                {
                    m_InvalidObjectIndices.Add(i);
                }

                m_IsInvalidObjectList |= isInvalid;
            }

            m_IsInvalidRawObjectList = false;
            m_InvalidRawObjectIndices.Clear();
            var rawObjectArrayProp = GetRawObjectArrayProperty(property);
            for (int i = 0; i < rawObjectArrayProp.arraySize; i++)
            {
                var element = rawObjectArrayProp.GetArrayElementAtIndex(i);
                isInvalid = !IsValidRawObject(element);

                if (isInvalid)
                {
                    m_InvalidRawObjectIndices.Add(i);
                }

                m_IsInvalidRawObjectList |= isInvalid;
            }
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            DrawDefault(ref rect, property, label);

            DrawEntityDataBase(ref rect, property, label);

            SerializedProperty
                centerProperty = GetCenterProperty(property),
                objectArrayProperty = GetObjectArrayProperty(property),
                rawObjectArrayProperty = GetRawObjectArrayProperty(property);

            PropertyField(ref rect, centerProperty);
            Space(ref rect, 10);

            if (m_IsInvalidObjectList || m_IsInvalidRawObjectList)
            {
                HelpBox(ref rect, 
                    $"Invalid Objects({m_InvalidObjectIndices.Count + m_InvalidRawObjectIndices.Count}) Founded", 
                    MessageType.Warning);
                if (Button(ref rect, "Remove all invalid objects"))
                {
                    for (int i = m_InvalidObjectIndices.Count - 1; i >= 0; i--)
                    {
                        objectArrayProperty.DeleteArrayElementAtIndex(
                            m_InvalidObjectIndices[i]
                            );
                    }
                    for (int i = m_InvalidRawObjectIndices.Count - 1; i >= 0; i--)
                    {
                        rawObjectArrayProperty.DeleteArrayElementAtIndex(
                            m_InvalidRawObjectIndices[i]
                            );
                    }
                    m_InvalidObjectIndices.Clear();
                    m_InvalidRawObjectIndices.Clear();
                }
            }
            else
            {
                HelpBox(ref rect, "All Objects Norminal", MessageType.Info);
            }
            
            DrawFrom(ref rect, centerProperty);
        }

        private static bool IsValidObject(SerializedProperty element)
        {
            Reference<EntityBase> reference = new Reference<EntityBase>(SerializedPropertyHelper.ReadReference<EntityBase>(element.FindPropertyRelative("m_Object")).Hash);

            return !reference.IsEmpty() && reference.IsValid();
        }
        private static bool IsValidRawObject(SerializedProperty element)
        {
            var prefab = SerializedPropertyHelper.ReadPrefabReference(element.FindPropertyRelative("m_Object"));

            return !prefab.IsNone() && prefab.IsValid();
        }
    }
}
