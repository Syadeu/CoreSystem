namespace Syadeu.Collections
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RequireGlobalConfigAttribute : System.Attribute
    {
        internal ConfigLocation m_Location;
        internal string m_Name;

        public RequireGlobalConfigAttribute(ConfigLocation location)
        {
            m_Location = location;
            m_Name = string.Empty;
        }
        public RequireGlobalConfigAttribute(string name)
        {
            m_Location = ConfigLocation.Sub;
            m_Name = name;
        }
    }
}
