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
            public MonoBehaviour Prefab;
            [Tooltip("최대로 생성될 수 있는 숫자입니다. 값이 음수면 무한")]
            public int MaxInstanceCount = -1;
        }
        private struct RecycleObject
        {
            public int Index { get; }
            public MonoBehaviour Prefab { get; }
            public int MaxCount { get; }
            public List<MonoBehaviour> Instances { get; }

            public RecycleObject(int index, MonoBehaviour prefab, int maxCount)
            {
                Index = index;
                Prefab = prefab;
                MaxCount = maxCount;
                Instances = new List<MonoBehaviour>();
            }
        }

        public List<ObjectSetting> m_ObjectSettings = new List<ObjectSetting>();

        private Dictionary<int, RecycleObject> RecycleObjects { get; } = new Dictionary<int, RecycleObject>();

        /// <summary>
        /// 해당 타입과 일치하는 리사이클 인스턴스를 받아옵니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetRecycleObject<T>() where T : RecycleableMonobehaviour
        {
            for (int i = 0; i < Instance.m_ObjectSettings.Count; i++)
            {
                if (Instance.m_ObjectSettings[i].Prefab is T)
                {
                    return GetRecycleObject(i) as T;
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
        public static RecycleableMonobehaviour GetRecycleObject(int index)
        {
            RecycleObject obj = Instance.RecycleObjects[index];
            if (!(obj.Prefab is RecycleableMonobehaviour)) throw new InvalidOperationException("리사이클 오브젝트가 아님");

            for (int i = 0; i < obj.Instances.Count; i++)
            {
                if (obj.Instances[i] is RecycleableMonobehaviour temp &&
                    !temp.Activated)
                {
                    temp.Activated = true;
                    temp.OnInitialize();
                    
                    return temp;
                }
            }

            // 생성
            if ((obj.MaxCount < 0 ||
                obj.MaxCount > obj.Instances.Count) && 
                obj.Prefab is RecycleableMonobehaviour prefab)
            {
                RecycleableMonobehaviour recycleObj = Instantiate(prefab);
                recycleObj.IngameIndex = obj.Instances.Count;
                obj.Instances.Add(recycleObj);

                recycleObj.Activated = true;
                recycleObj.OnInitialize();

                return recycleObj;
            }

            "Return null because this item has reached maximum instance count lock".ToLog();
            return null;
        }

        public static MonoBehaviour CreateObject(int index, Transform parent = null)
        {
            RecycleObject obj = Instance.RecycleObjects[index];

            if (obj.MaxCount < 0 || obj.Instances.Count < obj.MaxCount)
            {
                MonoBehaviour temp = Instantiate(obj.Prefab, parent);
                obj.Instances.Add(temp);
                return temp;
            }

            return null;
        }
        public static T CreateObject<T>(int index, Transform parent = null) where T : MonoBehaviour
        {
            MonoBehaviour temp = CreateObject(index, parent);
            if (temp == null || !(temp is T output)) return null;

            return output;
        }

        private void OnEnable()
        {
            RecycleObjects.Clear();
            for (int i = 0; i < m_ObjectSettings.Count; i++)
            {
                RecycleObject obj = new RecycleObject(i, m_ObjectSettings[i].Prefab, m_ObjectSettings[i].MaxInstanceCount);
                RecycleObjects.Add(i, obj);
            }
        }
    }
}
