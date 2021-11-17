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

using Syadeu.Collections;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Entities;

using System;
using System.Runtime.InteropServices;

using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using AABB = Syadeu.Collections.AABB;
using Syadeu.Collections.Proxy;
using UnityEngine;

namespace Syadeu.Presentation.Proxy
{
    /// <summary>
    /// <see cref="Entity{T}"/> 의 트랜스폼입니다.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ProxyTransform : IProxyTransform, IEquatable<ProxyTransform>
    {
        #region Statics
        public static readonly ProxyTransform Null = new ProxyTransform(-1);

        internal static readonly int2 ProxyNull = new int2(-1, -1);
        internal static readonly int2 ProxyQueued = new int2(-2, -2);
        #endregion

        [NativeDisableUnsafePtrRestriction] unsafe internal readonly NativeProxyData.UnsafeList* m_Pointer;
        internal readonly int m_Index;
        internal readonly int m_Generation;
        internal readonly Hash m_Hash;

        unsafe internal ProxyTransform(NativeProxyData.UnsafeList* p, int index, int generation, Hash hash)
        {
            m_Pointer = p;
            m_Index = index;
            m_Generation = generation;
            m_Hash = hash;
        }
        unsafe private ProxyTransform(int unused)
        {
            m_Pointer = null;
            m_Index = -1;
            m_Generation = unused;
            m_Hash = Hash.Empty;
        }

        unsafe internal ProxyTransformData* Pointer => (*m_Pointer)[m_Index];
        unsafe internal ref ProxyTransformData Ref => ref *(*m_Pointer)[m_Index];

        internal void SetProxy(int2 proxyIndex)
        {
            Ref.m_ProxyIndex = proxyIndex;
        }

        public readonly struct ReadOnly
        {
            public readonly int index;
            public readonly int generation;

            public readonly bool enableCull;
            public readonly bool isVisible;

            public readonly bool hasProxy;
            public readonly bool hasProxyQueued;
            public readonly bool isDestroyed;
            public readonly PrefabReference prefab;

            public readonly float3 position;
            public readonly quaternion rotation;
            public readonly float3 scale;

            public readonly float3 center;
            public readonly float3 size;
            public readonly AABB aabb;

            unsafe internal ReadOnly(ProxyTransformData* p)
            {
                index = (*p).m_Index;
                generation = (*p).m_Generation;

                enableCull = (*p).m_EnableCull;
                isVisible = (*p).m_IsVisible;

                int2 proxyIdx = (*p).m_ProxyIndex;
                hasProxy = !proxyIdx.Equals(ProxyNull);
                hasProxyQueued = !proxyIdx.Equals(ProxyNull) && proxyIdx.Equals(ProxyQueued);
                isDestroyed = (*p).destroyed;
                prefab = (*p).m_Prefab;

                position = (*p).m_Translation;
                rotation = (*p).m_Rotation;
                scale = (*p).m_Scale;

                center = (*p).m_Center;
                size = (*p).m_Size;
                aabb = (*p).GetAABB();
            }
        }
        public ReadOnly AsReadOnly()
        {
            unsafe
            {
                return new ReadOnly(Pointer);
            }
        }

#pragma warning disable IDE1006 // Naming Styles
        public int index
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return m_Index;
            }
        }
        public int generation
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return m_Generation;
            }
        }

        public bool enableCull
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                return Ref.m_EnableCull;
            }
            set
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                if (Ref.m_EnableCull == value) return;

                if (!value && !hasProxy && !hasProxyQueued)
                {
                    Ref.m_ProxyIndex = ProxyQueued;
                    PresentationSystem<DefaultPresentationGroup, GameObjectProxySystem>.System.m_OverrideRequestProxies.Enqueue(m_Index);
                }

                Ref.m_EnableCull = value;
            }
        }
        public bool isVisible
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                return Ref.m_IsVisible;
            }
            internal set
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                Ref.m_IsVisible = value;
            }
        }

        public bool hasProxy
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                //if (Ref.m_GpuInstanced) return false;

                if (Ref.m_ProxyIndex.Equals(ProxyNull)) return false;
                return true;
            }
        }
        public bool hasProxyQueued
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                //if (Ref.m_GpuInstanced) return false;

                if (Ref.m_ProxyIndex.Equals(ProxyNull)) return false;
                else if (Ref.m_ProxyIndex.Equals(ProxyQueued)) return true;

                return false;
            }
        }
        public bool gpuInstanced
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                return Ref.m_GpuInstanced;
            }
        }

        UnityEngine.Object ITransform.proxy => proxy;
        IProxyMonobehaviour IProxyTransform.proxy => proxy;
        public RecycleableMonobehaviour proxy
        {
            get
            {
                if (isDestroyed || isDestroyQueued || !hasProxy || hasProxyQueued) return null;

                int2 proxyIndex = Ref.m_ProxyIndex;
                return PresentationSystem<DefaultPresentationGroup, GameObjectProxySystem>.System.m_Instances[proxyIndex.x][proxyIndex.y];
            }
        }
        public bool isDestroyed
        {
            get
            {
                unsafe
                {
                    if (m_Generation.Equals(-1) || m_Pointer == null) return true;
                    else if (!Ref.m_IsOccupied || 
                        !m_Hash.Equals(Ref.m_Hash)) return true;
                    else if (!Ref.m_Generation.Equals(m_Generation)) return true;
                }
                return false;
            }
        }
        public bool isDestroyQueued
        {
            get => Ref.m_DestroyQueued;
        }
        IPrefabReference IProxyTransform.prefab => prefab;
        public PrefabReference prefab
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.m_Prefab;
            }
        }

        public float3 position
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                #region Hierarchy

                if (Ref.m_ParentIndex < 0)
                {
                    return Ref.translation;
                }
                else
                {
                    unsafe
                    {
                        var parentData = m_Pointer->m_TransformBuffer[Ref.m_ParentIndex];
                        float4x4 local2world = float4x4.TRS(parentData.m_Translation, parentData.m_Rotation, parentData.m_Scale);

                        $"{localPosition}".ToLog();

                        return math.mul(local2world, new float4(Ref.m_Translation, 1)).xyz;
                    }
                }

                #endregion
            }
            set
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                #region Hierarchy

                if (Ref.m_ParentIndex < 0)
                {
                    Ref.translation = value;
                }
                else
                {
                    unsafe
                    {
                        var parentData = m_Pointer->m_TransformBuffer[Ref.m_ParentIndex];
                        float4x4 world2local = math.inverse(float4x4.TRS(parentData.m_Translation, parentData.m_Rotation, parentData.m_Scale));

                        Ref.m_Translation = math.mul(world2local, new float4(value, 1)).xyz;
                    }
                }

                unsafe
                {
                    for (int i = 0; i < Ref.m_ChildIndices.Length; i++)
                    {
                        var data = m_Pointer->m_TransformBuffer[Ref.m_ChildIndices[i]];
                        ProxyTransform tr = new ProxyTransform(m_Pointer, data.m_Index, data.m_Generation, data.m_Hash);

                        PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(tr));
                    }
                }

                #endregion

                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
            }
        }
        public float3 localPosition
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                // .001f
                //return math.round(Ref.m_Translation * 1000) * 0.001f;
                return Ref.m_Translation;
            }
            set
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                Ref.m_Translation = value;

                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
            }
        }
        public quaternion rotation
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                if (Ref.m_ParentIndex < 0)
                {
                    return Ref.rotation;
                }
                else
                {
                    unsafe
                    {
                        var parentData = m_Pointer->m_TransformBuffer[Ref.m_ParentIndex];
                        
                        quaternion result = math.mul(Ref.m_Rotation, parentData.m_Rotation);

                        return result;
                    }
                }
            }
            set
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                #region Hierarchy

                if (Ref.m_ParentIndex < 0)
                {
                    Ref.rotation = value;
                }
                else
                {
                    unsafe
                    {
                        var parentData = m_Pointer->m_TransformBuffer[Ref.m_ParentIndex];

                        quaternion parentRot = parentData.m_Rotation;
                        quaternion rotation = math.mul(parentRot, value);
                        
                        Ref.m_Rotation = rotation;
                    }
                }

                unsafe
                {
                    for (int i = 0; i < Ref.m_ChildIndices.Length; i++)
                    {
                        var data = m_Pointer->m_TransformBuffer[Ref.m_ChildIndices[i]];
                        ProxyTransform tr = new ProxyTransform(m_Pointer, data.m_Index, data.m_Generation, data.m_Hash);

                        PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(tr));
                    }
                }

                #endregion

                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
            }
        }
        public quaternion localRotation
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                return Ref.m_Rotation;
            }
            set
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                Ref.m_Rotation = value;

                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
            }
        }
        public float3 eulerAngles
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                float3 temp = rotation.Euler();
                return temp * UnityEngine.Mathf.Rad2Deg;
            }
            set
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                float3 temp = value * UnityEngine.Mathf.Deg2Rad;
                // .001f
                temp = math.round(temp * 1000) * 0.001f;

                rotation = quaternion.EulerZXY(temp);
            }
        }
        public float3 localEulerAngles
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                return Ref.m_Rotation.Euler() * Mathf.Rad2Deg;
            }
            set
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                float3 temp = value * Mathf.Deg2Rad;
                // .001f
                temp = math.round(temp * 1000) * 0.001f;

                Ref.m_Rotation = quaternion.EulerZXY(temp);

                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
            }
        }
        public float3 scale
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                if (Ref.m_ParentIndex < 0)
                {
                    return Ref.scale;
                }
                else
                {
                    unsafe
                    {
                        var parentData = m_Pointer->m_TransformBuffer[Ref.m_ParentIndex];

                        return parentData.m_Scale / Ref.m_Scale;
                    }
                }
            }
            set
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                
                #region Hierarchy

                if (Ref.m_ParentIndex < 0)
                {
                    Ref.scale = value;
                }
                else
                {
                    unsafe
                    {
                        var parentData = m_Pointer->m_TransformBuffer[Ref.m_ParentIndex];

                        Ref.m_Scale = math.mul(parentData.m_Scale, value);
                    }
                }

                unsafe
                {
                    for (int i = 0; i < Ref.m_ChildIndices.Length; i++)
                    {
                        var data = m_Pointer->m_TransformBuffer[Ref.m_ChildIndices[i]];
                        ProxyTransform tr = new ProxyTransform(m_Pointer, data.m_Index, data.m_Generation, data.m_Hash);

                        PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(tr));
                    }
                }

                #endregion

                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
            }
        }
        public float3 localScale
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                return Ref.m_Scale;
            }
            set
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                Ref.m_Scale = value;

                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
            }
        }

        public float3 right
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return math.mul(Ref.m_Rotation, math.right());
            }
        }
        public float3 up
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return math.mul(Ref.m_Rotation, math.up());
            }
        }
        public float3 forward
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return math.mul(Ref.m_Rotation, math.forward());
            }
        }

        public float3 center
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.m_Center;
            }
        }
        public float3 size
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.m_Size;
            }
        }
        public AABB aabb
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return new AABB(Ref.m_Center + Ref.m_Translation, Ref.m_Size).Rotation(in Ref.m_Rotation, in Ref.m_Scale);
            }
        }

        public float4x4 localToWorldMatrix
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return float4x4.TRS(Ref.m_Translation, Ref.m_Rotation, Ref.m_Scale);
            }
        }
        public float4x4 worldToLocalMatrix
        {
            get
            {
                if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return math.inverse(float4x4.TRS(Ref.m_Translation, Ref.m_Rotation, Ref.m_Scale));
            }
        }

