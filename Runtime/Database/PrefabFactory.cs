using Syadeu.Mono;
using System;
using UnityEngine;

namespace Syadeu.Database
{
    public struct PrefabFactory
    {
        public static readonly Vector3 INIT_POSITION = new Vector3(-9999, -9999, -9999);

        public static PrefabReference<GameObject> MakePrefab(string name, params Type[] components)
        {
            CoreSystem.Logger.ThreadBlock(nameof(MakePrefab), Internal.ThreadInfo.Unity);

            GameObject obj = new GameObject(name, components);
            obj.SetActive(false);
            obj.hideFlags = HideFlags.HideInHierarchy;
            obj.transform.position = INIT_POSITION;

            PrefabList.Instance.ObjectSettings.Add(new PrefabList.ObjectSetting(name, null, false)
            {
                m_IsRuntimeObject = true,
                m_Prefab = obj
            });
            return new PrefabReference<GameObject>(PrefabList.Instance.ObjectSettings.Count - 1);
        }
    }
}
