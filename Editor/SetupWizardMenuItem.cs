#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
#endif

using System;

namespace SyadeuEditor
{
    /// <summary>
    /// <see cref="CoreSystemSetupWizard"/> 에 새로운 메뉴탭을 추가할 수 있는 <see langword="abstract"/> 입니다.
    /// </summary>
    public abstract class SetupWizardMenuItem : IComparable<SetupWizardMenuItem>
    {
        public abstract string Name { get; }
        public abstract int Order { get; }

        public virtual void OnInitialize() { }
        public abstract void OnGUI();
        public virtual bool Predicate() => true;

        int IComparable<SetupWizardMenuItem>.CompareTo(SetupWizardMenuItem other)
        {
            if (Order < other.Order) return -1;
            else if (Order > other.Order) return 1;
            return 0;
        }
    }
}
