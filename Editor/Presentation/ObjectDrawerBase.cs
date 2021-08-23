﻿using Syadeu.Internal;
using System;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public abstract class ObjectDrawerBase : IDisposable
    {
        public abstract object TargetObject { get; }
        public abstract string Name { get; }

        public abstract void OnGUI();
        public virtual void Dispose() { }

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

        public static ObjectDrawerBase ToDrawer(object parentObject, MemberInfo memberInfo)
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
