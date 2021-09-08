using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Syadeu;
using Syadeu.Database;
using UnityEditor;
using UnityEngine;

#if CORESYSTEM_GOOGLE
using Google.Apis.Sheets.v4.Data;
#endif

namespace SyadeuEditor
{
    public static class ValuePairEditor
    {
        public enum DrawMenu
        {
            None = 0,

            Int = 1 << 0,
            Double = 1 << 1,
            String = 1 << 2,
            Bool = 1 << 3,
            IntArray = 1 << 4,
            DoubleArray = 1 << 5,
            StringArray = 1 << 6,
            BoolArray = 1 << 7,
            Delegate = 1 << 8,

            All = ~0
        }
        public static void DrawValueContainer(this ValuePairContainer container, string name)
            => DrawValueContainer(container, name, DrawMenu.All, null);
        public static void DrawValueContainer(this ValuePairContainer container, 
            string name, DrawMenu drawMenu, Action<ValuePair> onDrawItem
#if CORESYSTEM_GOOGLE
            , string syncSheetName = null
#endif
            )
        {
            //const string Box = "Box";

            //Color originColor = GUI.backgroundColor;
            //GUI.backgroundColor = Color.cyan;
            using (new EditorUtils.BoxBlock(Color.cyan))
            {
                //GUI.backgroundColor = originColor;
                #region Header
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorUtils.StringHeader(name, 15);
#if CORESYSTEM_GOOGLE
                    if (!string.IsNullOrEmpty(syncSheetName) && GUILayout.Button("Sync", GUILayout.Width(50)))
                    {
                        container.SyncWithGoogleSheet(
                            container.Contains("Index") ? (int)container.GetValue("Index") : 1, syncSheetName);
                    }
#endif
                    #region Add Menu Items
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        GenericMenu typeMenu = new GenericMenu();
                        if (drawMenu.HasFlag(DrawMenu.Int))
                        {
                            typeMenu.AddItem(new GUIContent("Int"), false, () =>
                            {
                                container.Add<int>("New Int Value", 0);
                            });
                        }
                        if (drawMenu.HasFlag(DrawMenu.Double))
                        {
                            typeMenu.AddItem(new GUIContent("Double"), false, () =>
                            {
                                container.Add<double>("New Double Value", 0);
                            });
                        }
                        if (drawMenu.HasFlag(DrawMenu.String))
                        {
                            typeMenu.AddItem(new GUIContent("String"), false, () =>
                            {
                                container.Add<string>("New String Value", "");
                            });
                        }
                        if (drawMenu.HasFlag(DrawMenu.Bool))
                        {
                            typeMenu.AddItem(new GUIContent("Bool"), false, () =>
                            {
                                container.Add<bool>("New Bool Value", false);
                            });
                        }
                        if (drawMenu.HasFlag(DrawMenu.IntArray))
                        {
                            typeMenu.AddItem(new GUIContent("Int Array"), false, () =>
                            {
                                container.Add<List<int>>("New Int Array", new List<int>());
                            });
                        }
                        if (drawMenu.HasFlag(DrawMenu.DoubleArray))
                        {
                            typeMenu.AddItem(new GUIContent("Double Array"), false, () =>
                            {
                                container.Add<List<double>>("New Double Array", new List<double>());
                            });
                        }
                        if (drawMenu.HasFlag(DrawMenu.BoolArray))
                        {
                            typeMenu.AddItem(new GUIContent("Bool Array"), false, () =>
                            {
                                container.Add<List<bool>>("New Bool Array", new List<bool>());
                            });
                        }
                        if (drawMenu.HasFlag(DrawMenu.StringArray))
                        {
                            typeMenu.AddItem(new GUIContent("String Array"), false, () =>
                            {
                                container.Add<List<string>>("New String Array", new List<string>());
                            });
                        }
                        //if (drawMenu.HasFlag(DrawMenu.Delegate))
                        //{
                        //    typeMenu.AddItem(new GUIContent("Delegate"), false, () =>
                        //    {
                        //        container.Add<UnityEngine.Events.UnityEvent>("New Delegate Value", new UnityEngine.Events.UnityEvent());
                        //    });
                        //}
                        
                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.position = Event.current.mousePosition;
                        typeMenu.DropDown(rect);
                    }
                    #endregion
                }
                #endregion

