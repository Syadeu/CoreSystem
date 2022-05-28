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

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
#define CACHEABLE
#endif

#if UNITY_2019_1_OR_NEWER
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE
#if UNITY_2019 || !UNITY_2020_1_OR_NEWER
#define UNITYENGINE_OLD
#else
using Syadeu.Collections.ResourceControl.LowLevel;
using Syadeu.Collections.Buffer.LowLevel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using Unity.Mathematics;
#endif

using UnityEngine;

namespace Syadeu.Collections.ResourceControl
{
    //////////////////////////////////////////////////////////////////////////////////////////
    /*                                   Critical Section                                   */
    /*                                       수정금지                                        */
    /*                                                                                      */
    /*                          Unsafe pointer를 포함하는 코드입니다                          */
    //////////////////////////////////////////////////////////////////////////////////////////

    [AddComponentMenu("")]
    public sealed class ResourceManager : StaticMonobehaviour<ResourceManager>
    {
        protected override bool EnableLog => false;
        protected override bool HideInInspector => true;

#if !UNITYENGINE_OLD
        private const string c_FileUri = "file:///";
        private const float c_UnloadTime = 300.0f;

        [NonSerialized] private NativeList<UnsafeAssetBundleInfo> m_AssetBundleInfos;
        [NonSerialized] private List<AssetContainer> m_AssetBundles;

        [NonSerialized] private JobHandle 
            m_GlobalJobHandle,
            m_LateUpdateJobHandle,
            m_MapingJobHandle;

        // key = path, value = bundleIndex
        [NonSerialized] private NativeHashMap<Hash, Mapped> m_MappedAssets;
        [NonSerialized] private Hash m_ReferenceCheckSum;

        private NativeList<Mapped> m_WaitForUnloadIndices;

        #region Initialize

        protected override void OnInitialize()
        {
            m_AssetBundleInfos = new NativeList<UnsafeAssetBundleInfo>(AllocatorManager.Persistent);
            m_AssetBundles = new List<AssetContainer>();

            m_MappedAssets = new NativeHashMap<Hash, Mapped>(1024, AllocatorManager.Persistent);
            m_WaitForUnloadIndices = new NativeList<Mapped>(1024, AllocatorManager.Persistent);

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
#if UNITY_ADDRESSABLES
            m_AddressableInitializeHandle = Addressables.InitializeAsync();
#endif
        }
        protected override void OnShutdown()
        {
            m_MapingJobHandle.Complete();

            for (int i = 0; i < m_AssetBundleInfos.Length; i++)
            {
                m_AssetBundleInfos[i].Dispose();
            }

            m_AssetBundleInfos.Dispose();
            m_AssetBundles = null;

            m_MappedAssets.Dispose();
            m_WaitForUnloadIndices.Dispose();
        }

        #endregion

        #region Event Handlers

        private void SceneManager_activeSceneChanged(Scene previous, Scene target)
        {
#if UNITY_ADDRESSABLES
            ResourceHashMap.Instance.UnloadSceneAssets(target);
            ResourceHashMap.Instance.LoadSceneAssets(target, null);
#endif
        }

        #endregion

        #region Monobehaviour Messages

        private void LateUpdate()
        {
            if (!m_MapingJobHandle.IsCompleted) return;
            m_MapingJobHandle.Complete();
            //m_LateUpdateJobHandle.Complete();

            for (int i = m_WaitForUnloadIndices.Length - 1; i >= 0; i--)
            {
                Mapped index = m_WaitForUnloadIndices[i];
                var bundleP = GetUnsafeAssetBundleInfo(in index.bundleIndex);

                ref UnsafeAssetInfo assetInfo = ref bundleP.Value.GetAssetInfo(in index.assetIndex);
                if (assetInfo.assetHandleType == AssetHandleType.Pinned)
                {
                    m_WaitForUnloadIndices.RemoveAt(i);
                    continue;
                }

                if (!assetInfo.lastUsage.IsExceeded(c_UnloadTime))
                {
                    continue;
                }

                assetInfo.loaded = false;

                AssetContainer bundle = Instance.m_AssetBundles[index.bundleIndex];
                bundle.UnloadAsset(assetInfo.key.ToString());

                m_WaitForUnloadIndices.RemoveAt(i);

                CoreHelper.Log(Channel.Core,
                    $"Resource({assetInfo.key}) has been unloaded for exceeding usage time.");
            }
            
            //m_GlobalJobHandle = JobHandle.CombineDependencies(m_GlobalJobHandle, m_LateUpdateJobHandle);
            JobHandle.ScheduleBatchedJobs();
        }

        #endregion

        #region Privates

