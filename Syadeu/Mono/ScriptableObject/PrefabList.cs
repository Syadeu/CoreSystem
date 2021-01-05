using System;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Mono
{
    public sealed class PrefabList : StaticSettingEntity<PrefabList>
    {
#if UNITY_EDITOR
        [MenuItem("Syadeu/Edit Prefab List", priority = 100)]
        public static void MenuItem()
        {
            Selection.activeObject = Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#endif

        [Serializable]
        public sealed class ObjectSetting
        {
            public string m_Name;

            [Space]
            [Tooltip("오브젝트의 프리팹입니다")]
            public RecycleableMonobehaviour Prefab;
            [Tooltip("최대로 생성될 수 있는 숫자입니다. 값이 음수면 무한")]
            public int MaxInstanceCount = -1;
            [Tooltip("사용가능한 객체가 없어서 생성할때 한번에 생성할 갯수")]
            [Range(1, 10)] public int InstanceCreationBlock = 1;
        }
        
        public List<ObjectSetting> m_ObjectSettings = new List<ObjectSetting>();

        /// <inheritdoc cref="PrefabManager.GetRecycleObject{T}"/>
        public static T GetRecycleObject<T>() where T : RecycleableMonobehaviour
            => PrefabManager.GetRecycleObject<T>();
        /// <inheritdoc cref="PrefabManager.GetRecycleObject(int)"/>
        public static RecycleableMonobehaviour GetRecycleObject(int index)
            => PrefabManager.GetRecycleObject(index);
    }
}
