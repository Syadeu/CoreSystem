namespace Syadeu.Database
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RequireGlobalConfigAttribute : System.Attribute
    {
        public ConfigLocation Location;
        public RequireGlobalConfigAttribute(ConfigLocation location) { Location = location; }
    }
}
