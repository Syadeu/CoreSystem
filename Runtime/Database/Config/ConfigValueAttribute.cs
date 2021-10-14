namespace Syadeu.Collections
{
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigValueAttribute : System.Attribute
    {
        public string Header = null;
        public string Name = null;

        public string DefaultString = string.Empty;
        public int DefaultInt32 = 0;
        public float DefaultSingle = 0;
        public bool DefaultBoolen = false;
    }
}
