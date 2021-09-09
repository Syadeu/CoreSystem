using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    /// <summary>
    /// <see cref="ObjectBase"/>
    /// </summary>
    public class ObjectBaseDrawer : ObjectDrawerBase
    {
        protected static readonly Dictionary<ObjectBase, ObjectBaseDrawer> Pool = new Dictionary<ObjectBase, ObjectBaseDrawer>();

        public readonly ObjectBase m_TargetObject;
        private Type m_Type;
        private ObsoleteAttribute m_Obsolete;
        private ReflectionDescriptionAttribute m_Description;

        private readonly MemberInfo[] m_Members;
        protected readonly ObjectDrawerBase[] m_ObjectDrawers;

        public override sealed object TargetObject => m_TargetObject;
        public Type Type => m_Type;
        public override string Name => m_TargetObject.Name;
        public override int FieldCount => m_ObjectDrawers.Length;

        public static ObjectBaseDrawer GetDrawer(ObjectBase objectBase)
        {
            if (Pool.TryGetValue(objectBase, out var drawer)) return drawer;

            if (TypeHelper.TypeOf<Syadeu.Presentation.Map.MapDataEntityBase>.Type.IsAssignableFrom(objectBase.GetType()))
            {
                drawer = new MapDataEntityDrawer(objectBase);
            }
            else if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(objectBase.GetType()))
            {
                drawer = new EntityDrawer(objectBase);
            }
            else drawer = new ObjectBaseDrawer(objectBase);

            Pool.Add(objectBase, drawer);

            return drawer;
        }

        protected ObjectBaseDrawer(ObjectBase objectBase)
        {
            m_TargetObject = objectBase;
            m_Type = objectBase.GetType();
            m_Obsolete = m_Type.GetCustomAttribute<ObsoleteAttribute>();
            m_Description = m_Type.GetCustomAttribute<ReflectionDescriptionAttribute>();

            m_Members = ReflectionHelper.GetSerializeMemberInfos(m_Type);
            m_ObjectDrawers = new ObjectDrawerBase[m_Members.Length];
            for (int i = 0; i < m_ObjectDrawers.Length; i++)
            {
                m_ObjectDrawers[i] = ToDrawer(m_TargetObject, m_Members[i], true);
            }
        }
        public override sealed void OnGUI()
        {
            const string c_ObsoleteMsg = "This type marked as deprecated.\n{0}";

            using (new EditorUtils.BoxBlock(Color.black))
            {
                if (m_Obsolete != null)
                {
                    EditorGUILayout.HelpBox(string.Format(c_ObsoleteMsg, m_Obsolete.Message),
                        m_Obsolete.IsError ? MessageType.Error : MessageType.Warning);
                }

                DrawGUI();
            }
        }
        protected virtual void DrawGUI()
        {
            EditorUtils.StringRich(Name + EditorUtils.String($": {Type.Name}", 11), 20);
            EditorGUILayout.Space(3);
            EditorUtils.Line();

            DrawDescription();

            for (int i = 0; i < m_ObjectDrawers.Length; i++)
            {
                DrawField(m_ObjectDrawers[i]);
            }
        }
        protected void DrawField(ObjectDrawerBase drawer)
        {
            if (drawer == null)
            {
                EditorGUILayout.LabelField($"not support");
                return;
            }
            try
            {
                drawer.OnGUI();
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField($"Error at {drawer.Name} {ex.Message}");
                Debug.LogException(ex);
            }
        }
        protected void DrawDescription()
        {
            if (m_Description == null) return;

            EditorGUILayout.HelpBox(m_Description.m_Description, MessageType.Info);
        }
    }
    //[EditorTool("TestTool", typeof(EntityWindow))]
    //public sealed class TestTool : EditorTool
    //{
    //    public override void OnToolGUI(EditorWindow window)
    //    {
    //        EditorGUILayout.LabelField("test");
    //        base.OnToolGUI(window);
    //    }
    //}
}
