using Newtonsoft.Json;
using Syadeu.Collections.Converters;
using System;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Collections
{
    [JsonConverter(typeof(PrefabReferenceJsonConverter))]
    public interface IPrefabReference : IEquatable<IPrefabReference>, IValidation
    {
        long Index { get; }
        UnityEngine.Object Asset { get; }

        IPrefabResource GetObjectSetting();

        AsyncOperationHandle LoadAssetAsync();
        AsyncOperationHandle<T> LoadAssetAsync<T>() where T : UnityEngine.Object;
        void UnloadAsset();
        void ReleaseInstance(UnityEngine.GameObject obj);

        bool IsNone();
    }
    public interface IPrefabReference<T> : IPrefabReference, IEquatable<IPrefabReference<T>>
    {
        new T Asset { get; }
    }
}
