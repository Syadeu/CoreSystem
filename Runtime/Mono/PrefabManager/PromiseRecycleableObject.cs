using System;
using UnityEngine;

#if UNITY_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
#endif

namespace Syadeu.Mono
{
#if UNITY_ADDRESSABLES
    public sealed class PromiseRecycleableObject
    {
        private RecycleableMonobehaviour m_Output = null;

        private Scene m_CalledScene;

        private PrefabManager.RecycleObject RecycleObjectSet;
        private AsyncOperationHandle<GameObject> m_Operation;
        private bool m_ManualInit = false;
        private Action<RecycleableMonobehaviour> m_OnCompleted = null;

        public bool IsDone => m_Output != null;
        public RecycleableMonobehaviour Target => m_Output;

        internal PromiseRecycleableObject(RecycleableMonobehaviour recycleableMonobehaviour)
        {
            m_Output = recycleableMonobehaviour;
        }
        internal PromiseRecycleableObject(PrefabManager.RecycleObject obj)
        {
            RecycleObjectSet = obj;

            m_CalledScene = SceneManager.GetActiveScene();
            m_Operation = obj.RefPrefab.InstantiateAsync(PrefabManager.INIT_POSITION, Quaternion.identity, PrefabManager.Instance.transform);
            m_Operation.Completed += M_Operation_Completed;
        }
        internal PromiseRecycleableObject(PrefabManager.RecycleObject obj, Action<RecycleableMonobehaviour> onCompleted, bool manualInit)
        {
            RecycleObjectSet = obj;

            m_CalledScene = SceneManager.GetActiveScene();
            m_Operation = obj.RefPrefab.InstantiateAsync(PrefabManager.INIT_POSITION, Quaternion.identity, PrefabManager.Instance.transform);
            m_ManualInit = manualInit;
            m_OnCompleted = onCompleted;
            m_Operation.Completed += M_Operation_Completed;
        }

        private void M_Operation_Completed(AsyncOperationHandle<GameObject> obj)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (!currentScene.Equals(m_CalledScene))
            {
                $"{obj.Result.name} is return because Scene has been changed".ToLog();
                RecycleObjectSet.RefPrefab.ReleaseInstance(obj.Result);
                return;
            }

            RecycleableMonobehaviour recycleable = obj.Result.GetComponent<RecycleableMonobehaviour>();
            if (recycleable == null)
            {
                recycleable = obj.Result.AddComponent<ManagedRecycleObject>();
            }

            recycleable.CreatedWithAddressable = true;
            recycleable.InternalOnCreated();
            obj.Result.SetActive(false);
            RecycleObjectSet.Promises.Remove(this);
            RecycleObjectSet.AddNewInstance(recycleable);

            m_Output = recycleable;

            if (m_OnCompleted != null)
            {
                if (!m_ManualInit)
                {
                    recycleable.Initialize();
                }
                m_OnCompleted.Invoke(recycleable);
            }
        }
    }
#endif
}
