namespace Syadeu
{
    public interface IStaticSetting : IInitialize
    {
        bool Initialized { get; }
        void OnInitialized();
    }
}
