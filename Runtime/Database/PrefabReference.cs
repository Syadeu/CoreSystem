using Syadeu.Mono;
using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Collections
{
    [Serializable]
    public readonly struct PrefabReference : IPrefabReference, IEquatable<PrefabReference>
    {
        public static readonly PrefabReference Invalid = new PrefabReference(-1);
        public static readonly PrefabReference None = new PrefabReference(-2);

        private readonly long m_Idx;

        public long Index => m_Idx;
        public UnityEngine.Object Asset
        {
            get
            {
                var set = GetObjectSetting();
                if (set == null) return null;

                return set.LoadedObject;
            }
        }

        public PrefabReference(int idx)
        {
            m_Idx = idx;
        }
        public PrefabReference(long idx)
        {
            m_Idx = idx;
        }

        public bool Equals(PrefabReference other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(IPrefabReference other) => m_Idx.Equals(other.Index);

        IPrefabResource IPrefabReference.GetObjectSetting() => GetObjectSetting();
        public PrefabList.ObjectSetting GetObjectSetting()
        {
            if (!IsValid() || Equals(None)) return null;
            return PrefabList.Instance.ObjectSettings[(int)m_Idx];
        }
        public AsyncOperationHandle LoadAssetAsync() => GetObjectSetting().LoadAssetAsync();
        public AsyncOperationHandle<T> LoadAssetAsync<T>() where T : UnityEngine.Object => GetObjectSetting().LoadAssetAsync<T>();
        public void UnloadAsset() => GetObjectSetting().UnloadAsset();
        public void ReleaseInstance(UnityEngine.GameObject obj) => GetObjectSetting().ReleaseInstance(obj);

        public bool IsNone() => Equals(None);
        public bool IsValid() => !Equals(Invalid) && m_Idx < PrefabList.Instance.ObjectSettings.Count;

        public static PrefabReference Find(string name)
        {
            var obj = PrefabList.Instance.ObjectSettings.FindFor((other) => other.m_Name.Equals(name));
            if (obj != null)
            {
                return new PrefabReference(PrefabList.Instance.ObjectSettings.IndexOf(obj));
            }
            return Invalid;
        }

        public static implicit operator int(PrefabReference a) => (int)a.m_Idx;
        public static implicit operator PrefabReference(int a)
        {
            if (0 >= a && a >= PrefabList.Instance.ObjectSettings.Count)
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"Cannot found prefab index of {a}. Request ignored.");
                return Invalid;
            }
            return new PrefabReference(a);
        }
        public static implicit operator string(PrefabReference a) => a.GetObjectSetting().m_Name;
        public static implicit operator PrefabList.ObjectSetting(PrefabReference a) => a.GetObjectSetting();
    }
    [Serializable]
    public readonly struct PrefabReference<T> : IPrefabReference<T>, IEquatable<PrefabReference<T>>
        where T : UnityEngine.Object
    {
        public static readonly PrefabReference<T> Invalid = new PrefabReference<T>(-1);
        public static readonly PrefabReference<T> None = new PrefabReference<T>(-2);

        private readonly long m_Idx;

        public long Index => m_Idx;

        UnityEngine.Object IPrefabReference.Asset
        {
            get
            {
                var set = GetObjectSetting();
                if (set == null) return null;

                return set.LoadedObject;
            }
        }
        public T Asset
        {
            get
            {
                var set = GetObjectSetting();
                if (set == null) return null;

                var target = set.LoadedObject;
                if (target == null) return null;
                return (T)target;
            }
        }

        public PrefabReference(int idx)
        {
            m_Idx = idx;
        }
        public PrefabReference(long idx)
        {
            m_Idx = idx;
        }

        public bool Equals(PrefabReference<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(IPrefabReference other) => m_Idx.Equals(other.Index);
        public bool Equals(IPrefabReference<T> other) => m_Idx.Equals(other.Index);

        IPrefabResource IPrefabReference.GetObjectSetting() => GetObjectSetting();
        public PrefabList.ObjectSetting GetObjectSetting()
        {
            if (!IsValid() || Equals(None)) return null;
            return PrefabList.Instance.ObjectSettings[(int)m_Idx];
        }
        public AsyncOperationHandle<T> LoadAssetAsync() => GetObjectSetting().LoadAssetAsync<T>();
        public void UnloadAsset() => GetObjectSetting().UnloadAsset();

        public AsyncOperationHandle<UnityEngine.GameObject> InstantiateAysnc(in float3 pos, in quaternion rot, in UnityEngine.Transform parent) => GetObjectSetting().InstantiateAsync(in pos, in rot, in parent);
        public void ReleaseInstance(UnityEngine.GameObject obj) => GetObjectSetting().ReleaseInstance(obj);

        public bool IsNone() => Equals(None);
        public bool IsValid() => !Equals(Invalid) && 0 <= m_Idx && m_Idx < PrefabList.Instance.ObjectSettings.Count;

        public static PrefabReference<T> Find(string name)
        {
            var obj = PrefabList.Instance.ObjectSettings.FindFor((other) => other.m_Name.Equals(name));
            if (obj != null)
            {
                return new PrefabReference<T>(PrefabList.Instance.ObjectSettings.IndexOf(obj));
            }
            return Invalid;
        }

        public static implicit operator int(PrefabReference<T> a) => (int)a.m_Idx;
        public static implicit operator PrefabReference<T>(int a)
        {
            if (0 >= a && a >= PrefabList.Instance.ObjectSettings.Count)
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"Cannot found prefab index of {a}. Request ignored.");
                return Invalid;
            }
            return new PrefabReference<T>(a);
        }
        public static implicit operator string(PrefabReference<T> a) => a.GetObjectSetting().m_Name;
        public static implicit operator PrefabList.ObjectSetting(PrefabReference<T> a) => a.GetObjectSetting();

        public static implicit operator PrefabReference(PrefabReference<T> a) => new PrefabReference(a.m_Idx);
    }
}
