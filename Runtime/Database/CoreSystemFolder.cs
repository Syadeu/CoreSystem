using UnityEngine;
using System.IO;

namespace Syadeu.Collections
{
    public static class CoreSystemFolder
    {
        private const string DEFAULT_PATH = "../CoreSystem";
        private const string MODULE_PATH = "Modules";
        private static string c_ApplicationDataPath = Application.dataPath;

        public static string ApplicationDataPath => c_ApplicationDataPath;
        public static string RootPath => Path.Combine(c_ApplicationDataPath, "..");
        public static string CoreSystemDataPath => Path.Combine(c_ApplicationDataPath, DEFAULT_PATH);
        public static string ModulePath => Path.Combine(CoreSystemDataPath, MODULE_PATH);
        public static string LuaPath => Path.Combine(ModulePath, "Lua");

        private const string ENTITYDATA_PATH = "Entities";
        private const string ATTRIBUTES = "Attributes";
        private const string ACTION = "Actions";
        private const string DATA = "Data";
        public static string EntityPath => Path.Combine(ModulePath, ENTITYDATA_PATH);
        public static string AttributePath => Path.Combine(ModulePath, ATTRIBUTES);
        public static string ActionPath => Path.Combine(ModulePath, ACTION);
        public static string DataPath => Path.Combine(ModulePath, DATA);
    }
}
