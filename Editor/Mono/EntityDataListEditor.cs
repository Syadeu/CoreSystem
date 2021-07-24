using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
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
            //Asset.m_Entites = null;
            //Asset.m_Attributes = null;
            //EditorUtility.SetDirty(target);

            LuaEditor.Reload();
            //Asset.LoadData();

            //if (Asset.m_Attributes == null) m_AttributeNames = Array.Empty<string>();
            //else
            //{
            //    var temp = Asset.m_Attributes.Select((other) => other.Name).ToList();
            //    temp.Insert(0, "None");
            //    m_AttributeNames = temp.ToArray();
            //}
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
            //if (Asset.m_Entites != null) tempList.AddRange(Asset.m_Entites);
            //if (Asset.m_Attributes != null) tempList.AddRange(Asset.m_Attributes);
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
            //if (Asset.m_Entites != null) tempList.AddRange(Asset.m_Entites);
            //if (Asset.m_Attributes != null) tempList.AddRange(Asset.m_Attributes);
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
            readonly ObsoleteAttribute m_Deprecated = null;
            bool[] m_OpenAttributes = Array.Empty<bool>();

            public TreeObjectElement(VerticalTreeView treeView, EntityDataBase entity) : base(treeView, entity)
            {
                m_Type = entity.GetType();
                m_Deprecated = m_Type.GetCustomAttribute<ObsoleteAttribute>();
                m_Drawer = ReflectionHelperEditor.GetDrawer(entity, c_DefaultProperties);
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

                Target.Name = EditorGUILayout.TextField("Name: ", Target.Name);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Hash: ", Target.Hash.ToString());
                EditorGUI.EndDisabledGroup();

                Color color1 = Color.black, color2 = Color.black;
                color1.a = .5f;

                using (new EditorUtils.BoxBlock(color1))
                {
                    if (Target.Attributes == null) Target.Attributes = new List<Hash>();
                    EditorGUILayout.BeginHorizontal();
                    EditorUtils.StringRich("Attributes", 15);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        Target.Attributes.Add(Hash.Empty);
                        return;
                    }
                    if (Target.Attributes.Count > 0 && GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        Target.Attributes.RemoveAt(Target.Attributes.Count - 1);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel += 1;

                    using (new EditorUtils.BoxBlock(color2))
                    {
                        DrawAttributes();
                    }

                    EditorGUI.indentLevel -= 1;
                }

                EditorUtils.Line();

                EntityDataList.Instance.m_Objects[Target.Hash] = (ObjectBase)m_Drawer.OnGUI(false, true);
            }

            private void DrawAttributes()
            {
                for (int i = 0; i < Target.Attributes.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    int idx = i;
                    EditorGUI.BeginChangeCheck();
                    idx = EditorGUILayout.DelayedIntField(idx, GUILayout.Width(80));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (idx >= Target.Attributes.Count) idx = Target.Attributes.Count - 1;

                        Hash cache = Target.Attributes[i];
                        Target.Attributes.RemoveAt(i);
                        Target.Attributes.Insert(idx, cache);
                    }

                    ReflectionHelperEditor.DrawAttributeSelector(null, (attHash) => Target.Attributes[i] = attHash, Target.Attributes[i], m_Type);

                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        if (Target.Attributes.Count == 1)
                        {
                            Target.Attributes.Clear();
                            m_OpenAttributes = Array.Empty<bool>();
                            EditorGUILayout.EndHorizontal();
                            break;
                        }

                        var temp = m_OpenAttributes.ToList();
                        temp.RemoveAt(i);
                        m_OpenAttributes = temp.ToArray();
                        Target.Attributes.RemoveAt(i);
                        if (i != 0) i--;
                    }

                    m_OpenAttributes[i] = GUILayout.Toggle(m_OpenAttributes[i],
                        m_OpenAttributes[i] ? EditorUtils.FoldoutOpendString : EditorUtils.FoldoutClosedString
                        , EditorUtils.MiniButton, GUILayout.Width(20));
                    EditorGUILayout.EndHorizontal();

                    if (m_OpenAttributes[i])
                    {
                        Color color3 = Color.red;
                        color3.a = .7f;
                        AttributeBase targetAtt = GetSelectedAttribute(Target.Attributes[i]);

                        EditorGUI.indentLevel += 1;

                        using (new EditorUtils.BoxBlock(color3))
                        {
                            if (targetAtt == null)
                            {
                                EditorGUILayout.HelpBox(
                                    "This attribute is invalid.",
                                    MessageType.Error);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(
                                    "This is shared attribute. Anything made changes in this inspector view will affect to original attribute directly not only as this entity.",
                                    MessageType.Info);

                                SetAttribute(Target.Attributes[i], ReflectionHelperEditor.GetDrawer(targetAtt).OnGUI());
                            }
                        }

                        EditorGUI.indentLevel -= 1;
                    }

                    if (m_OpenAttributes[i]) EditorUtils.Line();
                }
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

                Target.Name = EditorGUILayout.TextField("Name: ", Target.Name);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Hash: ", Target.Hash.ToString());
                EditorGUI.EndDisabledGroup();
                ReflectionHelperEditor.DrawPrefabReference("Prefab: ", (idx) => Target.Prefab = idx, Target.Prefab);

                Color color1 = Color.black, color2 = Color.black;
                color1.a = .5f;

                using (new EditorUtils.BoxBlock(color1))
                {
                    if (Target.Attributes == null) Target.Attributes = new List<Hash>();
                    EditorGUILayout.BeginHorizontal();
                    EditorUtils.StringRich("Attributes", 15);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        Target.Attributes.Add(Hash.Empty);
                        return;
                    }
                    if (Target.Attributes.Count > 0 && GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        Target.Attributes.RemoveAt(Target.Attributes.Count - 1);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel += 1;

                    using (new EditorUtils.BoxBlock(color2))
                    {
                        DrawAttributes();
                    }

                    EditorGUI.indentLevel -= 1;
                }

                EditorUtils.Line();

                EntityDataList.Instance.m_Objects[Target.Hash] = (ObjectBase)m_Drawer.OnGUI(false, true);
            }
            private void DrawAttributes()
            {
                for (int i = 0; i < Target.Attributes.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    int idx = i;
                    EditorGUI.BeginChangeCheck();
                    idx = EditorGUILayout.DelayedIntField(idx, GUILayout.Width(80));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (idx >= Target.Attributes.Count) idx = Target.Attributes.Count - 1;

                        Hash cache = Target.Attributes[i];
                        Target.Attributes.RemoveAt(i);
                        Target.Attributes.Insert(idx, cache);
                    }

                    ReflectionHelperEditor.DrawAttributeSelector(null, (attHash) => Target.Attributes[i] = attHash, Target.Attributes[i], m_Type);

                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        if (Target.Attributes.Count == 1)
                        {
                            Target.Attributes.Clear();
                            m_OpenAttributes = Array.Empty<bool>();
                            EditorGUILayout.EndHorizontal();
                            break;
                        }

                        var temp = m_OpenAttributes.ToList();
                        temp.RemoveAt(i);
                        m_OpenAttributes = temp.ToArray();
                        Target.Attributes.RemoveAt(i);
                        if (i != 0) i--;
                    }

                    m_OpenAttributes[i] = GUILayout.Toggle(m_OpenAttributes[i],
                        m_OpenAttributes[i] ? EditorUtils.FoldoutOpendString : EditorUtils.FoldoutClosedString
                        , EditorUtils.MiniButton, GUILayout.Width(20));
                    EditorGUILayout.EndHorizontal();

                    if (m_OpenAttributes[i])
                    {
                        Color color3 = Color.red;
                        color3.a = .7f;
                        AttributeBase targetAtt = GetSelectedAttribute(Target.Attributes[i]);

                        EditorGUI.indentLevel += 1;

                        using (new EditorUtils.BoxBlock(color3))
                        {
                            if (targetAtt == null)
                            {
                                EditorGUILayout.HelpBox(
                                    "This attribute is invalid.",
                                    MessageType.Error);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(
                                    "This is shared attribute. Anything made changes in this inspector view will affect to original attribute directly not only as this entity.",
                                    MessageType.Info);

                                SetAttribute(Target.Attributes[i], ReflectionHelperEditor.GetDrawer(targetAtt).OnGUI());
                            }
                        }

                        EditorGUI.indentLevel -= 1;
                    }

                    if (m_OpenAttributes[i]) EditorUtils.Line();
                }
            }
        }
        private static AttributeBase GetSelectedAttribute(Hash attHash)
        {
            if (attHash.Equals(Hash.Empty)) return null;
            if (EntityDataList.Instance.m_Objects.TryGetValue(attHash, out ObjectBase val))
            {
                return (AttributeBase)val;
            }
            return null;
        }
        private static void SetAttribute(Hash attHash, object att)
        {
            if (attHash.Equals(Hash.Empty)) return;
            if (EntityDataList.Instance.m_Objects.ContainsKey(attHash))
            {
                EntityDataList.Instance.m_Objects[attHash] = (AttributeBase)att;
            }
            else EntityDataList.Instance.m_Objects.Add(attHash, (AttributeBase)att);
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
