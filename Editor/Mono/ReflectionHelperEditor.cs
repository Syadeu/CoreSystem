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

namespace SyadeuEditor
{
    public sealed class ReflectionHelperEditor
    {
        static GUIStyle m_SelectorStyle = null;
        static GUIStyle SelectorStyle
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

        public sealed class Drawer
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
        public static Drawer GetDrawer(object ins, params string[] ignore) => new Drawer(ins, ignore);
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
        public static void DrawPrefabReference(string name, Action<int> setter, PrefabReference current)
        {
            string displayName;
            if (current.m_Idx >= 0) displayName = current.GetObjectSetting().m_Name;
            else displayName = "None";

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);

            if (!string.IsNullOrEmpty(name)) GUILayout.Label(name);

            if (GUILayout.Button(displayName, SelectorStyle, GUILayout.ExpandWidth(true)))
            {
                Rect rect = GUILayoutUtility.GetLastRect();
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
        public static void DrawAttributeSelector(string name, Action<Hash> setter, Hash current, Type entityType)
        {
            string displayName;
            AttributeBase att = (AttributeBase)EntityDataList.Instance.GetObject(current);
            if (current.Equals(Hash.Empty)) displayName = "None";
            else if (att == null) displayName = "Attribute Not Found";
            else displayName = att.Name;

            EntityAcceptOnlyAttribute acceptOnly = entityType.GetCustomAttribute<EntityAcceptOnlyAttribute>();

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);

            if (!string.IsNullOrEmpty(name))
            {
                GUILayout.Label(name);
            }

            if (GUILayout.Button(displayName, SelectorStyle, GUILayout.ExpandWidth(true)))
            {
                Rect tempRect = GUILayoutUtility.GetLastRect();
                tempRect.position = Event.current.mousePosition;

                var atts = EntityDataList.Instance.GetAttributes()
                    .Where((other) =>
                    {
                        Type attType = other.GetType();
                        if (acceptOnly != null)
                        {
                            if (!acceptOnly.AttributeType.IsAssignableFrom(attType)) return false;
                        }

                        AttributeAcceptOnlyAttribute requireEntity = attType.GetCustomAttribute<AttributeAcceptOnlyAttribute>();
                        if (requireEntity == null) return true;

                        if (requireEntity.Type.Equals(entityType)) return true;
                        return false;
                    })
                    .ToArray();
                PopupWindow.Show(tempRect, 
                    SelectorPopup<Hash, AttributeBase>.GetWindow(atts, setter, (att) =>
                    {
                        return att.Hash;
                    })
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
                GUILayout.Label(name);
            }
            
            if (GUILayout.Button(displayName, SelectorStyle, GUILayout.ExpandWidth(true)))
            {
                Rect rect = GUILayoutUtility.GetRect(150, 300);
                rect.position = Event.current.mousePosition;

                if (targetType == null)
                {
                    PopupWindow.Show(rect, SelectorPopup<Hash, ObjectBase>.GetWindow(EntityDataList.Instance.m_Objects.Values.ToArray(), setter, (att) =>
                    {
                        return att.Hash;
                    }));
                }
                else if (
                    TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(targetType))
                {
                    ObjectBase[] entities = EntityDataList.Instance.GetEntities()
                        .Where((other) => other.GetType().Equals(targetType) ||
                                targetType.IsAssignableFrom(other.GetType()))
                        .ToArray();

                    PopupWindow.Show(rect, SelectorPopup<Hash, ObjectBase>.GetWindow(entities, setter, (att) =>
                    {
                        return att.Hash;
                    }));
                }
                else
                {
                    AttributeBase[] attributes = EntityDataList.Instance.GetAttributes()
                        .Where((other) => other.GetType().Equals(targetType) ||
                                targetType.IsAssignableFrom(other.GetType()))
                        .ToArray();

                    PopupWindow.Show(rect, SelectorPopup<Hash, AttributeBase>.GetWindow(attributes, setter, (att) =>
                    {
                        return att.Hash;
                    }));
                }
            }

            GUILayout.EndHorizontal();
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

                name = ReflectionHelper.SerializeMemberInfoName(members[i]);

                EditorGUI.BeginDisabledGroup(members[i].GetCustomAttribute<ReflectionSealedViewAttribute>() != null);

                if (DrawSystemField(obj, declaredType, name, getter, out object value))
                {
                    setter.Invoke(obj, value);
                }
                else if (declaredType.IsArray)
                {
                    Color color1 = Color.black, color2 = Color.gray, color3 = Color.green;
                    color1.a = .5f; color2.a = .5f; color3.a = .5f;
                    Color originColor = GUI.backgroundColor;
                    int prevIndent = EditorGUI.indentLevel;

                    using (new EditorUtils.BoxBlock(color3))
                    {
                        IList list = (IList)getter.Invoke(obj);
                        #region Header
                        EditorGUILayout.BeginHorizontal();
                        EditorUtils.StringRich(name, 13);
                        if (GUILayout.Button("+", GUILayout.Width(20)))
                        {
                            Array newArr = Array.CreateInstance(declaredType.GetElementType(), list != null ? list.Count + 1 : 1);
                            if (list != null && list.Count > 0) Array.Copy((Array)list, newArr, list.Count);

                            setter.Invoke(obj, newArr);
                            list = newArr;
                        }
                        if (list?.Count > 0 && GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            Array newArr = Array.CreateInstance(declaredType.GetElementType(), list.Count - 1);
                            if (list != null && list.Count > 0) Array.Copy((Array)list, newArr, newArr.Length);

                            setter.Invoke(obj, newArr);
                            list = newArr;
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
                                using (new EditorUtils.BoxBlock(j % 2 == 0 ? color1 : color2))
                                {
                                    if (list[j] == null) list[j] = Activator.CreateInstance(elementType);
                                    list[j] = DrawObject(list[j]);
                                }

                                EditorGUI.indentLevel--;
                                if (j + 1 < list.Count) EditorUtils.Line();
                            }
                        }
                    }
                }
                #region Unity Types
                else if (DrawUnityField(obj, declaredType, name, getter, out value))
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
                //else if (declaredType.DeclaringType != null &&
                //        TypeHelper.TypeOf<IReference>.Type.IsAssignableFrom(declaredType.DeclaringType))
                //{

                //}
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
                else
                {
                    //setter(obj, DrawObject(getter(obj)));
                    EditorGUILayout.LabelField($"Not Supported Type: {name}.{declaredType.Name}");
                }

                EditorGUI.EndDisabledGroup();
            }

            return obj;
        }
        private static bool DrawSystemField(object ins, Type declaredType, string name, Func<object, object> getter, out object value)
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
                value = EditorGUILayout.TextField(name, (string)getter.Invoke(ins));
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
        private static bool DrawUnityField(object ins, Type declaredType, string name, Func<object, object> getter, out object value)
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
            //else EditorGUILayout.LabelField($"not added {declaredType.Name}");

            return false;
        }
        private static bool DrawUnityMathField(object ins, Type declaredType, string name, Func<object, object> getter, out object value)
        {
            value = null;
            if (declaredType.Equals(TypeHelper.TypeOf<int3>.Type))
            {
                int3 gets = (int3)getter.Invoke(ins);
                Vector3Int temp = EditorGUILayout.Vector3IntField(name, new Vector3Int(gets.x, gets.y, gets.z));

                value = new int3(temp.x, temp.y, temp.z);
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
