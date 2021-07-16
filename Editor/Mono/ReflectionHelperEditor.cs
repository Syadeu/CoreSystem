﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Syadeu.Database;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    public sealed class ReflectionHelperEditor
    {
        public sealed class Drawer
        {
            public object m_Instance;
            public Type m_Type;
            public MemberInfo[] m_Members;

            public Drawer(object ins, params string[] ignore)
            {
                m_Instance = ins;
                m_Type = ins.GetType();
                List<MemberInfo> list = ReflectionHelper.GetSerializeMemberInfos(ins.GetType()).ToList();
                if (ignore != null && ignore.Length > 0)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (ignore.Contains(list[i].Name))
                        {
                            list.RemoveAt(i);
                        }
                    }
                }
                m_Members = list.ToArray();
            }

            public void OnGUI() => OnGUI(true);
            public void OnGUI(bool drawHeader)
            {
                if (drawHeader) EditorUtils.StringRich(m_Type.Name, 15, true);
                for (int i = 0; i < m_Members.Length; i++)
                {
                    DrawMember(m_Instance, m_Members[i]);
                }
            }
        }
        public static Drawer GetDrawer(object ins, params string[] ignore) => new Drawer(ins, ignore);

        public static void DrawMember(object ins, MemberInfo memberInfo, int depth = 0)
        {
            Type declaredType;
            Action<object, object> setter;
            Func<object, object> getter;
            if (memberInfo is FieldInfo field)
            {
                declaredType = field.FieldType;
                setter = field.SetValue;
                getter = field.GetValue;
            }
            else if (memberInfo is PropertyInfo property)
            {
                declaredType = property.PropertyType;
                setter = property.SetValue;
                getter = property.GetValue;
            }
            else throw new NotImplementedException();
            string name = ReflectionHelper.SerializeMemberInfoName(memberInfo);

            int spaceCount = memberInfo.GetCustomAttributes<SpaceAttribute>().Count();
            for (int i = 0; i < spaceCount; i++)
            {
                EditorGUILayout.Space();
            }

            TooltipAttribute tooltip = memberInfo.GetCustomAttribute<TooltipAttribute>();
            if (tooltip != null)
            {
                EditorGUILayout.HelpBox(tooltip.tooltip, MessageType.Info);
            }

            if (declaredType.Equals(TypeHelper.TypeOf<int>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.IntField(name, (int)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<bool>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.Toggle(name, (bool)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<float>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.FloatField(name, (float)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<double>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.DoubleField(name, (double)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<long>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.LongField(name, (long)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<ulong>.Type))
            {
                setter.Invoke(ins, ulong.Parse(EditorGUILayout.LongField(name, (long.Parse(getter.Invoke(ins).ToString()))).ToString()));
            }
            #region Unity Types
            else if (declaredType.Equals(TypeHelper.TypeOf<UnityEngine.Rect>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.RectField(name, (UnityEngine.Rect)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<UnityEngine.RectInt>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.RectIntField(name, (UnityEngine.RectInt)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<UnityEngine.Vector3>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.Vector3Field(name, (UnityEngine.Vector3)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<UnityEngine.Vector3Int>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.Vector3IntField(name, (UnityEngine.Vector3Int)getter.Invoke(ins)));
            }
            #endregion
            else if (declaredType.Equals(TypeHelper.TypeOf<string>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.TextField(name, (string)getter.Invoke(ins)));
            }
            else if (declaredType.IsEnum)
            {
                //string[] enumNames = Enum.GetNames(declaredType);
                Enum idx = (Enum)getter.Invoke(ins);
                Enum selected;
                if (declaredType.GetCustomAttribute<FlagsAttribute>() != null)
                {
                    selected = EditorGUILayout.EnumFlagsField(name, idx);
                }
                else selected = EditorGUILayout.EnumPopup(name, idx);

                setter.Invoke(ins, selected);
            }
            else if (declaredType.IsArray)
            {
                IList list = (IList)getter.Invoke(ins);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(name);
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    Array newArr = Array.CreateInstance(declaredType.GetElementType(), list != null ? list.Count + 1 : 1);
                    if (list != null && list.Count > 0) Array.Copy((Array)list, newArr, list.Count);

                    newArr.SetValue(default, newArr.Length - 1);

                    setter.Invoke(ins, newArr);
                    list = newArr;
                }
                if (list?.Count > 0 && GUILayout.Button("-", GUILayout.Width(20)))
                {
                    Array newArr = Array.CreateInstance(declaredType.GetElementType(), list.Count - 1);
                    if (list != null && list.Count > 0) Array.Copy((Array)list, newArr, newArr.Length);

                    setter.Invoke(ins, newArr);
                    list = newArr;
                }
                EditorGUILayout.EndHorizontal();
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        EditorGUI.indentLevel += 1;

                        if (declaredType.GetElementType().Equals(TypeHelper.TypeOf<string>.Type))
                        {
                            list[i] = EditorGUILayout.TextField((string)list[i]);
                        }

                        EditorGUI.indentLevel -= 1;
                    }
                }
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Hash>.Type))
            {
                setter.Invoke(ins, new Hash(ulong.Parse(EditorGUILayout.LongField(name, (long.Parse(getter.Invoke(ins).ToString()))).ToString())));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<ValuePairContainer>.Type))
            {
                ValuePairContainer container = (ValuePairContainer)getter.Invoke(ins);
                if (container == null)
                {
                    container = new ValuePairContainer();
                    setter.Invoke(ins, container);
                }

                container.DrawValueContainer(name);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<LuaScript>.Type))
            {
                LuaScript scr = (LuaScript)getter.Invoke(ins);
                if (scr == null)
                {
                    scr = string.Empty;
                    setter.Invoke(ins, scr);
                }
                scr.DrawFunctionSelector(name);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<LuaScriptContainer>.Type))
            {
                LuaScriptContainer container = (LuaScriptContainer)getter.Invoke(ins);
                if (container == null)
                {
                    container = new LuaScriptContainer();
                    setter.Invoke(ins, container);
                }
                container.DrawGUI(name);
            }
            else
            {
                if (depth > 4) return;

                EditorGUILayout.LabelField(name);
                EditorGUI.indentLevel += 1;
                MemberInfo[] insider = ReflectionHelper.GetSerializeMemberInfos(declaredType);
                for (int i = 0; i < insider.Length; i++)
                {
                    //EditorGUILayout.LabelField(insider[i].Name);
                    object temp = getter.Invoke(ins);
                    DrawMember(temp, insider[i], depth++);
                }

                EditorGUI.indentLevel -= 1;
            }
        }
    }
}
