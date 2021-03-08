using System;

#if UNITY_EDITOR
#endif

namespace Syadeu
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CustomStaticSettingAttribute : Attribute
    {
        public string CustomPath = "";
    }
}
