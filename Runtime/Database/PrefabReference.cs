﻿using Newtonsoft.Json;
using Syadeu.Database.Converters;
using Syadeu.Mono;
using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Syadeu.Database
{
    [Serializable, JsonConverter(typeof(PrefabReferenceJsonConvereter))]
    public struct PrefabReference : IEquatable<PrefabReference>, IValidation
    {
        public static readonly PrefabReference Invalid = new PrefabReference(-1);
        public static readonly PrefabReference None = new PrefabReference(-2);

        [JsonProperty(Order = 0)] public readonly long m_Idx;

        public PrefabReference(int idx)
        {
            m_Idx = idx;
        }
        public PrefabReference(long idx)
        {
            m_Idx = idx;
        }

        public bool Equals(PrefabReference other) => m_Idx.Equals(other.m_Idx);

        public PrefabList.ObjectSetting GetObjectSetting()
        {
            if (!IsValid() || Equals(None)) return null;
            return PrefabList.Instance.ObjectSettings[(int)m_Idx];
        }

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
}
