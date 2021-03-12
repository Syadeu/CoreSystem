using Syadeu.Database;
using System;
using System.Collections;

namespace Syadeu
{
    public struct CoreRoutine : IValidation, IEquatable<CoreRoutine>
    {
        private Guid m_Guid;

        public bool IsEditor { get; }
        public bool IsBackground { get; }

        internal object Object { get; }
        internal IEnumerator Iterator { get; }

        public bool IsRunning
        {
            get
            {
                if (IsEditor) return CoreSystem.m_EditorCoroutines.ContainsKey(this);
                if (IsBackground)
                {
                    return CoreSystem.Instance.m_CustomBackgroundUpdates.ContainsKey(this);
                }
                return CoreSystem.Instance.m_CustomUpdates.ContainsKey(this);
            }
        }

        internal CoreRoutine(object obj, IEnumerator iter, bool isEditor, bool isBackground)
        {
            m_Guid = Guid.NewGuid();

            Object = obj;
            Iterator = iter;

            IsEditor = isEditor;
            IsBackground = isBackground;
        }

        public bool IsValid() => Iterator != null;
        public bool Equals(CoreRoutine other) => m_Guid.Equals(other.m_Guid);
    }
}
