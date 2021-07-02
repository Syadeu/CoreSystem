namespace Syadeu
{
    public struct CoreRoutine : Database.IValidation, System.IEquatable<CoreRoutine>, ICustomYieldAwaiter
    {
        private System.Guid m_Guid;

        /// <summary>
        /// 에디터에서 실행되는 루틴인가요?
        /// </summary>
        public bool IsEditor { get; }
        /// <summary>
        /// 백그라운드에서 실행되는 루틴인가요?
        /// </summary>
        public bool IsBackground { get; }

        internal object Object { get; }
        internal System.Collections.IEnumerator Iterator { get; }

        public string ObjectName { get; }

        /// <summary>
        /// 현재 이 루틴이 실행 중인지 반환합니다.
        /// </summary>
        public bool IsRunning
        {
            get
            {
#if UNITY_EDITOR
                if (IsEditor) return CoreSystem.m_EditorCoroutines.ContainsKey(this);
#endif
                if (IsBackground)
                {
                    return CoreSystem.Instance.m_CustomBackgroundUpdates.ContainsKey(this);
                }
                return CoreSystem.Instance.m_CustomUpdates.ContainsKey(this);
            }
        }
        public bool KeepWait => IsRunning;

        internal CoreRoutine(object obj, System.Collections.IEnumerator iter, bool isEditor, bool isBackground)
        {
            m_Guid = System.Guid.NewGuid();

            Object = obj;
            Iterator = iter;
            
            ObjectName = iter.ToString();

            IsEditor = isEditor;
            IsBackground = isBackground;
        }

        public CoreRoutine(object obj, System.Collections.IEnumerator iter, bool isBackground) 
            : this(obj, iter, false, isBackground)
        {
        }
        public CoreRoutine(System.Collections.IEnumerator iter, bool isBackground)
            : this(null, iter, false, isBackground)
        {
        }

        public bool IsValid() => Iterator != null;
        public bool Equals(CoreRoutine other) => m_Guid.Equals(other.m_Guid);
    }
}
