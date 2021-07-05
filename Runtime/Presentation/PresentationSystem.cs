﻿//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif

using Syadeu.Database;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Assertions;

namespace Syadeu.Presentation
{
    /// <summary>
    /// 등록한 프레젠테이션 시스템을 받아오기 위한 struct 입니다.
    /// </summary>
    /// <remarks>
    /// 이 struct 로 시스템을 받아오려면 먼저 <seealso cref="PresentationSystemEntity{T}"/> 를 상속받고 시스템을 선언해야됩니다.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public struct PresentationSystem<T> : IValidation, ICustomYieldAwaiter where T : IPresentationSystem
    {
        public static PresentationSystem<T> Null = new PresentationSystem<T>(Hash.Empty, -1);
        private static PresentationSystem<T> s_Instance = Null;
        private static PresentationSystem<T> Instance
        {
            get
            {
                if (!((IValidation)s_Instance).IsValid())
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
        public static IPresentationSystemGroup SystemGroup
        {
            get
            {
                if (!IsValid()) throw new Exception();
                return PresentationManager.Instance.m_PresentationGroups[Instance.m_GroupHash].m_SystemGroup;
            }
        }

        private readonly Hash m_GroupHash;
        private readonly int m_Index;

        private PresentationSystem(Hash groupHash, int idx)
        {
            m_GroupHash = groupHash;
            m_Index = idx;
        }

        bool IValidation.IsValid() => !m_GroupHash.Equals(Hash.Empty) && m_Index >= 0;
        bool ICustomYieldAwaiter.KeepWait => !IsValid();

        public static bool IsValid() => ((IValidation)Instance).IsValid();
        public static T GetSystem()
        {
            Assert.IsTrue(IsValid(), $"{typeof(T).Name} System is not valid");
            try
            {
                return (T)PresentationManager.Instance.m_PresentationGroups[Instance.m_GroupHash].m_Systems[Instance.m_Index];
            }
            catch (Exception ex)
            {
                $"{typeof(T).Name}: {Instance.m_GroupHash}, {Instance.m_Index}".ToLog();

                UnityEngine.Debug.LogError(ex);
                throw;
            }
        }
        public static ICustomYieldAwaiter GetAwaiter() => Instance;
    }
}
