using Syadeu.Collections;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.ComponentModel;

using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    [Obsolete("Use Unity Serialized -> PropertyDrawer<T>", true)]
    public abstract class ObjectDrawerBase : IDisposable
    {
        public abstract object TargetObject { get; }
        public abstract string Name { get; }
        public virtual int FieldCount => 1;

        public abstract void OnGUI();
        public virtual void Dispose() { }

        protected void DrawSystemAttribute(in Attribute attribute)
        {
            if (attribute is SpaceAttribute)
            {
                EditorGUILayout.Space();
            }
            else if (attribute is TooltipAttribute tooltip)
            {
                EditorGUILayout.HelpBox(tooltip.tooltip, MessageType.Info);
            }
            else if (attribute is DescriptionAttribute description)
            {
                EditorGUILayout.HelpBox(description.Description, MessageType.Info);
            }
            else if (attribute is HeaderAttribute header)
            {
                CoreGUI.Line();
                EditorUtilities.StringRich(header.header, 15);
            }
        }

        public static Type GetDeclaredType(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo field)
            {
                return field.FieldType;
            }
            else if (memberInfo is PropertyInfo property)
            {
                return property.PropertyType;
            }
            return null;
        }

        public static ObjectDrawerBase ToDrawer(object parentObject, MemberInfo memberInfo, bool drawName)
        {
            Type declaredType = GetDeclaredType(memberInfo);

            //if (declaredType.Equals(TypeHelper.TypeOf<ValuePairContainer>.Type))
            //{
            //    return new ValuePairContainerDrawer(parentObject, memberInfo);
            //}

            Type[] drawerTypes = TypeHelper.GetTypes(other => TypeHelper.TypeOf<ObjectDrawerBase>.Type.IsAssignableFrom(other));
            var iter = drawerTypes.Where((other) =>
            {
                if (!other.IsAbstract &&
                    other.BaseType.GenericTypeArguments.Length > 0 &&
                    other.BaseType.GenericTypeArguments[0].IsAssignableFrom(declaredType)) return true;
                return false;
            });
            if (iter.Any())
            {
                var ctor = TypeHelper.GetConstructorInfo(iter.First(), TypeHelper.TypeOf<object>.Type, TypeHelper.TypeOf<MemberInfo>.Type, TypeHelper.TypeOf<bool>.Type);

                if (ctor != null)
                {
                    return (ObjectDrawerBase)ctor.Invoke(new object[] { parentObject, memberInfo, drawName });
                }

                ctor = TypeHelper.GetConstructorInfo(iter.First(), TypeHelper.TypeOf<object>.Type, TypeHelper.TypeOf<MemberInfo>.Type);

                if (ctor != null)
                {
                    return (ObjectDrawerBase)ctor.Invoke(new object[] { parentObject, memberInfo });
                }
            }

            if (TypeHelper.TypeOf<PropertyBlockBase>.Type.IsAssignableFrom(declaredType))
            {
                return new ObjectDrawer(parentObject, memberInfo, true);
            }

            #region Primitive Types
            if (declaredType.IsEnum)
            {
                return new EnumDrawer(parentObject, memberInfo);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<int>.Type))
            {
                return new IntDrawer(parentObject, memberInfo, drawName);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<bool>.Type))
            {
                return new BoolenDrawer(parentObject, memberInfo, drawName);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<float>.Type))
            {
                return new FloatDrawer(parentObject, memberInfo, drawName);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<double>.Type))
            {
                return new DoubleDrawer(parentObject, memberInfo, drawName);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<long>.Type))
            {
                return new LongDrawer(parentObject, memberInfo);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<string>.Type))
            {
                return new StringDrawer(parentObject, memberInfo);
            }
            else if (declaredType.IsArray || typeof(IList).IsAssignableFrom(declaredType))
            {
                return new ArrayDrawer(parentObject, memberInfo);
            }
            #endregion

            #region Unity Types
            if (declaredType.Equals(TypeHelper.TypeOf<Vector4>.Type))
            {
                return new Vector4Drawer(parentObject, memberInfo);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<float4>.Type))
            {
                return new Float4Drawer(parentObject, memberInfo, drawName);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Vector3>.Type))
            {
                return new Vector3Drawer(parentObject, memberInfo);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<float3>.Type))
            {
                return new Float3Drawer(parentObject, memberInfo, drawName);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Vector2>.Type))
            {
                return new Vector2Drawer(parentObject, memberInfo);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<float2>.Type))
            {
                return new Float2Drawer(parentObject, memberInfo, drawName);
            }

            else if (declaredType.Equals(TypeHelper.TypeOf<Vector3Int>.Type))
            {
                return new Vector3IntDrawer(parentObject, memberInfo);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<int3>.Type))
            {
                return new Int3Drawer(parentObject, memberInfo, drawName);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Vector2Int>.Type))
            {
                return new Vector2IntDrawer(parentObject, memberInfo);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<int2>.Type))
            {
                return new Int2Drawer(parentObject, memberInfo, drawName);
            }

            else if (declaredType.Equals(TypeHelper.TypeOf<Quaternion>.Type))
            {
                return new QuaternionDrawer(parentObject, memberInfo);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<quaternion>.Type))
            {
                return new quaternionDrawer(parentObject, memberInfo, drawName);
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

            //return new NullDrawer(memberInfo, declaredType);
            throw new Exception();
        }
    }
}
