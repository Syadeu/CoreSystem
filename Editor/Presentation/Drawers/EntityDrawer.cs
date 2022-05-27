using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Syadeu.Collections;
using Unity.Mathematics;
using SyadeuEditor.Utilities;
using Syadeu.Mono;
using Syadeu;

namespace SyadeuEditor.Presentation
{
    [System.Obsolete("Use Unity Serialized -> PropertyDrawer<T>", true)]
    public class EntityDrawer : ObjectBaseDrawer
    {
        public EntityDataBase Target => (EntityDataBase)m_TargetObject;

        //GUIContent m_EnableCullName, m_DisableCullName;
        PrefabReferenceDrawer prefabReferenceDrawer = null;
        AttributeListDrawer attributeListDrawer;

        bool 
            m_OpenAABB = false;
        ObjectDrawerBase
            m_CenterDrawer = null, m_SizeDrawer = null;

        bool m_OpenCheckMesh = false;
        private RenderHierarchy[] m_RenderHierachies = Array.Empty<RenderHierarchy>();

        class RenderHierarchy
        {
            public Mesh mesh;
            public Material[] materials;

            public RenderHierarchy(Renderer renderer)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null)
                {
                    if (renderer is SkinnedMeshRenderer skinned)
                    {
                        mesh = skinned.sharedMesh;
                    }
                    else
                    {
                        //throw new Exception("??");
                        //"??".ToLogError();

                        mesh = null;
                    }
                }
                else
                {
                    mesh = filter.sharedMesh;
                }
                
