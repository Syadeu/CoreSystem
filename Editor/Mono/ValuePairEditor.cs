﻿using System;
using System.Collections;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using Syadeu;
using Syadeu.Database;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    public static class ValuePairEditor
    {
        public static void DrawValueContainer(this ValuePairContainer container, string syncSheetName = null)
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorUtils.StringHeader("Values", 15);
                    if (!string.IsNullOrEmpty(syncSheetName) && GUILayout.Button("Sync", GUILayout.Width(50)))
                    {
                        container.SyncWithGoogleSheet(
                            container.Contains("Index") ? (int)container.GetValue("Index") : 1, syncSheetName);
                    }
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        GenericMenu typeMenu = new GenericMenu();
                        typeMenu.AddItem(new GUIContent("Int"), false, () =>
                        {
                            container.Add<int>("New Int Value", 0);
                        });
                        typeMenu.AddItem(new GUIContent("Double"), false, () =>
                        {
                            container.Add<double>("New Double Value", 0);
                        });
                        typeMenu.AddItem(new GUIContent("String"), false, () =>
                        {
                            container.Add<string>("New String Value", "");
                        });
                        typeMenu.AddItem(new GUIContent("Bool"), false, () =>
                        {
                            container.Add<bool>("New Bool Value", false);
                        });
                        typeMenu.AddItem(new GUIContent("Int Array"), false, () =>
                        {
                            container.Add<List<int>>("New Int Array", new List<int>());
                        });
                        typeMenu.AddItem(new GUIContent("Double Array"), false, () =>
                        {
                            container.Add<List<double>>("New Double Array", new List<double>());
                        });
                        typeMenu.AddItem(new GUIContent("Bool Array"), false, () =>
                        {
                            container.Add<List<bool>>("New Bool Array", new List<bool>());
                        });
                        typeMenu.AddItem(new GUIContent("String Array"), false, () =>
                        {
                            container.Add<List<string>>("New String Array", new List<string>());
                        });
                        typeMenu.AddItem(new GUIContent("Delegate"), false, () =>
                        {
                            container.Add<Action>("New Delegate Value", () => { });
                        });
                        //;
                        //GUIUtility.GUIToScreenPoint(Event.current.mousePosition)
                        //GUILayoutUtility.GetRect()
                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.position = Event.current.mousePosition;
                        //rect.width = 100; rect.height = 400;
                        typeMenu.DropDown(rect);
                    }
                }

                EditorGUI.indentLevel += 1;
                //if (Target.m_Values == null) Target.m_Values = new ValuePairContainer();
                for (int i = 0; i < container.Count; i++)
                {
                    Syadeu.Database.ValueType valueType = container[i].GetValueType();
                    if (valueType == Syadeu.Database.ValueType.Array)
                    {
                        IList list = (IList)container[i].GetValue();
                        EditorGUILayout.BeginHorizontal();
                        if (list == null || list.Count == 0)
                        {
                            container[i].m_Name = EditorGUILayout.TextField(container[i].m_Name);
                            if (GUILayout.Button("+", GUILayout.Width(20)))
                            {
                                list.Add(Activator.CreateInstance(list.GetType().GenericTypeArguments[0]));
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        else
                        {
                            container[i].m_Name = EditorGUILayout.TextField(container[i].m_Name);
                            if (GUILayout.Button("+", GUILayout.Width(20)))
                            {
                                list.Add(Activator.CreateInstance(list.GetType().GenericTypeArguments[0]));
                            }
                            if (GUILayout.Button("-", GUILayout.Width(20)))
                            {
                                list.RemoveAt(list.Count - 1);
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUI.indentLevel += 1;
                            for (int a = 0; a < list.Count; a++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                if (list[a] is int intVal)
                                {
                                    list[a] = EditorGUILayout.IntField(intVal);
                                }
                                else if (list[a] is float floatVal)
                                {
                                    list[a] = EditorGUILayout.FloatField(floatVal);
                                }
                                else if (list[a] is bool boolVal)
                                {
                                    list[a] = EditorGUILayout.Toggle(boolVal);
                                }
                                else if (list[a] is string strVal)
                                {
                                    list[a] = EditorGUILayout.TextField(strVal);
                                }
                                if (GUILayout.Button("-", GUILayout.Width(20)))
                                {
                                    list.RemoveAt(a);
                                    a--;
                                    continue;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            
                            EditorGUI.indentLevel -= 1;
                        }
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        container[i].m_Name = EditorGUILayout.TextField(container[i].m_Name, GUILayout.Width(150));
                        switch (valueType)
                        {
                            case Syadeu.Database.ValueType.Int32:
                                int intFal = EditorGUILayout.IntField((int)container[i].GetValue());
                                if (!container[i].GetValue().Equals(intFal))
                                {
                                    container.SetValue(container[i].m_Name, intFal);
                                }
                                break;
                            case Syadeu.Database.ValueType.Double:
                                double doubleVal = EditorGUILayout.DoubleField((double)container[i].GetValue());
                                if (!container[i].GetValue().Equals(doubleVal))
                                {
                                    container.SetValue(container[i].m_Name, doubleVal);
                                }
                                break;
                            case Syadeu.Database.ValueType.String:
                                string stringVal = EditorGUILayout.TextField((string)container[i].GetValue());
                                if (!container[i].GetValue().Equals(stringVal))
                                {
                                    container.SetValue(container[i].m_Name, stringVal);
                                }
                                break;
                            case Syadeu.Database.ValueType.Boolean:
                                bool boolVal = EditorGUILayout.Toggle((bool)container[i].GetValue());
                                if (!container[i].GetValue().Equals(boolVal))
                                {
                                    container.SetValue(container[i].m_Name, boolVal);
                                }
                                break;
                            case Syadeu.Database.ValueType.Delegate:
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.TextField("Delegate");
                                EditorGUI.EndDisabledGroup();
                                break;
                            default:
                                EditorGUILayout.TextField($"{valueType}: {container[i].GetValue()}");
                                break;
                        }

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            container.RemoveAt(i);
                            i--;
                            continue;
                        }
                    }
                }
                EditorGUI.indentLevel -= 1;
            }
        }
        public static void SyncWithGoogleSheet(this ValuePairContainer container, int idx, string sheetName)
        {
            container.Clear();
            Sheet sheet = GoogleService.DownloadSheet(sheetName);
            container.AddRange(ToValuePairs(idx, sheet.Data[0]));

            ValuePair[] ToValuePairs(int idx, GridData data)
            {
                if (idx >= data.RowData.Count) return new ValuePair[0];

                List<string> names = new List<string>();

                for (int i = 0; i < sheet.Data[0].RowData[0].Values.Count; i++)
                {
                    if (string.IsNullOrEmpty(data.RowData[0].Values[i].FormattedValue)) break;
                    names.Add(data.RowData[0].Values[i].FormattedValue);
                }

                ValuePair[] valuePairs = new ValuePair[names.Count];
                for (int i = 0; i < valuePairs.Length; i++)
                {
                    if (1 < data.RowData.Count &&
                        i < data.RowData[/*1 + */idx].Values.Count)
                    {
                        string value = data.RowData[/*1 + */idx].Values[i].FormattedValue;

                        if (int.TryParse(value, out int intVal))
                        {
                            valuePairs[i] = ValuePair.New(names[i], intVal);
                        }
                        else if (float.TryParse(value, out float floatVal))
                        {
                            valuePairs[i] = ValuePair.New(names[i], floatVal);
                        }
                        else if (bool.TryParse(value, out bool boolVal))
                        {
                            valuePairs[i] = ValuePair.New(names[i], boolVal);
                        }
                        else
                        {
                            valuePairs[i] = ValuePair.New(names[i], value);
                        }
                    }
                }

                return valuePairs;
            }
        }
    }
}
