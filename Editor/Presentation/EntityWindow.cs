using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class EntityWindow : EditorWindowEntity<EntityWindow>
    {
        protected override string DisplayName => "Entity Window";

        ObjectBaseDrawer[] ObjectBaseDrawers;
        protected override void OnEnable()
        {
            ObjectBaseDrawers = new ObjectBaseDrawer[EntityDataList.Instance.m_Objects.Count];

            var temp = EntityDataList.Instance.m_Objects.Values.ToArray();
            for (int i = 0; i < temp.Length; i++)
            {
                ObjectBaseDrawers[i] = new ObjectBaseDrawer(temp[i]);
            }

            base.OnEnable();
        }
        private void OnGUI()
        {
            if (GUILayout.Button("save"))
            {
                EntityDataList.Instance.SaveData();
            }

            EditorGUILayout.LabelField("test");
            for (int i = 0; i < ObjectBaseDrawers.Length; i++)
            {
                ObjectBaseDrawers[i].OnGUI();
            }
        }

        public sealed class ObjectBaseDrawer : ObjectDrawerBase
        {
            private readonly ObjectBase m_TargetObject;
            private Type m_Type;
            private ObsoleteAttribute m_Obsolete;

            private readonly MemberInfo[] m_Members;
            private readonly ObjectDrawerBase[] m_ObjectDrawers;

            public override object TargetObject => m_TargetObject;
            public override string Name => m_TargetObject.Name;

            public ObjectBaseDrawer(ObjectBase objectBase)
            {
                m_TargetObject = objectBase;
                m_Type = objectBase.GetType();
                m_Obsolete = m_Type.GetCustomAttribute<ObsoleteAttribute>();

                m_Members = ReflectionHelper.GetSerializeMemberInfos(m_Type);
                m_ObjectDrawers = new ObjectDrawerBase[m_Members.Length];
                for (int i = 0; i < m_ObjectDrawers.Length; i++)
                {
                    m_ObjectDrawers[i] = ToDrawer(m_TargetObject, m_Members[i]);
                }
            }
            public override void OnGUI()
            {
                EditorGUILayout.LabelField(Name);
                for (int i = 0; i < m_ObjectDrawers.Length; i++)
                {
                    if (m_ObjectDrawers[i] == null) continue;

                    try
                    {
                        m_ObjectDrawers[i].OnGUI();
                    }
                    catch (Exception)
                    {
                        EditorGUILayout.LabelField($"Error at {m_ObjectDrawers[i].Name}");
                    }
                }
                EditorUtils.Line();
            }

            private static ObjectDrawerBase ToDrawer(object parentObject, MemberInfo memberInfo)
            {
                Type declaredType = GetDeclaredType(memberInfo);

                #region Primitive Types
                if (declaredType.IsEnum)
                {
                    return new EnumDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<int>.Type))
                {
                    return new IntDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<bool>.Type))
                {
                    return new BoolenDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<float>.Type))
                {
                    return new FloatDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<double>.Type))
                {
                    return new DoubleDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<long>.Type))
                {
                    return new LongDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<string>.Type))
                {
                    return new StringDrawer(parentObject, memberInfo);
                }
                #endregion

                #region Unity Types
                if (declaredType.Equals(TypeHelper.TypeOf<Vector3>.Type))
                {
                    return new Vector3Drawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<float2>.Type))
                {
                    return new Float2Drawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<int2>.Type))
                {
                    return new Int2Drawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<float3>.Type))
                {
                    return new Float3Drawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<int3>.Type))
                {
                    return new Int3Drawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<quaternion>.Type))
                {
                    return new quaternionDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<Quaternion>.Type))
                {
                    return new QuaternionDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<Color>.Type))
                {
                    return new ColorDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<Color32>.Type))
                {
                    return new Color32Drawer(parentObject, memberInfo);
                }
                #endregion

                return null;
            }
        }
    }
}