                materials = renderer.sharedMaterials;
            }
        }

        public EntityDrawer(ObjectBase objectBase) : base(objectBase)
        {
            //m_EnableCullName = new GUIContent("Enable Cull");
            //m_DisableCullName = new GUIContent("Disable Cull");

            if (objectBase is EntityBase entityBase)
            {
                prefabReferenceDrawer = GetDrawer<PrefabReferenceDrawer>("Prefab");
                prefabReferenceDrawer.DisableHeader = true;

                m_CenterDrawer = GetDrawer("Center");
                m_SizeDrawer = GetDrawer("Size");

                if (!entityBase.Prefab.IsNone() && entityBase.Prefab.IsValid())
                {
                    GameObject prefab = (GameObject)entityBase.Prefab.GetEditorAsset();

                    List<Renderer> renderers = new List<Renderer>();
                    prefab.GetComponentsInChildren(renderers);

                    m_RenderHierachies = new RenderHierarchy[renderers.Count];
                    for (int i = 0; i < renderers.Count; i++)
                    {
                        m_RenderHierachies[i] = new RenderHierarchy(renderers[i]);
                    }
                }
            }

            attributeListDrawer = new AttributeListDrawer(objectBase,
                TypeHelper.TypeOf<EntityDataBase>.Type.GetField("m_AttributeList", BindingFlags.NonPublic | BindingFlags.Instance));
        }
        //static bool CheckRendererAssets(Renderer[] renderers)
        //{
        //    foreach (var renderer in renderers)
        //    {
        //        for (int i = 0; i < renderer.materials.Length; i++)
        //        {
        //            var setting = PrefabList.Instance.GetSettingWithObject(renderer.materials[i]);
        //            if (setting != null)
        //            {
        //                return false;
        //            }
        //        }
        //    }
        //    return true;
        //}
        public static void DrawModel(EntityBase entity, bool disabled = false)
        {
            EntityDrawer baseDrawer = (EntityDrawer)GetDrawer(entity);
            var prefabReferenceDrawer = baseDrawer.GetDrawer<PrefabReferenceDrawer>("Prefab");
            prefabReferenceDrawer.DisableHeader = true;

            EditorUtilities.StringRich("Model", 15);

            using (new EditorGUILayout.HorizontalScope())
            {
                entity.SetCulling(CoreGUI.BoxToggleButton(
                    entity.EnableCull,
                    entity.EnableCull ? "Disable Cull" : "Enable Cull",
                    ColorPalettes.PastelDreams.TiffanyBlue,
                    ColorPalettes.PastelDreams.HotPink
                    ));
                entity.StaticBatching = CoreGUI.BoxToggleButton(
                    entity.StaticBatching,
                    "Static Batching",
                    ColorPalettes.PastelDreams.TiffanyBlue,
                    ColorPalettes.PastelDreams.HotPink,

                    GUILayout.Width(150)
                    );
            }
            CoreGUI.Line();
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                baseDrawer.DrawField(prefabReferenceDrawer);

                if (change.changed)
                {
                    if (!entity.Prefab.IsNone() && entity.Prefab.IsValid())
                    {
                        GameObject target = ((GameObject)entity.Prefab.GetEditorAsset());
                        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
                        if (renderers.Length > 0)
                        {
                            AABB aabb = renderers[0].bounds;
                            baseDrawer.m_RenderHierachies = new RenderHierarchy[renderers.Length];
                            for (int i = 1; i < renderers.Length; i++)
                            {
                                aabb.Encapsulate(renderers[i].bounds);
                                baseDrawer.m_RenderHierachies[i] = new RenderHierarchy(renderers[i]);
                            }
                            entity.Center = aabb.center - ((float3)target.transform.position);
                            entity.Size = aabb.size;
                        }
                    }
                }
            }

            CoreGUI.Line();
            using (new CoreGUI.BoxBlock(Color.black))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    baseDrawer.m_OpenAABB = EditorUtilities.Foldout(baseDrawer.m_OpenAABB, "AABB", 13);
                    using (new EditorGUI.DisabledGroupScope(entity.Prefab.IsNone() || !entity.Prefab.IsValid()))
                    {
                        if (GUILayout.Button("Auto", GUILayout.Width(60)))
                        {
                            GameObject target = ((GameObject)entity.Prefab.GetEditorAsset());
                            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

                            AABB aabb = new AABB(target.transform.position, 0);
                            foreach (var item in renderers)
                            {
                                aabb.Encapsulate(item.bounds);
                            }
                            entity.Center = aabb.center - ((float3)target.transform.position);
                            entity.Size = aabb.size;
                        }
                    }
                }

                if (baseDrawer.m_OpenAABB)
                {
                    EditorGUI.indentLevel++;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        baseDrawer.DrawField(baseDrawer.m_CenterDrawer);
                        baseDrawer.DrawField(baseDrawer.m_SizeDrawer);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            static void DrawRenderHierachies(RenderHierarchy[] renderHierarchies)
            {

            }
        }
        protected new void DrawHeader()
        {
            #region Default Information

            EditorUtilities.StringRich(Name + EditorUtilities.String($": {Type.Name}", 11), 20);
            EditorGUILayout.Space(3);
            CoreGUI.Line();

            DrawDescription();

            Target.Name = EditorGUILayout.TextField("Name: ", Target.Name);
            
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.TextField("Hash: ", Target.Hash.ToString());
            }

            #endregion

            if (Target is EntityBase entity)
            {
                using (new CoreGUI.BoxBlock(ColorPalettes.WaterFoam.Teal))
                {
                    DrawModel(entity);
                }
            }
            CoreGUI.Line();
            using (new CoreGUI.BoxBlock(Color.black))
            {
                attributeListDrawer.OnGUI();
            }
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
                drawerBase.Name.Equals("Prefab") || 
                drawerBase.Name.Equals("EnableCull") ||
                drawerBase.Name.Equals("Center") ||
                drawerBase.Name.Equals("Size") ||
                drawerBase.Name.Equals("StaticBatching")
                )
            {
                return false;
            }
            return true;
        }
        protected override void DrawGUI()
        {
            DrawHeader();
            CoreGUI.Line();

            for (int i = 0; i < Drawers.Length; i++)
            {
                if (!IsDrawable(Drawers[i]))
                {
                    continue;
                }

                DrawField(Drawers[i]);
            }
        }
    }
}
