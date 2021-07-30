using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// 시스템의 상위 시스템(<see cref="PresentationSystemEntity{T}"/>)을 선언합니다.<br/>
    /// 해당 시스템이 시작될 때 까지 이 시스템 그룸(<see cref="PresentationSystemGroup{T}"/>) 전체가 기다립니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SubSystemAttribute : Attribute
    {
        internal Type m_Target;

        public SubSystemAttribute(Type target)
        {
            m_Target = target;
        }
    }
}
