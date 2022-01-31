namespace CoreSystemAnalyzer
{
    public enum DiagnosticId
    {
        None = 0,

        // Performance enums 1+
        TypeofUsage = 1,
        TypeNameUsage,
        TypeFullNameUsage,
        TypeHashCodeUsage,
        TypeValueTypeUsage,

        //Usage enums 100+
        VarUsage = 100,

        //Style enums 200+
        UsingsOrder = 200,

        //Design enums 300+
    }
}
