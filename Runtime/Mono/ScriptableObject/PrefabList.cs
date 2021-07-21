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

            [Space]
            [Tooltip("오브젝트의 프리팹입니다")]
            public GameObject m_Prefab;
            public AssetReferenceGameObject m_RefPrefab;

            #region Deprecated
            [Tooltip("최대로 생성될 수 있는 숫자입니다. 값이 음수면 무한")]
            [Obsolete("", true)] public int MaxInstanceCount = -1;
            [Tooltip("사용가능한 객체가 없어서 생성할때 한번에 생성할 갯수")]
            [Obsolete("", true)] [Range(1, 10)] public int InstanceCreationBlock = 1;

            [Space]
            [Tooltip("미사용객체와 사용객체의 격차가 벌어질 경우 릴리즈 트리거를 실행할 격차")]
            [Obsolete("", true)] public int DeletionTriggerCount = 100;
            [Tooltip("트리거가 발동되고 릴리즈 될때까지 시간(초), 도중에 돌아오면 초기화")]
            [Obsolete("", true)] public int DeletionWaitSeconds = 300;
            #endregion

            public override string ToString() => m_Name;

            public bool IsAddressable => m_RefPrefab != null;
            public Queue<GameObject> Pool { get; } = new Queue<GameObject>();
            public GameObject Prefab
            {
                get
                {
                    if (m_RefPrefab == null || !m_RefPrefab.IsValid())
                    {
                        return m_Prefab;
                    }
                    return (GameObject)m_RefPrefab.Asset;
                }
            }
        }
        
        [SerializeField] private List<ObjectSetting> m_ObjectSettings = new List<ObjectSetting>();

        public List<ObjectSetting> ObjectSettings => m_ObjectSettings;

        //public void AddPrefab(GameObject obj)
        //{
        //    AssetReferenceGameObject temp = new AssetReferenceGameObject();
        //    obj.set
        //}
        //public void test()
        //{
        //    Addressables.ResourceManager.
        //}

        #region Deprecated
        [Obsolete("", true)]
        /// <inheritdoc cref="PrefabManager.GetRecycleObject{T}"/>
        public static T GetRecycleObject<T>() where T : RecycleableMonobehaviour
            => PrefabManager.GetRecycleObject<T>();
        [Obsolete("", true)]
        /// <inheritdoc cref="PrefabManager.GetRecycleObject(int)"/>
        public static RecycleableMonobehaviour GetRecycleObject(int index)
            => PrefabManager.GetRecycleObject(index);
        #endregion
    }
}
