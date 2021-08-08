using Syadeu.Database;
using Syadeu.Mono;
using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ProxyTransform : IEquatable<ProxyTransform>
    {
        #region Statics
        public static readonly ProxyTransform Null = new ProxyTransform(Hash.Empty);
        public static readonly Hash s_TranslationChanged = Hash.NewHash("Translation");
        public static readonly Hash s_RotationChanged = Hash.NewHash("Rotation");
        public static readonly Hash s_ScaleChanged = Hash.NewHash("Scale");

        public static readonly Hash s_RequestProxy = Hash.NewHash("RequestProxy");
        public static readonly Hash s_RemoveProxy = Hash.NewHash("RemoveProxy");

        internal static readonly int2 ProxyNull = new int2(-1, -1);
        internal static readonly int2 ProxyQueued = new int2(-2, -2);
        #endregion

        [Flags]
        public enum SynchronizeOption
        {
            Position    =   0b001,
            Rotation    =   0b010,
            Scale       =   0b100,

            TR          =   0b011,
            TRS         =   0b111
        }

        [NativeDisableUnsafePtrRestriction] unsafe internal readonly NativeProxyData.ProxyTransformData* m_Pointer;
        internal readonly ulong m_Hash;
        unsafe internal ProxyTransform(NativeProxyData.ProxyTransformData* p, ulong hash)
        {
            m_Pointer = p;
            m_Hash = hash;
        }
        unsafe private ProxyTransform(ulong hash)
        {
            m_Pointer = null;
            m_Hash = hash;
        }

        unsafe private ref NativeProxyData.ProxyTransformData Ref => ref *m_Pointer;
        internal void RequestProxy()
        {
            if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
            if (hasProxy) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Already has proxy");

            Ref.m_ProxyIndex = ProxyQueued;
            EventDescriptor<ProxyTransform>.Invoke(s_RequestProxy, this);
        }
        internal void RemoveProxy()
        {
            if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
            if (!hasProxy) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "does not have proxy");

            EventDescriptor<ProxyTransform>.Invoke(s_RemoveProxy, this);
        }
        internal void SetProxy(int2 proxyIndex)
        {
            Ref.m_ProxyIndex = proxyIndex;
        }

#pragma warning disable IDE1006 // Naming Styles
        public Hash index
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.m_Hash;
            }
        }

        public bool enableCull
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                return Ref.m_EnableCull;
            }
            set
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                Ref.m_EnableCull = value;
            }
        }
        public bool isVisible
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                return Ref.m_IsVisible;
            }
            internal set
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                Ref.m_IsVisible = value;
            }
        }

        public bool hasProxy
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                if (Ref.m_ProxyIndex.Equals(ProxyNull)) return false;
                return true;
            }
        }
        public bool hasProxyQueued
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                if (Ref.m_ProxyIndex.Equals(ProxyNull)) return false;
                else if (Ref.m_ProxyIndex.Equals(ProxyQueued)) return true;

                return false;
            }
        }
        public RecycleableMonobehaviour proxy
        {
            get
            {
                if (isDestroyed || !hasProxy || hasProxyQueued) return null;

                int2 proxyIndex = Ref.m_ProxyIndex;
                return PresentationSystem<GameObjectProxySystem>.System.m_Instances[proxyIndex.x][proxyIndex.y];
            }
        }
        public bool isDestroyed
        {
            get
            {
                unsafe
                {
                    if (m_Hash.Equals(0) || m_Pointer == null) return true;
                    if (m_Pointer->m_Hash != m_Hash) return true;
                }
                return false;
            }
        }
        public PrefabReference prefab
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.m_Prefab;
            }
        }

        public float3 position
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.translation;
            }
            set
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                Ref.translation = value;
                EventDescriptor<ProxyTransform>.Invoke(s_TranslationChanged, this);
            }
        }
        public quaternion rotation
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.rotation;
            }
            set
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                Ref.rotation = value;
                EventDescriptor<ProxyTransform>.Invoke(s_RotationChanged, this);
            }
        }
        public float3 eulerAngles
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                float3 temp = rotation.Euler();
                return temp * UnityEngine.Mathf.Rad2Deg;
            }
            set
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                float3 temp = value * UnityEngine.Mathf.Deg2Rad;
                rotation = quaternion.EulerZXY(temp);
            }
        }
        public float3 scale
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.scale;
            }
            set
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                Ref.scale = value;
                EventDescriptor<ProxyTransform>.Invoke(s_ScaleChanged, this);
            }
        }

        public float3 right
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return math.mul(Ref.m_Rotation, new float3(1, 0, 0));
            }
        }
        public float3 up
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return math.mul(Ref.m_Rotation, new float3(0, 1, 0));
            }
        }
        public float3 forward
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return math.mul(Ref.m_Rotation, new float3(0, 0, 1));
            }
        }

        public float3 center
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.m_Center;
            }
        }
        public float3 size
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Ref.m_Size;
            }
        }
        public AABB aabb
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return new AABB(Ref.m_Center + Ref.m_Translation, Ref.m_Size).Rotation(Ref.m_Rotation);
            }
        }

        public float4x4 localToWorldMatrix
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return Render.RenderSystem.LocalToWorldMatrix(Ref.m_Translation, Ref.m_Rotation);
            }
        }
        public float4x4 worldToLocalMatrix
        {
            get
            {
                if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return math.inverse(Render.RenderSystem.LocalToWorldMatrix(Ref.m_Translation, Ref.m_Rotation));
            }
        }

#pragma warning restore IDE1006 // Naming Styles

        public void Synchronize(SynchronizeOption option)
        {
            CoreSystem.Logger.ThreadBlock(nameof(ProxyTransform.Synchronize), Syadeu.Internal.ThreadInfo.Unity);

            UnityEngine.Transform tr = proxy.transform;
            if ((option & SynchronizeOption.Position) == SynchronizeOption.Position)
            {
                position = tr.position;
            }
            if ((option & SynchronizeOption.Rotation) == SynchronizeOption.Rotation)
            {
                rotation = tr.rotation;
            }
            if ((option & SynchronizeOption.Scale) == SynchronizeOption.Scale)
            {
                scale = tr.localScale;
            }
        }
        public void Destroy()
        {
            if (isDestroyed) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

            unsafe
            {
                if (m_Pointer->m_DestroyQueued)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                }

                m_Pointer->m_DestroyQueued = true;
            }
            PresentationSystem<GameObjectProxySystem>.System.Destroy(this);
        }

        public bool Equals(ProxyTransform other) => m_Hash.Equals(other.m_Hash);
    }
}
