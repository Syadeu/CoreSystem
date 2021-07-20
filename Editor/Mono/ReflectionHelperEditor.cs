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

namespace SyadeuEditor
{
    public sealed class ReflectionHelperEditor
    {
        public sealed class Drawer
        {
            const string c_EntityObsoleteWarning = "This type has been marked as deprecated.";

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
            public void OnGUI(bool drawHeader, bool ignoreDeprecated = false)
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

                for (int i = 0; i < m_Members.Length; i++)
                {
                    DrawMember(m_Instance, m_Members[i]);
                }
            }
        }
        public static Drawer GetDrawer(object ins, params string[] ignore) => new Drawer(ins, ignore);
        public static void DrawAssetReference(string name, Action<AssetReference> setter, AssetReference refAsset)
        {
            float iconHeight = EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing * 3;
            Vector2 iconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(iconHeight, iconHeight));
            string assetPath = AssetDatabase.GUIDToAssetPath(refAsset?.AssetGUID);
            Texture2D assetIcon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;

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
            EditorGUILayout.LabelField(name);

            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            rect = EditorGUI.IndentedRect(rect);
            if (EditorGUI.DropdownButton(rect, new GUIContent(displayName, assetIcon), FocusType.Passive/*, new GUIStyle("ObjectField")*/))
            {
                rect = GUILayoutUtility.GetLastRect();
                rect.position = Event.current.mousePosition;

                PopupWindow.Show(rect, AssetReferencePopup.GetWindow(setter, refAsset?.AssetGUID, displayName));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.SetIconSize(iconSize);
        }
        public static void DrawPrefabReference(string name, Action<int> setter, PrefabReference current)
        {
            string displayName;
            if (current.m_Idx >= 0) displayName = current.GetObjectSetting().m_Name;
            else displayName = "None";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(name));

            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            rect = EditorGUI.IndentedRect(rect);
            if (EditorGUI.DropdownButton(rect, new GUIContent(displayName), FocusType.Passive/*, new GUIStyle("ObjectField")*/))
            {
                rect = GUILayoutUtility.GetLastRect();
                rect.position = Event.current.mousePosition;

                PopupWindow.Show(rect, SelectorPopup<int, PrefabList.ObjectSetting>.GetWindow(PrefabList.Instance.ObjectSettings, setter, (objSet) =>
                {
                    for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
                    {
                        if (objSet.Equals(PrefabList.Instance.ObjectSettings[i])) return i;
                    }
                    return -1;
                }));
            }
            EditorGUILayout.EndHorizontal();
        }
        public static void DrawAttributeSelector(string name, Action<Hash> setter, Hash current)
        {
            string displayName;
            AttributeBase att = EntityDataList.Instance.GetAttribute(current);
            if (current.Equals(Hash.Empty)) displayName = "None";
            else if (att == null) displayName = "Attribute Not Found";
            else displayName = att.Name;

            if (!string.IsNullOrEmpty(name))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(name));
            }

            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            rect = EditorGUI.IndentedRect(rect);
            if (EditorGUI.DropdownButton(rect, new GUIContent(displayName), FocusType.Passive))
            {
                rect = GUILayoutUtility.GetLastRect();
                rect.position = Event.current.mousePosition;

                PopupWindow.Show(rect, SelectorPopup<Hash, AttributeBase>.GetWindow(EntityDataList.Instance.m_Attributes, setter, (att) =>
                {
                    return att.Hash;
                }));
            }
            if (!string.IsNullOrEmpty(name)) EditorGUILayout.EndHorizontal();
        }
        public static void DrawReferenceSelector(string name, Action<Hash> setter, IReference current, Type targetType)
        {
            string displayName;
            if (current == null || current.Equals(Hash.Empty)) displayName = "None";
            else
            {
                ObjectBase objBase = EntityDataList.Instance.GetObject(current.Hash);
                if (objBase == null) displayName = "None";
                else displayName = objBase.Name;
            }

            if (!string.IsNullOrEmpty(name))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(name));
            }
            
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            rect = EditorGUI.IndentedRect(rect);
            if (EditorGUI.DropdownButton(rect, new GUIContent(displayName), FocusType.Passive))
            {
                rect = GUILayoutUtility.GetLastRect();
                rect.position = Event.current.mousePosition;

                if (TypeHelper.TypeOf<EntityBase>.Type.IsAssignableFrom(targetType))
                {
                    PopupWindow.Show(rect, SelectorPopup<Hash, EntityBase>.GetWindow(EntityDataList.Instance.m_Entites, setter, (att) =>
                    {
                        return att.Hash;
                    }));
                }
                else
                {
                    PopupWindow.Show(rect, SelectorPopup<Hash, AttributeBase>.GetWindow(EntityDataList.Instance.m_Attributes, setter, (att) =>
                    {
                        return att.Hash;
                    }));
                }
            }
            if (!string.IsNullOrEmpty(name)) EditorGUILayout.EndHorizontal();
        }
        //public static void DrawEntitySelector(string name, Action<Hash> setter, Hash current)
        //{
        //    string displayName;
        //    if (current.Equals(Hash.Empty)) displayName = "None";
        //    else displayName = EntityDataList.Instance.GetEntity(current).Name;

        //    if (!string.IsNullOrEmpty(name))
        //    {
        //        EditorGUILayout.BeginHorizontal();
        //        EditorGUILayout.LabelField(new GUIContent(name));
        //    }
            
        //    Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
        //    rect = EditorGUI.IndentedRect(rect);
        //    if (EditorGUI.DropdownButton(rect, new GUIContent(displayName), FocusType.Passive))
        //    {
        //        rect = GUILayoutUtility.GetLastRect();
        //        rect.position = Event.current.mousePosition;

        //        PopupWindow.Show(rect, SelectorPopup<Hash, EntityBase>.GetWindow(EntityDataList.Instance.m_Entites, setter, (att) =>
        //        {
        //            return att.Hash;
        //        }));
        //    }
        //    if (!string.IsNullOrEmpty(name)) EditorGUILayout.EndHorizontal();
        //}
        
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

            bool disable = memberInfo.GetCustomAttribute<ReflectionSealedViewAttribute>() != null;

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

            EditorGUI.BeginDisabledGroup(disable);
            #region System Types
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
            #endregion
            #region Unity Types
            else if (TypeHelper.TypeOf<UnityEngine.Object>.Type.IsAssignableFrom(declaredType))
            {
                //UnityEngine.Object obj = EditorGUILayout.ObjectField(name, (UnityEngine.Object)getter.Invoke(ins), declaredType, false);

                //setter.Invoke(ins, obj);
                EditorGUILayout.LabelField(name, "UnityEngine.Object 는 json 으로 저장할 수 없으므로 지원하지 않습니다.");
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Rect>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.RectField(name, (Rect)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<RectInt>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.RectIntField(name, (RectInt)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Vector3>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.Vector3Field(name, (Vector3)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Vector3Int>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.Vector3IntField(name, (Vector3Int)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<AssetReference>.Type))
            {
                AssetReference refAsset = (AssetReference)getter.Invoke(ins);
                DrawAssetReference(name, (other) => setter.Invoke(ins, other), refAsset);
            }
            #endregion
            #region CoreSystem Types
            else if (TypeHelper.TypeOf<IReference>.Type.IsAssignableFrom(declaredType))
            {
                IReference objRef = (IReference)getter.Invoke(ins);
                Type targetType = declaredType.GetGenericArguments()[0];
                DrawReferenceSelector(name, (idx) =>
                {
                    ObjectBase objBase = EntityDataList.Instance.GetObject(idx);

                    Type makedT = typeof(Reference<>).MakeGenericType(targetType);
                    object temp = TypeHelper.GetConstructorInfo(makedT, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                        new object[] { objBase });

                    setter.Invoke(ins, temp);
                }, objRef, targetType);
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
            else if (declaredType.Equals(TypeHelper.TypeOf<PrefabReference>.Type))
            {
                PrefabReference prefabRef = (PrefabReference)getter.Invoke(ins);
                DrawPrefabReference(name, (idx) => setter.Invoke(ins, new PrefabReference(idx)), prefabRef);
            }
            //else if (declaredType.Equals(TypeHelper.TypeOf<EntityReference>.Type))
            //{
            //    EntityReference prefabRef = (EntityReference)getter.Invoke(ins);
            //    DrawEntitySelector(name, (idx) => setter.Invoke(ins, new EntityReference(idx)), prefabRef);
            //}
            #endregion
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
            EditorGUI.EndDisabledGroup();
        }
    }
}
