namespace Syadeu.Entities
{
    public interface IStaticSetting : IInitialize
    {
        bool Initialized { get; }
        void OnInitialize();
    }
}
