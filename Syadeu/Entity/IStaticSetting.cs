namespace Syadeu
{
    public interface IStaticSetting
    {
        bool Initialized { get; }
        void OnInitialized();
        void Initialize();
    }
}