        /// <summary>
        /// 버퍼에서 현재 사용중이 아닌 주소의 배열 인덱스를 반환합니다. 
        /// 만약 찾지 못하였다면 -1 을 반환합니다.
        /// </summary>
        /// <returns><see cref="m_AssetBundleInfos"/> 의 Index</returns>
        private int GetUnusedAssetBundleBuffer()
        {
            for (int i = 0; i < Instance.m_AssetBundleInfos.Length; i++)
            {
                if (!Instance.m_AssetBundleInfos[i].IsValid())
                {
                    return i;
                }
            }
            return -1;
        }
        /// <summary>
        /// 에셋 번들을 uri 주소 값으로 정보를 찾아서 반환합니다.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="assetBundle"></param>
        /// <returns></returns>
        private bool TryGetAssetBundleWithPath(string path, out AssetBundleInfo assetBundle)
        {
            FixedString4096Bytes temp = path;
            for (int i = 0; i < Instance.m_AssetBundleInfos.Length; i++)
            {
                if (Instance.m_AssetBundleInfos[i].uri.Equals(temp))
                {
                    assetBundle = GetAssetBundleInfo(i);
                    return true;
                }
            }

            assetBundle = default;
            return false;
        }
        /// <summary>
        /// 로드된 에셋 번들로 정보를 찾아서 반환합니다.
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="assetBundle"></param>
        /// <returns></returns>
        private bool TryGetAssetBundleWithBundle(AssetBundle bundle, out AssetBundleInfo assetBundle)
        {
            for (int i = 0; i < Instance.m_AssetBundles.Count; i++)
            {
                if (Instance.m_AssetBundles[i]?.AssetBundle == null) continue;
                else if (Instance.m_AssetBundles[i].AssetBundle.Equals(bundle))
                {
                    assetBundle = GetAssetBundleInfo(i);
                    return true;
                }
            }

            assetBundle = default;
            return false;
        }

        #endregion

        #region Internal

        internal static unsafe AssetBundleInfo GetAssetBundleInfo(in int index)
        {
            ref UnsafeAssetBundleInfo temp = ref Instance.m_AssetBundleInfos.ElementAt(index);

            AssetBundleInfo info
                = new AssetBundleInfo((UnsafeAssetBundleInfo*)UnsafeUtility.AddressOf(ref temp), temp.m_Generation);

            return info;
        }
        internal static unsafe UnsafeReference<UnsafeAssetBundleInfo> GetUnsafeAssetBundleInfo(in int index)
        {
            ref UnsafeAssetBundleInfo temp = ref Instance.m_AssetBundleInfos.ElementAt(index);
            UnsafeAssetBundleInfo* p = (UnsafeAssetBundleInfo*)UnsafeUtility.AddressOf(ref temp);

            return p;
        }
        internal static unsafe UnsafeReference<UnsafeAssetInfo> GetUnsafeAssetInfo(in Hash hash)
        {
            Instance.m_MapingJobHandle.Complete();

            if (!Instance.m_MappedAssets.TryGetValue(hash, out Mapped index))
            {
                return default;
            }

            UnsafeReference<UnsafeAssetBundleInfo> bundleP = GetUnsafeAssetBundleInfo(in index.bundleIndex);
            return bundleP.Value.GetAssetInfoPointer(in index.assetIndex);
        }
        internal static unsafe UnsafeReference<UnsafeAssetInfo> GetUnsafeAssetInfo(in Mapped index)
        {
            UnsafeReference<UnsafeAssetBundleInfo> bundleP = GetUnsafeAssetBundleInfo(in index.bundleIndex);
            return bundleP.Value.GetAssetInfoPointer(in index.assetIndex);
        }

        internal static unsafe AssetContainer GetAssetBundle(in int index)
        {
            //if (Instance.m_AssetBundles[index] == null)
            //{
            //    Instance.m_AssetBundles[index] = new AssetContainer();
            //}
            return Instance.m_AssetBundles[index];
        }

