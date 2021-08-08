using Syadeu.Database;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    [Obsolete("in develop", true)]
    unsafe internal sealed class ProxySystem : PresentationSystemEntity<ProxySystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private NativeList<Data> m_Data;
        private Output[] m_DataReferences;

        protected override PresentationResult OnInitialize()
        {
            m_Data.GetUnsafeList();
            //NativeSortExtension.BinarySearch(m_Data, )
            //m_Data.BinarySearch(m_Data[0], new BinarySearch());

            return base.OnInitialize();
        }

        private struct BinarySearch : IComparer<Data>
        {
            public int Compare(Data x, Data y)
            {
                throw new NotImplementedException();
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 96)]
        private struct IsOccupiedData
        {
            [FieldOffset(0)] public bool m_IsOccupied;
        }
        [StructLayout(LayoutKind.Explicit, Size = 96)]
        public struct TranslationData
        {
            [FieldOffset(32)] public float3 m_Translation;
        }
        [StructLayout(LayoutKind.Explicit, Size = 96)]
        private struct Data
        {
            // 1 bytes
            [FieldOffset(0)] public bool m_IsOccupied;
            [FieldOffset(1)] public bool m_EnableCull;
            [FieldOffset(2)] public bool m_IsVisible;
            [FieldOffset(3)] public bool m_DestroyQueued;

            // 4 bytes
            [FieldOffset(4)] public int m_Index;
            [FieldOffset(8)] public int2 m_ProxyIndex;

            // 8 bytes
            [FieldOffset(16)] public ulong m_Hash;
            [FieldOffset(24)] public PrefabReference m_Prefab;

            // 12 bytes
            [FieldOffset(32)] public float3 m_Translation;
            [FieldOffset(44)] public float3 m_Scale;
            [FieldOffset(56)] public float3 m_Center;
            [FieldOffset(68)] public float3 m_Size;

            // 16 bytes
            [FieldOffset(80)] public quaternion m_Rotation;
        }

        private class Output
        {
            private readonly Semaphore m_Semaphore;
            private readonly ProxySystem m_System;
            private readonly int m_Index;

            public Output(ProxySystem system, int idx)
            {
                m_Semaphore = new Semaphore(0, 1);
                m_System = system;
                m_Index = idx;
                m_Semaphore.Release();
            }

            public float3 position
            {
                get
                {
                    return float3.zero;
                }
                set
                {
                    
                }
            }
        }
    }
}
