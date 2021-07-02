//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif

using Syadeu.Database;
using System;

namespace Syadeu.Presentation
{
    public struct System<T> : IValidation where T : IPresentationSystem
    {
        public static System<T> Null = new System<T>(Hash.Empty);
        private static System<T> s_Instance = Null;
        internal static System<T> Instance
        {
            get
            {
                if (!s_Instance.IsValid())
                {
                    if (!PresentationManager.Instance.m_RegisteredGroup.TryGetValue(typeof(T), out Hash hash))
                    {
                        return Null;
                    }
                    s_Instance = new System<T>(hash);
                }
                return s_Instance;
            }
        }

        private readonly Hash m_Hash;
        private readonly int m_Index;

        public System(Hash groupHash)
        {
            m_Hash = groupHash;
            if (m_Hash.Equals(Hash.Empty)) m_Index = -1;
            else
            {
                var list = PresentationManager.Instance.m_PresentationGroups[m_Hash].m_Systems;
                int idx = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].GetType().Equals(typeof(T)))
                    {
                        idx = i;
                        break;
                    }
                }

                m_Index = idx;
            }
        }

        public bool IsValid() => !m_Hash.Equals(Hash.Empty) || m_Index < 0;
        public static T GetSystem()
        {
            if (!Instance.IsValid())
            {
                throw new Exception();
            }
            return (T)PresentationManager.Instance.m_PresentationGroups[Instance.m_Hash].m_Systems[Instance.m_Index];
        }
    }
}
