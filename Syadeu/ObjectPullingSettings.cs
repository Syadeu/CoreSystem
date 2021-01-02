using Syadeu.Extentions;
using Syadeu.Extentions.EditorUtils;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu
{
    public sealed class ObjectPullingSettings : StaticSettingEntity<ObjectPullingSettings>
    {
        [Serializable]
        public sealed class ObjectSetting
        {
            public string m_Name;

            [Space]
            [Tooltip("풀링할 오브젝트의 프리팹입니다")]
            public RecycleableMonobehaviour Prefab;
            [Tooltip("최대로 생성될 수 있는 숫자입니다. 값이 음수면 무한")]
            public int MaxInstanceCount;
        }
        private struct IngameObject
        {
            public int Index { get; }
            public RecycleableMonobehaviour Prefab { get; }
            public int MaxCount { get; }
            public List<RecycleableMonobehaviour> Instances { get; }

            public IngameObject(int index, RecycleableMonobehaviour prefab, int maxCount)
            {
                Index = index;
                Prefab = prefab;
                MaxCount = maxCount;
                Instances = new List<RecycleableMonobehaviour>();
            }
        }

        public List<ObjectSetting> m_ObjectSettings = new List<ObjectSetting>();

        private Dictionary<int, IngameObject> IngameObjects { get; } = new Dictionary<int, IngameObject>();

        /// <summary>
        /// 해당 타입과 일치하는 리사이클 인스턴스를 받아옵니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetObject<T>() where T : RecycleableMonobehaviour
        {
            for (int i = 0; i < Instance.m_ObjectSettings.Count; i++)
            {
                if (Instance.m_ObjectSettings[i].Prefab is T)
                {
                    return GetObject(i) as T;
                }
            }

            "FATAL ERROR :: INSTANCE LOAD FAILED".ToLogError();
            return null;
        }
        /// <summary>
        /// ObjectSettings 의 리스트 인덱스 번호로 인스턴스를 받아옵니다
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static RecycleableMonobehaviour GetObject(int index)
        {
            if (!Instance.IngameObjects.TryGetValue(index, out var obj))
            {
                "FATAL ERROR :: INSTANCE LOAD FAILED".ToLogError();
                return null;
            }

            RecycleableMonobehaviour temp;
            for (int i = 0; i < obj.Instances.Count; i++)
            {
                if (!obj.Instances[i].Activated)
                {
                    temp = obj.Instances[i];

                    temp.Activated = true;
                    temp.OnInitialize();
                    
                    return temp;
                }
            }

            // 무한 생성
            if (obj.MaxCount < 0 ||
                obj.MaxCount > obj.Instances.Count)
            {
                temp = Instantiate(obj.Prefab);
                temp.IngameIndex = obj.Instances.Count;
                obj.Instances.Add(temp);

                temp.Activated = true;
                temp.OnInitialize();

                return temp;
            }

            "Return null because this item has reached maximum instance count lock".ToLog();
            return null;
        }

        private void OnEnable()
        {
            IngameObjects.Clear();
            for (int i = 0; i < m_ObjectSettings.Count; i++)
            {
                IngameObject obj = new IngameObject(i, m_ObjectSettings[i].Prefab, m_ObjectSettings[i].MaxInstanceCount);
                IngameObjects.Add(i, obj);
            }
        }

#if UNITY_EDITOR
        [MenuItem("Syadeu/General/Edit Object Pulling Settings", priority = 0)]
        public static void MenuItem()
        {
            Selection.activeObject = Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#endif
    }
}