        internal static unsafe Promise<AssetBundle> LoadAssetBundle(UnsafeAssetBundleInfo* p)
        {
            int index = p->index;
            string uri = p->uri.ToString();

            if (!uri.StartsWith(c_FileUri))
            {
                AssetBundleLoadAsyncHandler handler = new AssetBundleLoadAsyncHandler();
                UnityWebRequest request;

#if CACHEABLE
                if (Instance.m_Manifest != null)
                {
                    string bundleName = Path.GetFileName(uri);
                    Cache cache = Caching.GetCacheByPath(Application.temporaryCachePath);
                    if (!cache.valid) cache = Caching.AddCache(Application.temporaryCachePath);

                    Caching.currentCacheForWriting = cache;
                    Hash128 hash = Instance.m_Manifest.GetAssetBundleHash(bundleName);

                    request = UnityWebRequestAssetBundle.GetAssetBundle(uri, hash, p->crc);
                }
                else
#endif
                {
                    request = UnityWebRequestAssetBundle.GetAssetBundle(uri, p->crc);
                }
                
                handler.Initialize(p, GetAssetBundle(in index), request);

                Promise<AssetBundle> promise = new Promise<AssetBundle>(handler);
                return promise;
            }

            string path = uri.Replace(c_FileUri, string.Empty);

            AssetBundle bundle;
            using (var st = File.OpenRead(path))
            {
                bundle = AssetBundle.LoadFromStream(st, p->crc);
            }

            if (bundle == null)
            {
                $"crc falid. {path} {p->crc}".ToLogError();
                return null;
            }

            p->loaded = true;

            AssetContainer container = GetAssetBundle(in index);
            container.AssetBundle = bundle;

            UpdateAssetInfos(p, bundle);

            return bundle;
        }
        internal static unsafe Promise<AssetBundle> LoadAssetBundleAsync(UnsafeAssetBundleInfo* p)
        {
            int index = p->index;
            string uri = p->uri.ToString();
            UnityWebRequest request;

#if CACHEABLE
            // If uri is targeting remote server, cache file.
            if (Instance.m_Manifest != null && !uri.StartsWith(c_FileUri))
            {
                string bundleName = Path.GetFileName(uri);
                Cache cache = Caching.GetCacheByPath(Application.temporaryCachePath);
                if (!cache.valid) cache = Caching.AddCache(Application.temporaryCachePath);

                Caching.currentCacheForWriting = cache;
                Hash128 hash = Instance.m_Manifest.GetAssetBundleHash(bundleName);
                request = UnityWebRequestAssetBundle.GetAssetBundle(uri, hash, p->crc);
            }
            else
#endif
            {
                request = UnityWebRequestAssetBundle.GetAssetBundle(uri, p->crc);
            }

            //AssetBundle bundle = AssetBundle.LoadFromFile(p->uri.ToString());
            var handler = new AssetBundleLoadAsyncHandler();
            handler.Initialize(p, GetAssetBundle(in index), request);

            return new Promise<AssetBundle>(handler);
        }

        internal static unsafe void UnloadAssetBundle(ref UnsafeAssetBundleInfo p, bool unloadAllLoadedObjects)
            => UnloadAssetBundle((UnsafeAssetBundleInfo*)UnsafeUtility.AddressOf(ref p), unloadAllLoadedObjects);
        internal static unsafe void UnloadAssetBundle(UnsafeAssetBundleInfo* p, bool unloadAllLoadedObjects)
        {
            int index = p->index;

            p->loaded = false;
            p->assets.Dispose();

            Instance.m_AssetBundles[index].AssetBundle.Unload(unloadAllLoadedObjects);
            Instance.m_AssetBundles[index].Clear();
        }

        internal static unsafe JobHandle UpdateAssetInfos(UnsafeAssetBundleInfo* p, AssetBundle assetBundle)
        {
            var assetNames = assetBundle.GetAllAssetNames().Select(str => (FixedString512Bytes)str).ToArray();
            NativeArray<FixedString512Bytes> names = new NativeArray<FixedString512Bytes>(assetNames, Allocator.TempJob);

            Instance.m_MapingJobHandle.Complete();

            if (!p->assets.IsCreated)
            {
                p->assets = new UnsafeList<UnsafeAssetInfo>(names.Length, AllocatorManager.Persistent, NativeArrayOptions.UninitializedMemory);
                p->assets.Length = names.Length;
            }
            else
            {
                p->assets.Clear();
            }

            UpdateAssetInfoJob job = new UpdateAssetInfoJob()
            {
                m_BundleIndex = p->index,
                m_Names = names,
                m_HashMap = p->assets.Ptr,
                m_MappedAssets = Instance.m_MappedAssets.AsParallelWriter()
            };

            JobHandle handle = job.Schedule(names.Length, 64, Instance.m_MapingJobHandle);
            p->m_JobHandle = JobHandle.CombineDependencies(p->m_JobHandle, handle);

            Instance.m_MapingJobHandle = JobHandle.CombineDependencies(Instance.m_MapingJobHandle, handle);

            return handle;
        }

        #region Job Structs

        [BurstCompile(CompileSynchronously = true)]
        private unsafe struct UpdateAssetInfoJob : IJobParallelFor
        {
            [ReadOnly] public int m_BundleIndex;
            [ReadOnly, DeallocateOnJobCompletion] public NativeArray<FixedString512Bytes> m_Names;
            [WriteOnly, NativeDisableUnsafePtrRestriction] public UnsafeAssetInfo* m_HashMap;
            [WriteOnly] public NativeHashMap<Hash, Mapped>.ParallelWriter m_MappedAssets;

