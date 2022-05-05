using Syadeu;
using Syadeu.Collections;
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
        private bool m_Changed = false;

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

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }
        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            return CoreGUI.GetLineHeight(1);
        }

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            if (m_Changed)
            {
                GUI.changed = true;
                m_Changed = false;
            }
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            #region Setup

            SerializedProperty
                idxProperty = GetIndexProperty(property),
                subAssetNameProperty = GetSubAssetNameProperty(property);

            PrefabReference currentValue 
                = SerializedPropertyHelper.ReadPrefabReference(idxProperty, subAssetNameProperty);
            IPrefabResource objSetting = currentValue.GetObjectSetting();

            GUIContent displayName = currentValue.GetDisplayName();

            #endregion

            #region Rect Setup
            
            Rect 
                propertyRect = EditorGUI.IndentedRect(rect.Pop()),
                buttonRect, expandRect;

            if (!property.IsInArray())
            {
                var rects = AutoRect.DivideWithRatio(propertyRect, .2f, .8f);
                EditorGUI.LabelField(rects[0], label);
                buttonRect = rects[1];

                Rect[] tempRects = AutoRect.DivideWithFixedWidthRight(rects[1], 20);
                expandRect = tempRects[0];
                AutoRect.AlignRect(ref buttonRect, expandRect);
            }
            else
            {
                if (property.GetParent().CountInProperty() > 1)
                {
                    var rects = AutoRect.DivideWithRatio(propertyRect, .2f, .8f);
                    EditorGUI.LabelField(rects[0], label);
                    buttonRect = rects[1];

                    Rect[] tempRects = AutoRect.DivideWithFixedWidthRight(rects[1], 20, 20);
                    expandRect = tempRects[0];
                    AutoRect.AlignRect(ref buttonRect, expandRect);
                }
                else
                {
                    buttonRect = propertyRect;

                    Rect[] tempRects = AutoRect.DivideWithFixedWidthRight(propertyRect, 20, 20);
                    expandRect = tempRects[0];
                    AutoRect.AlignRect(ref buttonRect, expandRect);
                }
            }

            #endregion

            bool clicked = CoreGUI.BoxButton(buttonRect, displayName, ColorPalettes.PastelDreams.Mint, () =>
            {
                GenericMenu menu = new GenericMenu();

                menu.AddDisabledItem(displayName);
                menu.AddSeparator(string.Empty);

                if (objSetting == null)
                {
                    menu.AddDisabledItem(new GUIContent("Select"));
                }
                else
                {
                    menu.AddItem(new GUIContent("Select"), false, () =>
                    {
                        Selection.activeObject = currentValue.GetEditorAsset();
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    });
                }

                menu.AddDisabledItem(new GUIContent("Edit"));

                menu.ShowAsContext();
            });

            bool disable = currentValue.IsNone() || !currentValue.IsValid();
            using (new EditorGUI.DisabledGroupScope(disable))
            {
                string str = property.isExpanded ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString;
                property.isExpanded = CoreGUI.BoxToggleButton(
                    expandRect,
                    property.isExpanded,
                    new GUIContent(str),
                    ColorPalettes.PastelDreams.TiffanyBlue,
                    ColorPalettes.PastelDreams.HotPink
                    );

                //if (property.isExpanded)
                //{
                //    Editor.CreateCachedEditor(currentValue.GetEditorAsset(), null, ref editor);

                //    using (new EditorGUI.DisabledGroupScope(true))
                //    {
                //        editor.DrawHeader();
                //        editor.OnInspectorGUI();
                //    }
                //}
            }

            //if (GUI.Button(propRects[1], displayName, EditorStyleUtilities.SelectorStyle))
            if (clicked)
            {
                Rect popupRect = GUILayoutUtility.GetRect(150, 300);
                popupRect.position = Event.current.mousePosition;

                Type type = fieldInfo.FieldType;
#pragma warning disable SD0001 // typeof(T) 는 매 호출시마다 Reflection 으로 새로 값을 받아옵니다.
                if (type.IsArray)
                {
                    type = type.GetElementType();
                }
                //else if (TypeHelper.InheritsFrom(type, typeof(IList<>)))
                //{
                //    type = type.GetGenericArguments()[0];
                //}
#pragma warning restore SD0001 // typeof(T) 는 매 호출시마다 Reflection 으로 새로 값을 받아옵니다.
                List<ReferenceAsset> list;
                if (type.GetGenericArguments().Length > 0)
                {
                    Type targetType = type.GetGenericArguments()[0];

                    list = PrefabList.Instance.ObjectSettings
                        .Where(other =>
                        {
                            UnityEngine.Object editorAsset = other.GetEditorAsset();
                            if (editorAsset == null) return false;

                            if (editorAsset is GameObject gameObj)
                            {
                                if (TypeHelper.TypeOf<GameObject>.Type.Equals(targetType))
                                {
                                    return true;
                                }
                                else if (TypeHelper.InheritsFrom<UnityEngine.Component>(targetType))
                                {
                                    return gameObj.GetComponent(targetType) != null;
                                }
                                return false;
                            }
                            else if (TypeHelper.InheritsFrom(editorAsset.GetType(), targetType))
                            {
                                return true;
                            }
                            //else if (targetType.IsAssignableFrom(editorAsset.GetType()))
                            //{
                            //    return true;
                            //}

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

                        //m_ChangedTarget = prefabIdx;
                        m_Changed = true;
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
                clicked = CoreGUI.BoxButton(displayName.text, ColorPalettes.PastelDreams.Mint, () =>
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
