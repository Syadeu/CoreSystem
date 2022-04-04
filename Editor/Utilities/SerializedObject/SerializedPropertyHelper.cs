using Syadeu.Collections;
using Syadeu.Presentation;
using System;
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
            return PropertyDrawerHelper.GetTargetObjectOfProperty(t).GetType();
        }
        public static bool IsTypeOf<T>(this SerializedProperty t)
        {
            return TypeHelper.TypeOf<T>.Type.Name.Equals(t.type);
        }
        public static bool IsInArray(this SerializedProperty t)
        {
            return PropertyDrawerHelper.IsPropertyInArray(t);
        }

        public static SerializedProperty GetParent(this SerializedProperty t)
        {
            return PropertyDrawerHelper.GetParentOfProperty(t);
        }
    }
}