            public void Execute(int i)
            {
                UnsafeAssetInfo assetInfo = new UnsafeAssetInfo()
                {
                    key = m_Names[i],
                    loaded = false,
                };

                Hash hash = new Hash(m_Names[i]);

                UnsafeUtility.WriteArrayElement(m_HashMap, i, assetInfo);
                m_MappedAssets.TryAdd(hash, new Mapped(m_BundleIndex, i));
            }
        }
        
        #endregion

        [NotBurstCompatible]
        internal static unsafe bool HasAsset(UnsafeReference<UnsafeAssetBundleInfo> bundleP, in FixedString512Bytes key)
        {
            Instance.m_MapingJobHandle.Complete();

            if (!bundleP.Value.loaded)
            {
                CoreHelper.LogError(LogChannel.Collections,
                    $"Cound not load asset {key}. Target AssetBundle is not loaded.");

                return false;
            }

            Hash hash = new Hash(key.ToString().ToLowerInvariant());
            if (!Instance.m_MappedAssets.ContainsKey(hash))
            {
                return false;
            }

            return true;
        }
        [NotBurstCompatible]
        internal static unsafe bool HasAsset(UnsafeReference<UnsafeAssetBundleInfo> bundleP, in Hash key)
        {
            Instance.m_MapingJobHandle.Complete();

            if (!bundleP.Value.loaded)
            {
                CoreHelper.LogError(LogChannel.Collections,
                    $"Cound not load asset {key}. Target AssetBundle is not loaded.");

                return false;
            }

            if (!Instance.m_MappedAssets.ContainsKey(key))
            {
                return false;
            }

            return true;
        }

        private static unsafe bool ValidateAsset(
            UnsafeReference<UnsafeAssetBundleInfo> bundleP, in Hash hash, out Mapped index)
        {
            if (!bundleP.Value.loaded)
            {
                CoreHelper.LogError(LogChannel.Collections,
                    $"Cound not load asset {hash}. Target AssetBundle is not loaded.");

                index = default(Mapped);
                return false;
            }
            else if (!Instance.m_MappedAssets.TryGetValue(hash, out index))
            {
                CoreHelper.LogError(LogChannel.Collections,
                    $"Cound not load asset {hash}. Target is not listed in target AssetBundle.");

                return false;
            }

            return true;
        }
        [NotBurstCompatible]
        internal static unsafe AssetInfo LoadAsset(UnsafeReference<UnsafeAssetBundleInfo> bundleP, in Hash hash)
        {
            Instance.m_MapingJobHandle.Complete();
            if (!ValidateAsset(bundleP, in hash, out Mapped index))
            {
                return AssetInfo.Invalid;
            }

            ref UnsafeAssetInfo assetInfo = ref bundleP.Value.GetAssetInfo(in index.assetIndex);
            AssetInfo asset = new AssetInfo(bundleP, Hash.NewHash(), hash);
            assetInfo.checkSum ^= asset.m_InstanceID;
            Instance.m_ReferenceCheckSum ^= asset.m_InstanceID;

            InternalLoadAsset(ref assetInfo, in index, assetInfo.key.ToString());

            return asset;
        }
        [NotBurstCompatible]
        internal static unsafe AssetInfo LoadAssetAsync(UnsafeReference<UnsafeAssetBundleInfo> bundleP, in Hash hash)
        {
            Instance.m_MapingJobHandle.Complete();
            if (!ValidateAsset(bundleP, in hash, out Mapped index))
            {
                return AssetInfo.Invalid;
            }

            ref UnsafeAssetInfo assetInfo = ref bundleP.Value.GetAssetInfo(in index.assetIndex);
            AssetInfo asset = new AssetInfo(bundleP, Hash.NewHash(), hash);
            assetInfo.checkSum ^= asset.m_InstanceID;
            Instance.m_ReferenceCheckSum ^= asset.m_InstanceID;

            InternalLoadAssetAsync(ref assetInfo, in index, assetInfo.key.ToString());

            return asset;
        }
        [NotBurstCompatible]
        internal static unsafe AssetInfo LoadAsset(UnsafeReference<UnsafeAssetBundleInfo> bundleP, in FixedString512Bytes key)
        {
            return LoadAsset(bundleP, new Hash(key.ToString().ToLowerInvariant()));
        }
        [NotBurstCompatible]
        internal static unsafe AssetInfo LoadAssetAsync(UnsafeReference<UnsafeAssetBundleInfo> bundleP, in FixedString512Bytes key)
        {
            return LoadAssetAsync(bundleP, new Hash(key.ToString().ToLowerInvariant()));
        }

