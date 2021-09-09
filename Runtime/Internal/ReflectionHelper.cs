using Newtonsoft.Json;
using Syadeu.Database;
using System;
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
        /// <summary>
        /// 해당 타입내 Serialize 가 될 수 있는 맴버의 정보를 Array 로 반환합니다.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static MemberInfo[] GetSerializeMemberInfos(Type t)
        {
            return t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where((other) =>
                    {
                        if (other.MemberType != MemberTypes.Field &&
                            other.MemberType != MemberTypes.Property)
                        {
                            return false;
                        }

                        if (!CanSerialized(other)) return false;
                        if (other.Name.Contains(backingField))
                        {
                            string propertyName = SerializeMemberInfoName(other);
                            PropertyInfo property = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (property == null)
                            {
                                $"{other.Name} : {propertyName}".ToLog();
                                return false;
                            }
                            if (!CanSerialized(property)) return false;
                        }

                        return true;
                    })
                    .ToArray();
        }
        private static bool CanSerialized(MemberInfo member)
        {
            if (member.GetCustomAttribute<NonSerializedAttribute>(true) != null ||
                member.GetCustomAttribute<JsonIgnoreAttribute>(true) != null ||
                member.GetCustomAttribute<HideInInspector>() != null) return false;

            if (member is PropertyInfo property)
            {
                if (!property.CanWrite || !property.CanRead) return false;
            }
            return true;
        }
    }
}
