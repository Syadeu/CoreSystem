using UnityEngine.Events;

namespace Syadeu.Mono
{
    public sealed class ManagedRecycleObject : RecycleableMonobehaviour
    {
        public UnityEvent onCreated;
        public UnityEvent onInitialize;
        public UnityEvent onTerminate;

        public override void OnCreated() => onCreated?.Invoke();
        public override void OnInitialize() => onInitialize?.Invoke();
        public override void OnTerminate() => onTerminate?.Invoke();
    }
}
