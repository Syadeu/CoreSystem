using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using SyadeuEditor.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(EntityDataList))]
    public sealed class EntityDataListEditor : EditorEntity<EntityDataList>
    {
        private VerticalTreeView treeView;

        private static string[] m_AttributeNames;

        private void OnEnable()
        {
            LuaEditor.Reload();
            if (Asset.m_Objects == null) m_AttributeNames = Array.Empty<string>();
            else
            {
                var temp = Asset
                    .GetAttributes()
                    .Select((other) => other.Name)
                    .ToList();
                temp.Insert(0, "None");
                m_AttributeNames = temp.ToArray();
            }

            List<object> tempList = new List<object>();
            if (Asset.m_Objects != null) tempList.AddRange(Asset.m_Objects.Values);

            treeView = new VerticalTreeView(Asset, serializedObject);
            treeView.OnDirty += RefreshTreeView;
            treeView
                .SetupElements(tempList, (other) =>
                {
                    VerticalTreeElement element;
                    VerticalFolderTreeElement folder;
                    if (other is EntityBase entity)
                    {
                        folder = treeView.GetOrCreateFolder<ObjectFolder>(entity.GetType().Name);
                        element = new TreeEntityElement(treeView, entity);
                    }
                    else if (other is AttributeBase attribute)
                    {
                        folder = treeView.GetOrCreateFolder<AttributeFolder>(attribute.GetType().Name);
                        element = new TreeAttributeElement(treeView, attribute);
                    }
                    else
                    {
                        EntityDataBase objBase = (EntityDataBase)other;
                        if (objBase.GetType().BaseType.Equals(TypeHelper.TypeOf<EntityDataBase>.Type))
                        {
                            folder = treeView.GetOrCreateFolder<ObjectFolder>(objBase.GetType().Name);
                        }
                        else folder = treeView.GetOrCreateFolder<ObjectFolder>(objBase.GetType().BaseType.Name);
                        element = new TreeObjectElement(treeView, objBase);
                    }

                    element.SetParent(folder);
                    return element;
                })
            #region Button
                .MakeAddButton(() =>
                {
                    if (treeView.SelectedToolbar == 0)
                    {
                        Type[] types = TypeHelper.GetTypes((other) => !other.IsAbstract && 
                            TypeHelper.TypeOf<ObjectBase>.Type.IsAssignableFrom(other) &&
                            !TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(other));

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.position = Event.current.mousePosition;

                        PopupWindow.Show(rect, SelectorPopup<Type, Type>.GetWindow(types, (t) =>
                        {
                            if (Asset.m_Objects == null) Asset.m_Objects = new Dictionary<Hash, ObjectBase>();

                            ObjectBase ins = (ObjectBase)Activator.CreateInstance(t);

                            Asset.m_Objects.Add(ins.Hash, ins);
                            RefreshTreeView();
                        },
                        (t) => t,
                        (t) =>
                        {
                            if (t.GetCustomAttribute<ObsoleteAttribute>() != null)
                            {
                                return $"[Deprecated] {t.Name}";
                            }
                            else return t.Name;
                        }));
                    }
                    else if (treeView.SelectedToolbar == 1)
                    {
                        Type[] types = TypeHelper.GetTypes((other) => !other.IsAbstract && TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(other));

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.position = Event.current.mousePosition;

                        PopupWindow.Show(rect, SelectorPopup<Type, Type>.GetWindow(types, (t) =>
                        {
                            if (Asset.m_Objects == null) Asset.m_Objects = new Dictionary<Hash, ObjectBase>();

                            AttributeBase ins = (AttributeBase)Activator.CreateInstance(t);

                            Asset.m_Objects.Add(ins.Hash, ins);
                            RefreshTreeView();
                        },
                        (t) => t,
                        (t) =>
                        {
                            if (t.GetCustomAttribute<ObsoleteAttribute>() != null)
                            {
                                return $"[Deprecated] {t.Name}";
                            }
                            else return t.Name;
                        }));
                    }

                    List<object> tempList = new List<object>();
                    if (Asset.m_Objects != null) tempList.AddRange(Asset.m_Objects.Values);
                    return tempList;
                })
                .MakeRemoveButton((other) =>
                {
                    ObjectBase target = (ObjectBase)other.TargetObject;

                    Asset.m_Objects.Remove(target.Hash);

                    List<object> tempList = new List<object>();
                    if (Asset.m_Objects != null) tempList.AddRange(Asset.m_Objects.Values);
                    return tempList;
                })
            #endregion
                .MakeToolbar("Entities", "Attributes")
                //.MakeCustomSearchFilter((element, txt) =>
                //{
                //    string targetName;
                //    string typeString;

                //    if (element is TreeEntityElement entityEle)
                //    {
                //        typeString = entityEle.Target.GetType().Name;

                //    }
                //    else if (element is TreeAttributeElement attEle)
                //    {
                //        typeString = attEle.Target.GetType().Name;

                //    }
                //    else
                //    {
                //        targetName = element.Name;
                //        typeString = element.Name;
                //    }

                //    return true;
                //})
                ;
        }
        private void RefreshTreeView()
        {
            if (Asset.m_Objects == null) m_AttributeNames = Array.Empty<string>();
            else
            {
                var temp = Asset
                    .GetAttributes()
                    .Select((other) => other.Name)
                    .ToList();
                temp.Insert(0, "None");
                m_AttributeNames = temp.ToArray();
            }

            List<object> tempList = new List<object>();
            if (Asset.m_Objects != null) tempList.AddRange(Asset.m_Objects.Values);
            treeView.Refresh(tempList);
        }
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Clear"))
            {
                //Asset.m_Entites?.Clear();
                //Asset.m_Attributes?.Clear();
                Asset.Purge();
                EditorUtils.SetDirty(Asset);
                RefreshTreeView();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load"))
            {
                Asset.LoadData();
                EditorUtils.SetDirty(Asset);
                RefreshTreeView();
            }
            if (GUILayout.Button("Save"))
            {
                EditorUtils.SetDirty(Asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Asset.SaveData();

                RefreshTreeView();
            }
            EditorGUILayout.EndHorizontal();
            EditorUtils.SectorLine();
            EditorGUILayout.Space();

            if (treeView == null) OnEnable();
            treeView.OnGUI();

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        private class ObjectFolder : VerticalFolderTreeElement
        {
            public override bool HideElementInTree => Tree.SelectedToolbar != 0 || base.HideElementInTree;

            public ObjectFolder() { }
            public ObjectFolder(VerticalTreeViewEntity tree, string name) : base(tree, name)
            {
                m_Name = name;
            }
        }
        private class AttributeFolder : VerticalFolderTreeElement
        {
            public override bool HideElementInTree => Tree.SelectedToolbar != 1 || base.HideElementInTree;

            public AttributeFolder() { }
            public AttributeFolder(VerticalTreeViewEntity tree, string name) : base(tree, name)
            {
                m_Name = name;
            }
        }
        private class TreeObjectElement : VerticalTreeElement<EntityDataBase>
        {
            static string[] c_DefaultProperties = new string[] { "Name", "Hash", "Attributes" };
            const string c_EntityObsoleteWarning = "This object type has been marked as deprecated.";

            public override string Name
            {
                get
                {
                    if (m_Deprecated != null)
                    {
                        return $"[Deprecated] {Target.Name}";
                    }
                    return Target.Name;
                }
            }
            public override bool HideElementInTree
                => Tree.SelectedToolbar != 0 || base.HideElementInTree;

            readonly Type m_Type;
            readonly ReflectionHelperEditor.Drawer m_Drawer;
            readonly ReflectionHelperEditor.AttributeListDrawer m_AttributeDrawer;
            readonly ObsoleteAttribute m_Deprecated = null;
            bool[] m_OpenAttributes = Array.Empty<bool>();

            public TreeObjectElement(VerticalTreeView treeView, EntityDataBase entity) : base(treeView, entity)
            {
                m_Type = entity.GetType();
                m_Deprecated = m_Type.GetCustomAttribute<ObsoleteAttribute>();
                m_Drawer = ReflectionHelperEditor.GetDrawer(entity, c_DefaultProperties);
                if (Target.Attributes == null) Target.Attributes = new List<Hash>();
                m_AttributeDrawer = ReflectionHelperEditor.GetAttributeDrawer(m_Type, Target.Attributes);
            }
            public override void OnGUI()
            {
                if (Target.Attributes == null) m_OpenAttributes = Array.Empty<bool>();
                else if (m_OpenAttributes.Length != Target.Attributes.Count)
                {
                    m_OpenAttributes = new bool[Target.Attributes.Count];
                }

                if (m_Deprecated != null)
                {
                    EditorGUILayout.HelpBox(c_EntityObsoleteWarning, m_Deprecated.IsError ? MessageType.Error : MessageType.Warning);
                }
                EditorUtils.StringRich(m_Type.Name, 15);

                //EditorGUILayout.HelpBox("제발 한글 쓰지마라", MessageType.Warning);
                Target.Name = EditorGUILayout.TextField("Name: ", Target.Name);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Hash: ", Target.Hash.ToString());
                EditorGUI.EndDisabledGroup();

                Color color1 = Color.black;
                color1.a = .5f;

                using (new EditorUtils.BoxBlock(color1))
                {
                    m_AttributeDrawer.OnGUI();
                }

                EditorUtils.Line();

                EntityDataList.Instance.m_Objects[Target.Hash] = (ObjectBase)m_Drawer.OnGUI(false, true);
            }
        }
        private class TreeEntityElement : VerticalTreeElement<EntityBase>
        {
            static string[] c_DefaultProperties = new string[] { "Name", "Hash", "Prefab", "Attributes" };
            const string c_EntityObsoleteWarning = "This entity type has been marked as deprecated.";

            public override bool HideElementInTree
                => Tree.SelectedToolbar != 0 || base.HideElementInTree;

            readonly Type m_Type = null;
            readonly ReflectionHelperEditor.Drawer m_Drawer;
            readonly ReflectionHelperEditor.AttributeListDrawer m_AttributeDrawer;
            readonly ObsoleteAttribute m_Deprecated = null;
            bool[] m_OpenAttributes = Array.Empty<bool>();

            public TreeEntityElement(VerticalTreeView treeView, EntityBase entity) : base(treeView, entity)
            {
                m_Deprecated = entity.GetType().GetCustomAttribute<ObsoleteAttribute>();
                if (m_Deprecated != null)
                {
                    m_Name = $"[Deprecated] {Target.Name}";
                }
                else m_Name = Target.Name;
                m_Drawer = ReflectionHelperEditor.GetDrawer(entity, c_DefaultProperties);

                m_Type = Target.GetType();

                m_AttributeDrawer = ReflectionHelperEditor.GetAttributeDrawer(m_Type, Target.Attributes);
            }
            public override void OnGUI()
            {
                if (Target.Attributes == null) m_OpenAttributes = Array.Empty<bool>();
                else if (m_OpenAttributes.Length != Target.Attributes.Count)
                {
                    m_OpenAttributes = new bool[Target.Attributes.Count];
                }

                if (m_Deprecated != null)
                {
                    EditorGUILayout.HelpBox(c_EntityObsoleteWarning, m_Deprecated.IsError ? MessageType.Error : MessageType.Warning);
                }
                EditorUtils.StringRich(m_Type.Name, 15);

                //EditorGUILayout.HelpBox("제발 한글 쓰지마라", MessageType.Warning);
                Target.Name = EditorGUILayout.TextField("Name: ", Target.Name);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Hash: ", Target.Hash.ToString());
                EditorGUI.EndDisabledGroup();
                ReflectionHelperEditor.DrawPrefabReference("Prefab: ", (idx) => Target.Prefab = idx, Target.Prefab);

                Color color1 = Color.black, color2 = Color.black;
                color1.a = .5f;

                using (new EditorUtils.BoxBlock(color1))
                {
                    m_AttributeDrawer.OnGUI();
                }

                EditorUtils.Line();

                EntityDataList.Instance.m_Objects[Target.Hash] = (ObjectBase)m_Drawer.OnGUI(false, true);
            }
        }
        private class TreeAttributeElement : VerticalTreeElement<AttributeBase>
        {
            static string[] c_DefaultProperties = new string[] { "Name", "Hash" };

            public override string Name
            {
                get
                {
                    if (obsolete != null)
                    {
                        return $"[Deprecated] {Target.Name}";
                    }
                    else return Target.Name;
                }
            }
            public override bool HideElementInTree
                => Tree.SelectedToolbar != 1 || base.HideElementInTree;

            ObsoleteAttribute obsolete;
            ReflectionHelperEditor.Drawer m_Drawer;

            public TreeAttributeElement(VerticalTreeView treeView, AttributeBase att) : base(treeView, att)
            {
                obsolete = att.GetType().GetCustomAttribute<ObsoleteAttribute>();
                //if (obsolete != null)
                //{
                //    m_Name = $"[Deprecated] {Target.Name}";
                //}
                //else m_Name = Target.Name;
                m_Drawer = ReflectionHelperEditor.GetDrawer(att);

            }
            public override void OnGUI()
            {
                Color temp = Color.black; temp.a = .5f;
                using (new EditorUtils.BoxBlock(temp))
                {
                    EntityDataList.Instance.m_Objects[Target.Hash] = (ObjectBase)m_Drawer.OnGUI();
                }
            }
        }
    }
}
