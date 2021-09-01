using UnityEngine.Events;

namespace Syadeu.Presentation.Proxy
{
    public sealed class ManagedRecycleObject : RecycleableMonobehaviour
    {
        public UnityEvent onCreation;
        public UnityEvent onInitializion;
        public UnityEvent onTermination;

        protected override void OnCreated() => onCreation?.Invoke();
        protected override void OnInitialize() => onInitializion?.Invoke();
        protected override void OnTerminate() => onTermination?.Invoke();
    }
}
