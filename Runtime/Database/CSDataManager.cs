using Newtonsoft.Json;
using System.Collections.Generic;

using UnityEngine;
using Syadeu.Mono;
using Syadeu.Collections.Converters;
using Syadeu.Internal;
using System;
using System.Reflection;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Collections
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
        private static JsonSerializerSettings SerializerSettings = null;

        public override void OnInitialize()
        {
            Initialize();
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void SetEditor()
        {
            Initialize();
        }
#endif
        private static void Initialize()
        {
            SetJsonConverters();

            

            Type[] types = TypeHelper.GetTypes((other) => TypeHelper.TypeOf<IStaticInitializer>.Type.IsAssignableFrom(other));
            for (int i = 0; i < types.Length; i++)
            {
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(types[i].TypeHandle);
            }
        }

        private static void SetJsonConverters()
        {
            if (s_JsonConverterSet) return;
            JsonConvert.DefaultSettings = GetSerializerSettings;
            s_JsonConverterSet = true;
        }
        private static JsonSerializerSettings GetSerializerSettings()
        {
            if (SerializerSettings == null)
            {
                SerializerSettings = new JsonSerializerSettings
                {
                    Converters = new List<JsonConverter>
                    {
                        new Int2JsonConverter(),
                        new Int3JsonConverter(),
                        new Float2JsonConverter(),
                        new Float3JsonConverter(),
                        new QuaternionJsonConvereter()
                        //new PrefabReferenceJsonConvereter()
                    }
                };

                Type[] customConverters = TypeHelper.GetTypes((other) => other.GetCustomAttribute<CustomJsonConverterAttribute>() != null);
                for (int i = 0; i < customConverters.Length; i++)
                {
                    SerializerSettings.Converters.Add((JsonConverter)Activator.CreateInstance(customConverters[i]));
                }
            }

            return SerializerSettings;
        }
    }
}
