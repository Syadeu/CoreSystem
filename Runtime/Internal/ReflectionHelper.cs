using Newtonsoft.Json;
using Syadeu.Database;
using System;
using System.Linq;
using System.Reflection;

namespace Syadeu.Internal
{
    public sealed class ReflectionHelper
    {
        public static string SerializeMemberInfoName(MemberInfo memberInfo)
        {
            const string backingField = "k__BackingField";
            const string memberPrefix = "m_";

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
            }

            return output;
        }
        public static MemberInfo[] GetSerializeMemberInfos(Type t)
        {
            return t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where((other) =>
                    {
                        if (other.MemberType != MemberTypes.Field)
                        {
                            return false;
                        }

                        return other.GetCustomAttribute<NonSerializedAttribute>() == null;
                    })
                    .ToArray();
        }
    }
}
