using System;
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

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;
using Syadeu.Mono;
using Syadeu.Presentation;
using Unity.Mathematics;
using Syadeu;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;

namespace SyadeuEditor
{
    public sealed class ReflectionHelperEditor
    {
        static ReflectionHelperEditor s_Instance;
        static ReflectionHelperEditor Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new ReflectionHelperEditor();
                }

                return s_Instance;
            }
        }

        readonly Dictionary<object, Drawer> m_CachedDrawer = new Dictionary<object, Drawer>();
        readonly Dictionary<object, AttributeListDrawer> m_CachedAttributeListDrawer = new Dictionary<object, AttributeListDrawer>();

        static GUIStyle m_SelectorStyle = null;
        public static GUIStyle SelectorStyle
        {
            get
            {
                if (m_SelectorStyle == null)
                {
                    GUIStyle st = new GUIStyle(EditorUtils.TextField);
                    st.clipping = TextClipping.Clip;
                    st.stretchWidth = true;
                    st.alignment = TextAnchor.MiddleCenter;
                    st.wordWrap = true;

                    m_SelectorStyle = st;
                }

                return m_SelectorStyle;
            }
        }

        public abstract class DrawerBase
        {

        }
        public sealed class Drawer : DrawerBase
        {
            const string c_EntityObsoleteWarning = "This type has been marked as deprecated.";

            public object m_Instance;
            public Type m_Type;
            public string[] m_Ignores;

            public Drawer(object ins, params string[] ignore)
            {
                m_Instance = ins;
                m_Type = ins.GetType();
                m_Ignores = ignore;
            }

            public object OnGUI() => OnGUI(true);
            public object OnGUI(bool drawHeader, bool ignoreDeprecated = false)
            {
                if (!ignoreDeprecated)
                {
                    ObsoleteAttribute obsolete = m_Type.GetCustomAttribute<ObsoleteAttribute>();
                    if (obsolete != null)
                    {
                        EditorGUILayout.HelpBox(c_EntityObsoleteWarning, MessageType.Warning);
                    }
                }

                if (drawHeader) EditorUtils.StringRich(m_Type.Name, 15, true);
                ReflectionDescriptionAttribute description = m_Type.GetCustomAttribute<ReflectionDescriptionAttribute>();
                if (description != null)
                {
                    EditorGUILayout.HelpBox(description.m_Description, MessageType.Info);
                }

                return DrawObject(m_Instance, m_Ignores);
            }
        }
        public sealed class AttributeListDrawer : DrawerBase
        {
            Color m_Color = Color.black;

            public string m_Name;
            Type m_EntityType;
            Type m_ListType;
            readonly List<Hash> m_CurrentList;
            bool[] m_OpenAttributes;

            Drawer[] m_AttributeDrawers;

            public AttributeListDrawer(string name, Type entityType, List<Hash> list)
            {
                m_Name = name;
                m_EntityType = entityType;
                m_ListType = list.GetType();
                m_CurrentList = list;

                OnListChange();
            }
            private void OnListChange()
            {
                if (m_CurrentList.Count > 0)
                {
                    m_OpenAttributes = new bool[m_CurrentList.Count];
                    m_AttributeDrawers = new Drawer[m_CurrentList.Count];
                }
                else
                {
                    m_OpenAttributes = Array.Empty<bool>();
                    m_AttributeDrawers = Array.Empty<Drawer>();
                }
                
                for (int i = 0; i < m_AttributeDrawers.Length; i++)
                {
                    EntityDataList.Instance.m_Objects.TryGetValue(m_CurrentList[i], out var temp);
                    AttributeBase targetAtt = temp == null ? null : (AttributeBase)temp;
                    if (targetAtt == null) continue;

                    m_AttributeDrawers[i] = GetDrawer(targetAtt);
                }
            }

            public IList<Hash> OnGUI()
            {
                if (string.IsNullOrEmpty(m_Name)) m_Name = "Attributes";

                EditorGUILayout.BeginHorizontal();
                EditorUtils.StringRich(m_Name, 15);
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    m_CurrentList.Add(Hash.Empty);

                    OnListChange();
                }
                if (m_CurrentList.Count > 0 && GUILayout.Button("-", GUILayout.Width(20)))
                {
                    m_CurrentList.RemoveAt(m_CurrentList.Count - 1);

                    OnListChange();
                }
                EditorGUILayout.EndHorizontal();

                using (new EditorUtils.BoxBlock(m_Color))
                {
                    DrawList();
                }
                return m_CurrentList;
            }
            private void DrawList()
            {
                for (int i = 0; i < m_CurrentList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    int idx = i;
                    EditorGUI.BeginChangeCheck();
                    idx = EditorGUILayout.DelayedIntField(idx, GUILayout.Width(40));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (idx >= m_CurrentList.Count) idx = m_CurrentList.Count - 1;

                        Hash cache = m_CurrentList[i];
                        m_CurrentList.RemoveAt(i);
                        m_CurrentList.Insert(idx, cache);

                        OnListChange();
                    }

                    DrawAttributeSelector(null, (attHash) =>
                    {
                        m_CurrentList[i] = attHash;

                        AttributeBase targetAtt = (AttributeBase)EntityDataList.Instance.GetObject(m_CurrentList[i]);
                        if (targetAtt != null)
                        {
                            m_AttributeDrawers[i] = GetDrawer(targetAtt);
                        }
                    }, m_CurrentList[i], m_EntityType);

                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        if (m_CurrentList.Count == 1)
                        {
                            m_CurrentList.Clear();
                            OnListChange();

                            EditorGUILayout.EndHorizontal();
                            break;
                        }

                        m_CurrentList.RemoveAt(i);
                        OnListChange();

                        if (i != 0) i--;
                    }

                    m_OpenAttributes[i] = GUILayout.Toggle(m_OpenAttributes[i],
                        m_OpenAttributes[i] ? EditorUtils.FoldoutOpendString : EditorUtils.FoldoutClosedString
                        , EditorUtils.MiniButton, GUILayout.Width(20));

                    if (GUILayout.Button("C", GUILayout.Width(20)))
                    {
                        AttributeBase cloneAtt = (AttributeBase)EntityDataList.Instance.GetObject(m_CurrentList[i]).Clone();

                        cloneAtt.Hash = Hash.NewHash();
                        cloneAtt.Name += "_Clone";
                        EntityDataList.Instance.m_Objects.Add(cloneAtt.Hash, cloneAtt);

                        m_CurrentList[i] = cloneAtt.Hash;
                        OnListChange();
                    }

                    EditorGUILayout.EndHorizontal();

                    if (m_OpenAttributes[i])
                    {
                        Color color3 = Color.red;
                        color3.a = .7f;

                        //EditorGUI.indentLevel += 1;

                        using (new EditorUtils.BoxBlock(color3))
                        {
                            if (m_AttributeDrawers[i] == null)
                            {
                                EditorGUILayout.HelpBox(
                                    "This attribute is invalid.",
                                    MessageType.Error);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(
                                    "This is shared attribute. Anything made changes in this inspector view will affect to original attribute directly not only as this entity.",
                                    MessageType.Info);

                                SetAttribute(m_CurrentList[i], m_AttributeDrawers[i].OnGUI());
                            }
                        }

                        //EditorGUI.indentLevel -= 1;
                    }

                    if (m_OpenAttributes[i]) EditorUtils.Line();
                }
            }
            private static void SetAttribute(Hash attHash, object att)
            {
                if (attHash.Equals(Hash.Empty)) return;
                if (EntityDataList.Instance.m_Objects.ContainsKey(attHash))
                {
                    EntityDataList.Instance.m_Objects[attHash] = (AttributeBase)att;
                }
                else EntityDataList.Instance.m_Objects.Add(attHash, (AttributeBase)att);
            }
        }

        public static Drawer GetDrawer(object ins, params string[] ignore)
        {
            if (!Instance.m_CachedDrawer.TryGetValue(ins, out Drawer drawer))
            {
                drawer = new Drawer(ins, ignore);
                Instance.m_CachedDrawer.Add(ins, drawer);
            }

            return drawer;
        }
        public static AttributeListDrawer GetAttributeDrawer(Type fromEntity, List<Hash> list)
        {
            if (!Instance.m_CachedAttributeListDrawer.TryGetValue(list, out var drawer))
            {
                drawer = new AttributeListDrawer(string.Empty, fromEntity, list);
                Instance.m_CachedAttributeListDrawer.Add(list, drawer);
            }
            return drawer;
        }

        public static void DrawAssetReference(string name, Action<AssetReference> setter, AssetReference refAsset)
        {
            //float iconHeight = EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing * 3;
            //Vector2 iconSize = EditorGUIUtility.GetIconSize();
            //EditorGUIUtility.SetIconSize(new Vector2(iconHeight, iconHeight));
            string assetPath = AssetDatabase.GUIDToAssetPath(refAsset?.AssetGUID);
            //Texture2D assetIcon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;

            string displayName;
            AddressableAssetEntry entry = null;
            if (refAsset != null /*&& refAsset.IsValid()*/)
            {
                entry = AddressableAssetSettingsDefaultObject.GetSettings(true).FindAssetEntry(refAsset.AssetGUID);
                if (entry == null)
                {
                    displayName = "Not Addressable: " + assetPath.Split('/').Last();
                }
                else displayName = entry.address.Split('/').Last();
            }
            else displayName = "None";

            EditorGUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(name)) EditorGUILayout.LabelField(name);

            if (GUILayout.Button(displayName, SelectorStyle, GUILayout.ExpandWidth(true)))
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.position = Event.current.mousePosition;

                PopupWindow.Show(rect, AssetReferencePopup.GetWindow(setter, refAsset?.AssetGUID, displayName));
            }

            //Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            //rect = EditorGUI.IndentedRect(rect);
            //if (EditorGUI.DropdownButton(rect, new GUIContent(displayName, assetIcon), FocusType.Passive/*, new GUIStyle("ObjectField")*/))
            //{
            //    rect = GUILayoutUtility.GetLastRect();
            //    rect.position = Event.current.mousePosition;

            //    PopupWindow.Show(rect, AssetReferencePopup.GetWindow(setter, refAsset?.AssetGUID, displayName));
            //}

            EditorGUILayout.EndHorizontal();
            //EditorGUIUtility.SetIconSize(iconSize);
        }
        public static void DrawPrefabReference(string name, Action<int> setter, IPrefabReference current)
        {
            string displayName;
            if (current.Index >= 0)
            {
                PrefabList.ObjectSetting objSetting = current.GetObjectSetting();
                displayName = objSetting == null ? "INVALID" : objSetting.m_Name;
            }
            else if (current.Equals(PrefabReference.None))
            {
                displayName = "None";
            }
            else
            {
                displayName = "INVALID";
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);

            if (!string.IsNullOrEmpty(name)) GUILayout.Label(name, GUILayout.Width(Screen.width * .25f));

            if (GUILayout.Button(displayName, SelectorStyle, GUILayout.ExpandWidth(true)))
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
                            if (other.m_RefPrefab.editorAsset == null) return false;

                            if (type.GenericTypeArguments[0].IsAssignableFrom(other.m_RefPrefab.editorAsset.GetType()))
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

                try
                {
                    PopupWindow.Show(rect, SelectorPopup<int, PrefabList.ObjectSetting>.GetWindow(list, setter, (objSet) =>
                    {
                        for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
                        {
                            if (objSet.Equals(PrefabList.Instance.ObjectSettings[i])) return i;
                        }
                        return -1;
                    }, -2));
                }
                catch (ExitGUIException)
                {
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        private static void DrawAttributeSelector(string name, Action<Hash> setter, Hash current, Type entityType)
        {
            string displayName;
            EntityDataList.Instance.m_Objects.TryGetValue(current, out var attVal);
            //AttributeBase att = (AttributeBase)EntityDataList.Instance.GetObject(current);
            AttributeBase att = attVal == null ? null : (AttributeBase)attVal;
            if (current.Equals(Hash.Empty)) displayName = "None";
            else if (att == null) displayName = "Attribute Not Found";
            else displayName = att.Name;

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);

            if (!string.IsNullOrEmpty(name))
            {
                GUILayout.Label(name);
            }

            if (GUILayout.Button(displayName, SelectorStyle, GUILayout.ExpandWidth(true)))
            {
                EntityAcceptOnlyAttribute acceptOnly = entityType.GetCustomAttribute<EntityAcceptOnlyAttribute>();
                if (acceptOnly != null && (
                    acceptOnly.AttributeTypes == null || 
                    acceptOnly.AttributeTypes.Length == 0))
                {
                    throw new Exception($"entity({entityType.Name}) has null attribute accepts");
                }

                Rect tempRect = GUILayoutUtility.GetLastRect();
                tempRect.position = Event.current.mousePosition;

                var atts = EntityDataList.Instance.GetAttributes()
                    .Where((other) =>
                    {
                        Type attType = other.GetType();
                        bool attCheck = false;
                        if (acceptOnly != null)
                        {
                            for (int i = 0; i < acceptOnly.AttributeTypes.Length; i++)
                            {
                                if (acceptOnly.AttributeTypes[i].IsAssignableFrom(attType))
                                {
                                    attCheck = true;
                                    break;
                                }
                            }
                        }
                        else attCheck = true;

                        if (!attCheck) return false;
                        attCheck = false;

                        AttributeAcceptOnlyAttribute requireEntity = attType.GetCustomAttribute<AttributeAcceptOnlyAttribute>();
                        if (requireEntity == null) return true;

                        if (requireEntity.Types == null || requireEntity.Types.Length == 0)
                        {
                            return false;
                        }
                        else
                        {
                            for (int i = 0; i < requireEntity.Types.Length; i++)
                            {
                                if (requireEntity.Types[i].IsAssignableFrom(entityType))
                                {
                                    attCheck = true;
                                    break;
                                }
                            }
                        }

                        if (!attCheck) return false;
                        return true;
                    })
                    .ToArray();
                PopupWindow.Show(tempRect, 
                    SelectorPopup<Hash, AttributeBase>.GetWindow(atts, setter, (att) =>
                    {
                        return att.Hash;
                    }, Hash.Empty)
                    );
            }

            GUILayout.EndHorizontal();
        }
        public static void DrawReferenceSelector(string name, Action<Hash> setter, IReference current, Type targetType)
        {
            string displayName;
            if (current == null || current.Hash.Equals(Hash.Empty)) displayName = "None";
            else
            {
                ObjectBase objBase = EntityDataList.Instance.GetObject(current.Hash);
                if (objBase == null) displayName = "None";
                else displayName = objBase.Name;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            
            if (!string.IsNullOrEmpty(name))
            {
                GUILayout.Label(name, GUILayout.Width(Screen.width * .25f));
            }
            
            if (GUILayout.Button(displayName, SelectorStyle, GUILayout.ExpandWidth(true)))
            {
                Rect rect = GUILayoutUtility.GetRect(150, 300);
                rect.position = Event.current.mousePosition;

                if (targetType == null)
                {
                    try
                    {
                        PopupWindow.Show(rect, SelectorPopup<Hash, ObjectBase>.GetWindow(
                        list: EntityDataList.Instance.m_Objects.Values.ToArray(),
                        setter: setter,
                        getter: (att) =>
                        {
                            return att.Hash;
                        },
                        noneValue: Hash.Empty
                        ));
                    }
                    catch (ExitGUIException)
                    {
                    }
                }
                else if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(targetType))
                {
                    ObjectBase[] entities = EntityDataList.Instance.GetEntities()
                        .Where((other) => other.GetType().Equals(targetType) ||
                                targetType.IsAssignableFrom(other.GetType()))
                        .ToArray();

                    try
                    {
                        PopupWindow.Show(rect, SelectorPopup<Hash, ObjectBase>.GetWindow(
                        list: entities,
                        setter: setter,
                        getter: (att) =>
                        {
                            return att.Hash;
                        },
                        noneValue: Hash.Empty
                        ));
                    }
                    catch (ExitGUIException)
                    {
                    }
                }
                else
                {
                    AttributeBase[] attributes = EntityDataList.Instance.GetAttributes()
                        .Where((other) => other.GetType().Equals(targetType) ||
                                targetType.IsAssignableFrom(other.GetType()))
                        .ToArray();

                    try
                    {
                        PopupWindow.Show(rect, SelectorPopup<Hash, AttributeBase>.GetWindow(
                        list: attributes,
                        setter: setter,
                        getter: (att) =>
                        {
                            return att.Hash;
                        },
                        noneValue: Hash.Empty
                        ));
                    }
                    catch (ExitGUIException)
                    {
                    }
                }
            }

            GUILayout.EndHorizontal();
        }
        
        public static IList DrawList(string name, IList list)
        {
            Type declaredType = list.GetType();

            Color color1 = Color.black, color2 = Color.gray, color3 = Color.green;
            color1.a = .5f; color2.a = .5f; color3.a = .5f;
            Color originColor = GUI.backgroundColor;
            int prevIndent = EditorGUI.indentLevel;

            using (new EditorUtils.BoxBlock(color3))
            {
                #region Header
                EditorGUILayout.BeginHorizontal();
                if (!string.IsNullOrEmpty(name)) EditorUtils.StringRich(name, 13);
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    if (list.IsFixedSize)
                    {
                        Array newArr = Array.CreateInstance(declaredType.GetElementType(), list != null ? list.Count + 1 : 1);
                        if (list != null && list.Count > 0) Array.Copy((Array)list, newArr, list.Count);
                        list = newArr;
                    }
                    else
                    {
                        list.Add(Activator.CreateInstance(declaredType.GetGenericArguments()[0]));
                    }
                }
                if (list?.Count > 0 && GUILayout.Button("-", GUILayout.Width(20)))
                {
                    if (list.IsFixedSize)
                    {
                        Array newArr = Array.CreateInstance(declaredType.GetElementType(), list.Count - 1);
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

                if (list != null)
                {
                    Type elementType = declaredType.GetElementType();
                    MemberInfo[] insider = ReflectionHelper.GetSerializeMemberInfos(elementType);

                    for (int j = 0; j < list.Count; j++)
                    {
                        EditorGUI.indentLevel++;

                        GUILayout.BeginHorizontal(EditorUtils.Box);
                        using (new EditorUtils.BoxBlock(j % 2 == 0 ? color1 : color2))
                        {
                            if (list[j] == null)
                            {
                                if (elementType.Equals(TypeHelper.TypeOf<string>.Type))
                                {
                                    list[j] = string.Empty;
                                }
                                else
                                {
                                    list[j] = Activator.CreateInstance(elementType);
                                }
                            }

                            #region CoreSystem Types
                            if (TypeHelper.TypeOf<IReference>.Type.IsAssignableFrom(elementType))
                            {
                                IReference objRef = (IReference)list[j];
                                Type targetType;
                                Type[] generics = elementType.GetGenericArguments();
                                if (generics.Length > 0) targetType = elementType.GetGenericArguments()[0];
                                else targetType = null;

                                DrawReferenceSelector(string.Empty, (idx) =>
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
                            else if (elementType.Equals(TypeHelper.TypeOf<LuaScript>.Type))
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
                            else if (DrawUnityField(list[j], elementType, string.Empty, (other) => list[j], out object value))
                            {
                                list[j] = value;
                            }
                            else if (DrawUnityMathField(list[j], elementType, string.Empty, (other) => list[j], out value))
                            {
                                list[j] = value;
                            }
                            else if (elementType.Equals(TypeHelper.TypeOf<AssetReference>.Type))
                            {
                                AssetReference refAsset = (AssetReference)list[j];
                                DrawAssetReference(string.Empty, (other) => list[j] = other, refAsset);
                            }
                            #endregion
                            else if (DrawSystemField(list[j], elementType, string.Empty, (other) => list[j], out value))
                            {
                                list[j] = value;
                            }
                            else
                                list[j] = DrawObject(list[j]);
                        }
                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            if (list.IsFixedSize)
                            {
                                IList newArr = Array.CreateInstance(declaredType.GetElementType(), list.Count - 1);
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
                }
            }

            return list;
        }

        public static object DrawObject(object obj, params string[] ignores)
        {
            Type objType = obj.GetType();
            MemberInfo[] members = ReflectionHelper.GetSerializeMemberInfos(objType);

            Type declaredType;
            Action<object, object> setter;
            Func<object, object> getter;
            string name;
            for (int i = 0; i < members.Length; i++)
            {
                if (ignores.Contains(members[i].Name)) continue;

                if (members[i] is FieldInfo field)
                {
                    declaredType = field.FieldType;

                    setter = field.SetValue;
                    getter = field.GetValue;
                }
                else if (members[i] is PropertyInfo property)
                {
                    declaredType = property.PropertyType;

                    setter = property.SetValue;
                    getter = property.GetValue;
                }
                else throw new NotImplementedException();

                if (members[i] is EventInfo ||
                    TypeHelper.TypeOf<Delegate>.Type.IsAssignableFrom(declaredType))
                {
                    continue;
                }

                name = ReflectionHelper.SerializeMemberInfoName(members[i]);

                #region Member Attributes

                var attributes = members[i].GetCustomAttributes();
                foreach (var item in attributes)
                {
                    if (item is SpaceAttribute)
                    {
                        EditorGUILayout.Space();
                    }
                    else if (item is TooltipAttribute tooltip)
                    {
                        EditorGUILayout.HelpBox(tooltip.tooltip, MessageType.Info);
                    }
                    else if (item is ReflectionDescriptionAttribute description)
                    {
                        EditorGUILayout.HelpBox(description.m_Description, MessageType.Info);
                    }
                    else if (item is HeaderAttribute header)
                    {
                        EditorUtils.Line();
                        EditorUtils.StringRich(header.header, 15);
                    }
                }

                #endregion

                EditorGUI.BeginDisabledGroup(members[i].GetCustomAttribute<ReflectionSealedViewAttribute>() != null);

                #region Unity Types
                if (DrawUnityField(obj, declaredType, name, getter, out object value))
                {
                    setter.Invoke(obj, value);
                }
                else if (DrawUnityMathField(obj, declaredType, name, getter, out value))
                {
                    setter.Invoke(obj, value);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<AssetReference>.Type))
                {
                    AssetReference refAsset = (AssetReference)getter.Invoke(obj);
                    DrawAssetReference(name, (other) => setter.Invoke(obj, other), refAsset);
                }
                #endregion
                #region CoreSystem Types
                else if (TypeHelper.TypeOf<IReference>.Type.IsAssignableFrom(declaredType))
                {
                    IReference objRef = (IReference)getter.Invoke(obj);
                    Type targetType;
                    Type[] generics = declaredType.GetGenericArguments();
                    if (generics.Length > 0) targetType = declaredType.GetGenericArguments()[0];
                    else targetType = null;

                    DrawReferenceSelector(name, (idx) =>
                    {
                        ObjectBase objBase = EntityDataList.Instance.GetObject(idx);

                        Type makedT;
                        if (targetType != null) makedT = typeof(Reference<>).MakeGenericType(targetType);
                        else makedT = TypeHelper.TypeOf<Reference>.Type;

                        object temp = TypeHelper.GetConstructorInfo(makedT, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                            new object[] { objBase });

                        setter.Invoke(obj, temp);
                    }, objRef, targetType);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<Hash>.Type))
                {
                    Hash hash = (Hash)getter.Invoke(obj);
                    long temp = EditorGUILayout.LongField(name, long.Parse(hash.ToString()));
                    setter.Invoke(obj, new Hash(ulong.Parse(temp.ToString())));

                    //setter.Invoke(obj, new Hash(ulong.Parse(EditorGUILayout.LongField(name, (long.Parse(getter.Invoke(obj).ToString()))).ToString())));
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<ValuePairContainer>.Type))
                {
                    ValuePairContainer container = (ValuePairContainer)getter.Invoke(obj);
                    if (container == null)
                    {
                        container = new ValuePairContainer();
                        setter.Invoke(obj, container);
                    }

                    container.DrawValueContainer(name);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<LuaScript>.Type))
                {
                    LuaScript scr = (LuaScript)getter.Invoke(obj);
                    if (scr == null)
                    {
                        scr = string.Empty;
                        setter.Invoke(obj, scr);
                    }
                    scr.DrawFunctionSelector(name);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<LuaScriptContainer>.Type))
                {
                    LuaScriptContainer container = (LuaScriptContainer)getter.Invoke(obj);
                    if (container == null)
                    {
                        container = new LuaScriptContainer();
                        setter.Invoke(obj, container);
                    }
                    container.DrawGUI(name);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<PrefabReference>.Type))
                {
                    PrefabReference prefabRef = (PrefabReference)getter.Invoke(obj);
                    DrawPrefabReference(name, (idx) => setter.Invoke(obj, new PrefabReference(idx)), prefabRef);
                }
                #endregion
                else if (DrawSystemField(obj, declaredType, name, getter, out value))
                {
                    setter.Invoke(obj, value);
                }
                else if (declaredType.IsArray)
                {
                    IList list = (IList)getter.Invoke(obj);
                    if (list == null) list = Array.CreateInstance(declaredType.GetElementType(), 0);

                    setter.Invoke(obj, DrawList(name, list));
                }
                else
                {
                    //setter(obj, DrawObject(getter(obj)));
                    EditorGUILayout.LabelField($"Not Supported Type: {name}.{declaredType.Name}");
                }

                EditorGUI.EndDisabledGroup();
            }

            return obj;
        }
        public static bool DrawSystemField(object ins, Type declaredType, string name, Func<object, object> getter, out object value)
        {
            value = null;
            if (declaredType.Equals(TypeHelper.TypeOf<int>.Type))
            {
                value = EditorGUILayout.IntField(name, (int)getter.Invoke(ins));
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<bool>.Type))
            {
                value = EditorGUILayout.Toggle(name, (bool)getter.Invoke(ins));
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<float>.Type))
            {
                value = EditorGUILayout.FloatField(name, (float)getter.Invoke(ins));
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<double>.Type))
            {
                value = EditorGUILayout.DoubleField(name, (double)getter.Invoke(ins));
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<long>.Type))
            {
                value = EditorGUILayout.LongField(name, (long)getter.Invoke(ins));
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<ulong>.Type))
            {
                value = ulong.Parse(EditorGUILayout.LongField(name, (long.Parse(getter.Invoke(ins).ToString()))).ToString());
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<string>.Type))
            {
                object stringTarget = getter.Invoke(ins);
                string currentValue;
                if (stringTarget == null) currentValue = string.Empty;
                else currentValue = (string)getter.Invoke(ins);

                if (currentValue.Length > 30)
                {
                    EditorGUILayout.LabelField(name);
                    value = EditorGUILayout.TextArea(currentValue, GUILayout.Height(50));
                }
                else value = EditorGUILayout.TextField(name, currentValue);

                return true;
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

                value = selected;
                return true;
            }
            //else EditorGUILayout.LabelField($"not added {declaredType.Name}");

            return false;
        }
        public static bool DrawUnityField(object ins, Type declaredType, string name, Func<object, object> getter, out object value)
        {
            value = null;
            if (TypeHelper.TypeOf<UnityEngine.Object>.Type.IsAssignableFrom(declaredType))
            {
                EditorGUILayout.LabelField(name, "UnityEngine.Object 는 json 으로 저장할 수 없으므로 지원하지 않습니다.");
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Rect>.Type))
            {
                value = EditorGUILayout.RectField(name, (Rect)getter.Invoke(ins));
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<RectInt>.Type))
            {
                value = EditorGUILayout.RectIntField(name, (RectInt)getter.Invoke(ins));
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Vector3>.Type))
            {
                value = EditorGUILayout.Vector3Field(name, (Vector3)getter.Invoke(ins));
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Vector3Int>.Type))
            {
                value = EditorGUILayout.Vector3IntField(name, (Vector3Int)getter.Invoke(ins));
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Quaternion>.Type))
            {
                EditorGUILayout.LabelField("UnityEngine.Quaternion is not support." +
                    "Use Unity.Mathematics.quaternion");
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Color>.Type))
            {
                value = EditorGUILayout.ColorField(name, (Color)getter.Invoke(ins));
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Color32>.Type))
            {
                value = EditorGUILayout.ColorField(name, (Color32)getter.Invoke(ins));
                return true;
            }
            //else if (declaredType.Equals(TypeHelper.TypeOf<InputAction>.Type))
            //{
            //    InputAction inputAction = (InputAction)getter.Invoke(ins);
            //    EditorGUILayout.EnumPopup("inputType: ", inputAction.type);
            //    for (int i = 0; i < inputAction.controls.Count; i++)
            //    {
            //        EditorGUILayout.TextField(inputAction.controls[i].path);
            //    }
                
            //    //new SerializedProperty(inputAction);
            //}
                //else EditorGUILayout.LabelField($"not added {declaredType.Name}");

                return false;
        }
        public static bool DrawUnityMathField(object ins, Type declaredType, string name, Func<object, object> getter, out object value)
        {
            value = null;
            if (declaredType.Equals(TypeHelper.TypeOf<int2>.Type))
            {
                int2 gets = (int2)getter.Invoke(ins);
                Vector2Int temp = EditorGUILayout.Vector2IntField(name, new Vector2Int(gets.x, gets.y));

                value = new int2(temp.x, temp.y);
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<int3>.Type))
            {
                int3 gets = (int3)getter.Invoke(ins);
                Vector3Int temp = EditorGUILayout.Vector3IntField(name, new Vector3Int(gets.x, gets.y, gets.z));

                value = new int3(temp.x, temp.y, temp.z);
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<float2>.Type))
            {
                float2 temp = EditorGUILayout.Vector2Field(name, (float2)getter.Invoke(ins));
                value = temp;
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<float3>.Type))
            {
                float3 temp = EditorGUILayout.Vector3Field(name, (float3)getter.Invoke(ins));
                value = temp;
                return true;
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<quaternion>.Type))
            {
                Vector4 temp =
                    EditorGUILayout.Vector4Field(name, ((quaternion)getter.Invoke(ins)).value);
                value = new quaternion(temp);
                return true;
            }

            return false;
        }
    }
}
