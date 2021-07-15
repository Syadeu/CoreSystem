using MoonSharp.Interpreter;
using Syadeu.Database;
using Syadeu.Database.CreatureData;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class CreatureSystem : PresentationSystemEntity<CreatureSystem>
    {
        private const string c_AttributeWarning = "Attribute({0}) on entity({1}) has invaild value. {2}. Request Ignored.";

        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        GameObjectProxySystem m_ProxySystem;

        private readonly List<DataGameObject> m_Creatures = new List<DataGameObject>();
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

        public DataGameObject Spawn(Hash hash)
        {
            Creature entity = CreatureDataList.Instance.GetEntity(hash);
            var prefabInfo = PrefabList.Instance.ObjectSettings[entity.m_PrefabIdx];

            DataGameObject dataObj = m_ProxySystem.CreateNewPrefab(entity.m_PrefabIdx, Vector3.zero, Quaternion.identity, Vector3.one, false,
                (dataObj, mono) =>
                {
                    CreatureBrain brain = (CreatureBrain)mono;
                    //brain.m_SpawnPointIdx = spawnPointIdx;
                    brain.m_DataHash = hash;
                    brain.m_IsSpawnedFromManager = true;
                    //brain.transform.position = pos;
                    //brain.transform.SetParent(Instance.transform);

                    brain.Initialize();

                    //$"{m_DataIdx}: spawnpoint {spawnPointIdx}".ToLog();
                    //GetCreatureSet(m_DataIdx).m_SpawnRanges[spawnPointIdx].m_InstanceCount++;
                    brain.m_UniqueIdx = m_Creatures.Count;
                    m_Creatures.Add(dataObj);

                    ProcessEntityOnCreated(this, entity, dataObj, mono);
                });

            
            return dataObj;
        }

        public static void InvokeLua(LuaScript scr, Creature entity, DataGameObject dataObj, RecycleableMonobehaviour mono,
            in string calledAttName, in string calledScriptName)
        {
            if (scr == null || !scr.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Creature,
                    string.Format(c_AttributeWarning, calledAttName, entity.m_Name, $"function({calledScriptName}) is null"));
                return;
            }

            List<object> args = ToArgument(mono.gameObject, dataObj, scr.m_Args);
            try
            {
                scr.Invoke(args);
            }
            catch (ScriptRuntimeException runtimeEx)
            {
                CoreSystem.Logger.LogWarning(Channel.Creature,
                    string.Format(c_AttributeWarning, calledAttName, entity.m_Name, $"An Error Raised while invoke function({calledScriptName})") +
                    "\n" + runtimeEx.DecoratedMessage);
                return;
            }
            catch (Exception ex)
            {
                CoreSystem.Logger.LogWarning(Channel.Creature,
                    string.Format(c_AttributeWarning, calledAttName, entity.m_Name, $"An Error Raised while invoke function({calledScriptName})") +
                    "\n" + ex.Message);
                return;
            }

            static List<object> ToArgument(GameObject gameObj, DataGameObject dataObj, IList<LuaArg> args)
            {
                if (args == null || args.Count == 0) return null;
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
                    else if (TypeHelper.TypeOf<DataComponentEntity>.Type.IsAssignableFrom(args[i].Type))
                    {
                        temp.Add(dataObj.GetComponent(args[i].Type));
                    }
                    else throw new NotImplementedException($"{args[i].Type.Name}");
                }
                return temp;
            }
        }
        private static void ProcessEntityOnCreated(CreatureSystem system, Creature entity, DataGameObject dataObj, RecycleableMonobehaviour mono)
        {
            CreatureAttribute[] attributes = entity.m_Attributes.Select((other) => CreatureDataList.Instance.GetAttribute(other)).ToArray();
            for (int i = 0; i < attributes.Length; i++)
            {
                CreatureAttribute att = attributes[i];
                dataObj.AddComponent(att.GetType());

                if (system.m_Processors.TryGetValue(att.GetType(), out List<ICreatureAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        processors[i].OnCreated(att, entity, dataObj, (CreatureBrain)mono);
                    }
                }
            }

            //for (int i = 0; i < attributes.Length; i++)
            //{
            //    if (attributes[i].OnEntityStart == null || !attributes[i].OnEntityStart.IsValid()) continue;

            //    InvokeLua(attributes[i].OnEntityStart, entity, dataObj, mono,
            //        calledAttName: attributes[i].GetType().Name,
            //        calledScriptName: "OnEntityStart");
            //}
        }
    }
}
