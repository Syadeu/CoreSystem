using Syadeu;
using Syadeu.Database;
using Syadeu.Database.Lua;
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
            if (DeclaredType.GetCustomAttribute<FlagsAttribute>() != null)
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
        const string c_NameFormat = "{0} <size=9>: {1}</size>";

        private Type m_DeclaredType;
        private Type m_ElementType;

        private Color
            color1, color2, color3;

        public bool m_Open = false;

        public readonly List<ObjectDrawerBase> m_ElementDrawers = new List<ObjectDrawerBase>();
        public readonly List<bool> m_ElementOpen = new List<bool>();

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
            Reload();

            color1 = Color.black; color2 = Color.gray; color3 = Color.green;
            color1.a = .5f; color2.a = .5f; color3.a = .5f;
        }
        public ArrayDrawer(object parentObject, Type declaredType, Action<IList> setter, Func<IList> getter) : base(parentObject, declaredType, setter, getter)
        {
            m_DeclaredType = declaredType;
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
            Reload();

            color1 = Color.black; color2 = Color.gray; color3 = Color.green;
            color1.a = .5f; color2.a = .5f; color3.a = .5f;
        }

        private void Reload()
        {
            m_ElementOpen.Clear();
            m_ElementDrawers.Clear();

            IList list = Getter.Invoke();
            if (list == null)
            {
                if (DeclaredType.IsArray)
                {
                    list = Array.CreateInstance(m_ElementType, 0);
                }
                else list = (IList)Activator.CreateInstance(m_DeclaredType);

                Setter.Invoke(list);
            }

            for (int i = 0; i < list.Count; i++)
            {
                m_ElementDrawers.Add(GetElementDrawer(list, i));
                m_ElementOpen.Add(false);
            }
        }

        private ObjectDrawerBase GetElementDrawer(IList list, int i)
        {
            if (m_ElementType.IsEnum)
            {
                return new EnumDrawer(list, m_ElementType, (other) => list[i] = other, () => (Enum)list[i]);
            }
            else if (m_ElementType.Equals(TypeHelper.TypeOf<int>.Type))
            {
                return new IntDrawer(list, m_ElementType, (other) => list[i] = other, () => (int)list[i]);
            }
            else if (m_ElementType.Equals(TypeHelper.TypeOf<int2>.Type))
            {
                return new Int2Drawer(list, m_ElementType, (other) => list[i] = other, () => (int2)list[i]);
            }
            else if (m_ElementType.Equals(TypeHelper.TypeOf<int3>.Type))
            {
                return new Int3Drawer(list, m_ElementType, (other) => list[i] = other, () => (int3)list[i]);
            }
            else if (m_ElementType.Equals(TypeHelper.TypeOf<float>.Type))
            {
                return new FloatDrawer(list, m_ElementType, (other) => list[i] = other, () => (float)list[i]);
            }
            else if (m_ElementType.Equals(TypeHelper.TypeOf<float2>.Type))
            {
                return new Float2Drawer(list, m_ElementType, (other) => list[i] = other, () => (float2)list[i]);
            }
            else if (m_ElementType.Equals(TypeHelper.TypeOf<float3>.Type))
            {
                return new Float3Drawer(list, m_ElementType, (other) => list[i] = other, () => (float3)list[i]);
            }
            else if (m_ElementType.Equals(TypeHelper.TypeOf<bool>.Type))
            {
                return new BoolenDrawer(list, m_ElementType, (other) => list[i] = other, () => (bool)list[i]);
            }
            else if (m_ElementType.Equals(TypeHelper.TypeOf<string>.Type))
            {
                return new StringDrawer(list, m_ElementType, (other) => list[i] = other, () => (string)list[i]);
            }
            else if (m_ElementType.IsArray || typeof(IList).IsAssignableFrom(m_ElementType))
            {
                return new ArrayDrawer(list, m_ElementType, (other) => list[i] = other, () => (IList)list[i]);
            }

            else if (TypeHelper.TypeOf<IReference>.Type.IsAssignableFrom(m_ElementType))
            {
                return new ReferenceDrawer(list, m_ElementType, (other) => list[i] = other, () => (IReference)list[i]);
            }

            else
            {
                return new ObjectDrawer(list[i], m_ElementType, string.Empty);
            }
        }

        public override IList Draw(IList list)
        {
            if (list == null) list = (IList)Activator.CreateInstance(m_DeclaredType);

            using (new EditorUtils.BoxBlock(color3))
            {
                #region Header
                EditorGUILayout.BeginHorizontal();
                m_Open = EditorUtils.Foldout(m_Open, 
                    string.Format(c_NameFormat, Name, TypeHelper.ToString(m_ElementType))
                    , 13);

                GUILayout.FlexibleSpace();
                GUILayout.Label(EditorUtils.String($"{list.Count}: ", 10), EditorUtils.HeaderStyle);

                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    object newValue = Activator.CreateInstance(m_ElementType);
                    if (list.IsFixedSize)
                    {
                        Array newArr = Array.CreateInstance(m_ElementType, list.Count + 1);
                        if (list != null && list.Count > 0) Array.Copy((Array)list, newArr, list.Count);
                        list = newArr;
                        list[list.Count - 1] = newValue;
                    }
                    else
                    {
                        list.Add(newValue);
                    }
                    m_ElementDrawers.Add(GetElementDrawer(list, list.Count - 1));
                    m_ElementOpen.Add(false);
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
                    m_ElementOpen.RemoveAt(m_ElementOpen.Count - 1);
                    m_ElementDrawers.RemoveAt(m_ElementDrawers.Count - 1);
                }
                EditorGUILayout.EndHorizontal();
                #endregion

                if (m_Open && m_ElementDrawers.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    using (new EditorUtils.BoxBlock(color2))
                    {
                        for (int i = 0; i < m_ElementDrawers.Count; i++)
                        {
                            if (m_ElementDrawers[i] == null) continue;

                            if (m_ElementDrawers[i].FieldCount > 1)
                            {
                                m_ElementOpen[i] = EditorGUILayout.Foldout(m_ElementOpen[i], $"Element {i}", true);
                            }
                            else m_ElementOpen[i] = true;

                            if (!m_ElementOpen[i]) continue;

                            EditorGUI.indentLevel++;
                            using (new EditorUtils.BoxBlock(Color.black))
                            {
                                EditorGUILayout.BeginHorizontal();

                                m_ElementDrawers[i].OnGUI();

                                if (GUILayout.Button("-", GUILayout.Width(20)))
                                {
                                    list = RemoveAt(list, i);
                                    i--;
                                }
                                EditorGUILayout.EndHorizontal();

                                if (i + 1 < m_ElementDrawers.Count) EditorUtils.Line();
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }

            return list;
        }
        private IList RemoveAt(IList list, int i)
        {
            if (list.IsFixedSize)
            {
                Array newArr = Array.CreateInstance(m_ElementType, list.Count - 1);
                Array.Copy((Array)list, newArr, i);

                if (i + 1 < list.Count)
                {
                    Array.Copy((Array)list, i + 1, newArr, i, list.Count - i - 1);
                }
                
                list = newArr;
            }
            else
            {
                list.RemoveAt(i);
            }

            m_ElementDrawers.RemoveAt(i);
            m_ElementOpen.RemoveAt(i);

            return list;
        }
    }

    public sealed class ObjectDrawer : ObjectDrawerBase
    {
        private object m_TargetObject;
        private string m_Name;

        //private Action<object> m_Getter;

        public override object TargetObject => m_TargetObject;
        public override string Name => m_Name;
        public override int FieldCount => DrawerBases.Count;

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
        //public ObjectDrawer(object obj, MemberInfo member, string name)
        //{
        //    m_TargetObject = obj;
        //    m_Name = name;

        //    m_Getter = (other) =>
        //    {
        //        if (member is FieldInfo field)
        //        {
        //            field.GetValue(obj)
        //        }
        //    }

        //    MemberInfo[] members = ReflectionHelper.GetSerializeMemberInfos(declaredType);
        //    for (int a = 0; a < members.Length; a++)
        //    {
        //        ObjectDrawerBase drawer = ToDrawer(obj, members[a], true);
        //        DrawerBases.Add(drawer);
        //    }
        //}

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