                EditorGUI.indentLevel += 1;
                //if (Target.m_Values == null) Target.m_Values = new ValuePairContainer();
                for (int i = 0; i < container?.Count; i++)
                {
                    Hash hash = container[i].Hash;

                    Syadeu.Database.ValueType valueType = container[i].GetValueType();
                    if (valueType == Syadeu.Database.ValueType.Array)
                    {
                        IList list = (IList)container[i].GetValue();
                        EditorGUILayout.BeginHorizontal();
                        if (list == null || list.Count == 0)
                        {
                            container[i].Name = EditorGUILayout.TextField(container[i].Name);
                            if (GUILayout.Button("+", GUILayout.Width(20)))
                            {
                                list.Add(Activator.CreateInstance(list.GetType().GenericTypeArguments[0]));
                            }
                            if (GUILayout.Button("-", GUILayout.Width(20)))
                            {
                                container.RemoveAt(i);
                                i--;
                                EditorGUILayout.EndHorizontal();
                                continue;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        else
                        {
                            container[i].Name = EditorGUILayout.TextField(container[i].Name);
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

                        onDrawItem?.Invoke(container[i]);
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(container[i].GetValueType().ToString(), GUILayout.Width(65));

                        container[i].Name = EditorGUILayout.TextField(container[i].Name, GUILayout.MinWidth(100));
                        switch (valueType)
                        {
                            case Syadeu.Database.ValueType.Int32:
                                int intFal = EditorGUILayout.IntField((int)container[i].GetValue(), GUILayout.MinWidth(100));
                                if (!container[i].GetValue().Equals(intFal))
                                {
                                    container.SetValue(container[i].Name, intFal);
                                }
                                break;
                            case Syadeu.Database.ValueType.Double:
                                double doubleVal = EditorGUILayout.DoubleField((double)container[i].GetValue(), GUILayout.MinWidth(100));
                                if (!container[i].GetValue().Equals(doubleVal))
                                {
                                    container.SetValue(container[i].Name, doubleVal);
                                }
                                break;
                            case Syadeu.Database.ValueType.String:
                                string stringVal = EditorGUILayout.TextField((string)container[i].GetValue(), GUILayout.MinWidth(100));
                                if (!container[i].GetValue().Equals(stringVal))
                                {
                                    container.SetValue(container[i].Name, stringVal);
                                }
                                break;
                            case Syadeu.Database.ValueType.Boolean:
                                bool boolVal = EditorGUILayout.ToggleLeft(string.Empty, (bool)container[i].GetValue(), GUILayout.MinWidth(100));
                                if (!container[i].GetValue().Equals(boolVal))
                                {
                                    container.SetValue(container[i].Name, boolVal);
                                }
                                break;
                            case Syadeu.Database.ValueType.Delegate:
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.TextField($"Delegate: {container[i].GetValue()}", GUILayout.MinWidth(100));
                                EditorGUI.EndDisabledGroup();
                                break;
                            default:
                                EditorGUILayout.TextField($"{valueType}: {container[i].GetValue()}", GUILayout.MinWidth(100));
                                break;
                        }

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            container.RemoveAt(i);
                            i--;
                            continue;
                        }
                    }

                    onDrawItem?.Invoke(container[i]);
                }
                EditorGUI.indentLevel -= 1;
            }
        }
        public static void DrawValuePair(this ValuePair valuePair)
        {
            Hash hash = valuePair.Hash;

            Syadeu.Database.ValueType valueType = valuePair.GetValueType();
            if (valueType == Syadeu.Database.ValueType.Array)
            {
                IList list = (IList)valuePair.GetValue();
                EditorGUILayout.BeginHorizontal();
                if (list == null || list.Count == 0)
                {
                    valuePair.Name = EditorGUILayout.TextField(valuePair.Name);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        list.Add(Activator.CreateInstance(list.GetType().GenericTypeArguments[0]));
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    valuePair.Name = EditorGUILayout.TextField(valuePair.Name);
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

                //onDrawItem?.Invoke(valuePair);
                //continue;
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                valuePair.Name = EditorGUILayout.TextField(valuePair.Name, GUILayout.Width(150));
                switch (valueType)
                {
                    case Syadeu.Database.ValueType.Int32:
                        int intFal = EditorGUILayout.IntField((int)valuePair.GetValue());
                        //if (!valuePair.GetValue().Equals(intFal))
                        //{
                        //    container.SetValue(valuePair.Name, intFal);
                        //}
                        break;
                    case Syadeu.Database.ValueType.Double:
                        double doubleVal = EditorGUILayout.DoubleField((double)valuePair.GetValue());
                        //if (!valuePair.GetValue().Equals(doubleVal))
                        //{
                        //    container.SetValue(valuePair.Name, doubleVal);
                        //}
                        break;
                    case Syadeu.Database.ValueType.String:
                        string stringVal = EditorGUILayout.TextField((string)valuePair.GetValue());
                        //if (!valuePair.GetValue().Equals(stringVal))
                        //{
                        //    container.SetValue(valuePair.Name, stringVal);
                        //}
                        break;
                    case Syadeu.Database.ValueType.Boolean:
                        bool boolVal = EditorGUILayout.Toggle((bool)valuePair.GetValue());
                        //if (!valuePair.GetValue().Equals(boolVal))
                        //{
                        //    container.SetValue(valuePair.Name, boolVal);
                        //}
                        break;
                    case Syadeu.Database.ValueType.Delegate:
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.TextField("Delegate");
                        EditorGUI.EndDisabledGroup();
                        break;
                    default:
                        EditorGUILayout.TextField($"{valueType}: {valuePair.GetValue()}");
                        break;
                }

                //if (GUILayout.Button("-", GUILayout.Width(20)))
                //{
                //    container.RemoveAt(i);
                //    i--;
                //    continue;
                //}
            }
        }
#if CORESYSTEM_GOOGLE
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
#endif
    }
}
