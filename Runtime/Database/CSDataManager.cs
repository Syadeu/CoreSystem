using Newtonsoft.Json;
using System.Collections.Generic;

using UnityEngine;
using Syadeu.Mono;
using Syadeu.Database.Converters;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Database
{
    [StaticManagerIntializeOnLoad]
    [StaticManagerDescription(
        "Internal data work system.\n" +
        "This system do the basic data works like authuring json converter types"
        )]
    [UnityEngine.AddComponentMenu("")]
    internal sealed class CSDataManager : StaticDataManager<CSDataManager>
    {
        private static bool s_JsonConverterSet = false;

        public override void OnInitialize()
        {
            SetJsonConverters();
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void SetEditor()
        {
            SetJsonConverters();
        }
#endif

        private static void SetJsonConverters()
        {
            if (s_JsonConverterSet) return;

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new Int2JsonConverter(),
                    new Int3JsonConverter(),
                    new Float2JsonConverter(),
                    new Float3JsonConverter(),
                    new QuaternionJsonConvereter(),
                }
            };

            s_JsonConverterSet = true;
        }
    }
}
