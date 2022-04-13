using Syadeu.Collections;
using Syadeu.Presentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    public static class SerializedPropertyHelper
    {
        private static GUIContent
            s_None = new GUIContent(("None")),
            s_Invalid = new GUIContent("Invalid");

        #region PrefabReference

        public static void ApplyToProperty(this in PrefabReference t, SerializedProperty property) => SetPrefabReference(property, t);
        public static void ApplyToProperty(this in PrefabReference t, SerializedProperty idxProperty, SerializedProperty subAssetNameProperty) => SetPrefabReference(idxProperty, subAssetNameProperty, t.Index, t.SubAssetName);
        public static GUIContent GetDisplayName(this in PrefabReference t)
        {
            if (t.IsNone()) return s_None;
            else
            {
                IPrefabResource resource = t.GetObjectSetting();
                if (resource == null) return s_Invalid;

                if (t.IsSubAsset)
                {
                    return new GUIContent(resource.Name + $"[{t.SubAssetName}]");
                }

                return new GUIContent(resource.Name);
            }
        }

        public static PrefabReference ReadPrefabReference(SerializedProperty property)
        {
            SerializedProperty
                idxProperty = property.FindPropertyRelative("m_Idx"),
                subAssetNameProperty = property.FindPropertyRelative("m_SubAssetName");

            return new PrefabReference(
                idxProperty.longValue,
                ReadFixedString128Bytes(subAssetNameProperty).ToString());
        }
        public static PrefabReference ReadPrefabReference(SerializedProperty idx, SerializedProperty subAssetName)
        {
            return new PrefabReference(
                idx.longValue,
                ReadFixedString128Bytes(subAssetName).ToString());
        }

        public static void SetPrefabReference(SerializedProperty property, PrefabReference prefabReference)
        {
            SerializedProperty
                idxProperty = property.FindPropertyRelative("m_Idx"),
                subAssetNameProperty = property.FindPropertyRelative("m_SubAssetName");

            idxProperty.longValue = prefabReference.Index;
            SetFixedString128Bytes(subAssetNameProperty, prefabReference.SubAssetName);
        }
        public static void SetPrefabReference(
            SerializedProperty idxProperty, SerializedProperty subAssetNameProperty,
            long idx, FixedString128Bytes subAssetName)
        {
            idxProperty.longValue = idx;
            SetFixedString128Bytes(subAssetNameProperty, subAssetName);
        }

        #endregion

        #region Reference

        public static Reference ReadReference(SerializedProperty property)
        {
            SerializedProperty hashProp = property.FindPropertyRelative("m_Hash");
            return new Reference(ReadHash(hashProp));
        }
        public static Reference<T> ReadReference<T>(SerializedProperty property)
            where T : class, IObject
        {
            SerializedProperty hashProp = property.FindPropertyRelative("m_Hash");
            return new Reference<T>(ReadHash(hashProp));
        }
        public static void SetReference(SerializedProperty property, Reference reference)
        {
            SerializedProperty hashProp = property.FindPropertyRelative("m_Hash");
            SetHash(hashProp, reference);
        }

        #endregion

        #region Hash

        public static void ApplyToProperty(this in Hash hash, SerializedProperty property) => SetHash(property, hash);

        public static Hash ReadHash(SerializedProperty property)
        {
            SerializedProperty
                bits = property.FindPropertyRelative("mBits");

            return new Hash((ulong)bits.longValue);
        }
        public static void SetHash(SerializedProperty property, Hash hash)
        {
            SerializedProperty
                bits = property.FindPropertyRelative("mBits");

            bits.longValue = (long)(ulong)hash;
        }

        #endregion

        #region ConstActionReference

        public static void SetConstActionReference(SerializedProperty property, Guid guid, params object[] args)
        {
            ConstActionReferenceSetGuid(property, guid);
            ConstActionReferenceSetArguments(property, args);
        }
        public static void ConstActionReferenceSetGuid(SerializedProperty property, Guid guid)
        {
            var guidProp = property.FindPropertyRelative("m_Guid");
            guidProp.stringValue = guid.ToString();
        }
        public static void ConstActionReferenceSetArguments(SerializedProperty property, params object[] args)
        {
            var argsProp = property.FindPropertyRelative("m_Arguments");

            argsProp.ClearArray();
            for (int i = 0; i < args.Length; i++)
            {
                argsProp.InsertArrayElementAtIndex(0);
            }
            for (int i = 0; i < args.Length; i++)
            {
                argsProp.GetArrayElementAtIndex(i).managedReferenceValue = args[i];
            }
        }

        #endregion

        #region Unity.Collections

        public static void ApplyToProperty(this in FixedString128Bytes t, SerializedProperty property) => SetFixedString128Bytes(property, t);

        private static class FixedString128Fields
        {
            private static FieldInfo
                s_Utf8LengthInBytes, s_bytes;

            public const string
                utf8LengthInBytesStr = "utf8LengthInBytes",
                bytesStr = "bytes";

            public static FieldInfo utf8LengthInBytes
            {
                get
                {
                    if (s_Utf8LengthInBytes == null)
                    {
                        s_Utf8LengthInBytes = TypeHelper.TypeOf<FixedString128Bytes>.GetFieldInfo(utf8LengthInBytesStr);
                    }
                    return s_Utf8LengthInBytes;
                }
            }
            public static FieldInfo bytes
            {
                get
                {
                    if (s_bytes == null)
                    {
                        s_bytes = TypeHelper.TypeOf<FixedString128Bytes>.GetFieldInfo(bytesStr);
                    }

                    return s_bytes;
                }
            }
        }

        public static void SetFixedString128Bytes(
            SerializedProperty property, FixedString128Bytes str)
        {
            SerializedProperty
                utf8LengthInBytes = property.FindPropertyRelative(FixedString128Fields.utf8LengthInBytesStr),
                bytes = property.FindPropertyRelative(FixedString128Fields.bytesStr);

            utf8LengthInBytes.intValue = (ushort)FixedString128Fields.utf8LengthInBytes.GetValue(str);
            FixedBytes126 bytes126 = (FixedBytes126)FixedString128Fields.bytes.GetValue(str);

            SetFixedBytes126(bytes, bytes126);
        }
        public static FixedString128Bytes ReadFixedString128Bytes(SerializedProperty property)
        {
            SerializedProperty
                utf8LengthInBytes = property.FindPropertyRelative(FixedString128Fields.utf8LengthInBytesStr),
                bytes = property.FindPropertyRelative(FixedString128Fields.bytesStr);

            FixedString128Bytes result = new FixedString128Bytes();
            object boxed = result;

            FixedString128Fields.utf8LengthInBytes.SetValue(boxed, (ushort)utf8LengthInBytes.intValue);
            FixedString128Fields.bytes.SetValue(boxed, ReadFixedBytes126(bytes));

            result = (FixedString128Bytes)boxed;

            return result;
        }

        public static FixedBytes126 ReadFixedBytes126(SerializedProperty property)
        {
            SerializedProperty item = property.FindPropertyRelative("offset0000");
            FixedBytes126 result = new FixedBytes126();
            result.offset0000 = ReadFixedBytes16(item);

            item.Next(false);
            result.offset0016 = ReadFixedBytes16(item);

            item.Next(false);
            result.offset0032 = ReadFixedBytes16(item);

            item.Next(false);
            result.offset0048 = ReadFixedBytes16(item);

            item.Next(false);
            result.offset0064 = ReadFixedBytes16(item);

            item.Next(false);
            result.offset0080 = ReadFixedBytes16(item);

            item.Next(false);
            result.offset0096 = ReadFixedBytes16(item);

            item.Next(false);
            result.byte0112 = (byte)item.intValue;

            item.Next(false);
            result.byte0113 = (byte)item.intValue;

            item.Next(false);
            result.byte0114 = (byte)item.intValue;

            item.Next(false);
            result.byte0115 = (byte)item.intValue;

            item.Next(false);
            result.byte0116 = (byte)item.intValue;

            item.Next(false);
            result.byte0117 = (byte)item.intValue;

            item.Next(false);
            result.byte0118 = (byte)item.intValue;

            item.Next(false);
            result.byte0119 = (byte)item.intValue;

            item.Next(false);
            result.byte0120 = (byte)item.intValue;

            item.Next(false);
            result.byte0121 = (byte)item.intValue;

            item.Next(false);
            result.byte0122 = (byte)item.intValue;

            item.Next(false);
            result.byte0123 = (byte)item.intValue;

            item.Next(false);
            result.byte0124 = (byte)item.intValue;

            item.Next(false);
            result.byte0125 = (byte)item.intValue;

            return result;
        }
        public static void SetFixedBytes126(SerializedProperty property, FixedBytes126 bytes)
        {
            SerializedProperty item = property.FindPropertyRelative("offset0000");
            SetFixedBytes16(item, bytes.offset0000);

            item.Next(false);
            SetFixedBytes16(item, bytes.offset0016);

            item.Next(false);
            SetFixedBytes16(item, bytes.offset0032);

            item.Next(false);
            SetFixedBytes16(item, bytes.offset0048);

            item.Next(false);
            SetFixedBytes16(item, bytes.offset0064);

            item.Next(false);
            SetFixedBytes16(item, bytes.offset0080);

            item.Next(false);
            SetFixedBytes16(item, bytes.offset0096);

            item.Next(false);
            item.intValue = bytes.byte0112;

            item.Next(false);
            item.intValue = bytes.byte0113;

            item.Next(false);
            item.intValue = bytes.byte0114;

            item.Next(false);
            item.intValue = bytes.byte0115;

            item.Next(false);
            item.intValue = bytes.byte0116;

            item.Next(false);
            item.intValue = bytes.byte0117;

            item.Next(false);
            item.intValue = bytes.byte0118;

            item.Next(false);
            item.intValue = bytes.byte0119;

            item.Next(false);
            item.intValue = bytes.byte0120;

            item.Next(false);
            item.intValue = bytes.byte0121;

            item.Next(false);
            item.intValue = bytes.byte0122;

            item.Next(false);
            item.intValue = bytes.byte0123;

            item.Next(false);
            item.intValue = bytes.byte0124;

            item.Next(false);
            item.intValue = bytes.byte0125;
        }

        public static FixedBytes16 ReadFixedBytes16(SerializedProperty property)
        {
            SerializedProperty item = property.FindPropertyRelative("byte0000");
            FixedBytes16 result = new FixedBytes16();
            result.byte0000 = (byte)item.intValue;

            item.Next(false);
            result.byte0001 = (byte)item.intValue;

            item.Next(false);
            result.byte0002 = (byte)item.intValue;

            item.Next(false);
            result.byte0003 = (byte)item.intValue;

            item.Next(false);
            result.byte0004 = (byte)item.intValue;

            item.Next(false);
            result.byte0005 = (byte)item.intValue;

            item.Next(false);
            result.byte0006 = (byte)item.intValue;

            item.Next(false);
            result.byte0007 = (byte)item.intValue;

            item.Next(false);
            result.byte0008 = (byte)item.intValue;

            item.Next(false);
            result.byte0009 = (byte)item.intValue;

            item.Next(false);
            result.byte0010 = (byte)item.intValue;

            item.Next(false);
            result.byte0011 = (byte)item.intValue;

            item.Next(false);
            result.byte0012 = (byte)item.intValue;

            item.Next(false);
            result.byte0013 = (byte)item.intValue;

            item.Next(false);
            result.byte0014 = (byte)item.intValue;

            item.Next(false);
            result.byte0015 = (byte)item.intValue;

            return result;
        }
        public static void SetFixedBytes16(SerializedProperty property, FixedBytes16 bytes)
        {
            SerializedProperty item = property.FindPropertyRelative("byte0000");
            item.intValue = bytes.byte0000;

            item.Next(false);
            item.intValue = bytes.byte0001;

            item.Next(false);
            item.intValue = bytes.byte0002;

            item.Next(false);
            item.intValue = bytes.byte0003;

            item.Next(false);
            item.intValue = bytes.byte0004;

            item.Next(false);
            item.intValue = bytes.byte0005;

            item.Next(false);
            item.intValue = bytes.byte0006;

            item.Next(false);
            item.intValue = bytes.byte0007;

            item.Next(false);
            item.intValue = bytes.byte0008;

            item.Next(false);
            item.intValue = bytes.byte0009;

            item.Next(false);
            item.intValue = bytes.byte0010;

            item.Next(false);
            item.intValue = bytes.byte0011;

            item.Next(false);
            item.intValue = bytes.byte0012;

            item.Next(false);
            item.intValue = bytes.byte0013;

            item.Next(false);
            item.intValue = bytes.byte0014;

            item.Next(false);
            item.intValue = bytes.byte0015;
        }

        #endregion

        public static Vector3 GetVector3(this SerializedProperty t)
        {
            if (t.propertyType == SerializedPropertyType.Vector3)
            {
                return t.vector3Value;
            }
            else if (t.propertyType == SerializedPropertyType.Vector3Int)
            {
                return t.vector3IntValue;
            }
            else if (t.IsTypeOf<float3>())
            {
                SerializedProperty
                    x = t.FindPropertyRelative("x"),
                    y = t.FindPropertyRelative("y"),
                    z = t.FindPropertyRelative("z");

                return new Vector3(x.floatValue, y.floatValue, z.floatValue);
            }
            else if (t.IsTypeOf<int3>())
            {
                SerializedProperty
                    x = t.FindPropertyRelative("x"),
                    y = t.FindPropertyRelative("y"),
                    z = t.FindPropertyRelative("z");

                return new Vector3(x.intValue, y.intValue, z.intValue);
            }

            throw new NotImplementedException();
        }

        public static Type GetSystemType(this SerializedProperty t)
        {
            return t.GetTargetObject()?.GetType();
            //return t.GetFieldInfo().FieldType;
        }

        public static bool IsTypeOf<T>(this SerializedProperty t)
        {
            return TypeHelper.TypeOf<T>.Type.Name.Equals(t.type);
        }
        public static bool IsInArray(this SerializedProperty prop)
        {
            if (prop == null) return false;

            const string c_Str = ".Array.data[";
            return prop.propertyPath.Contains(c_Str);
        }
        public static SerializedProperty GetParentArrayOfProperty(this SerializedProperty prop, out int index)
        {
            index = -1;
            if (prop == null) return null;

            string[] elements = prop.propertyPath.Split('.');
            string path = string.Empty;
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i].Equals("Array"))
                {
                    index = System.Convert.ToInt32(elements[i + 1].Replace("data[", string.Empty).Replace("]", string.Empty));
                    break;
                }
                else if (!string.IsNullOrEmpty(path))
                {
                    path += ".";
                }

                path += elements[i];
            }

            return prop.serializedObject.FindProperty(path);
        }
        /// <summary>
        /// Gets the object the property represents.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static object GetTargetObject(this SerializedProperty prop)
        {
            if (prop == null) return null;

            const char spliter = '.';
            const string
                arrayContext = ".Array.data[",
                arrayStart = "[",
                arrayEnd = "]";

            string path = prop.propertyPath.Replace(arrayContext, arrayStart);
            object obj = prop.serializedObject.targetObject;
            string[] elements = path.Split(spliter);
            foreach (string element in elements)
            {
                if (element.Contains(arrayStart))
                {
                    string elementName = element.Substring(0, element.IndexOf(arrayStart));
                    int index = System.Convert.ToInt32(element.Substring(element.IndexOf(arrayStart)).Replace(arrayStart, string.Empty).Replace(arrayEnd, string.Empty));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }

        public static SerializedProperty GetParent(this SerializedProperty prop)
        {
            if (prop == null) return null;

            string path = prop.propertyPath;
            string[] elements = path.Split('.');

            string parentPath = string.Empty;
            for (int i = 0; i < elements.Length - 1; i++)
            {
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parentPath += ".";
                }

                parentPath += elements[i];
            }

            return prop.serializedObject.FindProperty(parentPath);
        }

        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }
        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }

        public static int ChildCount(this SerializedProperty t)
        {
            var temp = t.Copy();
            temp.Next(true);

            int count = 0;
            do
            {
                count++;
            } while (temp.Next(false) && temp.depth > t.depth);

            return count;
        }

        #region Draw Method

        private static Dictionary<Type, PropertyDrawer> s_CachedPropertyDrawers = new Dictionary<Type, PropertyDrawer>();
        private static FieldInfo s_CachedPropertyTypeField, s_CachedPropertyUseChildField;
        private static FieldInfo CachedPropertyTypeField
        {
            get
            {
                if (s_CachedPropertyTypeField == null)
                {
                    const string c_Name = "m_Type";

                    Type drawerAttType = TypeHelper.TypeOf<CustomPropertyDrawer>.Type;
                    s_CachedPropertyTypeField = drawerAttType.GetField(c_Name, BindingFlags.NonPublic | BindingFlags.Instance);
                }

                return s_CachedPropertyTypeField;
            }
        }
        private static FieldInfo CachedPropertyUseChildField
        {
            get
            {
                if (s_CachedPropertyUseChildField == null)
                {
                    const string c_Name = "m_UseForChildren";

                    Type drawerAttType = TypeHelper.TypeOf<CustomPropertyDrawer>.Type;
                    s_CachedPropertyUseChildField = drawerAttType.GetField(c_Name, BindingFlags.NonPublic | BindingFlags.Instance);
                }

                return s_CachedPropertyUseChildField;
            }
        }

        public static float GetPropertyHeight(this SerializedProperty t, GUIContent label)
        {
            PropertyDrawer propertyDrawer = GetPropertyDrawer(t);
            if (propertyDrawer != null)
            {
                return propertyDrawer.GetPropertyHeight(t, label);
            }
            return EditorGUI.GetPropertyHeight(t, label, true);
        }

        public static void Draw(this SerializedProperty t, Rect rect, GUIContent label, bool includeChildren)
        {
            PropertyDrawer propertyDrawer = GetPropertyDrawer(t);

            if (propertyDrawer == null)
            {
                EditorGUI.PropertyField(rect, t, label, includeChildren);
                return;
            }

            propertyDrawer.OnGUI(rect, t, label);
        }
        public static void Draw(this SerializedProperty t, ref AutoRect rect, GUIContent label, bool includeChildren)
        {
            PropertyDrawer propertyDrawer = GetPropertyDrawer(t);

            if (propertyDrawer == null)
            {
                Rect temp = rect.Pop(EditorGUI.GetPropertyHeight(t));
                EditorGUI.PropertyField(
                    temp
                    , t, label, includeChildren);
                //EditorGUI.LabelField(temp, "not found");
                return;
            }

            propertyDrawer.OnGUI(rect.Pop(propertyDrawer.GetPropertyHeight(t, label)), t, label);
        }
        public static bool HasCustomPropertyDrawer(this SerializedProperty t)
        {
            return GetPropertyDrawer(t) != null;
        }
        private static PropertyDrawer GetPropertyDrawer(SerializedProperty t)
        {
            Type propertyType = t.GetSystemType();

            if (!s_CachedPropertyDrawers.TryGetValue(propertyType, out PropertyDrawer propertyDrawer))
            {
                Type foundDrawerType = null;
                Type foundDrawerTargetType = null;

                //UnityEngine.Debug.Log($"{propertyType.Name} start");
                foreach (var drawerType in TypeHelper.GetTypesIter(t => !t.IsAbstract && !t.IsInterface && t.GetCustomAttributes<CustomPropertyDrawer>().Any()))
                {
                    foreach (var customPropertyDrawer in drawerType.GetCustomAttributes<CustomPropertyDrawer>())
                    {
                        Type targetType = (Type)CachedPropertyTypeField.GetValue(customPropertyDrawer);
                        bool useChild = (bool)CachedPropertyUseChildField.GetValue(customPropertyDrawer);
                        //UnityEngine.Debug.Log(
                        //    $"{propertyType.Name}:: target:{targetType.Name} " +
                        //    $"usechild:{useChild} ? {TypeHelper.InheritsFrom(propertyType, targetType)}");
                        if (targetType.Equals(propertyType))
                        {
                            //$"target:{targetType.Name} {propertyType.Name}".ToLog();
                            foundDrawerType = drawerType;

                            break;
                        }
                        else if (
                            useChild && TypeHelper.InheritsFrom(propertyType, targetType))
                        {
                            if (foundDrawerType != null)
                            {
                                // 만약 더 상위를 타겟으로 하고 있으면 교체
                                if (TypeHelper.InheritsFrom(foundDrawerTargetType, targetType))
                                {
                                    foundDrawerType = drawerType;
                                    foundDrawerTargetType = targetType;
                                }

                                continue;
                            }

                            foundDrawerType = drawerType;
                            foundDrawerTargetType = targetType;
                        }
                    }
                }

                if (foundDrawerType != null)
                {
                    propertyDrawer = (PropertyDrawer)Activator.CreateInstance(foundDrawerType);
                }
                s_CachedPropertyDrawers.Add(propertyType, propertyDrawer);
            }

            SetupPropertyDrawer(propertyDrawer, t);
            return propertyDrawer;
        }
        private static void SetupPropertyDrawer(PropertyDrawer propertyDrawer, SerializedProperty property)
        {
            if (propertyDrawer == null) return;

            FieldInfo fieldInfoField = TypeHelper.TypeOf<PropertyDrawer>.GetFieldInfo("m_FieldInfo");
            fieldInfoField.SetValue(propertyDrawer, property.GetFieldInfo());
        }

        #endregion

        public static FieldInfo GetFieldInfo(this SerializedProperty prop)
        {
            if (prop == null) return null;

            string path = prop.propertyPath.Replace(".Array.data[", "[");
            Type t = prop.serializedObject.targetObject.GetType();
            object currentValue = prop.serializedObject.targetObject;
            FieldInfo currentField = null;
            string[] elements = path.Split('.');

            foreach (string element in elements)
            {
                Type currentType = currentField == null ? t : currentField.FieldType;
                if (currentField != null)
                {
                    currentValue = currentField.GetValue(currentValue);
                }

                if (currentType.Equals(TypeHelper.TypeOf<object>.Type))
                {
                    currentType = currentValue.GetType();
                }
                //if (currentType.IsArray) currentType = currentType.GetElementType();

                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf("["));
                    currentField = TypeHelper.GetFieldInfoRecursive(currentType, elementName);
                    if (currentField == null)
                    {
                        throw new Exception($"1. from ({currentType.FullName}) {elementName}:{path}");
                    }
                }
                else
                {
                    currentField = TypeHelper.GetFieldInfoRecursive(currentType, element);
                    if (currentField == null)
                    {
                        throw new Exception($"2. from ({currentType.FullName}) :: {element}:{path}");
                    }
                }
            }

            return currentField;
        }
    }

    //[CustomPropertyDrawer(typeof(List<>), true)]
    //internal sealed class SerializedArrayDrawer : PropertyDrawer<Array>
    //{
    //    protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
    //    {
    //        EditorGUI.LabelField(rect.Pop(), "test");
    //    }
    //}
}
