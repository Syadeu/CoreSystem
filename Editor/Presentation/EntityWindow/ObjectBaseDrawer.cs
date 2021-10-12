using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static readonly Dictionary<ObjectBase, ObjectBaseDrawer> Pool = new Dictionary<ObjectBase, ObjectBaseDrawer>();

        public readonly ObjectBase m_TargetObject;
        private Type m_Type;
        private ObsoleteAttribute m_Obsolete;
        private ReflectionDescriptionAttribute m_Description;

        private readonly MemberInfo[] m_Members;
        private readonly ObjectDrawerBase[] m_ObjectDrawers;

        public override sealed object TargetObject => m_TargetObject;
        public Type Type => m_Type;
        public override string Name => m_TargetObject.Name;
        public override int FieldCount => m_ObjectDrawers.Length;
        public ObjectDrawerBase[] Drawers => m_ObjectDrawers;

        public static ObjectBaseDrawer GetDrawer(ObjectBase objectBase)
        {
            if (Pool.TryGetValue(objectBase, out var drawer)) return drawer;

            Type objType = objectBase.GetType();
            if (TypeHelper.TypeOf<Syadeu.Presentation.Map.MapDataEntityBase>.Type.IsAssignableFrom(objType))
            {
                drawer = new MapDataEntityDrawer(objectBase);
                Pool.Add(objectBase, drawer);
                return drawer;
            }

            Type[] drawerTypes = TypeHelper.GetTypes((other) => TypeHelper.TypeOf<ObjectBaseDrawer>.Type.IsAssignableFrom(other));
            var iter = drawerTypes.Where((other) =>
            {
                if (!other.IsAbstract &&
                    other.BaseType.GenericTypeArguments.Length > 0 &&
                    other.BaseType.GenericTypeArguments[0].IsAssignableFrom(objType))
                {
                    return true;
                }
                return false;
            });
            if (iter.Any())
            {
                var ctor = TypeHelper.GetConstructorInfo(iter.First(), TypeHelper.TypeOf<ObjectBase>.Type);

                if (ctor != null)
                {
                    drawer = (ObjectBaseDrawer)ctor.Invoke(new object[] { objectBase });
                    Pool.Add(objectBase, drawer);
                    return drawer;
                }
            }

            if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(objType))
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
            DrawHeader();
            DrawDescription();

            for (int i = 0; i < m_ObjectDrawers.Length; i++)
            {
                DrawField(m_ObjectDrawers[i]);
            }
        }
        protected void DrawHeader()
        {
            EditorUtils.StringRich(Name + EditorUtils.String($": {Type.Name}", 11), 20);
            EditorGUILayout.Space(3);
            EditorUtils.Line();
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

        protected bool IsObsolete(out ObsoleteAttribute obsolete)
        {
            obsolete = m_Obsolete;
            if (m_Obsolete == null) return false;
            return true;
        }
        protected MemberInfo GetMember(string name)
        {
            for (int i = 0; i < m_Members.Length; i++)
            {
                if (m_Members[i].Name.Equals(name)) return m_Members[i];
            }
            return null;
        }
        protected ObjectDrawerBase GetDrawer(string name)
        {
            for (int i = 0; i < m_ObjectDrawers.Length; i++)
            {
                if (m_ObjectDrawers[i].Name.Equals(name))
                {
                    return m_ObjectDrawers[i];
                }
            }
            return null;
        }
        protected T GetDrawer<T>(string name) where T : ObjectDrawerBase
            => (T)(GetDrawer(name));
    }

    public abstract class ObjectBaseDrawer<T> : ObjectBaseDrawer
        where T : ObjectBase
    {
        public new T TargetObject => (T)m_TargetObject;

        protected ObjectBaseDrawer(ObjectBase objectBase) : base(objectBase)
        {
        }

        public static FieldInfo GetField(string name, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return TypeHelper.TypeOf<T>.Type.GetField(name, bindingFlags);
        }
        public static PropertyInfo GetProperty(string name, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return TypeHelper.TypeOf<T>.Type.GetProperty(name, bindingFlags);
        }
        public TA GetValue<TA>(MemberInfo memberInfo)
        {
            object value;
            if (memberInfo is FieldInfo field) value = field.GetValue(TargetObject);
            else value = ((PropertyInfo)memberInfo).GetValue(TargetObject);

            return value == null ? default(TA) : (TA)value;
        }
    }
}
