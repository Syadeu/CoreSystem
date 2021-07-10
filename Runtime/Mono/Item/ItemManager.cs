using Syadeu.Mono;
using Syadeu.Presentation;
using System;
using UnityEngine;

namespace Syadeu.Database
{
    [StaticManagerIntializeOnLoad]
    public sealed class ItemManager : StaticDataManager<ItemManager>
    {
        public override void OnInitialize()
        {
            ConsoleWindow.CreateCommand((cmd) =>
            {
                int rnd = UnityEngine.Random.Range(0, ItemDataList.Instance.m_Items.Count);
                Item item = ItemDataList.Instance.m_Items[rnd];

                Transform cam = Camera.main.transform;
                Ray ray = new Ray(cam.position, cam.forward);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    SpawnItem(item.m_Hash, hit.point, Quaternion.identity);
                }
            }, "item", "spawn", "random");
        }

        public void SpawnItem(Hash hash, Vector3 pos, Quaternion rot)
        {
            Item item = ItemDataList.Instance.GetItem(hash);
            ItemInstance itemIns = item.CreateInstance();

            //Type componentType = PresentationSystem<GameObjectProxySystem>
            //    .GetSystem()
            //    .GetGenericType<ItemInstance[]>();

            //PrefabManager.GetRecycleObjectAsync(item.m_PrefabIdx, (other) =>
            //{
            //    other.transform.position = pos;
            //    other.transform.rotation = rot;

            //    MonoBehaviour<ItemInstance[]> dataComponent = other.GetComponent<MonoBehaviour<ItemInstance[]>>();
            //    if (dataComponent == null)
            //    {
            //        dataComponent = other.gameObject.AddComponent(componentType) as MonoBehaviour<ItemInstance[]>;
            //    }
            //    dataComponent.m_Value = new ItemInstance[1] { itemIns };

            //    itemIns.m_ProxyObject = other.gameObject;
            //});

            PresentationSystem<GameObjectProxySystem>.System.RequestPrefab(item.m_PrefabIdx, pos, rot, (data)=>
            {
                DataTransform tr = data.GetTransform();
                tr.position = new ThreadSafe.Vector3(123, 123, 123);

                $"out {tr.position}".ToLog();
            });
        }
    }
}