#pragma warning restore IDE1006 // Naming Styles

        public void Synchronize(IProxyTransform.SynchronizeOption option)
        {
            CoreSystem.Logger.ThreadBlock(nameof(ProxyTransform.Synchronize), Syadeu.Internal.ThreadInfo.Unity);

            if (isDestroyed || isDestroyQueued) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

            if (!hasProxy) return;

            UnityEngine.Transform tr = proxy.transform;
            if ((option & IProxyTransform.SynchronizeOption.Position) == IProxyTransform.SynchronizeOption.Position)
            {
                position = tr.position;
            }
            if ((option & IProxyTransform.SynchronizeOption.Rotation) == IProxyTransform.SynchronizeOption.Rotation)
            {
                rotation = tr.rotation;
            }
            if ((option & IProxyTransform.SynchronizeOption.Scale) == IProxyTransform.SynchronizeOption.Scale)
            {
                scale = tr.localScale;
            }
        }

        public void SetParent(in ProxyTransform parent)
        {
            unsafe
            {
                m_Pointer->m_TransformBuffer[parent.m_Index].m_ChildIndices.Add(m_Index);
                m_Pointer->m_TransformBuffer[m_Index].m_ParentIndex = parent.m_Index;
            }
        }
        public void RemoveParent()
        {
            unsafe
            {
                if (m_Pointer->m_TransformBuffer[m_Index].m_ParentIndex < 0) return;

                int parentIdx = m_Pointer->m_TransformBuffer[m_Index].m_ParentIndex;
                m_Pointer->m_TransformBuffer[parentIdx].m_ChildIndices.RemoveFor(m_Index);
                m_Pointer->m_TransformBuffer[m_Index].m_ParentIndex = -1;
            }
        }

        public void Destroy()
        {
            if (isDestroyed)
            {
                CoreSystem.Logger.LogError(Channel.Proxy,
                    "Cannot access this transform because it is destroyed.");
                return;
            }

            unsafe
            {
                if ((*m_Pointer)[m_Index]->m_DestroyQueued)
                {
                    CoreSystem.Logger.LogError(Channel.Proxy,
                        "Cannot access this transform because it is destroyed.");
                    return;
                }
            }
            PresentationSystem<DefaultPresentationGroup, GameObjectProxySystem>.System.Destroy(in this);
        }

        public bool Equals(ProxyTransform other) => 
            m_Index.Equals(other.m_Index) && 
            m_Generation.Equals(other.m_Generation);
        public bool Equals(ITransform other)
        {
            if (!(other is ProxyTransform tr)) return false;
            return Equals(tr);
        }

        public override int GetHashCode() => m_Index * 397 ^ m_Generation;
    }
}
