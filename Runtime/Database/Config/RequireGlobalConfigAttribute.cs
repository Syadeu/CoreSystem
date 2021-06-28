namespace Syadeu.Database
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RequireGlobalConfigAttribute : System.Attribute
    {
        internal ConfigLocation m_Location;
        internal string m_Name;

        public RequireGlobalConfigAttribute()
        {
            m_Location = ConfigLocation.Global;
        }
        public RequireGlobalConfigAttribute(string name)
        {
            m_Location = ConfigLocation.Sub;
            m_Name = name;
        }
    }
}
