// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Collections.Threading;
using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Syadeu.Presentation
{
    public sealed class UtilitySystem : PresentationSystemEntity<UtilitySystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private Random m_Random;

        private UnsafeStream m_PrefabPreloadStream;
        private int m_ExpectedPrefabPreloadCount;
        private AtomicSafeInteger m_PrefabPreloadCounter;

        public override bool IsStartable => m_PrefabPreloadCounter.Equals(m_ExpectedPrefabPreloadCount);

        protected override PresentationResult OnInitialize()
        {
            m_Random = new Random();
            m_Random.InitState();

            #region Prefab Preloader

            m_PrefabPreloadStream = new UnsafeStream(10240, AllocatorManager.Persistent);
            {
                using (PrefabPreloader prefabPreloader = new PrefabPreloader(m_PrefabPreloadStream.AsWriter()))
                {
                    foreach (var item in EntityDataList.Instance.GetData(t => TypeHelper.TypeOf<IPrefabPreloader>.Type.IsAssignableFrom(t.GetType())).Select(t => (IPrefabPreloader)t))
                    {
                        item.Register(prefabPreloader);
                    }
                }

                var rdr = m_PrefabPreloadStream.AsReader();
                int count = rdr.BeginForEachIndex(0);
                m_ExpectedPrefabPreloadCount = 0;
                for (int i = 0; i < count; i++)
                {
                    var prefab = rdr.Read<PrefabReference>();

                    if (prefab.Asset != null) continue;

                    var oper = prefab.LoadAssetAsync();
                    oper.Completed += PrefabPreloadCounter;
                    m_ExpectedPrefabPreloadCount++;

#if DEBUG_MODE
                    CoreSystem.Logger.Log(Channel.Entity,
                        $"Preloading prefab({prefab.GetObjectSetting().Name}).");
#endif
                }
                rdr.EndForEachIndex();
            }
            
            #endregion

            return base.OnInitialize();
        }
        private void PrefabPreloadCounter(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle obj)
        {
            m_PrefabPreloadCounter.Increment();
        }

        protected override void OnDispose()
        {
        }

        protected override PresentationResult OnStartPresentation()
        {


            return base.OnStartPresentation();
        }

        public int CreateHashCode() => m_Random.NextInt(int.MinValue, int.MaxValue);
    }

    public struct PrefabPreloader : IDisposable
    {
        private UnsafeStream.Writer m_Writer;

        internal PrefabPreloader(UnsafeStream.Writer wr)
        {
            m_Writer = wr;

            m_Writer.BeginForEachIndex(0);
        }

        public void Add(PrefabReference prefab)
        {
            m_Writer.Write(prefab);
        }
        public void Add(params PrefabReference[] prefabs)
        {
            for (int i = 0; i < prefabs.Length; i++)
            {
                m_Writer.Write(prefabs[i]);
            }
        }

        void IDisposable.Dispose()
        {
            m_Writer.EndForEachIndex();
        }
    }
    /// <summary>
    /// Application initialize 단계에서 에셋을 프리로드할 수 있는 interface 입니다.
    /// </summary>
    public interface IPrefabPreloader
    {
        /// <summary>
        /// Preload 를 등록하는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="PrefabPreloader.Add(PrefabReference)"/> 로 등록할 수 있습니다.
        /// </remarks>
        /// <param name="loader"></param>
        void Register(PrefabPreloader loader);
    }

}
