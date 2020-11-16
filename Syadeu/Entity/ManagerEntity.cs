namespace Syadeu
{
    public abstract class ManagerEntity : UnityEngine.MonoBehaviour
    {
        public static System.Threading.Thread MainThread { get; protected set; }
        public static System.Threading.Thread BackgroundThread { get; protected set; }

        protected static bool IsMainthread()
        {
            if (MainThread == null || System.Threading.Thread.CurrentThread == MainThread)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// OnBackgroundUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        protected static System.Collections.Concurrent.ConcurrentQueue<(object, System.Collections.IEnumerator)> OnBackgroundCustomUpdate { get; } = new System.Collections.Concurrent.ConcurrentQueue<(object, System.Collections.IEnumerator)>();
        /// <summary>
        /// OnUnityUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        protected static System.Collections.Concurrent.ConcurrentQueue<(object, System.Collections.IEnumerator)> OnUnityCustomUpdate { get; } = new System.Collections.Concurrent.ConcurrentQueue<(object, System.Collections.IEnumerator)>();
        /// <summary>
        /// OnUnityUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        public void StartUnityUpdate(System.Collections.IEnumerator enumerator) => OnUnityCustomUpdate.Enqueue((this, enumerator));
        /// <summary>
        /// OnBackgroundUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        public void StartBackgroundUpdate(System.Collections.IEnumerator enumerator) => OnBackgroundCustomUpdate.Enqueue((this, enumerator));
        public void StopUnityUpdate(System.Collections.IEnumerator enumerator) => CoreSystem.RemoveUnityUpdate(this, enumerator);
        public void StopBackgroundUpdate(System.Collections.IEnumerator enumerator) => CoreSystem.RemoveBackgroundUpdate(this, enumerator);
    }
}
