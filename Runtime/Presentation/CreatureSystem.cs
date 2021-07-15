using MoonSharp.Interpreter;
using Syadeu.Database;
using Syadeu.Database.CreatureData;
using Syadeu.Database.CreatureData.Attributes;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class CreatureSystem : PresentationSystemEntity<CreatureSystem>
    {
        private const string c_AttributeWarning = "Attribute({0}) on entity({1}) has invaild value. {2}. Request Ignored.";

        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => true;

        GameObjectProxySystem m_ProxySystem;

        private NativeList<Hash> m_CreatureIdxes;
        private NativeHashSet<Hash> m_CreatureIdxSet;
        private NativeHashSet<Hash> m_DeadCreatureIdxSet;
        private readonly Dictionary<Type, List<ICreatureAttributeProcessor>> m_Processors = new Dictionary<Type, List<ICreatureAttributeProcessor>>();

        #region Presentation Methods
        protected override PresentationResult OnInitializeAsync()
        {
            m_CreatureIdxes = new NativeList<Hash>(1000, Allocator.Persistent);
            m_CreatureIdxSet = new NativeHashSet<Hash>(1000, Allocator.Persistent);
            m_DeadCreatureIdxSet = new NativeHashSet<Hash>(1000, Allocator.Persistent);

            Type[] processors = TypeHelper.GetTypes((other) =>
            {
                return !other.IsAbstract && !other.IsInterface && TypeHelper.TypeOf<ICreatureAttributeProcessor>.Type.IsAssignableFrom(other);
            });
            for (int i = 0; i < processors.Length; i++)
            {
                ICreatureAttributeProcessor processor = (ICreatureAttributeProcessor)Activator.CreateInstance(processors[i]);
                processor.CreatureSystem = this;
                if (!m_Processors.TryGetValue(processor.TargetAttribute, out var values))
                {
                    values = new List<ICreatureAttributeProcessor>();
                    m_Processors.Add(processor.TargetAttribute, values);
                }
                values.Add(processor);
            }
            RequestSystem<GameObjectProxySystem>((other) =>
            {
                m_ProxySystem = other;

                foreach (var item in m_Processors.Values)
                {
                    for (int i = 0; i < item.Count; i++)
                    {
                        item[i].ProxySystem = m_ProxySystem;
                    }
                }
            });

            return base.OnInitializeAsync();
        }
        protected override PresentationResult OnStartPresentation()
        {
            m_ProxySystem.OnDataObjectDestoryAsync += M_ProxySystem_OnDataObjectDestoryAsync;

            return base.OnStartPresentation();
        }
        private void M_ProxySystem_OnDataObjectDestoryAsync(DataGameObject obj)
        {
            if (!m_CreatureIdxSet.Contains(obj.m_Idx)) return;

            CreatureInfoDataComponent info = obj.GetComponent<CreatureInfoDataComponent>();
            ProcessEntityOnDestory(this, obj);

            m_CreatureIdxes.RemoveFor(obj.m_Idx);
            m_CreatureIdxSet.Remove(obj.m_Idx);
            m_DeadCreatureIdxSet.Remove(obj.m_Idx);
        }
        protected override PresentationResult OnPresentationAsync()
        {
            for (int i = 0; i < m_CreatureIdxes.Length; i++)
            {
                DataGameObject obj = m_ProxySystem.GetDataGameObject(m_CreatureIdxes[i]);
                CreatureInfoDataComponent info = obj.GetComponent<CreatureInfoDataComponent>();

                if (m_DeadCreatureIdxSet.Contains(obj.m_Idx)) continue;
                ProcessEntityOnPresentation(this, obj);
            }

            return base.OnPresentation();
        }
        protected override PresentationResult AfterPresentationAsync()
        {
            for (int i = 0; i < m_CreatureIdxes.Length; i++)
            {
                DataGameObject obj = m_ProxySystem.GetDataGameObject(m_CreatureIdxes[i]);
                CreatureInfoDataComponent info = obj.GetComponent<CreatureInfoDataComponent>();

                if (!m_DeadCreatureIdxSet.Contains(obj.m_Idx) && !info.IsAlive)
                {
                    ProcessEntityOnDead(this, obj);
                    m_DeadCreatureIdxSet.Add(obj.m_Idx);
                }
            }
            return base.AfterPresentation();
        }

        public override void Dispose()
        {
            m_CreatureIdxes.Dispose();
            m_CreatureIdxSet.Dispose();
            m_DeadCreatureIdxSet.Dispose();
        }
        #endregion

        public DataGameObject Spawn(Hash hash, Vector3 position, Quaternion rotation, Vector3 localSize)
        {
            CoreSystem.Logger.NotNull(m_ProxySystem, "ProxySystem is not initialized");
            Creature entity = CreatureDataList.Instance.GetEntity(hash);

            DataGameObject dataObj = m_ProxySystem.CreateNewPrefab(entity.m_PrefabIdx, position, rotation, localSize, false,
                (dataObj, mono) =>
                {
                    CreatureBrain brain = (CreatureBrain)mono;
                    CreatureInfoDataComponent info = dataObj.AddComponent<CreatureInfoDataComponent>();
                    info.m_CreatureInfo = (Creature)entity.Clone();
                    info.m_Brain = brain;

                    //brain.m_SpawnPointIdx = spawnPointIdx;
                    brain.m_DataHash = hash;
                    brain.m_DataObject = dataObj;
                    //brain.m_IsSpawnedFromManager = true;
                    //brain.transform.position = pos;
                    //brain.transform.SetParent(Instance.transform);

                    ProcessEntityOnCreated(this, dataObj);

                    brain.Initialize();

                    //$"{m_DataIdx}: spawnpoint {spawnPointIdx}".ToLog();
                    //GetCreatureSet(m_DataIdx).m_SpawnRanges[spawnPointIdx].m_InstanceCount++;
                    //brain.m_UniqueIdx = m_Creatures.Length;

                    m_CreatureIdxes.Add(dataObj.m_Idx);
                    m_CreatureIdxSet.Add(dataObj.m_Idx);

                    
                    
                });
            $"spawn {hash}".ToLog();
            return dataObj;
        }

        public static void InvokeLua(LuaScript scr, DataGameObject dataObj,
            in string calledAttName, in string calledScriptName)
        {
            Creature entity = dataObj.GetComponent<CreatureInfoDataComponent>().m_CreatureInfo;
            if (scr == null || !scr.IsValid())
            {
                CoreSystem.Logger.LogWarning(Channel.Creature,
                    string.Format(c_AttributeWarning, calledAttName, entity.m_Name, $"function({calledScriptName}) is null"));
                return;
            }

            RecycleableMonobehaviour mono = dataObj.transform.ProxyObject;
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
        private static void ProcessEntityOnCreated(CreatureSystem system, DataGameObject dataObj)
        {
            Creature entity = dataObj.GetComponent<CreatureInfoDataComponent>().m_CreatureInfo;

            CreatureAttribute[] attributes = entity.m_Attributes.Select((other) => CreatureDataList.Instance.GetAttribute(other)).ToArray();
            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i] == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Creature,
                        $"Entity({entity.m_Name}) has empty attribute. This is not allowed. Request Ignored.");
                    continue;
                }

                CreatureAttribute att = attributes[i];
                dataObj.AddComponent(att.GetType());

                if (system.m_Processors.TryGetValue(att.GetType(), out List<ICreatureAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        processors[j].OnCreated(att, dataObj);
                    }

                    CoreSystem.Logger.Log(Channel.Creature, $"Processed OnCreated at entity({entity.m_Name}), count {processors.Count}");
                }
            }
        }
        private static void ProcessEntityOnPresentation(CreatureSystem system, DataGameObject dataObj)
        {
            Creature entity = dataObj.GetComponent<CreatureInfoDataComponent>().m_CreatureInfo;

            CreatureAttribute[] attributes = entity.m_Attributes.Select((other) => CreatureDataList.Instance.GetAttribute(other)).ToArray();
            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i] == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Creature,
                        $"Entity({entity.m_Name}) has empty attribute. This is not allowed. Request Ignored.");
                    continue;
                }

                CreatureAttribute att = attributes[i];
                if (system.m_Processors.TryGetValue(att.GetType(), out List<ICreatureAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        processors[j].OnPresentation(att, dataObj);
                    }
                }
            }
        }
        private static void ProcessEntityOnDead(CreatureSystem system, DataGameObject dataObj)
        {
            Creature entity = dataObj.GetComponent<CreatureInfoDataComponent>().m_CreatureInfo;
            CoreSystem.Logger.Log(Channel.Creature, $"Processing On Dead {entity.m_Name}");

            CreatureAttribute[] attributes = entity.m_Attributes.Select((other) => CreatureDataList.Instance.GetAttribute(other)).ToArray();
            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i] == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Creature,
                        $"Entity({entity.m_Name}) has empty attribute. This is not allowed. Request Ignored.");
                    continue;
                }

                CreatureAttribute att = attributes[i];
                if (system.m_Processors.TryGetValue(att.GetType(), out List<ICreatureAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        processors[j].OnDead(att, dataObj);
                    }
                }
            }
        }
        private static void ProcessEntityOnDestory(CreatureSystem system, DataGameObject dataObj)
        {
            Creature entity = dataObj.GetComponent<CreatureInfoDataComponent>().m_CreatureInfo;
            CoreSystem.Logger.Log(Channel.Creature, $"Processing On Destory {entity.m_Name}");

            CreatureAttribute[] attributes = entity.m_Attributes.Select((other) => CreatureDataList.Instance.GetAttribute(other)).ToArray();
            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i] == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Creature,
                        $"Entity({entity.m_Name}) has empty attribute. This is not allowed. Request Ignored.");
                    continue;
                }

                CreatureAttribute att = attributes[i];
                if (system.m_Processors.TryGetValue(att.GetType(), out List<ICreatureAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        processors[j].OnDestory(att, dataObj);
                    }
                }
            }
        }
    }

    public sealed class CreatureInfoDataComponent : DataComponentEntity
    {
        public Creature m_CreatureInfo;
        public CreatureBrain m_Brain;

        public bool IsAlive => m_CreatureInfo.m_HP > 0;
    }
}
