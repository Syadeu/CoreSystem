//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif

using Syadeu.Database;
using System;

namespace Syadeu.Presentation
{
    public struct PresentationSystem<T> : IValidation where T : IPresentationSystem
    {
        public static PresentationSystem<T> Null = new PresentationSystem<T>(Hash.Empty, -1);
        private static PresentationSystem<T> s_Instance = Null;
        private static PresentationSystem<T> Instance
        {
            get
            {
                if (!s_Instance.IsValid())
                {
                    if (!PresentationManager.Instance.m_RegisteredGroup.TryGetValue(typeof(T), out Hash hash))
                    {
                        "null out1".ToLog();
                        return Null;
                    }
                    var list = PresentationManager.Instance.m_PresentationGroups[hash].m_Systems;
                    int idx = -1;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].GetType().Equals(typeof(T)))
                        {
                            idx = i;
                            break;
                        }
                    }
                    if (idx < 0)
                    {
                        "null out2".ToLog();
                        return Null;
                    }
                    s_Instance = new PresentationSystem<T>(hash, idx);
                }
                return s_Instance;
            }
        }

        private readonly Hash m_Hash;
        private readonly int m_Index;

        private PresentationSystem(Hash groupHash, int idx)
        {
            m_Hash = groupHash;
            m_Index = idx;
        }

        public bool IsValid() => !m_Hash.Equals(Hash.Empty) && m_Index >= 0;
        public static T GetSystem()
        {
            if (!Instance.IsValid())
            {
                throw new Exception();
            }
            try
            {
                return (T)PresentationManager.Instance.m_PresentationGroups[Instance.m_Hash].m_Systems[Instance.m_Index];
            }
            catch (Exception ex)
            {
                $"{typeof(T).Name}: {Instance.m_Hash}, {Instance.m_Index}".ToLog();

                UnityEngine.Debug.LogError(ex);
                throw;
            }
        }
    }
}
