using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Syadeu.Internal
{
    public sealed class TypeHelper
    {
        public sealed class TypeOf<T>
        {
            public static readonly Type Type = typeof(T);
            public static readonly string Name = Type.Name;
            public static readonly string FullName = Type.FullName;
            public static readonly bool IsAbstract = Type.IsAbstract;
            public static readonly bool IsArray = Type.IsArray;

            private static Type[] s_Interfaces = null;
            public static Type[] Interfaces
            {
                get
                {
                    if (s_Interfaces == null) s_Interfaces = Type.GetInterfaces();
                    return s_Interfaces;
                }
            }

            private static MemberInfo[] s_Members = null;
            public static MemberInfo[] Members
            {
                get
                {
                    if (s_Members == null) s_Members = Type.GetMembers((BindingFlags)~0);
                    return s_Members;
                }
            }

            private static MethodInfo[] s_Methods = null;
            public static MethodInfo[] Methods
            {
                get
                {
                    if (s_Methods == null) s_Methods = Type.GetMethods((BindingFlags)~0);
                    return s_Methods;
                }
            }

            public static ConstructorInfo GetConstructorInfo(params Type[] args)
                => TypeHelper.GetConstructorInfo(Type, args);
        }
        public sealed class Enum<T> where T : struct, IConvertible
        {
            public static readonly bool IsFlag = TypeOf<T>.Type.GetCustomAttribute<FlagsAttribute>() != null;
            public static readonly string[] Names = Enum.GetNames(TypeOf<T>.Type);
            public static readonly int[] Values = ((T[])Enum.GetValues(TypeOf<T>.Type)).Select((other) => Convert.ToInt32(other)).ToArray();

            public static string ToString(T enumValue)
            {
                int target = Convert.ToInt32(enumValue);
                if (IsFlag)
                {
                    string temp = string.Empty;
                    for (int i = 0; i < Values.Length; i++)
                    {
                        if (Values[i] == 0 && !target.Equals(0)) continue;
                        if ((target & Values[i]) == Values[i])
                        {
                            if (!string.IsNullOrEmpty(temp)) temp += ", ";
                            temp += Names[i];
                        }
                    }

                    return temp;
                }
                else
                {
                    for (int i = 0; i < Values.Length; i++)
                    {
                        if (target.Equals(Values[i])) return Names[i];
                    }
                }

                throw new ArgumentException(nameof(enumValue));
            }
        }

        private static readonly Assembly[] s_Assemblies = AppDomain.CurrentDomain.GetAssemblies();
        private static readonly Type[] s_AllTypes = s_Assemblies.Where(a => !a.IsDynamic).SelectMany(a => a.GetTypes()).ToArray();

        public static Type[] GetTypes(Func<Type, bool> predictate) => s_AllTypes.Where(predictate).ToArray();
        public static ConstructorInfo GetConstructorInfo(Type t, params Type[] args)
        {
            return t.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, CallingConventions.HasThis, args, null);
        }
    }
}
