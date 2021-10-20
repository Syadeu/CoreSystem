﻿using Syadeu;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using SyadeuEditor.Utilities;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class ActorWeaponDataDrawer : ObjectBaseDrawer<ActorWeaponData>
    {
        private ActorWeaponPreviewScene m_PreviewScene = null;
        private FieldInfo m_FXBoundsField;
        private ArrayDrawer m_FXBoundsDrawer;

        public ActorWeaponDataDrawer(ObjectBase weaponData) : base(weaponData)
        {
            m_FXBoundsField = GetField("m_FXBounds");
            m_FXBoundsDrawer = GetDrawer<ArrayDrawer>("FXBounds");
        }

        protected override void DrawGUI()
        {
            DrawHeader();
            DrawDescription();

            for (int i = 0; i < Drawers.Length; i++)
            {
                if (Drawers[i].Name.Equals("FXBounds"))
                {
                    using (new EditorUtilities.BoxBlock(Color.black))
                    {
                        DrawFXBounds();
                    }

                    continue;
                }
                DrawField(Drawers[i]);
            }
        }

        private void DrawFXBounds()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                m_FXBoundsDrawer.DrawHeader();
                EditorGUI.BeginDisabledGroup(m_PreviewScene != null && m_PreviewScene.IsOpened);
                if (GUILayout.Button("Open"))
                {
                    if (m_PreviewScene == null)
                    {
                        m_PreviewScene = Stage.CreateInstance<ActorWeaponPreviewScene>();
                    }

                    m_PreviewScene.Open(TargetObject);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(m_PreviewScene == null || !m_PreviewScene.IsOpened);
                if (GUILayout.Button("Close"))
                {
                    m_PreviewScene.Close();
                }
                EditorGUI.EndDisabledGroup();
            }

            if (!m_FXBoundsDrawer.m_Open) return;
            EditorGUI.indentLevel++;
            FXBounds[] fXBounds = GetValue<FXBounds[]>(m_FXBoundsField);

            for (int i = 0; i < fXBounds.Length; i++)
            {
                using (new EditorUtilities.BoxBlock(Color.white))
                {
                    using (new EditorGUI.DisabledGroupScope(m_PreviewScene == null || !m_PreviewScene.IsOpened))
                    {
                        if (GUILayout.Button(
                            m_PreviewScene != null && m_PreviewScene.IsActiveFXBounds(i) ?
                            "Disable Preview" : "Enable Preview"))
                        {
                            m_PreviewScene.SetActiveFXBounds(i, !m_PreviewScene.IsActiveFXBounds(i));
                        }
                    }
                    m_FXBoundsDrawer.DrawElementAt(i);
                }
            }

            EditorGUI.indentLevel--;
        }

    }
}