using UnityEngine.Events;

namespace Syadeu.Mono
{
    public sealed class ManagedRecycleObject : RecycleableMonobehaviour
    {
        public UnityEvent onCreation;
        public UnityEvent onInitializion;
        public UnityEvent onTermination;

        public override void OnCreated() => onCreation?.Invoke();
        public override void OnInitialize() => onInitializion?.Invoke();
        public override void OnTerminate() => onTermination?.Invoke();
    }
}
