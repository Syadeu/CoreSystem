using System;

namespace Syadeu
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CustomStaticSettingAttribute : Attribute
    {
        public string CustomPath = "";

        public CustomStaticSettingAttribute() { }
        public CustomStaticSettingAttribute(string customPath)
        {
            CustomPath = customPath;
        }
    }
}
