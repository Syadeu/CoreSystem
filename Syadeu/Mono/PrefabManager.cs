using Syadeu.Mono;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono
{
    public class PrefabManager : StaticManager<PrefabManager>
    {
        #region Initialize

        internal struct RecycleObject
        {
            public int Index { get; }
            public RecycleableMonobehaviour Prefab { get; }
            public int MaxCount { get; }
            public List<RecycleableMonobehaviour> Instances { get; }

            public RecycleObject(int index, RecycleableMonobehaviour prefab, int maxCount)
            {
                Index = index;
                Prefab = prefab;
                MaxCount = maxCount;
                Instances = new List<RecycleableMonobehaviour>();
            }
        }
        public override bool DontDestroy => false;
        internal Dictionary<int, RecycleObject> RecycleObjects { get; } = new Dictionary<int, RecycleObject>();

        public override void OnInitialize()
        {
            for (int i = 0; i < PrefabList.Instance.m_ObjectSettings.Count; i++)
            {
                RecycleObject obj = new RecycleObject(
                    i, 
                    PrefabList.Instance.m_ObjectSettings[i].Prefab,
                    PrefabList.Instance.m_ObjectSettings[i].MaxInstanceCount);
                RecycleObjects.Add(i, obj);
            }
        }

        #endregion

        /// <summary>
        /// �ش� Ÿ��(<typeparamref name="T"/>)�� ��ġ�ϴ� ������Ŭ �ν��Ͻ��� �޾ƿɴϴ�.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetRecycleObject<T>() where T : RecycleableMonobehaviour
        {
            for (int i = 0; i < PrefabList.Instance.m_ObjectSettings.Count; i++)
            {
                if (PrefabList.Instance.m_ObjectSettings[i].Prefab is T)
                {
                    return (T)GetRecycleObject(i);
                }
            }

            throw new InvalidCastException($"CoreSystem.Prefab :: {typeof(T).Name}�� ��ġ�ϴ� Ÿ���� ������ ����Ʈ�� ��ϵ����ʾ� ã�� �� ����");
        }
        /// <summary>
        /// <see cref="PrefabList"/> �� ����Ʈ(<see cref="PrefabList.m_ObjectSettings"/>) �ε��� ��ȣ�� 
        /// �ν��Ͻ��� �޾ƿɴϴ�
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static RecycleableMonobehaviour GetRecycleObject(int index)
        {
            RecycleObject obj = Instance.RecycleObjects[index];

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

            // ����
            if (obj.MaxCount < 0 ||
                obj.MaxCount > obj.Instances.Count)
            {
                RecycleableMonobehaviour recycleObj = Instantiate(obj.Prefab);
                recycleObj.IngameIndex = obj.Instances.Count;
                recycleObj.IsHandledByManager = true;
                obj.Instances.Add(recycleObj);

                recycleObj.Activated = true;
                recycleObj.OnInitialize();

                return recycleObj;
            }

            //"Return null because this item has reached maximum instance count lock".ToLog();
            return null;
        }

        //public static MonoBehaviour CreateObject(int index, Transform parent = null)
        //{
        //    RecycleObject obj = Instance.RecycleObjects[index];

        //    if (obj.MaxCount < 0 || obj.Instances.Count < obj.MaxCount)
        //    {
        //        MonoBehaviour temp = Instantiate(obj.Prefab, parent);
        //        obj.Instances.Add(temp);
        //        return temp;
        //    }

        //    return null;
        //}
        //public static T CreateObject<T>(int index, Transform parent = null) where T : MonoBehaviour
        //{
        //    MonoBehaviour temp = CreateObject(index, parent);
        //    if (temp == null || !(temp is T output)) return null;

        //    return output;
        //}
    }
}