        [NotBurstCompatible]
        private static unsafe void InternalLoadAsset(ref UnsafeAssetInfo assetInfo, in Mapped index, string key)
        {
            // If the asset has already loaded, skip the load process
            if (assetInfo.loaded) return;

            assetInfo.loaded = true;
            assetInfo.lastUsage = Timer.Start();

            AssetContainer bundle = Instance.m_AssetBundles[index.bundleIndex];
            bundle.LoadAsset(key);
        }
        [NotBurstCompatible]
        private static unsafe void InternalLoadAssetAsync(ref UnsafeAssetInfo assetInfo, in Mapped index, in string key)
        {
            // If the asset has already loaded, skip the load process
            if (assetInfo.loaded) return;

            assetInfo.loaded = true;
            assetInfo.lastUsage = Timer.Start();

            AssetContainer bundle = Instance.m_AssetBundles[index.bundleIndex];
            bundle.LoadAssetAsync(key);
        }

        internal static unsafe void Reserve(UnsafeReference<UnsafeAssetBundleInfo> bundleP, in AssetInfo asset)
        {
            if (!bundleP.Value.loaded)
            {
                throw new InvalidOperationException();
            }

            Mapped index = Instance.m_MappedAssets[asset.m_Key];
            ref UnsafeAssetInfo assetInfo = ref bundleP.Value.assets.ElementAt(index.assetIndex);
            if (!assetInfo.loaded)
            {
                throw new Exception("2");
            }

            assetInfo.checkSum ^= asset.m_InstanceID;
            Instance.m_ReferenceCheckSum ^= asset.m_InstanceID;

            if (assetInfo.checkSum.IsEmpty() && !Instance.m_WaitForUnloadIndices.Contains(index))
            {
                Instance.m_WaitForUnloadIndices.Add(index);
            }

            asset.RemoveLoadedFrame();
        }

        #endregion

        #region Addressables

#if UNITY_ADDRESSABLES
        private AsyncOperationHandle<IResourceLocator> m_AddressableInitializeHandle;
        internal readonly Dictionary<AssetRuntimeKey, AsyncOperationHandle<IResourceLocation>> m_Locations = new Dictionary<AssetRuntimeKey, AsyncOperationHandle<IResourceLocation>>();
        
        private static object EvaluateKey(IKeyEvaluator runtimeKey)
        {
            return ((IKeyEvaluator)runtimeKey).RuntimeKey;
        }
        private static Type EvaluateType(Type type)
        {
            if (type.IsArray)
            {
                type = type.GetElementType();
            }
            else if (type.IsGenericType && typeof(IList<>) == type.GetGenericTypeDefinition())
            {
                type = type.GetGenericArguments()[0];
            }

            return type;
        }

        internal static AsyncOperationHandle<TObject> CreateCompletedOperation<TObject>(TObject result, System.Exception exception)
        {
            return Addressables.ResourceManager.CreateCompletedOperationWithException(result, exception);
        }
        internal static AsyncOperationHandle<TObject> CreateCompletedOperationExeception<TObject>(TObject result, object runtimeKey, Type type)
        {
            return Addressables.ResourceManager.CreateCompletedOperationWithException(result, new InvalidKeyException(runtimeKey, type));
        }
        internal static AsyncOperationHandle<TObject> CreateCompletedOperation<TObject>(TObject result)
        {
            return Addressables.ResourceManager.CreateCompletedOperation(result, string.Empty);
        }

        public static AsyncOperationHandle<IResourceLocation> GetLocation(AssetReference runtimeKey, Type type)
        {
            AssetRuntimeKey key = runtimeKey.RuntimeKey;
            if (Instance.m_Locations.TryGetValue(key, out AsyncOperationHandle<IResourceLocation> location)) return location;

            object stringKey = EvaluateKey(runtimeKey);
            type = EvaluateType(type);

            location = Addressables.ResourceManager.StartOperation(
                     FindResourceLocationOperation.Get(stringKey, type),
                     Instance.m_AddressableInitializeHandle
                     );

            Instance.m_Locations[key] = location;
            return location;
        }
        public static AsyncOperationHandle<IResourceLocation> GetLocation<TObject>(AssetReference runtimeKey)
        {
            Type type = TypeHelper.TypeOf<TObject>.Type;
            return GetLocation(runtimeKey, type);
        }

        public static AsyncOperationHandle LoadAssetAsync(AssetReference runtimeKey)
        {
            var locationHandle = GetLocation(runtimeKey, TypeHelper.TypeOf<UnityEngine.Object>.Type);

            return LoadAssetAsync(locationHandle);
        }
        public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(AssetReference runtimeKey)
            where TObject : UnityEngine.Object
        {
            var locationHandle = GetLocation(runtimeKey, TypeHelper.TypeOf<TObject>.Type);

            return LoadAssetAsync<TObject>(locationHandle);
        }

