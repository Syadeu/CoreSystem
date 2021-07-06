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

            public Type[] Interfaces = Type.GetInterfaces();
        }
        private static readonly Assembly[] s_Assemblies = AppDomain.CurrentDomain.GetAssemblies();
        private static readonly Type[] s_AllTypes = s_Assemblies.Where(a => !a.IsDynamic).SelectMany(a => a.GetTypes()).ToArray();

        public static Type[] GetTypes(Func<Type, bool> predictate) => s_AllTypes.Where(predictate).ToArray();
    }
}
