﻿using Syadeu;
using Syadeu.Database;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
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

        public EnumDrawer(object parentObject, Type declaredType, Action<Enum> setter, Func<Enum> getter) : base(parentObject, declaredType, setter, getter)
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
        private bool m_DrawName;

        public IntDrawer(object parentObject, MemberInfo memberInfo, bool drawName) : base(parentObject, memberInfo)
        {
            m_DrawName = drawName;
        }

        public IntDrawer(object parentObject, Type declaredType, Action<int> setter, Func<int> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override int Draw(int currentValue)
        {
            if (m_DrawName) return EditorGUILayout.IntField(Name, currentValue);
            return EditorGUILayout.IntField(currentValue);
        }
    }
    public sealed class BoolenDrawer : ObjectDrawer<bool>
    {
        private bool m_DrawName;

        public BoolenDrawer(object parentObject, MemberInfo memberInfo, bool drawName) : base(parentObject, memberInfo)
        {
            m_DrawName = drawName;
        }

        public BoolenDrawer(object parentObject, Type declaredType, Action<bool> setter, Func<bool> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override bool Draw(bool currentValue)
        {
            if (m_DrawName) return EditorGUILayout.Toggle(Name, currentValue);
            return EditorGUILayout.Toggle(currentValue);
        }
    }
    public sealed class FloatDrawer : ObjectDrawer<float>
    {
        private bool m_DrawName;

        public FloatDrawer(object parentObject, MemberInfo memberInfo, bool drawName) : base(parentObject, memberInfo)
        {
            m_DrawName = drawName;
        }

        public FloatDrawer(object parentObject, Type declaredType, Action<float> setter, Func<float> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override float Draw(float currentValue)
        {
            if (m_DrawName) return EditorGUILayout.FloatField(Name, currentValue);
            return EditorGUILayout.FloatField(currentValue);
        }
    }
    public sealed class DoubleDrawer : ObjectDrawer<double>
    {
        private bool m_DrawName;

        public DoubleDrawer(object parentObject, MemberInfo memberInfo, bool drawName) : base(parentObject, memberInfo)
        {
            m_DrawName = drawName;
        }

        public DoubleDrawer(object parentObject, Type declaredType, Action<double> setter, Func<double> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override double Draw(double currentValue)
        {
            if (m_DrawName) return EditorGUILayout.DoubleField(Name, currentValue);
            return EditorGUILayout.DoubleField(currentValue);
        }
    }
    public sealed class LongDrawer : ObjectDrawer<long>
    {
        public LongDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }

        public LongDrawer(object parentObject, Type declaredType, Action<long> setter, Func<long> getter) : base(parentObject, declaredType, setter, getter)
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

        public StringDrawer(object parentObject, Type declaredType, Action<string> setter, Func<string> getter) : base(parentObject, declaredType, setter, getter)
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

        public readonly List<ObjectDrawerBase> m_ElementDrawers = new List<ObjectDrawerBase>();
        public bool m_Open = false;

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
            for (int i = 0; i < Getter.Invoke()?.Count; i++)
            {
                m_ElementDrawers.Add(GetElementDrawer(Getter.Invoke(), i));
            }

            color1 = Color.black; color2 = Color.gray; color3 = Color.green;
            color1.a = .5f; color2.a = .5f; color3.a = .5f;
        }
        private ObjectDrawerBase GetElementDrawer(IList list, int i)
        {
            if (m_ElementType.Equals(TypeHelper.TypeOf<int>.Type))
            {
                return new IntDrawer(list, m_ElementType, (other) => list[i] = other, () => (int)list[i]);
            }
            else if (m_ElementType.Equals(TypeHelper.TypeOf<int2>.Type))
            {
                return new Int2Drawer(list, m_ElementType, (other) => list[i] = other, () => (int2)list[i]);
            }
            else
            {

                return new ObjectDrawer(list[i], m_ElementType, string.Empty);
            }
        }

        public ArrayDrawer(object parentObject, Type declaredType, Action<IList> setter, Func<IList> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override IList Draw(IList list)
        {
            if (list == null) list = (IList)Activator.CreateInstance(m_DeclaredType);

            var block = new EditorUtils.BoxBlock(color3);

            #region Header
            EditorGUILayout.BeginHorizontal();
            m_Open = EditorUtils.Foldout(m_Open, Name, 13);
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
                m_ElementDrawers.Add(GetElementDrawer(list, list.Count - 1));
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
                m_ElementDrawers.RemoveAt(m_ElementDrawers.Count - 1);
            }
            EditorGUILayout.EndHorizontal();
            #endregion

            if (!m_Open)
            {
                block.Dispose();
                return list;
            }

            EditorGUI.indentLevel++;
            using (new EditorUtils.BoxBlock(color2))
            {
                for (int i = 0; i < m_ElementDrawers.Count; i++)
                {
                    if (m_ElementDrawers[i] == null) continue;

                    m_ElementDrawers[i].OnGUI();
                }
            }
            EditorGUI.indentLevel--;

            block.Dispose();

            //for (int j = 0; j < list.Count; j++)
            //{
            //    EditorGUI.indentLevel++;

            //    GUILayout.BeginHorizontal(EditorUtils.Box);
            //    using (new EditorUtils.BoxBlock(j % 2 == 0 ? color1 : color2))
            //    {
            //        if (list[j] == null)
            //        {
            //            if (m_ElementType.Equals(TypeHelper.TypeOf<string>.Type))
            //            {
            //                list[j] = string.Empty;
            //            }
            //            else
            //            {
            //                list[j] = Activator.CreateInstance(m_ElementType);
            //            }
            //        }

            //        #region CoreSystem Types
            //        if (TypeHelper.TypeOf<IReference>.Type.IsAssignableFrom(m_ElementType))
            //        {
            //            IReference objRef = (IReference)list[j];
            //            Type targetType;
            //            Type[] generics = m_ElementType.GetGenericArguments();
            //            if (generics.Length > 0) targetType = m_ElementType.GetGenericArguments()[0];
            //            else targetType = null;

            //            ReflectionHelperEditor.DrawReferenceSelector(string.Empty, (idx) =>
            //            {
            //                ObjectBase objBase = EntityDataList.Instance.GetObject(idx);

            //                Type makedT;
            //                if (targetType != null) makedT = typeof(Reference<>).MakeGenericType(targetType);
            //                else makedT = TypeHelper.TypeOf<Reference>.Type;

            //                object temp = TypeHelper.GetConstructorInfo(makedT, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
            //                    new object[] { objBase });

            //                list[j] = temp;
            //            }, objRef, targetType);
            //        }
            //        else if (m_ElementType.Equals(TypeHelper.TypeOf<LuaScript>.Type))
            //        {
            //            LuaScript scr = (LuaScript)list[j];
            //            if (scr == null)
            //            {
            //                scr = string.Empty;
            //                list[j] = scr;
            //            }
            //            scr.DrawFunctionSelector(string.Empty);
            //        }
            //        #endregion
            //        #region Unity Types
            //        else if (ReflectionHelperEditor.DrawUnityField(list[j], m_ElementType, string.Empty, (other) => list[j], out object value))
            //        {
            //            list[j] = value;
            //        }
            //        else if (ReflectionHelperEditor.DrawUnityMathField(list[j], m_ElementType, string.Empty, (other) => list[j], out value))
            //        {
            //            list[j] = value;
            //        }
            //        else if (m_ElementType.Equals(TypeHelper.TypeOf<AssetReference>.Type))
            //        {
            //            AssetReference refAsset = (AssetReference)list[j];
            //            ReflectionHelperEditor.DrawAssetReference(string.Empty, (other) => list[j] = other, refAsset);
            //        }
            //        #endregion
            //        else if (ReflectionHelperEditor.DrawSystemField(list[j], m_ElementType, string.Empty, (other) => list[j], out value))
            //        {
            //            list[j] = value;
            //        }
            //        else
            //            list[j] = ReflectionHelperEditor.DrawObject(list[j]);
            //    }
            //    if (GUILayout.Button("-", GUILayout.Width(20)))
            //    {
            //        if (list.IsFixedSize)
            //        {
            //            IList newArr = Array.CreateInstance(m_ElementType, list.Count - 1);
            //            if (list != null && list.Count > 0)
            //            {
            //                for (int a = 0, b = 0; a < list.Count; a++)
            //                {
            //                    if (a.Equals(j)) continue;

            //                    newArr[b] = list[a];

            //                    b++;
            //                }
            //            }
            //            list = newArr;
            //        }
            //        else list.RemoveAt(j);

            //        j--;
            //    }
            //    GUILayout.EndHorizontal();

            //    if (j + 1 < list.Count) EditorUtils.Line();
            //    EditorGUI.indentLevel--;
            //}

            return list;
        }
    }

    public sealed class ObjectDrawer : ObjectDrawerBase
    {
        private object m_TargetObject;
        private string m_Name;

        public override object TargetObject => m_TargetObject;
        public override string Name => m_Name;

        private readonly List<ObjectDrawerBase> DrawerBases = new List<ObjectDrawerBase>();

        public ObjectDrawer(object obj, Type declaredType, string name)
        {
            m_TargetObject = obj;
            m_Name = name;

            MemberInfo[] members = ReflectionHelper.GetSerializeMemberInfos(declaredType);
            for (int a = 0; a < members.Length; a++)
            {
                ObjectDrawerBase drawer = ToDrawer(obj, members[a], true);
                DrawerBases.Add(drawer);
            }
        }

        public override void OnGUI()
        {
            using (new EditorUtils.BoxBlock(Color.black))
            {
                for (int i = 0; i < DrawerBases.Count; i++)
                {
                    if (DrawerBases[i] == null)
                    {
                        EditorGUILayout.LabelField("notsupport");
                        continue;
                    }

                    DrawerBases[i].OnGUI();
                }
            }
        }
    }
}
