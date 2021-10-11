using Syadeu.Collections;
using System.Reflection;

namespace SyadeuEditor.Presentation
{
    public sealed class ValuePairContainerDrawer : ObjectDrawer<ValuePairContainer>
    {
        public ValuePairContainerDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override ValuePairContainer Draw(ValuePairContainer currentValue)
        {
            if (currentValue == null)
            {
                currentValue = new ValuePairContainer();
            }
            currentValue.DrawValueContainer(Name);

            return currentValue;
        }
    }
}
