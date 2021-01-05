using Syadeu.Mono;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syadeu.Mono
{
    public class PrefabManager : StaticManager<PrefabManager>
    {
        #region Initialize

        internal class RecycleObject
        {
            public int Index { get; }
            public RecycleableMonobehaviour Prefab { get; }
            public int MaxCount { get; }
            public int InstanceCreationBlock { get; }
            public List<RecycleableMonobehaviour> Instances { get; }

            public RecycleObject(int i, PrefabList.ObjectSetting setting)
            {
                Index = i;
                Prefab = setting.Prefab;
                MaxCount = setting.MaxInstanceCount;
                InstanceCreationBlock = setting.InstanceCreationBlock;

                Instances = new List<RecycleableMonobehaviour>();
            }
        }
        public override string DisplayName => "Prefab Manager";
        public override bool DontDestroy => false;
        public override bool HideInHierarchy => false;
        internal Dictionary<int, RecycleObject> RecycleObjects { get; } = new Dictionary<int, RecycleObject>();
        public override void OnInitialize()
        {
            for (int i = 0; i < PrefabList.Instance.m_ObjectSettings.Count; i++)
            {
                RecycleObject obj = new RecycleObject(i, PrefabList.Instance.m_ObjectSettings[i]);
                RecycleObjects.Add(i, obj);
            }
        }
        public override void OnStart()
        {
            StartUnityUpdate(Updater());
        }
        private IEnumerator Updater()
        {
            while (Initialized)
            {
                foreach (var recycle in RecycleObjects.Values)
                {
                    for (int i = 0; i < recycle.Instances.Count; i++)
                    {
                        if (!recycle.Instances[i].Activated) continue;
                        if (SyadeuSettings.Instance.m_PMErrorAutoFix && recycle.Instances[i].Transfrom == null)
                        {
                            recycle.Instances.RemoveAt(i);
                            i--;
                            continue;
                        }

                        if (recycle.Instances[i].OnActivated != null &&
                            !recycle.Instances[i].OnActivated.Invoke())
                        {
                            recycle.Instances[i].Terminate();
                        }
                        if (i != 0 && i % 1000 == 0) yield return null;
                    }
                }

                yield return null;
            }

            //Debug.Log("exit");
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
                    if (temp.Transfrom == null)
                    {
                        obj.Instances.RemoveAt(i);
                        i--;
                        continue;
                    }

                    temp.Activated = true;
                    temp.OnInitialize();

                    return temp;
                }
            }

            // ����
            if (obj.MaxCount < 0 ||
                obj.MaxCount > obj.Instances.Count)
            {
                RecycleableMonobehaviour recycleObj = null;
                if (!IsMainthread())
                {
                    CoreSystem.AddForegroundJob(() =>
                    {
                        recycleObj = Instance.InternalInstantiate(obj);
                    }).Await();
                }
                else recycleObj = Instance.InternalInstantiate(obj);
                return recycleObj;
            }

            //"Return null because this item has reached maximum instance count lock".ToLog();
            return null;
        }
        private RecycleableMonobehaviour InternalInstantiate(RecycleObject obj)
        {
            for (int i = 0; i < obj.InstanceCreationBlock; i++)
            {
                RecycleableMonobehaviour recycleObj = Instantiate(obj.Prefab, transform);
                recycleObj.OnCreated();

                recycleObj.IngameIndex = obj.Instances.Count;
                obj.Instances.Add(recycleObj);
            }
            return GetRecycleObject(obj.Index);
        }

        public int GetInstanceCount(int index) => RecycleObjects[index].Instances.Count;
        /// <summary>
        /// �ش� �ε����� ��� �ν��Ͻ����� ����Ʈ�� ��� ��ȯ�մϴ�.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IReadOnlyList<RecycleableMonobehaviour> GetInstances(int index)
            => RecycleObjects[index].Instances;
        /// <summary>
        /// ��ϵ� ��� ���� ��� �������� �޾ƿɴϴ�.
        /// �ε��� ��ȣ�� �������� ��Ƽ� ����Ʈ�� ��ȯ�մϴ�.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<KeyValuePair<int, RecycleableMonobehaviour>> GetRecycleObjectList()
        {
            KeyValuePair<int, RecycleableMonobehaviour>[] list = new KeyValuePair<int, RecycleableMonobehaviour>[RecycleObjects.Count];

            for (int i = 0; i < list.Length; i++)
            {
                list[i] = new KeyValuePair<int, RecycleableMonobehaviour>(i, RecycleObjects[i].Prefab);
            }

            return list;
        }
    }
}
