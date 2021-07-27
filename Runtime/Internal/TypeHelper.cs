using System;
using System.Linq;
using System.Reflection;

namespace Syadeu.Internal
{
    public sealed class TypeHelper
    {
        public sealed class TypeOf<T>
        {
            public static Type Type = typeof(T);
            public static string Name = Type.Name;
            public static string FullName = Type.FullName;
            public static bool IsAbstract = Type.IsAbstract;
            public static bool IsArray = Type.IsArray;

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