        private static AsyncOperationHandle<TObject> ProvideResource<TObject>(AsyncOperationHandle<IResourceLocation> oper)
        {
            return Addressables.ResourceManager.ProvideResource<TObject>(oper.Result);
        }
        public static AsyncOperationHandle LoadAssetAsync(AsyncOperationHandle<IResourceLocation> location)
        {
            var handle = Addressables.ResourceManager.CreateChainOperation(
                location,
                ProvideResource<UnityEngine.Object>
                );

            return handle;
        }
        public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(AsyncOperationHandle<IResourceLocation> location)
            where TObject : UnityEngine.Object
        {
            var handle = Addressables.ResourceManager.CreateChainOperation(
                location,
                ProvideResource<TObject>
                );

            return handle;
        }

        public static AsyncOperationHandle<GameObject> InstantiateAsync(
            AsyncOperationHandle<IResourceLocation> location,
            InstantiationParameters parameters)
        {
            var temp = Addressables.ResourceManager.CreateChainOperation(location,
                delegate (AsyncOperationHandle<IResourceLocation> t)
                {
                    return Addressables.ResourceManager.ProvideInstance(
                        Addressables.InstanceProvider,
                        t.Result,
                        parameters
                        );
                });
            

            return temp;
        }

        public static void Release(AsyncOperationHandle handle)
        {
            Addressables.Release(handle);
        }
#endif

        #endregion

        #region Inner Classes

        internal sealed class AssetContainer
        {
            private AssetBundle m_AssetBundle;
            //public string[] m_Dependencies = Array.Empty<string>();
            // 현재 로드된 에셋들의 HashMap 입니다.
            private Dictionary<Hash, Promise<UnityEngine.Object>> m_Assets;

            public AssetBundle AssetBundle
            {
                get => m_AssetBundle;
                set => m_AssetBundle = value;
            }

            public AssetContainer() : this(null) { }
            public AssetContainer(AssetBundle assetBundle)
            {
                m_AssetBundle = assetBundle;
                m_Assets = new Dictionary<Hash, Promise<UnityEngine.Object>>();
            }

            public bool IsLoadedAsset(Hash hash)
            {
                return m_Assets.ContainsKey(hash);
            }
            public Promise<UnityEngine.Object> GetAsset(Hash hash)
            {
                if (m_Assets.TryGetValue(hash, out Promise<UnityEngine.Object> promise))
                {
                    return promise;
                }
                return null;
            }
            public Promise<UnityEngine.Object> LoadAsset(string key)
            {
				key = key.ToLowerInvariant();
                Hash hash = new Hash(key);

                if (m_Assets.TryGetValue(hash, out Promise<UnityEngine.Object> promise))
                {
                    return promise;
                }

                promise = new Promise<UnityEngine.Object>(m_AssetBundle.LoadAsset(key));
                m_Assets.Add(hash, promise);

                return promise;
            }
            public Promise<UnityEngine.Object> LoadAssetAsync(string key)
            {
				key = key.ToLowerInvariant();
                Hash hash = new Hash(key);

                if (m_Assets.TryGetValue(hash, out Promise<UnityEngine.Object> promise))
                {
                    return promise;
                }

                var request = m_AssetBundle.LoadAssetAsync(key);
                AssetRequest assetRequest = AssetRequest.Initialize(request);
                promise = new Promise<UnityEngine.Object>(assetRequest);
                
                m_Assets.Add(hash, promise);

                return promise;
            }
            public void UnloadAsset(string key)
            {
                key = key.ToLowerInvariant();
                Hash hash = new Hash(key);

                if (!m_Assets.TryGetValue(hash, out Promise<UnityEngine.Object> promise))
                {
                    return;
                }

                Resources.UnloadAsset(promise.Value);

                promise.Dispose();
                m_Assets.Remove(hash);

                if (m_Assets.Count == 0)
                {
                    m_AssetBundle.Unload(true);
                }
            }

            public void Clear()
            {
                m_AssetBundle = null;
                m_Assets.Clear();
            }
        }
        internal struct Mapped : IEquatable<Mapped>
        {
            public int bundleIndex;
            public int assetIndex;

            public Mapped(int bundle, int asset)
            {
                bundleIndex = bundle;
                assetIndex = asset;
            }

            public bool Equals(Mapped other) => bundleIndex == other.bundleIndex && assetIndex == other.assetIndex;
        }

        #endregion

        private AssetBundleManifest m_Manifest;
        public static AssetBundleManifest Manifest { get => Instance.m_Manifest; set => Instance.m_Manifest = value; }

