using Syadeu.Database;
using Syadeu.Database.CreatureData;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class CreatureSystem : PresentationSystemEntity<CreatureSystem>
    {
        private const string c_AttributeWarning = "Attribute({0}) on entity({1}) has invaild value. At {2}";

        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        GameObjectProxySystem m_ProxySystem;

        private readonly Dictionary<Type, List<ICreatureAttributeProcessor>> m_Processors = new Dictionary<Type, List<ICreatureAttributeProcessor>>();

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<GameObjectProxySystem>((other) => m_ProxySystem = other);

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            Type[] processors = TypeHelper.GetTypes((other) =>
            {
                return !other.IsAbstract && !other.IsInterface && TypeHelper.TypeOf<ICreatureAttributeProcessor>.Type.IsAssignableFrom(other);
            });
            for (int i = 0; i < processors.Length; i++)
            {
                ICreatureAttributeProcessor processor = (ICreatureAttributeProcessor)Activator.CreateInstance(processors[i]);
                if (!m_Processors.TryGetValue(processor.TargetAttribute, out var values))
                {
                    values = new List<ICreatureAttributeProcessor>();
                    m_Processors.Add(processor.TargetAttribute, values);
                }
                values.Add(processor);
            }

            return base.OnInitializeAsync();
        }

        public void Spawn(Hash hash)
        {
            Creature entity = CreatureDataList.Instance.GetEntity(hash);
            var prefabInfo = PrefabList.Instance.ObjectSettings[entity.m_PrefabIdx];

            DataGameObject dataObj = m_ProxySystem.CreateNewPrefab(entity.m_PrefabIdx, Vector3.zero, Quaternion.identity, Vector3.one, false,
                (dataObj, mono) =>
                {
                    for (int i = 0; i < entity.m_Attributes.Count; i++)
                    {
                        CreatureAttribute att = CreatureDataList.Instance.GetAttribute(entity.m_Attributes[i]);
                        dataObj.AddComponent(att.GetType());

                        if (TryInvokeLua(att.OnEntityStart, entity, att))
                        {
                            att.OnEntityStart.Invoke(ToArgument(mono.gameObject, dataObj, att.OnEntityStart.m_Args));
                        }
                    }
                });

            ProcessEntityOnCreated(this, entity, dataObj);
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

        private static bool TryInvokeLua(LuaScript scr, Creature entity, ICreatureAttribute att)
        {
            if (scr == null) return false;
            if (!scr.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Creature,
                    string.Format(c_AttributeWarning, att.GetType().Name, entity.m_Name, "OnEntityStart"));
                return false;
            }
            return true;
        }
        private static void ProcessEntityOnCreated(CreatureSystem system, Creature entity, DataGameObject dataObj)
        {
            for (int i = 0; i < entity.m_Attributes.Count; i++)
            {
                CreatureAttribute att = CreatureDataList.Instance.GetAttribute(entity.m_Attributes[i]);
                if (system.m_Processors.TryGetValue(att.GetType(), out var processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        processors[i].OnCreated(att, entity, dataObj);
                    }
                }
            }
        }
    }
}
