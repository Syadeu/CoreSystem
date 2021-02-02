namespace Syadeu
{
    public enum SystemFlag
    {
        None = 0,

        MainSystem = 1 << 0,
        SubSystem = 1 << 1,
        MonoSystems = MainSystem | SubSystem,

        Data = 1 << 2,

        All = ~0
    }
}
