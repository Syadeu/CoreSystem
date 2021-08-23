using Syadeu;
using Syadeu.Database;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SyadeuEditor.Presentation
{
    public sealed class EnumDrawer : ObjectDrawer<Enum>
    {
        public EnumDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override Enum Draw(Enum currentValue)
        {
            if (DelaredType.GetCustomAttribute<FlagsAttribute>() != null)
            {
                return EditorGUILayout.EnumFlagsField(Name, currentValue);
            }
            return EditorGUILayout.EnumPopup(Name, currentValue);
        }
    }
    public sealed class IntDrawer : ObjectDrawer<int>
    {
        public IntDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override int Draw(int currentValue)
        {
            return EditorGUILayout.IntField(Name, currentValue);
        }
    }
    public sealed class BoolenDrawer : ObjectDrawer<bool>
    {
        public BoolenDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override bool Draw(bool currentValue)
        {
            return EditorGUILayout.Toggle(Name, currentValue);
        }
    }
    public sealed class FloatDrawer : ObjectDrawer<float>
    {
        public FloatDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override float Draw(float currentValue)
        {
            return EditorGUILayout.FloatField(Name, currentValue);
        }
    }
    public sealed class DoubleDrawer : ObjectDrawer<double>
    {
        public DoubleDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override double Draw(double currentValue)
        {
            return EditorGUILayout.DoubleField(Name, currentValue);
        }
    }
    public sealed class LongDrawer : ObjectDrawer<long>
    {
        public LongDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override long Draw(long currentValue)
        {
            return EditorGUILayout.LongField(Name, currentValue);
        }
    }
    public sealed class StringDrawer : ObjectDrawer<string>
    {
        public StringDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override string Draw(string currentValue)
        {
            return EditorGUILayout.TextField(Name, currentValue);
        }
    }

    public sealed class ArrayDrawer : ObjectDrawer<IList>
    {
        private Type m_DeclaredType;
        private Type m_ElementType;

        private Color
            color1, color2, color3;

        public List<ObjectDrawerBase> m_ElementDrawers = new List<ObjectDrawerBase>();

        public ArrayDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
            m_DeclaredType = GetDeclaredType(memberInfo);
            if (m_DeclaredType.IsArray)
            {
                m_ElementType = m_DeclaredType.GetElementType();
            }
            else
            {
                try
                {
                    m_ElementType = m_DeclaredType.GetGenericArguments()[0];
                }
                catch (Exception)
                {
                    $"{m_DeclaredType.Name}".ToLog();
                    throw;
                }
            }

            color1 = Color.black; color2 = Color.gray; color3 = Color.green;
            color1.a = .5f; color2.a = .5f; color3.a = .5f;
        }
        public override IList Draw(IList list)
        {
            if (list == null) list = (IList)Activator.CreateInstance(m_DeclaredType);

            #region Header
            EditorGUILayout.BeginHorizontal();
            EditorUtils.StringRich(Name, 13);
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                if (list.IsFixedSize)
                {
                    Array newArr = Array.CreateInstance(m_ElementType, list.Count + 1);
                    if (list != null && list.Count > 0) Array.Copy((Array)list, newArr, list.Count);
                    list = newArr;
                }
                else
                {
                    list.Add(Activator.CreateInstance(m_ElementType));
                }
            }
            if (list.Count > 0 && GUILayout.Button("-", GUILayout.Width(20)))
            {
                if (list.IsFixedSize)
                {
                    Array newArr = Array.CreateInstance(m_ElementType, list.Count - 1);
                    if (list != null && list.Count > 0) Array.Copy((Array)list, newArr, newArr.Length);
                    list = newArr;
                }
                else
                {
                    list.RemoveAt(list.Count - 1);
                }
            }
            EditorGUILayout.EndHorizontal();
            #endregion

            for (int j = 0; j < list.Count; j++)
            {
                EditorGUI.indentLevel++;

                GUILayout.BeginHorizontal(EditorUtils.Box);
                using (new EditorUtils.BoxBlock(j % 2 == 0 ? color1 : color2))
                {
                    if (list[j] == null)
                    {
                        if (m_ElementType.Equals(TypeHelper.TypeOf<string>.Type))
                        {
                            list[j] = string.Empty;
                        }
                        else
                        {
                            list[j] = Activator.CreateInstance(m_ElementType);
                        }
                    }

                    #region CoreSystem Types
                    if (TypeHelper.TypeOf<IReference>.Type.IsAssignableFrom(m_ElementType))
                    {
                        IReference objRef = (IReference)list[j];
                        Type targetType;
                        Type[] generics = m_ElementType.GetGenericArguments();
                        if (generics.Length > 0) targetType = m_ElementType.GetGenericArguments()[0];
                        else targetType = null;

                        ReflectionHelperEditor.DrawReferenceSelector(string.Empty, (idx) =>
                        {
                            ObjectBase objBase = EntityDataList.Instance.GetObject(idx);

                            Type makedT;
                            if (targetType != null) makedT = typeof(Reference<>).MakeGenericType(targetType);
                            else makedT = TypeHelper.TypeOf<Reference>.Type;

                            object temp = TypeHelper.GetConstructorInfo(makedT, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                                new object[] { objBase });

                            list[j] = temp;
                        }, objRef, targetType);
                    }
                    else if (m_ElementType.Equals(TypeHelper.TypeOf<LuaScript>.Type))
                    {
                        LuaScript scr = (LuaScript)list[j];
                        if (scr == null)
                        {
                            scr = string.Empty;
                            list[j] = scr;
                        }
                        scr.DrawFunctionSelector(string.Empty);
                    }
                    #endregion
                    #region Unity Types
                    else if (ReflectionHelperEditor.DrawUnityField(list[j], m_ElementType, string.Empty, (other) => list[j], out object value))
                    {
                        list[j] = value;
                    }
                    else if (ReflectionHelperEditor.DrawUnityMathField(list[j], m_ElementType, string.Empty, (other) => list[j], out value))
                    {
                        list[j] = value;
                    }
                    else if (m_ElementType.Equals(TypeHelper.TypeOf<AssetReference>.Type))
                    {
                        AssetReference refAsset = (AssetReference)list[j];
                        ReflectionHelperEditor.DrawAssetReference(string.Empty, (other) => list[j] = other, refAsset);
                    }
                    #endregion
                    else if (ReflectionHelperEditor.DrawSystemField(list[j], m_ElementType, string.Empty, (other) => list[j], out value))
                    {
                        list[j] = value;
                    }
                    else
                        list[j] = ReflectionHelperEditor.DrawObject(list[j]);
                }
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    if (list.IsFixedSize)
                    {
                        IList newArr = Array.CreateInstance(m_ElementType, list.Count - 1);
                        if (list != null && list.Count > 0)
                        {
                            for (int a = 0, b = 0; a < list.Count; a++)
                            {
                                if (a.Equals(j)) continue;

                                newArr[b] = list[a];

                                b++;
                            }
                        }
                        list = newArr;
                    }
                    else list.RemoveAt(j);

                    j--;
                }
                GUILayout.EndHorizontal();

                if (j + 1 < list.Count) EditorUtils.Line();
                EditorGUI.indentLevel--;
            }

            return list;
        }
    }
}
