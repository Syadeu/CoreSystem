//using Newtonsoft.Json;
//using Syadeu.Database.Lua;
//using Syadeu.Presentation.Entities;
//using UnityEngine.Scripting;

//namespace Syadeu.Presentation.Attributes
//{
//    [AttributeAcceptOnly(typeof(EntityBase))]
//    public sealed class LuaFunctionAttribute : AttributeBase
//    {
//        [JsonProperty(Order = 0, PropertyName = "OnEntityCreated")] public LuaScriptContainer m_OnEntityCreated;
//        [JsonProperty(Order = 0, PropertyName = "OnEntityDestoryed")] public LuaScriptContainer m_OnEntityDestoryed;
//    }

//    [Preserve]
//    internal sealed class LuaFunctionProcessor : AttributeProcessor<LuaFunctionAttribute>
//    {
//        const string c_ScriptError = "Lua Function Attribute has an invalid lua function({0}) at Entity({1}). Request ignored.";

//        protected override void OnCreated(LuaFunctionAttribute attribute, EntityData<IEntityData> entity)
//        {
//            if (attribute.m_OnEntityCreated != null && attribute.m_OnEntityCreated.m_Scripts != null)
//            {
//                try
//                {
//                    attribute.m_OnEntityCreated.Invoke(entity.Target.gameObject);
//                }
//                catch (System.Exception ex)
//                {
//                    CoreSystem.Logger.LogWarning(Channel.Lua,
//                        string.Format(c_ScriptError, $"OnEntityCreated", entity.Name) +
//                        "\n" + ex.Message);
//                }
//            }
//        }
//        protected override void OnDestroy(LuaFunctionAttribute attribute, EntityData<IEntityData> entity)
//        {
//            if (attribute.m_OnEntityDestoryed != null && attribute.m_OnEntityDestoryed.m_Scripts != null)
//            {
//                try
//                {
//                    attribute.m_OnEntityDestoryed.Invoke(((IEntity)entity).gameObject);
//                }
//                catch (System.Exception ex)
//                {
//                    CoreSystem.Logger.LogWarning(Channel.Lua,
//                        string.Format(c_ScriptError, $"OnEntityDestoryed", entity.Name) +
//                        "\n" + ex.Message);
//                }
//            }
//        }
//    }
//}
