using Syadeu.Database;
using Syadeu.Internal;
using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="EntityBase"/>의 구성부을 선언할 수 있습니다.<br/><br/>
    /// class 맴버 선언을 그리 추천하고 싶지 않지만, 필요에 의해 선언이 내부에 되었다면,<br/>
    /// 해당 값을 복사하여 인스턴스를 만들기 위해 <see cref="Clone"/>을 override 하여 해당 값을 복사하여야합니다.<br/>
    /// </summary>
    /// <remarks>
    /// <seealso cref="AttributeProcessor"/>를 통해 이 구성부를 이용한 동작부를 선언할 수 있습니다.<br/>
    /// <see cref="AttributeBase"/>와 <seealso cref="AttributeProcessor"/>는 하나처럼 작동합니다.
    /// </remarks>
    public abstract class AttributeBase : IAttribute, ICloneable
    {
        public string Name { get; set; } = "New Attribute";
        [ReflectionSealedView] public Hash Hash { get; set; }

        public virtual object Clone()
        {
            AttributeBase att = (AttributeBase)MemberwiseClone();
            att.Name = string.Copy(Name);

            return att;
        }

        public override string ToString() => Name;
    }
}
