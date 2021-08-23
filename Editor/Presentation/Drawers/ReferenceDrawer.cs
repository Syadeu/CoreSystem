using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using System.Reflection;

namespace SyadeuEditor.Presentation
{
    public sealed class ReferenceDrawer : ObjectDrawer<IReference>
    {
        public ReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override IReference Draw(IReference currentValue)
        {
            Type targetType;
            Type[] generics = DeclaredType.GetGenericArguments();
            if (generics.Length > 0) targetType = DeclaredType.GetGenericArguments()[0];
            else targetType = null;

            ReflectionHelperEditor.DrawReferenceSelector(Name, (idx) =>
            {
                ObjectBase objBase = EntityDataList.Instance.GetObject(idx);

                Type makedT;
                if (targetType != null) makedT = typeof(Reference<>).MakeGenericType(targetType);
                else makedT = TypeHelper.TypeOf<Reference>.Type;

                object temp = TypeHelper.GetConstructorInfo(makedT, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                    new object[] { objBase });

                Setter.Invoke((IReference)temp);
            }, currentValue, targetType);

            return currentValue;
        }
    }
}
