#if UNITY_EDITOR
#endif

namespace Syadeu
{
    public interface IStaticSetting
    {
        bool Initialized { get; }
        void OnInitialized();
        void Initialize();
    }
}
