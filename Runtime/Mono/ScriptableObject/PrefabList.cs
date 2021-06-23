﻿using System;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

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
            public GameObject Prefab;
#if UNITY_ADDRESSABLES
            public AssetReferenceGameObject RefPrefab;
#endif
            [Tooltip("최대로 생성될 수 있는 숫자입니다. 값이 음수면 무한")]
            public int MaxInstanceCount = -1;
            [Tooltip("사용가능한 객체가 없어서 생성할때 한번에 생성할 갯수")]
            [Range(1, 10)] public int InstanceCreationBlock = 1;

            [Space]
            [Tooltip("미사용객체와 사용객체의 격차가 벌어질 경우 릴리즈 트리거를 실행할 격차")]
            public int DeletionTriggerCount = 100;
            [Tooltip("트리거가 발동되고 릴리즈 될때까지 시간(초), 도중에 돌아오면 초기화")]
            public int DeletionWaitSeconds = 300;
        }
        
        [SerializeField] private List<ObjectSetting> m_ObjectSettings = new List<ObjectSetting>();

        public List<ObjectSetting> ObjectSettings => m_ObjectSettings;

        /// <inheritdoc cref="PrefabManager.GetRecycleObject{T}"/>
        public static T GetRecycleObject<T>() where T : RecycleableMonobehaviour
            => PrefabManager.GetRecycleObject<T>();
        /// <inheritdoc cref="PrefabManager.GetRecycleObject(int)"/>
        public static RecycleableMonobehaviour GetRecycleObject(int index)
            => PrefabManager.GetRecycleObject(index);
    }
}
