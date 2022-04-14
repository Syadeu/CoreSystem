using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using SyadeuEditor.Utilities;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class EntityDebugWindow : EntityWindowMenuItem
    {
        public override int Order => 1;
        public override string Name => "Debugger";

        public override void OnIntialize(EntityWindow window)
        {
            TreeViewState = new TreeViewState();
            ListTreeView = new DebuggerListTreeView(window, TreeViewState);
        }

        private DebuggerListTreeView ListTreeView;
        private TreeViewState TreeViewState;

        public override void OnListGUI(Rect pos)
        {
            ListTreeView.OnGUI(pos);
        }
        public void Select(IEntityDataID instance)
        {
            ListTreeView.Select(instance);
        }

        Rect m_Position;
        Vector2 m_Scroll;
        private Entity<ObjectBase> m_Selected;
        private string m_SelectedName = string.Empty;
        private ObjectDrawerBase[] m_SelectedMembers = null;

        public Entity<ObjectBase> Selected
        {
            get => m_Selected;
            set
            {
                if (value.IsEmpty() || !value.IsValid())
                {
                    //$"1: {value.IsEmpty()} :: {value.IsValid()}".ToLog();
                    m_Selected = Entity<ObjectBase>.Empty;
                    m_SelectedName = string.Empty;
                    m_SelectedMembers = null;
                    return;
                }

                var entity = value.Target;
                m_SelectedName = entity.Name + EditorUtilities.String($": {entity.GetType().Name}", 11);

                MemberInfo[] temp = entity.GetType()
                    .GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where((other) =>
                    {
                        if (other.MemberType != MemberTypes.Field &&
                            other.MemberType != MemberTypes.Property) return false;

                        if (other.GetCustomAttribute<ObsoleteAttribute>() != null)
                        {
                            return false;
                        }

                        Type declaredType = ReflectionHelper.GetDeclaredType(other);

                        if (TypeHelper.TypeOf<Delegate>.Type.IsAssignableFrom(declaredType) ||
                            TypeHelper.TypeOf<IFixedReference>.Type.IsAssignableFrom(declaredType))
                        {
                            return false;
                        }

                        if (ReflectionHelper.IsBackingField(other)) return false;

                        return true;
                    })
                    .ToArray();
                m_SelectedMembers = new ObjectDrawerBase[temp.Length];
                for (int i = 0; i < temp.Length; i++)
                {
                    m_SelectedMembers[i] = ObjectDrawerBase.ToDrawer(entity, temp[i], true);
                }

                m_Selected = value;
            }
        }

        public override void OnViewGUI(Rect pos)
        {
            using (new GUI.GroupScope(pos))
            {
                Draw(pos);
            }
        }
        private void Draw(Rect pos)
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll, true, true,
                GUILayout.MaxWidth(pos.width), GUILayout.MaxHeight(pos.height)))
            using (new EditorUtilities.BoxBlock(Color.black))
            {
                if (!Application.isPlaying)
                {
                    EditorUtilities.StringRich("Debugger only works in runtime", true);
                    return;
                }

                if (m_Selected.IsEmpty())
                {
                    EditorUtilities.StringRich("Select Data", true);
                    return;
                }

                if (!m_Selected.IsValid())
                {
                    EditorUtilities.StringRich("This data has been destroyed", true);
                    return;
                }

                ObjectBase obj = m_Selected.Target;

                EditorUtilities.StringRich(m_SelectedName, 20);
                EditorGUILayout.Space(3);
                CoreGUI.Line();

                DrawDefaultInfomation(obj);

                if (obj is EntityDataBase entityDataBase)
                {
                    DrawEntity(entityDataBase);
                }

                CoreGUI.Line();

                for (int i = 0; i < m_SelectedMembers.Length; i++)
                {
                    if (m_SelectedMembers[i] is AttributeListDrawer ||
                        m_SelectedMembers[i].Name.Equals("Name") ||
                        m_SelectedMembers[i].Name.Equals("Hash") ||
                        m_SelectedMembers[i].Name.Equals("Idx") ||
                        m_SelectedMembers[i].Name.Equals("EnableCull") ||
                        m_SelectedMembers[i].Name.Equals("Prefab") ||
                        m_SelectedMembers[i].Name.Equals("Center") ||
                        m_SelectedMembers[i].Name.Equals("Size") ||
                        m_SelectedMembers[i].Name.Equals("transform"))
                    {
                        continue;
                    }
                    else if (m_SelectedMembers[i] is ArrayDrawer array)
                    {
                        if (TypeHelper.TypeOf<IFixedReference>.Type.IsAssignableFrom(array.ElementType)) continue;
                    }

                    m_SelectedMembers[i].OnGUI();
                }

                m_Scroll = scroll.scrollPosition;
            }
        }
        private void DrawDefaultInfomation(ObjectBase obj)
        {
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.TextField("Name: ", obj.Name);
                EditorGUILayout.TextField("Hash: ", obj.Hash.ToString());
                EditorGUILayout.TextField("Idx: ", obj.Idx.ToString());
            }
        }
        private void DrawEntity(EntityDataBase entity)
        {
            if (entity is EntityBase entityBase)
            {
                ProxyTransform proxy = entityBase.GetTransform();
                using (new EditorUtilities.BoxBlock(ColorPalettes.WaterFoam.Teal))
                {
                    EntityDrawer.DrawModel(entityBase, true);

                    if (proxy.hasProxy)
                    {
                        EditorGUILayout.ObjectField((UnityEngine.Object)proxy.proxy, TypeHelper.TypeOf<RecycleableMonobehaviour>.Type, true);
                    }

                    entityBase.Center
                        = EditorGUILayout.Vector3Field("Center", entityBase.Center);
                    entityBase.Size
                        = EditorGUILayout.Vector3Field("Size", entityBase.Size);
                }
                CoreGUI.Line();
                using (new EditorUtilities.BoxBlock(ColorPalettes.WaterFoam.Teal))
                {
                    EditorUtilities.StringRich("Transform", 15);
                    EditorGUI.indentLevel++;

                    proxy.position =
                        EditorGUILayout.Vector3Field("Position", proxy.position);

                    Vector3 eulerAngles = proxy.eulerAngles;

                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        eulerAngles = EditorGUILayout.Vector3Field("Rotation", eulerAngles);
                        if (change.changed)
                        {
                            proxy.eulerAngles = eulerAngles;
                        }
                    }

                    proxy.scale
                        = EditorGUILayout.Vector3Field("Scale", proxy.scale);

                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
