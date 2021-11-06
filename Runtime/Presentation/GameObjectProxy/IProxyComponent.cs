#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif


namespace Syadeu.Presentation.Proxy
{
    public interface IProxyComponent
    {
        public void OnProxyCreated(RecycleableMonobehaviour obj);
    }
}
