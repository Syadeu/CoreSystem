﻿using Syadeu.Collections;
using Syadeu.Mono;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(IPrefabReference), true)]
    public sealed class PrefabReferencePropertyDrawer : PropertyDrawer<IPrefabReference>
    {
        private static SerializedProperty GetIndexProperty(SerializedProperty property)
        {
            const string c_Name = "m_Idx";

            return property.FindPropertyRelative(c_Name);
        }
        private static SerializedProperty GetSubAssetNameProperty(SerializedProperty property)
        {
            const string c_Name = "m_SubAssetName";

            return property.FindPropertyRelative(c_Name);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing;
        }

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty 
                idxProperty = GetIndexProperty(property),
                subAssetNameProperty = GetSubAssetNameProperty(property);

            PrefabReference currentValue 
                = SerializedPropertyHelper.ReadPrefabReference(idxProperty, subAssetNameProperty);

            GUIContent displayName = currentValue.GetDisplayName();

            Rect propertyRect = rect.Pop();
            var propRects = AutoRect.DivideWithRatio(propertyRect, .2f, .8f);

            EditorGUI.LabelField(propRects[0], label);

            if (GUI.Button(propRects[1], displayName, EditorStyleUtilities.SelectorStyle))
            {
                Rect popupRect = GUILayoutUtility.GetRect(150, 300);
                popupRect.position = Event.current.mousePosition;

                Type type = fieldInfo.FieldType;
                List<ReferenceAsset> list;

                if (type.GenericTypeArguments.Length > 0)
                {
                    list = PrefabList.Instance.ObjectSettings
                        .Where((other) =>
                        {
                            if (other.GetEditorAsset() == null) return false;

                            if (type.GenericTypeArguments[0].IsAssignableFrom(other.GetEditorAsset().GetType()))
                            {
                                return true;
                            }
                            return false;
                        })
                        .Select(t => new ReferenceAsset()
                        {
                            index = PrefabList.Instance.ObjectSettings.IndexOf(t)
                        }).ToList();

                    for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
                    {
                        var subAssets = PrefabList.Instance.ObjectSettings[i].m_RefPrefab.GetSubAssets();
                        for (int h = 0; h < subAssets?.Count; h++)
                        {
                            if (!type.GenericTypeArguments[0].IsAssignableFrom(subAssets[h].TargetAsset.GetType()))
                            {
                                continue;
                            }

                            list.Add(new ReferenceAsset
                            {
                                index = i,
                                subAssetName = subAssets[h].TargetAsset.name
                            });
                        }
                    }
                }
                else
                {
                    list = PrefabList.Instance.ObjectSettings.Select(t => new ReferenceAsset()
                    {
                        index = PrefabList.Instance.ObjectSettings.IndexOf(t)
                    }).ToList();

                    for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
                    {
                        var subAssets = PrefabList.Instance.ObjectSettings[i].m_RefPrefab.GetSubAssets();

                        for (int h = 0; h < subAssets?.Count; h++)
                        {
                            list.Add(new ReferenceAsset
                            {
                                index = i,
                                subAssetName = subAssets[h].TargetAsset.name
                            });
                        }
                    }
                }

                var popup = SelectorPopup<ReferenceAsset, ReferenceAsset>.GetWindow(
                    list, (prefabIdx) =>
                    {
                        SerializedPropertyHelper.SetPrefabReference(
                            idxProperty, subAssetNameProperty,
                            prefabIdx.index, prefabIdx.subAssetName
                            );

                        idxProperty.serializedObject.ApplyModifiedProperties();
                    },
                    (objSet) =>
                    {
                            return objSet;
                    }, ReferenceAsset.None,
                    getName: (t) =>
                    {
                        if (t.subAssetName.IsEmpty)
                        {
                            return PrefabList.Instance.ObjectSettings[(int)t.index].Name;
                        }

                        return PrefabList.Instance.ObjectSettings[(int)t.index].Name + $"[{t.subAssetName}]";
                    });

                PopupWindow.Show(popupRect, popup);
            }
        }
        private struct ReferenceAsset
        {
            public static ReferenceAsset None => new ReferenceAsset { index = -2 };

            public long index;
            public FixedString128Bytes subAssetName;
        }
        private void DrawPrefabReference(string name, Action<int> setter, IPrefabReference current)
        {
            GUIContent displayName;
            if (current.Equals(PrefabReference.None))
            {
                displayName = new GUIContent("None");
            }
            else if (current.Index >= 0)
            {
                //PrefabList.ObjectSetting objSetting = current.GetObjectSetting();
                IPrefabResource objSetting = current.GetObjectSetting();
                displayName = objSetting == null ? new GUIContent("INVALID") : new GUIContent(objSetting.Name);
            }
            else
            {
                displayName = new GUIContent("INVALID");
            }

            bool clicked;
            {
                clicked = EditorUtilities.BoxButton(displayName.text, ColorPalettes.PastelDreams.Mint, () =>
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddDisabledItem(displayName);
                    menu.AddSeparator(string.Empty);

                    menu.AddItem(new GUIContent("Select"), false, () =>
                    {
                        Selection.activeObject = current.GetEditorAsset();
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    });
                    menu.AddDisabledItem(new GUIContent("Edit"));

                    menu.ShowAsContext();
                });
            }

            if (clicked)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.position = Event.current.mousePosition;

                Type type = current.GetType();
                List<PrefabList.ObjectSetting> list;

                if (type.GenericTypeArguments.Length > 0)
                {
                    list = PrefabList.Instance.ObjectSettings
                        .Where((other) =>
                        {
                            if (other.GetEditorAsset() == null) return false;

                            if (type.GenericTypeArguments[0].IsAssignableFrom(other.GetEditorAsset().GetType()))
                            {
                                return true;
                            }
                            return false;
                        }).ToList();
                }
                else
                {
                    list = PrefabList.Instance.ObjectSettings;
                }

                var popup = SelectorPopup<int, PrefabList.ObjectSetting>.GetWindow(list, setter, (objSet) =>
                {
                    for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
                    {
                        if (objSet.Equals(PrefabList.Instance.ObjectSettings[i])) return i;
                    }
                    return -1;
                }, -2);

                PopupWindow.Show(rect, popup);
            }
        }
    }
}