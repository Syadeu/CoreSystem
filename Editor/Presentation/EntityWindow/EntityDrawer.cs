using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public class EntityDrawer : ObjectBaseDrawer
    {
        public EntityDataBase Target => (EntityDataBase)m_TargetObject;

        GUIContent m_EnableCullName, m_DisableCullName;
        PrefabReferenceDrawer prefabReferenceDrawer = null;
        AttributeListDrawer attributeListDrawer;

        bool m_OpenCheckMesh = false;
        readonly List<MeshFilter> meshFilters = new List<MeshFilter>();

        public EntityDrawer(ObjectBase objectBase) : base(objectBase)
        {
            m_EnableCullName = new GUIContent("Enable Cull");
            m_DisableCullName = new GUIContent("Disable Cull");

            if (objectBase is EntityBase entityBase)
            {
                prefabReferenceDrawer = (PrefabReferenceDrawer)m_ObjectDrawers.Where((other) => other.Name.Equals("Prefab")).First();
                prefabReferenceDrawer.DisableHeader = true;

                if (!(entityBase.Prefab.GetEditorAsset() is GameObject))
                {
                    entityBase.Prefab = Syadeu.Database.PrefabReference<GameObject>.Invalid;
                }
                //if (!CheckMesh(entityBase)) m_OpenCheckMesh = true;
            }

            attributeListDrawer = new AttributeListDrawer(objectBase,
                TypeHelper.TypeOf<EntityDataBase>.Type.GetField("m_AttributeList", BindingFlags.NonPublic | BindingFlags.Instance));
        }

        protected void DrawHeader()
        {
            EditorUtils.StringRich(Name + EditorUtils.String($": {Type.Name}", 11), 20);
            EditorGUILayout.Space(3);
            EditorUtils.Line();

            DrawDescription();

            Target.Name = EditorGUILayout.TextField("Name: ", Target.Name);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Hash: ", Target.Hash.ToString());
            EditorGUI.EndDisabledGroup();
            if (Target is EntityBase entity)
            {
                using (new EditorUtils.BoxBlock(ColorPalettes.WaterFoam.Teal))
                {
                    EditorUtils.StringRich("Prefab", 15);

                    GUIContent enableCullName = entity.m_EnableCull ? m_DisableCullName : m_EnableCullName;
                    Rect enableCullRect = GUILayoutUtility.GetRect(
                        enableCullName,
                        EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
                    int enableCullID = GUIUtility.GetControlID(FocusType.Passive, enableCullRect);
                    switch (Event.current.GetTypeForControl(enableCullID))
                    {
                        case EventType.Repaint:
                            bool isHover = enableCullRect.Contains(Event.current.mousePosition);

                            Color origin = GUI.color;
                            GUI.color = entity.m_EnableCull ? ColorPalettes.PastelDreams.TiffanyBlue : ColorPalettes.PastelDreams.HotPink;

                            EditorStyles.toolbarButton.Draw(enableCullRect,
                                isHover, isActive: true, on: true, false);
                            GUI.color = origin;

                            var temp = new GUIStyle(EditorStyles.label);
                            temp.alignment = TextAnchor.MiddleCenter;
                            temp.Draw(enableCullRect, enableCullName, enableCullID);
                            break;
                        case EventType.MouseDown:
                            if (!enableCullRect.Contains(Event.current.mousePosition)) break;

                            if (Event.current.button == 0)
                            {
                                GUIUtility.hotControl = enableCullID;
                                entity.m_EnableCull = !entity.m_EnableCull;
                                Event.current.Use();
                            }

                            break;
                        case EventType.MouseUp:
                            if (GUIUtility.hotControl == enableCullID)
                            {
                                GUIUtility.hotControl = 0;
                            }
                            break;
                        default:
                            break;
                    }

                    DrawField(prefabReferenceDrawer);

                    //m_OpenCheckMesh = EditorUtils.Foldout(m_OpenCheckMesh, "Meshes");
                    //if (m_OpenCheckMesh)
                    //{
                    //    EditorGUI.indentLevel++;
                    //    CheckMesh(entity);
                    //    EditorGUI.indentLevel--;
                    //}
                }
            }
            using (new EditorUtils.BoxBlock(Color.black))
            {
                attributeListDrawer.OnGUI();
            }
        }
        protected bool CheckMesh(EntityBase entity)
        {
            if (!(entity.Prefab.GetEditorAsset() is GameObject obj))
            {
                return true;
            }

            bool green = true;
            meshFilters.Clear();
            obj.GetComponentsInChildren(true, meshFilters);
            for (int i = 0; i < meshFilters.Count; i++)
            {
                if (!meshFilters[i].sharedMesh.isReadable)
                {
                    //EditorGUILayout.ObjectField("Unreadable Mesh Found", meshFilters[i], TypeHelper.TypeOf<MeshFilter>.Type, false);
                    green = false;
                }
                //else
                //{
                //    EditorGUILayout.ObjectField("Mesh", meshFilters[i], TypeHelper.TypeOf<MeshFilter>.Type, false);
                //}
            }

            return green;
        }
        protected bool IsDrawable(ObjectDrawerBase drawerBase)
        {
            if (drawerBase == null)
            {
                EditorGUILayout.LabelField("null");
                return false;
            }

            if (drawerBase.Name.Equals("Name") ||
                drawerBase.Name.Equals("Hash") ||
                drawerBase.Name.Equals("Prefab")
                || drawerBase.Name.Equals("EnableCull")
                )
            {
                return false;
            }
            return true;
        }
        protected override void DrawGUI()
        {
            DrawHeader();
            EditorUtils.Line();

            for (int i = 0; i < m_ObjectDrawers.Length; i++)
            {
                if (!IsDrawable(m_ObjectDrawers[i]))
                {
                    continue;
                }

                DrawField(m_ObjectDrawers[i]);
            }
        }
    }
}