        public static void RegisterManifest(AssetBundle manifestBundle)
        {
            const string c_ManifestAssetName = "AssetBundleManifest";

            AssetBundleManifest manifest = manifestBundle.LoadAsset<AssetBundleManifest>(c_ManifestAssetName);
            manifestBundle.Unload(false);

            Instance.m_Manifest = manifest;
        }
        public static void RegisterManifest(AssetBundleManifest manifest)
        {
            Instance.m_Manifest = manifest;
        }

        /// <summary>
        /// 에셋 번들을 상대 경로로 추가하여 정보를 반환합니다. 
        /// </summary>
        /// <remarks>
        /// uri + <see cref="Application.dataPath"/> + <paramref name="path"/> 로 찾습니다.
        /// </remarks>
        /// <param name="path"></param>
        /// <returns></returns>
        public static AssetBundleInfo RegisterAssetBundlePath(string path)
        {
            if (path.StartsWith("Assets"))
            {
                path = path.Substring(6, path.Length - 6);
            }

            string uri = c_FileUri + Application.dataPath + path;
            return RegisterAssetBundleUri(uri);
        }
        /// <summary>
        /// 절대 경로로 에셋 번들을 추가하여 정보를 반환합니다.
        /// </summary>
        /// <remarks>
        /// uri + <paramref name="path"/> 로 찾습니다. 
        /// <seealso cref="UnityWebRequest"/> 을 통한 Uri 는 <seealso cref="RegisterAssetBundleUri(string, uint)"/>
        /// 를 참조하세요.
        /// </remarks>
        /// <param name="path"></param>
        /// <returns></returns>
        public static AssetBundleInfo RegisterAssetBundleAbsolutePath(string path)
        {
            string uri = c_FileUri + path;
            return RegisterAssetBundleUri(uri);
        }
        public static AssetBundleInfo RegisterAssetBundleUri(string uri, uint crc = 0)
        {
            if (Instance.TryGetAssetBundleWithPath(uri, out var bundle)) return bundle;

            int index = Instance.GetUnusedAssetBundleBuffer();
            AssetContainer assetContainer;
            if (index < 0)
            {
                index = Instance.m_AssetBundles.Count;
                var info = new UnsafeAssetBundleInfo(index)
                {
                    m_Using = true,

                    uri = uri,
                    crc = crc
                };

                assetContainer = new AssetContainer();
                Instance.m_AssetBundleInfos.Add(info);
                Instance.m_AssetBundles.Add(assetContainer);
            }
            else
            {
                ref var info = ref Instance.m_AssetBundleInfos.ElementAt(index);
                info.m_Generation++;

                info.m_Using = true;

                info.uri = uri;
                info.crc = crc;

                assetContainer = new AssetContainer();
                Instance.m_AssetBundles[index] = assetContainer;
            }

            if (Instance.m_Manifest == null && uri.StartsWith(c_FileUri))
            {
                string filePath = uri.Replace(c_FileUri, string.Empty),
                    directoryPath = Path.GetDirectoryName(filePath),
                    directoryName = directoryPath.Split(Path.DirectorySeparatorChar).Last(),
                    manifestPath = Path.Combine(directoryPath, directoryName);

                if (File.Exists(manifestPath))
                {
                    AssetBundle manifestBundle = AssetBundle.LoadFromFile(manifestPath);
                    RegisterManifest(manifestBundle);

                    //assetContainer.m_Dependencies = manifest.GetDirectDependencies(Path.GetFileName(filePath));

                }
            }

            //

            return GetAssetBundleInfo(in index);
        }
        /// <summary>
        /// 사용자가 미리 메모리에 로드한 <see cref="AssetBundle"/> 을 리소스 매니저에 등록합니다.
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <returns></returns>
        public static AssetBundleInfo RegisterAssetBundle(AssetBundle assetBundle)
        {
            // 만약 등록할 번들이 이미 등록된 번들이라면 즉시 반환합니다.
            if (Instance.TryGetAssetBundleWithBundle(assetBundle, out var bundle)) return bundle;

            int index = Instance.GetUnusedAssetBundleBuffer();
            if (index < 0)
            {
                index = Instance.m_AssetBundles.Count;
                var info = new UnsafeAssetBundleInfo(index)
                {
                    m_Using = true,

                    loaded = true
                };

                Instance.m_AssetBundleInfos.Add(info);
                Instance.m_AssetBundles.Add(new AssetContainer(assetBundle));
            }
            else
            {
                ref var info = ref Instance.m_AssetBundleInfos.ElementAt(index);
                info.m_Generation++;

                info.m_Using = true;

                info.loaded = true;

                Instance.m_AssetBundles[index] = new AssetContainer(assetBundle);
            }

            AssetBundleInfo bundleInfo = GetAssetBundleInfo(in index);
            unsafe
            {
                UpdateAssetInfos(bundleInfo.m_Pointer, assetBundle);
            }

            return bundleInfo;
        }
        public static void UnregisterAssetBundle(AssetBundleInfo assetBundle)
        {
            if (assetBundle.IsLoaded)
            {
                throw new InvalidDataException("asset bundle is not unloaded.");
            }

            int index = assetBundle.Ref.index;

            ref var info = ref Instance.m_AssetBundleInfos.ElementAt(index);

            info.m_Using = false;

            info.uri = string.Empty;
            info.loaded = false;

            if (info.assets.IsCreated)
            {
                info.assets.Dispose();
            }

            Instance.m_AssetBundles[index] = null;
        }

