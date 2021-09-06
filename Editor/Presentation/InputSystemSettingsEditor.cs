﻿using UnityEngine;
using UnityEditor;
using Syadeu.Presentation.Input;
using System.Reflection;
using System;

namespace SyadeuEditor.Presentation
{
    [CustomEditor(typeof(InputSystemSettings))]
    public sealed class InputSystemSettingsEditor : EditorEntity<InputSystemSettings>
    {
        SerializedProperty m_AdditionalInputActions;

        bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_AdditionalInputActions = serializedObject.FindProperty("m_AdditionalInputActions");
        }
        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                FieldInfo field = GetField("m_AdditionalInputActions");
                InputSystemSettings.CustomInputAction[] ori = (InputSystemSettings.CustomInputAction[])field.GetValue(Asset);

                InputSystemSettings.CustomInputAction[] temp = new InputSystemSettings.CustomInputAction[ori.Length + 1];
                Array.Copy(ori, temp, ori.Length);

                temp[temp.Length - 1] = new InputSystemSettings.CustomInputAction();

                field.SetValue(Asset, temp);
                serializedObject.Update();
            }
            if (GUILayout.Button("-"))
            {

            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(m_AdditionalInputActions);

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Show Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}