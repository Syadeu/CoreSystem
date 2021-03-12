namespace Syadeu
{
    public abstract class ManagerEntity : UnityEngine.MonoBehaviour
    {
        public static System.Threading.Thread MainThread { get; protected set; }
        public static System.Threading.Thread BackgroundThread { get; protected set; }

        protected static bool IsMainthread()
            => CoreSystem.IsThisMainthread();

        /// <summary>
        /// OnBackgroundUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        protected static System.Collections.Concurrent.ConcurrentQueue<CoreRoutine> OnBackgroundCustomUpdate { get; } = new System.Collections.Concurrent.ConcurrentQueue<CoreRoutine>();
        /// <summary>
        /// OnUnityUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        protected static System.Collections.Concurrent.ConcurrentQueue<CoreRoutine> OnUnityCustomUpdate { get; } = new System.Collections.Concurrent.ConcurrentQueue<CoreRoutine>();
        /// <summary>
        /// OnUnityUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        public CoreRoutine StartUnityUpdate(System.Collections.IEnumerator enumerator)
        {
            CoreRoutine routine = new CoreRoutine(this, enumerator, false, false);
            OnUnityCustomUpdate.Enqueue(routine);

            return routine;
        }
        /// <summary>
        /// OnBackgroundUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        public CoreRoutine StartBackgroundUpdate(System.Collections.IEnumerator enumerator)
        {
            CoreRoutine routine = new CoreRoutine(this, enumerator, false, true);
            OnBackgroundCustomUpdate.Enqueue(routine);

            return routine;
        }
        public void StopUnityUpdate(CoreRoutine routine) => CoreSystem.RemoveUnityUpdate(routine);
        public void StopBackgroundUpdate(CoreRoutine routine) => CoreSystem.RemoveBackgroundUpdate(routine);
    }
}
