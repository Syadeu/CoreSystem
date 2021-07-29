using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Syadeu.Mono
{
    [PreferBinarySerialization]
    public sealed class PrefabList : StaticSettingEntity<PrefabList>
    {
        [Serializable]
        public sealed class ObjectSetting
        {
            public string m_Name;
            public AssetReference m_RefPrefab;

            public Queue<GameObject> Pool { get; } = new Queue<GameObject>();

            public override string ToString() => m_Name;
        }
        
        [SerializeField] private List<ObjectSetting> m_ObjectSettings = new List<ObjectSetting>();

        public List<ObjectSetting> ObjectSettings => m_ObjectSettings;
    }
}