        /// <summary>
        /// 해당 키의 에셋의 에셋번들을 반환합니다.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static AssetBundleInfo GetAssetBundleWithAssetPath(in FixedString4096Bytes key)
        {
            CoreHelper.AssertMainThread();
            Instance.m_MapingJobHandle.Complete();

            Hash hash = new Hash(key.ToString().ToLowerInvariant());
            if (!Instance.m_MappedAssets.TryGetValue(hash, out Mapped index))
            {
#if UNITY_EDITOR
                string bundleName = UnityEditor.AssetDatabase.GetImplicitAssetBundleName(key.ToString());

                CoreHelper.LogError(LogChannel.Collections,
                    $"Asset({key}) is not registered. This asset is in the AssetBundle({bundleName}) but you didn\'t registered.");
#endif
                return AssetBundleInfo.Invalid;
            }

            return GetAssetBundleInfo(index.bundleIndex);
        }
        /// <summary>
        /// 해당 키의 에셋이 로드되었는지 반환합니다.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsLoadedAsset(in FixedString512Bytes key)
        {
            CoreHelper.AssertMainThread();
            Instance.m_MapingJobHandle.Complete();

            Hash hash = new Hash(key.ToString().ToLowerInvariant());
            if (!Instance.m_MappedAssets.TryGetValue(hash, out Mapped index))
            {
#if UNITY_EDITOR
                string bundleName = UnityEditor.AssetDatabase.GetImplicitAssetBundleName(key.ToString());

                CoreHelper.LogError(LogChannel.Collections,
                    $"Asset({key}) is not registered. This asset is in the AssetBundle({bundleName}) but you didn\'t registered.");
#endif
                return false;
            }

            UnsafeReference<UnsafeAssetBundleInfo> bundle = GetUnsafeAssetBundleInfo(in index.bundleIndex);
            UnsafeAssetInfo assetInfo = bundle.Value.assets.ElementAt(index.assetIndex);

            return assetInfo.loaded;
        }
        /// <summary>
        /// 해당 키의 에셋을 로드합니다.
        /// </summary>
        /// <remarks>
        /// 에셋을 로드하기전, 해당 에셋의 에셋 번들이 먼저 로드되어야 합니다. 
        /// <seealso cref="RegisterAssetBundle(AssetBundle)"/> 메소드를 제외한 나머지 메소드로 등록한 번들
        /// (<seealso cref="RegisterAssetBundleUri(string, uint)"/>) 와 같은)
        /// 은 먼저 <seealso cref="AssetBundleInfo.Load"/> 를 수행하세요.
        /// </remarks>
        /// <param name="key">이 값은 에디터상 상대 경로입니다 Assets/...</param>
        /// <returns></returns>
        public static AssetInfo LoadAsset(in FixedString512Bytes key)
        {
            CoreHelper.AssertMainThread();
            Instance.m_MapingJobHandle.Complete();

            Hash hash = new Hash(key.ToString().ToLowerInvariant());
            if (!Instance.m_MappedAssets.ContainsKey(hash))
            {
#if UNITY_EDITOR
                string bundleName = UnityEditor.AssetDatabase.GetImplicitAssetBundleName(key.ToString());

                CoreHelper.LogError(LogChannel.Collections,
                    $"Asset({key}) is not registered. This asset is in the AssetBundle({bundleName}) but you didn\'t registered.");
#endif
                return AssetInfo.Invalid;
            }

            Mapped index = Instance.m_MappedAssets[hash];

            if (!Instance.m_AssetBundleInfos[index.bundleIndex].loaded)
            {
                CoreHelper.LogError(LogChannel.Collections,
                    $"Cound not load asset {key}. Target AssetBundle is not loaded.");

                return AssetInfo.Invalid;
            }

            UnsafeReference<UnsafeAssetBundleInfo> p = GetUnsafeAssetBundleInfo(in index.bundleIndex);
            
            return LoadAsset(p, in key);
        }
#endif
    }

    //////////////////////////////////////////////////////////////////////////////////////////
    /*                                End of Critical Section                               */
    //////////////////////////////////////////////////////////////////////////////////////////
}

#endif