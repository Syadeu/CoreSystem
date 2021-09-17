using Syadeu;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using System;
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
                    using (new EditorUtils.BoxBlock(Color.black))
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
                //m_OpenFXBounds = EditorUtils.Foldout(m_OpenFXBounds, "FXBounds", 13);
                m_FXBoundsDrawer.DrawHeader();
                if (GUILayout.Button("Open"))
                {
                    if (m_PreviewScene == null)
                    {
                        m_PreviewScene = Stage.CreateInstance<ActorWeaponPreviewScene>();
                    }

                    m_PreviewScene.Open(TargetObject);
                }
                if (GUILayout.Button("Close"))
                {
                    StageUtility.GoToMainStage();
                }
            }

            if (!m_FXBoundsDrawer.m_Open) return;
            EditorGUI.indentLevel++;
            ActorWeaponData.FXBounds[] fXBounds = GetValue<ActorWeaponData.FXBounds[]>(m_FXBoundsField);

            for (int i = 0; i < fXBounds.Length; i++)
            {
                m_FXBoundsDrawer.DrawElementAt(i);
            }

            EditorGUI.indentLevel--;
        }

    }

    public sealed class ActorWeaponPreviewScene : EntityPreviewScene<ActorWeaponData>
    {
        private FieldInfo 
            m_PrefabField, m_FXBoundsField;

        private Reference<ObjectEntity> m_Prefab = Reference<ObjectEntity>.Empty;
        private GameObject m_PrefabInstance = null;

        GameObject[] m_PreviewFXBounds = Array.Empty<GameObject>();

        protected override void OnStageFirstTimeOpened()
        {
            m_PrefabField = GetField("m_Prefab");
            m_FXBoundsField = GetField("m_FXBounds");
        }

        protected override void OnStageOpened()
        {
            Reference<ObjectEntity> tempPrefab = GetValue<Reference<ObjectEntity>>(m_PrefabField);
            if (m_Prefab.IsEmpty() || !m_Prefab.Equals(tempPrefab))
            {
                if (m_PrefabInstance != null) DestroyImmediate(m_PrefabInstance);

                m_PrefabInstance = CreateObject(tempPrefab.GetObject());
                m_Prefab = tempPrefab;
            }
        }

        protected override void OnSceneGUI(SceneView obj)
        {
            ActorWeaponData.FXBounds[] fXBounds = GetValue<ActorWeaponData.FXBounds[]>(m_FXBoundsField);
            if (m_PreviewFXBounds.Length != fXBounds.Length)
            {
                m_PreviewFXBounds = new GameObject[fXBounds.Length];
            }

            for (int i = 0; i < fXBounds.Length; i++)
            {

            }
        }
    }
}
