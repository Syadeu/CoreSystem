using Syadeu;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Mono;
using SyadeuEditor.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SyadeuEditor.Presentation
{
    [Obsolete("Use Unity Serialized -> PropertyDrawer<T>", true)]
    public sealed class PrefabReferenceDrawer : ObjectDrawer<IPrefabReference>
    {
        private readonly ConstructorInfo m_Constructor;
        private bool 
            m_Open = false/*, m_WasEdited = false*/;

        Editor m_Editor = null;
        bool
            IsHover;

        public bool DisableHeader { get; set; } = false;

        public PrefabReferenceDrawer(IList list, int index, Type elementType) : base(list, index, elementType)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(elementType, TypeHelper.TypeOf<long>.Type, TypeHelper.TypeOf<string>.Type);

            IPrefabReference prefab = Getter.Invoke();
            if (!prefab.IsNone() && prefab.IsValid())
            {
                if (prefab.GetEditorAsset() == null)
                {
                    Setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1, string.Empty }));
                    return;
                }

                if (elementType.GenericTypeArguments.Length > 0)
                {
                    Type targetType = elementType.GenericTypeArguments[0];
                    if (!targetType.IsAssignableFrom(prefab.GetEditorAsset().GetType()))
                    {
                        Setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1, string.Empty }));
                    }
                }
            }
        }
        public PrefabReferenceDrawer(object parentObject, MemberInfo memberInfo, bool drawName) : base(parentObject, memberInfo)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<int>.Type, TypeHelper.TypeOf<string>.Type);

            IPrefabReference prefab = Getter.Invoke();
            if (!prefab.IsNone() && prefab.IsValid())
            {
                if (prefab.GetEditorAsset() == null)
                {
                    Setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1, string.Empty }));
                    return;
                }

                if (DeclaredType.GenericTypeArguments.Length > 0)
                {
                    Type targetType = DeclaredType.GenericTypeArguments[0];
                    if (!targetType.IsAssignableFrom(prefab.GetEditorAsset().GetType()))
                    {
                        Setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1, string.Empty }));
                    }
                }
            }

            DisableHeader = !drawName;
        }
        public PrefabReferenceDrawer(object parentObject, Type declaredType, Action<IPrefabReference> setter, Func<IPrefabReference> getter) : base(parentObject, declaredType, setter, getter)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<int>.Type, TypeHelper.TypeOf<string>.Type);

            IPrefabReference prefab = getter.Invoke();
            if (!prefab.IsNone() && prefab.IsValid())
            {
                if (prefab.GetEditorAsset() == null)
                {
                    setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1, string.Empty }));
                    return;
                }

                if (declaredType.GenericTypeArguments.Length > 0)
                {
                    Type targetType = declaredType.GenericTypeArguments[0];
                    if (!targetType.IsAssignableFrom(prefab.GetEditorAsset().GetType()))
                    {
                        setter.Invoke((IPrefabReference)m_Constructor.Invoke(new object[] { -1, string.Empty }));
                    }
                }
            }
        }

        public override IPrefabReference Draw(IPrefabReference currentValue)
        {
            //using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    DrawPrefabReference(DisableHeader ? string.Empty : Name,
                    (idx) =>
                    {
                        IPrefabReference prefab = (IPrefabReference)m_Constructor.Invoke(new object[] { idx.index, idx.subAssetName.ToString() });

                        IPrefabReference origin = Getter.Invoke();
                        Setter.Invoke(prefab);

                    //m_WasEdited = !origin.Equals(Getter.Invoke());
                    }, currentValue);
                    //if (m_WasEdited)
                    {
                        if (!currentValue.IsValid() || currentValue.IsNone())
                        {
                            m_Open = false;
                        }

                        //GUI.changed = true;
                        //m_WasEdited = false;
                    }

                    using (new EditorGUI.DisabledGroupScope(!currentValue.IsValid() || currentValue.IsNone()))
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        m_Open = CoreGUI.BoxToggleButton(
                            m_Open,
                            ColorPalettes.PastelDreams.TiffanyBlue,
                            ColorPalettes.PastelDreams.HotPink,
                            GUILayout.Width(20)
                            );
                        if (change.changed)
                        {
                            if (m_Open)
                            {
                                m_Editor = Editor.CreateEditor(currentValue.GetEditorAsset());
                            }
                            else
                            {
                                m_Editor = null;
                            }
                        }
                    }
                }

                if (!m_Open) return currentValue;

                using (new CoreGUI.BoxBlock(Color.black))
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        m_Editor.DrawHeader();
                        m_Editor.OnInspectorGUI();
                    }

                    if (m_Editor.HasPreviewGUI())
                    {
                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect = GUILayoutUtility.GetRect(rect.width, 100);
                        m_Editor.DrawPreview(rect);
                    }
                }
            }
            
            return currentValue;
        }

        private void DrawPrefabReference(string name, Action<ReferenceAsset> setter, IPrefabReference current)
        {
            GUIContent displayName;
            IPrefabResource objSetting;
            if (current.Equals(PrefabReference.None))
            {
                displayName = new GUIContent("None");
                objSetting = null;
            }
            else if (current.Index >= 0)
            {
                objSetting = current.GetObjectSetting();
                if (objSetting == null)
                {
                    displayName = new GUIContent("INVALID");
                }
                else
                {
                    displayName 
                        = current.IsSubAsset ? 
                        new GUIContent(objSetting.Name + $"[{current.SubAssetName}]") :
                        new GUIContent(objSetting.Name);
                }
            }
            else
            {
                displayName = new GUIContent("INVALID");
                objSetting = null;
            }

            bool clicked;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15);

                if (!string.IsNullOrEmpty(name))
                {
                    GUILayout.Label(name, GUILayout.Width(Screen.width * .25f));
                }
                clicked = CoreGUI.BoxButton(displayName.text, ColorPalettes.PastelDreams.Mint, () =>
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
                            Selection.activeObject = current.GetEditorAsset();
                            EditorGUIUtility.PingObject(Selection.activeObject);
                        });
                    }

                    menu.AddDisabledItem(new GUIContent("Edit"));

                    menu.ShowAsContext();
                });
            }

            if (clicked)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.position = Event.current.mousePosition;

                Type type = current.GetType();
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
                    list, setter, (objSet) =>
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

                PopupWindow.Show(rect, popup);
            }

            return;
        }

        private struct ReferenceAsset
        {
            public static ReferenceAsset None => new ReferenceAsset { index = -2 };

            public long index;
            public FixedString128Bytes subAssetName;
        }
    }
}
