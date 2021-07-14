using Syadeu.Database;
using Syadeu.Database.CreatureData;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class CreatureSystem : PresentationSystemEntity<CreatureSystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        GameObjectProxySystem m_ProxySystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<GameObjectProxySystem>((other) => m_ProxySystem = other);

            return base.OnInitialize();
        }

        public void Spawn(Hash hash)
        {
            Creature entity = CreatureDataList.Instance.GetEntity(hash);
            var prefabInfo = PrefabList.Instance.ObjectSettings[entity.m_PrefabIdx];

            m_ProxySystem.CreateNewPrefab(entity.m_PrefabIdx, Vector3.zero, Quaternion.identity, Vector3.one, false,
                (dataObj, mono) =>
                {
                    for (int i = 0; i < entity.m_OnSpawn.m_Scripts.Count; i++)
                    {
                        entity.m_OnSpawn.m_Scripts[i].Invoke(ToArgument(mono.gameObject, dataObj, entity.m_OnSpawn.m_Scripts[i].m_Args));
                    }
                });
            
        }
        private List<object> ToArgument(GameObject gameObj, DataGameObject dataObj, IList<LuaArg> args)
        {
            List<object> temp = new List<object>();
            for (int i = 0; i < args.Count; i++)
            {
                if (TypeHelper.TypeOf<MonoBehaviour>.Type.IsAssignableFrom(args[i].Type))
                {
                    temp.Add(gameObj.GetComponent(args[i].Type));
                }
                else if (args[i].Type.Equals(TypeHelper.TypeOf<DataGameObject>.Type))
                {
                    temp.Add(dataObj);
                }
                else
                {
                    temp.Add(dataObj.GetComponent(args[i].Type));
                }
            }
            return temp;
        }
    }
}
