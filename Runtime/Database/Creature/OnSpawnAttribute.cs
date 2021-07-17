using Newtonsoft.Json;
using Syadeu.Mono;
using Syadeu.Database.Lua;
using Syadeu.Presentation;
using Syadeu.Internal;

namespace Syadeu.Database.CreatureData.Attributes
{
    public sealed class OnSpawnAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "OnSpawn")] public LuaScriptContainer m_OnSpawn;
    }
    public sealed class OnSpawnProcessor : AttributeProcessor<OnSpawnAttribute>
    {
        const string c_ScriptError = "On Spawn Attribute has an invalid lua function({0}) at Entity({1}). Request ignored.";

        protected override void OnCreated(OnSpawnAttribute attribute, DataGameObject dataObj)
        {
            if (attribute.m_OnSpawn == null || attribute.m_OnSpawn.m_Scripts == null) return;

            try
            {
                attribute.m_OnSpawn.Invoke(dataObj);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogWarning(Channel.Lua,
                    string.Format(c_ScriptError, $"OnSpawn", dataObj.GetEntity().Name) +
                    "\n" + ex.Message);
            }

            //for (int i = 0; i < attribute.m_OnSpawn.m_Scripts.Count; i++)
            //{
            //    if (attribute.m_OnSpawn.m_Scripts[i] == null) continue;
            //    if (!attribute.m_OnSpawn.m_Scripts[i].IsValid())
            //    {
            //        CoreSystem.Logger.LogWarning(Channel.Lua,
            //            string.Format(c_ScriptError, $"OnSpawn: {i}", dataObj.GetComponent<CreatureInfoDataComponent>().m_CreatureInfo.m_Name));
            //        continue;
            //    }


            //    //CreatureSystem.InvokeLua(attribute.m_OnSpawn.m_Scripts[i], dataObj,
            //    //    calledAttName: TypeHelper.TypeOf<OnSpawnAttribute>.Name,
            //    //    calledScriptName: "OnSpawn");
            //}
        }
        protected override void OnDestory(OnSpawnAttribute attribute, DataGameObject dataObj)
        {
            "destory".ToLog();
        }
    }
}
