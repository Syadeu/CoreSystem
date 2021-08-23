using Syadeu.Database;
using System.Reflection;

namespace SyadeuEditor.Presentation
{
    public sealed class PrefabReferenceDrawer : ObjectDrawer<PrefabReference>
    {
        public PrefabReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override PrefabReference Draw(PrefabReference currentValue)
        {
            ReflectionHelperEditor.DrawPrefabReference(Name, 
                (idx) =>
                {
                    Setter.Invoke(new PrefabReference(idx));
                }, 
                currentValue);
            return currentValue;
        }
    }
}
