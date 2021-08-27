using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class PrefabReferenceDrawer : ObjectDrawer<IPrefabReference>
    {
        private readonly ConstructorInfo m_Constructor;

        public PrefabReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<long>.Type);
        }
        public PrefabReferenceDrawer(object parentObject, Type declaredType, Action<IPrefabReference> setter, Func<IPrefabReference> getter) : base(parentObject, declaredType, setter, getter)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<long>.Type);
        }

        public override IPrefabReference Draw(IPrefabReference currentValue)
        {
            ReflectionHelperEditor.DrawPrefabReference(Name,
                (idx) =>
                {
                    IPrefabReference prefab = (IPrefabReference)m_Constructor.Invoke(new object[] { idx });

                    Setter.Invoke(prefab);
                },
                currentValue);

            return currentValue;
        }
    }
}
