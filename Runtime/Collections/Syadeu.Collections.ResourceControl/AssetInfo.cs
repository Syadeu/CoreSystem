// Copyright 2022 Ikina Games
// Author : Seung Ha Kim (Syadeu)
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

#if UNITY_2020_1_OR_NEWER
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE

using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Collections.ResourceControl.LowLevel;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections.ResourceControl
{
    //////////////////////////////////////////////////////////////////////////////////////////
    /*                                   Critical Section                                   */
    /*                                       수정금지                                        */
    /*                                                                                      */
    /*                          Unsafe pointer를 포함하는 코드입니다                          */
    //////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// <see cref="AssetBundleInfo"/> 가 담고있는 에셋의 정보입니다.
    /// </summary>
    [BurstCompatible]
    [Guid("b92cc9a9-b577-4759-b623-d794bd86d430")]
    public struct AssetInfo : IValidation, IEquatable<AssetInfo>, IDisposable
    {
        public static AssetInfo Invalid => default(AssetInfo);

        [NativeDisableUnsafePtrRestriction, NonSerialized]
        internal readonly UnsafeReference<UnsafeAssetBundleInfo> m_BundlePointer;
        [NonSerialized]
        internal readonly Hash m_InstanceID, m_Key;
        private readonly Timer m_CreationTime;

        [NotBurstCompatible]
        internal ref UnsafeAssetInfo UnsafeInfo
        {
            get
            {
                var assetInfoPtr = ResourceManager.GetUnsafeAssetInfo(in m_Key);
                return ref assetInfoPtr.Value;
            }
        }
        /// <summary>
        /// <seealso cref="UnityEngine.AssetBundle"/> 내 에셋.
        /// </summary>
        /// <remarks>
        /// 반환된 객체는 <seealso cref="ResourceManager"/> 에서 <see cref="UnityEngine.AssetBundle"/> 단위로 관리되므로, 
        /// <see cref="UnityEngine.Object.Destroy(UnityEngine.Object)"/> 등과 같은 행위가 절때로 일어나서는 안됩니다.
        /// </remarks>
        [NotBurstCompatible]
        public UnityEngine.Object Asset
        {
            get
            {
                this.ThrowIfIsNotValid();

                ResourceManager.AssetContainer bundleInfo = ResourceManager.GetAssetBundle(m_BundlePointer.Value.index);

                UnsafeInfo.lastUsage = Timer.Start();
                return bundleInfo.LoadAsset(UnsafeInfo.key.ToString());
            }
        }
        /// <summary>
        /// 에셋이 로드되어 메모리에 존재하는지 반환합니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="AssetBundleInfo.LoadAssetAsync(in Hash)"/> 등의 비동기 로드를 실행하면 <see cref="Asset"/> 프로퍼티는 에셋이 로드될 때까지 null 을 반환합니다.
        /// </remarks>
        public bool IsLoaded
        {
            get
            {
                this.ThrowIfIsNotValid();

                ResourceManager.AssetContainer bundleInfo = ResourceManager.GetAssetBundle(m_BundlePointer.Value.index);
                return bundleInfo.GetAsset(m_Key) != null;
            }
        }
        /// <summary>
        /// 에셋 로드가 완료되면 실행되는 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// 에셋이 이미 로드된 상태라면, 등록할 이벤트는 즉시 실행됩니다.
        /// </remarks>
        public event Action<UnityEngine.Object> OnLoaded
        {
            add
            {
                this.ThrowIfIsNotValid();

                ResourceManager.AssetContainer bundleInfo = ResourceManager.GetAssetBundle(m_BundlePointer.Value.index);
                Promise<UnityEngine.Object> promise = bundleInfo.LoadAssetAsync(UnsafeInfo.key.ToString());

                promise.OnCompleted += value;
            }
            remove
            {
                this.ThrowIfIsNotValid();

                ResourceManager.AssetContainer bundleInfo = ResourceManager.GetAssetBundle(m_BundlePointer.Value.index);
                if (bundleInfo.GetAsset(m_Key) == null) return;

                Promise<UnityEngine.Object> promise = bundleInfo.LoadAssetAsync(UnsafeInfo.key.ToString());
                promise.OnCompleted -= value;
            }
        }
        public Hash InstanceID => m_InstanceID;
        /// <summary>
        /// 에셋의 키 값입니다.
        /// </summary>
        public Hash Key => m_Key;

        public AssetInfo(Hash key)
        {
            this = default(AssetInfo);

            m_Key = key;
        }
        internal unsafe AssetInfo(UnsafeReference<UnsafeAssetBundleInfo> bundle, Hash instanceID, Hash key)
        {
            this = default(AssetInfo);

            m_BundlePointer = bundle;
            m_InstanceID = instanceID;
            this.m_Key = key;

            m_CreationTime = Timer.Start();
        }

        public float GetElapsedTimeSinceLastUsage()
        {
            return UnsafeInfo.lastUsage.ElapsedTime;
        }

        /// <summary>
        /// 에셋의 레퍼런스를 반환합니다.
        /// </summary>
        public void Reserve()
        {
            ((IDisposable)this).Dispose();
        }
        /// <summary>
        /// 유효한 에셋인지 반환합니다.
        /// </summary>
        /// <returns></returns>
        [NotBurstCompatible]
        public bool IsValid()
        {
            ResourceManager.AssetContainer bundle;
            if (!m_BundlePointer.IsCreated) return false;

            bundle = ResourceManager.GetAssetBundle(m_BundlePointer.Value.index);

            return bundle.IsLoadedAsset(m_Key);
        }
        public bool Equals(AssetInfo other)
        {
            return m_BundlePointer.Equals(other.m_BundlePointer) && m_Key.Equals(other.m_Key);
        }

        void IDisposable.Dispose()
        {
            this.ThrowIfIsNotValid();

            ResourceManager.Reserve(m_BundlePointer, in this);
        }

        [NotBurstCompatible]
        public override string ToString() => m_Key.ToString();

        public static implicit operator UnityEngine.Object(AssetInfo t) => t.Asset;
    }

    /// <inheritdoc cref="AssetInfo"/>
    /// <typeparam name="T"></typeparam>
    [BurstCompatible]
    [Guid("FC6B34C4-B9CA-48AD-A041-9522729CCD2A")]
    public struct AssetInfo<T> : IValidation, IEquatable<AssetInfo<T>>, IDisposable
        where T : UnityEngine.Object
    {
        public static AssetInfo<T> Invalid => default(AssetInfo<T>);

        [NativeDisableUnsafePtrRestriction, NonSerialized]
        internal readonly UnsafeReference<UnsafeAssetBundleInfo> m_BundlePointer;
        [NonSerialized]
        internal readonly Hash m_InstanceID, m_Key;
        private readonly Timer m_CreationTime;

        [NotBurstCompatible]
        internal ref UnsafeAssetInfo UnsafeInfo
        {
            get
            {
                var assetInfoPtr = ResourceManager.GetUnsafeAssetInfo(in m_Key);
                return ref assetInfoPtr.Value;
            }
        }
        /// <summary>
        /// <seealso cref="UnityEngine.AssetBundle"/> 내 에셋.
        /// </summary>
        /// <remarks>
        /// 반환된 객체는 <seealso cref="ResourceManager"/> 에서 <see cref="UnityEngine.AssetBundle"/> 단위로 관리되므로, 
        /// <see cref="UnityEngine.Object.Destroy(UnityEngine.Object)"/> 등과 같은 행위가 절때로 일어나서는 안됩니다.
        /// </remarks>
        [NotBurstCompatible]
        public T Asset
        {
            get
            {
                ((AssetInfo)this).ThrowIfIsNotValid();

                ResourceManager.AssetContainer bundleInfo = ResourceManager.GetAssetBundle(m_BundlePointer.Value.index);

                UnsafeInfo.lastUsage = Timer.Start();
                return bundleInfo.LoadAsset(UnsafeInfo.key.ToString()) as T;
            }
        }
        /// <summary>
        /// 에셋이 로드되어 메모리에 존재하는지 반환합니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="AssetBundleInfo.LoadAssetAsync(in Hash)"/> 등의 비동기 로드를 실행하면 <see cref="Asset"/> 프로퍼티는 에셋이 로드될 때까지 null 을 반환합니다.
        /// </remarks>
        public bool IsLoaded
        {
            get
            {
                ((AssetInfo)this).ThrowIfIsNotValid();

                ResourceManager.AssetContainer bundleInfo = ResourceManager.GetAssetBundle(m_BundlePointer.Value.index);
                return bundleInfo.GetAsset(m_Key) != null;
            }
        }
        /// <summary>
        /// 에셋 로드가 완료되면 실행되는 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// 에셋이 이미 로드된 상태라면, 등록할 이벤트는 즉시 실행됩니다.
        /// </remarks>
        public event Action<UnityEngine.Object> OnLoadedUntyped
        {
            add
            {
                ((AssetInfo)this).ThrowIfIsNotValid();

                ResourceManager.AssetContainer bundleInfo = ResourceManager.GetAssetBundle(m_BundlePointer.Value.index);
                Promise<UnityEngine.Object> promise = bundleInfo.LoadAssetAsync(UnsafeInfo.key.ToString());

                promise.OnCompleted += value;
            }
            remove
            {
                ((AssetInfo)this).ThrowIfIsNotValid();

                ResourceManager.AssetContainer bundleInfo = ResourceManager.GetAssetBundle(m_BundlePointer.Value.index);
                if (bundleInfo.GetAsset(m_Key) == null) return;

                Promise<UnityEngine.Object> promise = bundleInfo.LoadAssetAsync(UnsafeInfo.key.ToString());
                promise.OnCompleted -= value;
            }
        }
        public Hash InstanceID => m_InstanceID;
        /// <summary>
        /// 에셋의 키 값입니다.
        /// </summary>
        public Hash Key => m_Key;

        public AssetInfo(Hash key)
        {
            this = default(AssetInfo<T>);

            m_Key = key;
        }
        internal unsafe AssetInfo(UnsafeReference<UnsafeAssetBundleInfo> bundle, Hash instanceID, Hash key)
        {
            this = default(AssetInfo<T>);

            m_BundlePointer = bundle;
            m_InstanceID = instanceID;
            this.m_Key = key;

            m_CreationTime = Timer.Start();
        }

        /// <summary>
        /// 마지막으로 사용된(<seealso cref="Asset"/> 에 접근하면 초기화됨) 후, 경과된 시간(초) 를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public float GetElapsedTimeSinceLastUsage()
        {
            return UnsafeInfo.lastUsage.ElapsedTime;
        }

        /// <summary>
        /// 에셋의 레퍼런스를 반환합니다.
        /// </summary>
        public void Reserve()
        {
            ((IDisposable)this).Dispose();
        }
        /// <summary>
        /// 유효한 에셋인지 반환합니다.
        /// </summary>
        /// <returns></returns>
        [NotBurstCompatible]
        public bool IsValid()
        {
            ResourceManager.AssetContainer bundle;
            if (!m_BundlePointer.IsCreated) return false;

            bundle = ResourceManager.GetAssetBundle(m_BundlePointer.Value.index);

            return bundle.IsLoadedAsset(m_Key);
        }
        public bool Equals(AssetInfo<T> other)
        {
            return m_BundlePointer.Equals(other.m_BundlePointer) && m_Key.Equals(other.m_Key);
        }

        void IDisposable.Dispose()
        {
            ((AssetInfo)this).ThrowIfIsNotValid();

            ResourceManager.Reserve(m_BundlePointer, this);
        }

        [NotBurstCompatible]
        public override string ToString() => m_Key.ToString();

        public static explicit operator AssetInfo<T>(AssetInfo t)
        {
            return new AssetInfo<T>(t.m_BundlePointer, t.m_InstanceID, t.m_Key);
        }
        public static implicit operator AssetInfo(AssetInfo<T> t)
        {
            return new AssetInfo(t.m_BundlePointer, t.m_InstanceID, t.m_Key);
        }
        public static implicit operator T(AssetInfo<T> t) => t.Asset;
    }

    //////////////////////////////////////////////////////////////////////////////////////////
    /*                                End of Critical Section                               */
    //////////////////////////////////////////////////////////////////////////////////////////
}

#endif