namespace Syadeu
{
    public interface IRecycleable
    {
        void OnInitialize();
        void OnTerminate();

        void Terminate();
    }
}
