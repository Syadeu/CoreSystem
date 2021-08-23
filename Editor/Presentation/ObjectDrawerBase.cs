using Syadeu;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
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

        public static ObjectDrawerBase ToDrawer(object parentObject, MemberInfo memberInfo, bool drawName)
        {
            Type declaredType = GetDeclaredType(memberInfo);

            if (declaredType.Equals(TypeHelper.TypeOf<ValuePairContainer>.Type))
            {
                return null;
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
            if (declaredType.Equals(TypeHelper.TypeOf<Vector3>.Type))
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

            Type[] drawerTypes = TypeHelper.GetTypes((other) => TypeHelper.TypeOf< ObjectDrawerBase>.Type.IsAssignableFrom(other));

            var iter = drawerTypes.Where((other) =>
            {
                if (!other.IsAbstract &&
                    other.BaseType.GenericTypeArguments.Length > 0 &&
                    other.BaseType.GenericTypeArguments[0].IsAssignableFrom(declaredType)) return true;
                return false;
            });
            if (iter.Any())
            {
                return (ObjectDrawerBase)TypeHelper.GetConstructorInfo(iter.First(), TypeHelper.TypeOf<object>.Type, TypeHelper.TypeOf<MemberInfo>.Type).Invoke(new object[] { parentObject, memberInfo });
            }

            return null;
        }
    }

    public sealed class PrefabReferenceDrawer : ObjectDrawer<PrefabReference>
    {
        public PrefabReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override PrefabReference Draw(PrefabReference currentValue)
        {
            ReflectionHelperEditor.DrawPrefabReference(Name, 
                (idx) =>
                {
                    Setter.Invoke(new PrefabReference(idx));
                }, 
                currentValue);
            return currentValue;
        }
    }
    public sealed class ReferenceDrawer : ObjectDrawer<IReference>
    {
        public ReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override IReference Draw(IReference currentValue)
        {
            Type targetType;
            Type[] generics = DeclaredType.GetGenericArguments();
            if (generics.Length > 0) targetType = DeclaredType.GetGenericArguments()[0];
            else targetType = null;

            ReflectionHelperEditor.DrawReferenceSelector(Name, (idx) =>
            {
                ObjectBase objBase = EntityDataList.Instance.GetObject(idx);

                Type makedT;
                if (targetType != null) makedT = typeof(Reference<>).MakeGenericType(targetType);
                else makedT = TypeHelper.TypeOf<Reference>.Type;

                object temp = TypeHelper.GetConstructorInfo(makedT, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                    new object[] { objBase });

                Setter.Invoke((IReference)temp);
            }, currentValue, targetType);

            return currentValue;
        }
    }
    public sealed class HashDrawer : ObjectDrawer<Hash>
    {
        public HashDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override Hash Draw(Hash currentValue)
        {
            long temp = EditorGUILayout.LongField(Name, long.Parse(currentValue.ToString()));
            return new Hash(ulong.Parse(temp.ToString()));
        }
    }
}
