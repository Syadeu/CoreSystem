using Newtonsoft.Json;
using Syadeu.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Syadeu.Internal
{
    public sealed class ReflectionHelper
    {
        const string backingField = "k__BackingField";
        const string memberPrefix = "m_";
        const string underBar = "_";

        public static Type GetDeclaredType(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo field) return field.FieldType;
            else if (memberInfo is PropertyInfo property) return property.PropertyType;

            throw new NotImplementedException();
        }
        public static T GetValue<T>(MemberInfo memberInfo, object obj)
        {
            if (memberInfo is FieldInfo field)
            {
                return (T)field.GetValue(obj);
            }
            else if (memberInfo is PropertyInfo property)
            {
                if (property.GetGetMethod() == null) return default(T);

                return (T)property.GetValue(obj);
            }

            throw new NotImplementedException();
        }
        public static bool IsProperty(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo) return true;
            else if (memberInfo is FieldInfo field && field.IsPinvokeImpl)
            {
                return true;
            }
            return false;
        }
        public static bool IsBackingField(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo field &&
                field.Name.Contains(backingField))
            {
                return true;
            }
            return false;
        }
        public static bool IsPrivate(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                return fieldInfo.IsPrivate;
            }
            else if (memberInfo is PropertyInfo property)
            {
                return property.GetGetMethod().IsPrivate;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 해당 맴버의 이름을 Serialize 정형화 이름으로 바꾸어 반환합니다.
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        public static string SerializeMemberInfoName(MemberInfo memberInfo)
        {
            string output;
            JsonPropertyAttribute jsonProperty = memberInfo.GetCustomAttribute<JsonPropertyAttribute>();
            if (jsonProperty != null && !string.IsNullOrEmpty(jsonProperty.PropertyName))
            {
                output = jsonProperty.PropertyName;
            }
            else
            {
                output = memberInfo.Name;
                if (output.Contains(backingField))
                {
                    output = output.Replace(backingField, string.Empty)
                        .Remove(0, 1);
                    output = output.Remove(output.Length - 1, 1);
                }
                else if (output.StartsWith(memberPrefix))
                {
                    output = output.Remove(0, 2);
                }
                else if (output.StartsWith(underBar))
                {
                    output = output.Remove(0, 1);
                }
            }

            return output;
        }

        private static readonly Dictionary<Type, MemberInfo[]> s_ParsedSerializeMemberInfos = new Dictionary<Type, MemberInfo[]>();

        /// <summary>
        /// 해당 타입내 Serialize 가 될 수 있는 맴버의 정보를 Array 로 반환합니다.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static MemberInfo[] GetSerializeMemberInfos(Type t)
        {
            if (s_ParsedSerializeMemberInfos.TryGetValue(t, out MemberInfo[] info))
            {
                return info;
            }

            var temp = t.GetMembers(
                BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic)
                    .Where((other) =>
                    {
                        if (other.GetCustomAttribute<JsonPropertyAttribute>() != null)
                        {
                            return true;
                        }

                        if (other.MemberType != MemberTypes.Field &&
                            other.MemberType != MemberTypes.Property)
                        {
                            return false;
                        }

                        if (IsBackingField(other) || !CanSerialized(other)) return false;
                        //if (other.Name.Contains(backingField))
                        //{
                        //    string propertyName = SerializeMemberInfoName(other);
                        //    PropertyInfo property = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        //    if (property == null)
                        //    {
                        //        $"{other.Name} : {propertyName}".ToLog();
                        //        return false;
                        //    }
                        //    if (!CanSerialized(property)) return false;
                        //}

                        return true;
                    })
                    .ToList();

            temp.Sort(new Comparer());
            info = temp.ToArray();
            s_ParsedSerializeMemberInfos.Add(t, info);
            return info;
        }
        private struct Comparer : IComparer<MemberInfo>
        {
            public int Compare(MemberInfo x, MemberInfo y)
            {
                JsonPropertyAttribute 
                    a = x.GetCustomAttribute<JsonPropertyAttribute>(),
                    b = y.GetCustomAttribute<JsonPropertyAttribute>();

                if (a == null) return -1;
                else if (b == null) return 1;

                if (a.Order < b.Order) return -1;
                else if (a.Order > b.Order) return 1;

                return 0;
            }
        }
        private static bool CanSerialized(MemberInfo member)
        {
            if (member.GetCustomAttribute<NonSerializedAttribute>(true) != null ||
                member.GetCustomAttribute<JsonIgnoreAttribute>(true) != null ||
                member.GetCustomAttribute<HideInInspector>() != null) return false;

            if (member is PropertyInfo property)
            {
                if (TypeHelper.TypeOf<Delegate>.Type.IsAssignableFrom(property.PropertyType))
                {
                    return false;
                }

                if (!property.CanWrite || !property.CanRead) return false;
            }
            else if (member is FieldInfo field)
            {
                if (TypeHelper.TypeOf<Delegate>.Type.IsAssignableFrom(field.FieldType))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
