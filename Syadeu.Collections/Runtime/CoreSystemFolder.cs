// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.IO;

namespace Syadeu.Collections
{
    public static class CoreSystemFolder
    {
        private static string DEFAULT_PATH = $"..{Path.DirectorySeparatorChar}CoreSystem";
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
