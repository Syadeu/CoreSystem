using Syadeu.Mono;
using Syadeu.Presentation;
using System;
using Unity.Mathematics;
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
                Item item;
                if (!string.IsNullOrEmpty(cmd))
                {
                    if (ulong.TryParse(cmd, out ulong hash))
                    {
                        item = ItemDataList.Instance.GetItem(hash);
                    }
                    else item = ItemDataList.Instance.GetItemByName(cmd);
                }
                else
                {
                    int rnd = UnityEngine.Random.Range(0, ItemDataList.Instance.m_Items.Count);
                    item = ItemDataList.Instance.m_Items[rnd];
                }
                if (item == null)
                {
                    ConsoleWindow.Log("Item Not found");
                    return;
                }

                Transform cam = Camera.main.transform;
                Ray ray = new Ray(cam.position, cam.forward);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    SpawnItem(item.m_Hash, hit.point, Quaternion.identity);
                }
            }, "item", "spawn");
            ConsoleWindow.CreateCommand((cmd) =>
            {
                for (int i = 0; i < 100; i++)
                {
                    int rnd = UnityEngine.Random.Range(0, ItemDataList.Instance.m_Items.Count);
                    Item item = ItemDataList.Instance.m_Items[rnd];
                    Vector3 pos = new Vector3(UnityEngine.Random.Range(-100, 100), 0, UnityEngine.Random.Range(-100, 100));

                    SpawnItem(item.m_Hash, pos, Quaternion.identity);
                }
            }, "item", "spawn", "random");
        }

        public void SpawnItem(Hash hash, Vector3 pos, Quaternion rot)
        {
            if (!GridManager.HasGrid(pos))
            {
                CoreSystem.Logger.LogError(Channel.Data, $"Can\'t spawn item {hash} at {pos}, There\'s no grid");
                return;
            }
            ref GridManager.Grid grid = ref GridManager.GetGrid(pos);
            if (!grid.HasCell(pos))
            {
                CoreSystem.Logger.LogError(Channel.Data, $"Can\'t spawn item {hash} at {pos}, There\'s no grid cell");
                return;
            }
            ref GridManager.GridCell cell = ref grid.GetCell(pos);
            if (cell.GetCustomData() != null)
            {
                CoreSystem.Logger.LogError(Channel.Data, $"Can\'t spawn item {hash} at {pos}, target grid cell has object");
                return;
            }

            Item item = ItemDataList.Instance.GetItem(hash);
            ItemInstance itemIns = item.CreateInstance();

            GameObjectProxySystem proxySystem = PresentationSystem<GameObjectProxySystem>.System;
            DataGameObject gameObject = proxySystem.CreateNewPrefab(item.m_PrefabIdx, pos, rot, Vector3.one);

            gameObject.UserTag = UserTag.GetUserTag("Object");
            gameObject.CustomTag = UserTag.GetCustomTag("Item");

            ItemDataComponent component = gameObject.AddComponent<ItemDataComponent>();
            component.GridIdxes = cell.Idxes;
            component.Item = itemIns;
        }

        
    }
    [Serializable]
    public sealed class ItemDataComponent : DataComponentEntity
    {
        public int2 GridIdxes;
        public ItemInstance Item;
    }
}
