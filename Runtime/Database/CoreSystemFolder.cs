using UnityEngine;
using System.IO;

namespace Syadeu.Database
{
    public static class CoreSystemFolder
    {
        private const string DEFAULT_PATH = "../CoreSystem";
        private const string MODULE_PATH = "Modules";
        private static string c_ApplicationDataPath = Application.dataPath;

        private const string ITEMDATA_PATH = "Items";
        private const string ITEMTYPES = "ItemTypes";
        private const string ITEMEFFECTS = "ItemEffects";

        public static string ApplicationDataPath => c_ApplicationDataPath;
        public static string RootPath => Path.Combine(c_ApplicationDataPath, "..");
        public static string CoreSystemDataPath => Path.Combine(c_ApplicationDataPath, DEFAULT_PATH);
        public static string ModulePath => Path.Combine(CoreSystemDataPath, MODULE_PATH);
        public static string LuaPath => Path.Combine(ModulePath, "Lua");
        public static string ItemPath => Path.Combine(ModulePath, ITEMDATA_PATH);
        public static string ItemTypePath => Path.Combine(ItemPath, ITEMTYPES);
        public static string ItemEffectTypePath => Path.Combine(ItemPath, ITEMEFFECTS);
    }
}
