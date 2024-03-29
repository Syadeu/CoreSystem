﻿using Syadeu.Collections;
using Syadeu.Presentation;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using System;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    [Obsolete("", true)]
    public sealed class ActorWeaponPreviewScene : EntityPreviewScene<ActorWeaponData>
    {
        private FieldInfo 
            m_PrefabField, m_FXBoundsField, m_FXBoundsEntityField,
            
            m_FXBoundsEntityPosField, m_FXBoundsEntityRotField, m_FXBoundsEntityScaleField;

        private Reference<ObjectEntity> m_Prefab = Reference<ObjectEntity>.Empty;
        private GameObject m_PrefabInstance = null;

        GameObject[] m_PreviewFXBounds = Array.Empty<GameObject>();
        bool[] m_EnablePreviewFXBounds = Array.Empty<bool>();

        public bool IsActiveFXBounds(int index) => m_EnablePreviewFXBounds[index];
        public void SetActiveFXBounds(int index, bool enable)
        {
            m_EnablePreviewFXBounds[index] = enable;
            if (!enable)
            {
                DestroyImmediate(m_PreviewFXBounds[index]);
                m_PreviewFXBounds[index] = null;
            }

            OnSceneGUI(SceneView.lastActiveSceneView);

            if (enable)
            {
                Selection.activeGameObject = m_PreviewFXBounds[index];
                EditorGUIUtility.PingObject(m_PreviewFXBounds[index]);

                SceneView.lastActiveSceneView.LookAt(m_PreviewFXBounds[index].transform.position, Quaternion.AngleAxis(-90, Vector3.up), 3.5f);
            }
        }

        protected override void OnStageFirstTimeOpened()
        {
            m_PrefabField = GetField("m_Prefab");
            m_FXBoundsField = GetField("m_FXBounds");

            m_FXBoundsEntityField = GetField<FXBounds>("m_FXEntity");
            m_FXBoundsEntityPosField = GetField<FXBounds>("m_LocalPosition");
            m_FXBoundsEntityRotField = GetField<FXBounds>("m_LocalRotation");
            m_FXBoundsEntityScaleField = GetField<FXBounds>("m_LocalScale");
        }

        protected override void OnStageOpened()
        {
            Reference<ObjectEntity> tempPrefab = GetValue<Reference<ObjectEntity>>(m_PrefabField);
            if (m_Prefab.IsEmpty() || !m_Prefab.Equals(tempPrefab))
            {
                if (m_PrefabInstance != null) DestroyImmediate(m_PrefabInstance);

                m_PrefabInstance = CreateObject(tempPrefab.GetObject());
                m_PrefabInstance.transform.hideFlags = HideFlags.NotEditable;
                m_Prefab = tempPrefab;
            }

            CheckValidation();
        }
        private void CheckValidation()
        {
            FXBounds[] fXBounds = GetValue<FXBounds[]>(m_FXBoundsField);
            if (m_PreviewFXBounds.Length != fXBounds.Length)
            {
                foreach (var item in m_PreviewFXBounds)
                {
                    DestroyImmediate(item);
                }
                m_PreviewFXBounds = new GameObject[fXBounds.Length];
                m_EnablePreviewFXBounds = new bool[fXBounds.Length];
            }
        }
        protected override void OnSceneGUI(SceneView obj)
        {
            FXBounds[] fXBounds = GetValue<FXBounds[]>(m_FXBoundsField);
            CheckValidation();

            for (int i = 0; i < fXBounds.Length; i++)
            {
                if (fXBounds[i].FXEntity.IsEmpty()) continue;

                if (!fXBounds[i].FXEntity.IsValid())
                {
                    if (m_PreviewFXBounds[i] != null)
                    {
                        DestroyImmediate(m_PreviewFXBounds[i]);
                        m_PreviewFXBounds[i] = null;
                    }

                    m_FXBoundsEntityField.SetValue(fXBounds[i], Reference<FXEntity>.Empty);
                    continue;
                }

                if (!m_EnablePreviewFXBounds[i]) continue;

                FXEntity targetFx = fXBounds[i].FXEntity.GetObject();
                GameObject targetFxPrefab = (GameObject)targetFx.Prefab.GetEditorAsset();
                TRS currentTRS = fXBounds[i].TRS;

                if (m_PreviewFXBounds[i] == null)
                {
                    m_PreviewFXBounds[i] = CreateObject(targetFxPrefab, fXBounds[i].TRS);
                    m_PreviewFXBounds[i].name = targetFx.Name;
                }
                else if (!PrefabUtility.GetCorrespondingObjectFromSource(m_PreviewFXBounds[i]).Equals(targetFxPrefab))
                {
                    DestroyImmediate(m_PreviewFXBounds[i]);
                    m_PreviewFXBounds[i] = CreateObject(targetFxPrefab, fXBounds[i].TRS);
                    m_PreviewFXBounds[i].name = targetFx.Name;
                }

                Transform tr = m_PreviewFXBounds[i].transform;
                if (!tr.position.Equals(currentTRS.m_Position))
                {
                    m_FXBoundsEntityPosField.SetValue(fXBounds[i], (float3)tr.position);
                    EntityWindow.Instance.IsDirty = true;
                }
                if (!tr.rotation.Equals(currentTRS.m_Rotation))
                {
                    m_FXBoundsEntityRotField.SetValue(fXBounds[i], (float3)tr.eulerAngles);
                    EntityWindow.Instance.IsDirty = true;
                }
                if (!tr.localScale.Equals(currentTRS.m_Scale))
                {
                    m_FXBoundsEntityScaleField.SetValue(fXBounds[i], (float3)tr.localScale);
                    EntityWindow.Instance.IsDirty = true;
                }
                
                
            }
        }
    }
}
