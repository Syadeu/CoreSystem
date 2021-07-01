namespace Syadeu.Database
{
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigValueAttribute : System.Attribute
    {
        public string Header = null;
        public string Name = null;
    }
}
