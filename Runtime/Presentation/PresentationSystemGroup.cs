//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif

using Syadeu.Database;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Syadeu.Presentation
{
    public struct PresentationSystemGroup<T> : IPresentationSystemGroup, IValidation where T : IPresentationRegister
    {
        public static PresentationSystemGroup<T> Null = new PresentationSystemGroup<T>(Hash.Empty);
        private static PresentationSystemGroup<T> s_Instance = Null;
        private static PresentationSystemGroup<T> Instance
        {
            get
            {
                if (!s_Instance.IsValid())
                {
                    s_Instance = new PresentationSystemGroup<T>(Hash.NewHash(typeof(T).Name));
                }
                return s_Instance;
            }
        }

        private readonly Hash m_GroupHash;

        public IReadOnlyList<IPresentationSystem> Systems
        {
            get
            {
                if (!IsValid())
                {
                    throw new Exception();
                }
                return PresentationManager.Instance.m_PresentationGroups[m_GroupHash].m_Systems;
            }
        }

        private PresentationSystemGroup(Hash groupHash)
        {
            m_GroupHash = groupHash;
        }

        public bool IsValid() => !m_GroupHash.Equals(Hash.Empty);

        public static IReadOnlyList<IPresentationSystem> GetSystems() => Instance.Systems;

        public static void Start() => PresentationManager.Instance.StartPresentation(Instance.m_GroupHash);
        public static void Stop() => PresentationManager.Instance.StopPresentation(Instance.m_GroupHash);
    }
}
