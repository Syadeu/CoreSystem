//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif

using Syadeu.Database;
using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <seealso cref="PresentationManager"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PresentationSystemEntity<T> : IPresentationSystem, IDisposable where T : class
    {
        public abstract bool EnableBeforePresentation { get; }
        public abstract bool EnableOnPresentation { get; }
        public abstract bool EnableAfterPresentation { get; }

        public virtual bool IsStartable => true;

        public PresentationSystemEntity()
        {
            ConfigLoader.LoadConfig(this);
        }
        ~PresentationSystemEntity()
        {
            Dispose();
        }

        public virtual PresentationResult OnStartPresentation() { return PresentationResult.Normal; }

        public virtual PresentationResult OnInitialize() { return PresentationResult.Normal; }
        public virtual PresentationResult OnInitializeAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult BeforePresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult BeforePresentationAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult OnPresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult OnPresentationAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult AfterPresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult AfterPresentationAsync() { return PresentationResult.Normal; }

        public virtual void Dispose() { }

        /// <summary>
        /// <see cref="OnInitialize"/> 혹은 <see cref="OnInitializeAsync"/> 에서만 수행되야됩니다.
        /// </summary>
        /// <typeparam name="TA"></typeparam>
        /// <param name="setter"></param>
        protected void RequestSystem<TA>(Action<TA> setter) where TA : class, IPresentationSystem
            => PresentationManager.RegisterRequestSystem<T, TA>(setter);
    }

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
