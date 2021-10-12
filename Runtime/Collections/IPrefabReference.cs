using Newtonsoft.Json;
using Syadeu.Collections.Converters;
using System;

namespace Syadeu.Collections
{
    [JsonConverter(typeof(PrefabReferenceJsonConvereter))]
    public interface IPrefabReference : IEquatable<IPrefabReference>, IValidation
    {
        long Index { get; }
        UnityEngine.Object Asset { get; }

        IPrefabResource GetObjectSetting();

        bool IsNone();
    }
    public interface IPrefabReference<T> : IPrefabReference, IEquatable<IPrefabReference<T>>
    {
        new T Asset { get; }
    }
}
