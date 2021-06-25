using System;
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
        public static void DrawValueContainer(this ValuePairContainer container)
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorUtils.StringHeader("Values", 15);
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
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        container[i].m_Name = EditorGUILayout.TextField(container[i].m_Name, GUILayout.Width(150));
                        switch (container[i].GetValueType())
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
