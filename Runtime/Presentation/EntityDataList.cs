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

using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Syadeu.Collections
{
    public sealed class EntityDataList : CLRSingleTone<EntityDataList>
    {
        const string jsonPostfix = "*.json";
        const string json = ".json";
        const string c_JsonFilePath = "{0}/{1}" + json;

        private static readonly Regex s_Whitespace = new Regex(@"\s+");
        private bool m_IsLoaded = false;

        public Dictionary<ulong, ObjectBase> m_Objects;
        private Dictionary<ulong, Hash> m_EntityNameHash;

        public static bool IsLoaded => Instance.m_IsLoaded;

        protected override void OnInitialize()
        {
            LoadData();
        }

        #region Data Works
        public void Purge()
        {
            m_Objects.Clear();
            m_EntityNameHash.Clear();
        }

        public void LoadData()
        {
            DirectoryCheck();

            m_Objects = new Dictionary<ulong, ObjectBase>();
            m_EntityNameHash = new Dictionary<ulong, Hash>();

            Load<EntityDataBase>(CoreSystemFolder.EntityPath);
            Load<AttributeBase>(CoreSystemFolder.AttributePath);
            Load<ActionBase>(CoreSystemFolder.ActionPath);
            Load<DataObjectBase>(CoreSystemFolder.DataPath);

            m_IsLoaded = true;
        }
        public async void LoadDataAsync(Action onComplete)
        {
            DirectoryCheck();

            m_Objects = new Dictionary<ulong, ObjectBase>();
            m_EntityNameHash = new Dictionary<ulong, Hash>();

            var iter1 = LoadAsync<EntityDataBase>(CoreSystemFolder.EntityPath);
            var iter2 = LoadAsync<AttributeBase>(CoreSystemFolder.AttributePath);
            var iter3 = LoadAsync<ActionBase>(CoreSystemFolder.ActionPath);
            var iter4 = LoadAsync<DataObjectBase>(CoreSystemFolder.DataPath);
            
            await Task.Run(async () =>
            {
                while (!iter1.IsCompleted)
                {
                    await Task.Yield();
                }
                while (!iter2.IsCompleted)
                {
                    await Task.Yield();
                }
                while (!iter3.IsCompleted)
                {
                    await Task.Yield();
                }
                while (!iter4.IsCompleted)
                {
                    await Task.Yield();
                }

                onComplete?.Invoke();
            });
        }

        private ParallelLoopResult LoadAsync<T>(string path) where T : ObjectBase
        {
            string[] dataPaths = Directory.GetFiles(path, jsonPostfix, SearchOption.AllDirectories);

            Type[] dataTypes = TypeHelper.GetTypes(TypePredicate<T>);

            return Parallel.For(0, dataPaths.Length, (i) =>
            {
                string lastFold = Path.GetFileName(Path.GetDirectoryName(dataPaths[i]));
                Type t = dataTypes.FindFor((other) => other.Name.Equals(lastFold));

                object obj;
                try
                {
                    obj = JsonConvert.DeserializeObject(File.ReadAllText(dataPaths[i]), t);
                    if (!(obj is T))
                    {
                        CoreSystem.Logger.LogWarning(Channel.Entity, $"Data({t?.Name}) at {dataPaths[i]} is invalid. This data has been ignored");
                        return;
                    }
                }
                catch (Exception)
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, $"Data({t?.Name}) at {dataPaths[i]} is invalid. This data has been ignored");
                    return;
                }
                T temp = (T)obj;

                if (m_Objects.ContainsKey(temp.Hash))
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, $"Data({t?.Name}) at {dataPaths[i]} is already registered. This data has been ignored and removed.");
                    File.Delete(dataPaths[i]);
                    return;
                }

                m_Objects.Add(temp.Hash, temp);
            });
        }
        private void Load<T>(string path) where T : ObjectBase
        {
            string[] dataPaths = Directory.GetFiles(path, jsonPostfix, SearchOption.AllDirectories);

            Type[] dataTypes = TypeHelper.GetTypes(TypePredicate<T>);
            for (int i = 0; i < dataPaths.Length; i++)
            {
                string lastFold = Path.GetFileName(Path.GetDirectoryName(dataPaths[i]));
                Type t = dataTypes.FindFor((other) => other.Name.Equals(lastFold));

                object obj;
                try
                {
                    obj = JsonConvert.DeserializeObject(File.ReadAllText(dataPaths[i]), t);
                    if (!(obj is T))
                    {
                        CoreSystem.Logger.LogWarning(Channel.Entity, $"1. Data({t?.Name}) at {dataPaths[i]} is invalid. This data has been ignored");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, 
                        $"2. Data({t?.Name}) at {dataPaths[i]} is invalid. " +
                        $"This data has been ignored.");
                    UnityEngine.Debug.LogError(ex);
                    continue;
                }
                T temp = (T)obj;

                if (m_Objects.ContainsKey(temp.Hash))
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, $"Data({t?.Name}) at {dataPaths[i]} is already registered. This data has been ignored and removed.");
                    File.Delete(dataPaths[i]);
                    continue;
                }

                m_Objects.Add(temp.Hash, temp);
            }
        }
        private bool TypePredicate<T>(Type other) => TypeHelper.TypeOf<T>.Type.IsAssignableFrom(other);

        public void SaveData<T>(T obj) where T : ObjectBase
        {
            DirectoryCheck();

            Type objType = obj.GetType();
            string objPath;
            if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(objType))
            {
                objPath = Path.Combine(CoreSystemFolder.EntityPath, objType.Name);
            }
            else if (TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(objType))
            {
                objPath = Path.Combine(CoreSystemFolder.AttributePath, objType.Name);
            }
            else if (TypeHelper.TypeOf<ActionBase>.Type.IsAssignableFrom(objType))
            {
                objPath = Path.Combine(CoreSystemFolder.ActionPath, objType.Name);
            }
            else if (TypeHelper.TypeOf<DataObjectBase>.Type.IsAssignableFrom(objType))
            {
                objPath = Path.Combine(CoreSystemFolder.DataPath, objType.Name);
            }
            else
            {
                throw new NotImplementedException();
            }

            if (!Directory.Exists(objPath)) Directory.CreateDirectory(objPath);

            if (obj.Hash.Equals(Hash.Empty)) obj.Hash = Hash.NewHash();

            File.WriteAllText(string.Format(c_JsonFilePath, objPath, ToFileName(obj)),
                JsonConvert.SerializeObject(obj, Formatting.Indented));
        }
        public void SaveData()
        {
            DirectoryCheck();

            Save<EntityDataBase>(CoreSystemFolder.EntityPath);
            Save<AttributeBase>(CoreSystemFolder.AttributePath);
            Save<ActionBase>(CoreSystemFolder.ActionPath);
            Save<DataObjectBase>(CoreSystemFolder.DataPath);

            DeleteEmptyFolders();

            void Save<T>(string path) where T : ObjectBase
            {
                T[] actions = GetData<T>();
                if (actions != null)
                {
                    string[] dataPaths = Directory.GetFiles(path, jsonPostfix, SearchOption.AllDirectories);
                    for (int i = 0; i < dataPaths.Length; i++)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(dataPaths[i]);
                        if (!actions.Where((other) => other.Name.Equals(fileName)).Any())
                        {
                            File.Delete(dataPaths[i]);
                        }
                    }

                    for (int i = 0; i < actions.Length; i++)
                    {
                        string dataPath = Path.Combine(path, actions[i].GetType().Name);
                        if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);

                        if (actions[i].Hash.Equals(Hash.Empty)) actions[i].Hash = Hash.NewHash();

                        File.WriteAllText(string.Format(c_JsonFilePath, dataPath, ToFileName(actions[i])),
                    JsonConvert.SerializeObject(actions[i], Formatting.Indented));
                    }
                }
            }
        }

        private void DeleteEmptyFolders()
        {
            DirectoryCheck();

            string[] paths = Directory.GetDirectories(CoreSystemFolder.EntityPath);
            for (int i = 0; i < paths.Length; i++)
            {
                if (Directory.GetFiles(paths[i]).Length > 0) continue;

                Directory.Delete(paths[i]);
            }
            paths = Directory.GetDirectories(CoreSystemFolder.AttributePath);
            for (int i = 0; i < paths.Length; i++)
            {
                if (Directory.GetFiles(paths[i]).Length > 0) continue;

                Directory.Delete(paths[i]);
            }
            paths = Directory.GetDirectories(CoreSystemFolder.ActionPath);
            for (int i = 0; i < paths.Length; i++)
            {
                if (Directory.GetFiles(paths[i]).Length > 0) continue;

                Directory.Delete(paths[i]);
            }
            paths = Directory.GetDirectories(CoreSystemFolder.DataPath);
            for (int i = 0; i < paths.Length; i++)
            {
                if (Directory.GetFiles(paths[i]).Length > 0) continue;

                Directory.Delete(paths[i]);
            }
        }
        private void DirectoryCheck()
        {
            if (!Directory.Exists(CoreSystemFolder.EntityPath)) Directory.CreateDirectory(CoreSystemFolder.EntityPath);
            if (!Directory.Exists(CoreSystemFolder.AttributePath)) Directory.CreateDirectory(CoreSystemFolder.AttributePath);
            if (!Directory.Exists(CoreSystemFolder.ActionPath)) Directory.CreateDirectory(CoreSystemFolder.ActionPath);
            if (!Directory.Exists(CoreSystemFolder.DataPath)) Directory.CreateDirectory(CoreSystemFolder.DataPath);
        }
        #endregion

        [Obsolete]
        public AttributeBase[] GetAttributes()
        {
            if (m_Objects == null) return Array.Empty<AttributeBase>();
            return m_Objects
                    .Where((other) =>
                    {
                        return TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(other.Value.GetType());
                    })
                    .Select((other) => (AttributeBase)other.Value)
                    .ToArray();
        }
        public ObjectBase[] GetData()
        {
            if (m_Objects == null) return Array.Empty<ObjectBase>();
            return m_Objects.Values.ToArray();
        }
        public IEnumerable<ObjectBase> GetData(Func<ObjectBase, bool> predicate)
        {
            return m_Objects.Values.Where(predicate);
        }
        public T[] GetData<T>() where T : ObjectBase
        {
            if (m_Objects == null) return Array.Empty<T>();
            return m_Objects
                    .Where(GetDataPredicate<T>)
                    .Select(other => (T)other.Value)
                    .ToArray();
        }
        private bool GetDataPredicate<T>(KeyValuePair<ulong, ObjectBase> pair)
        {
            Type type = pair.Value.GetType();
            return TypeHelper.TypeOf<T>.Type.IsAssignableFrom(type);
        }

        public ObjectBase GetObject(Hash hash)
        {
            if (hash.IsEmpty()) return null;
            else if (m_Objects.TryGetValue(hash, out var value)) return value;

            return null;
        }
        public ObjectBase GetObject(string name) => GetObject(m_EntityNameHash[Hash.NewHash(name)]);

        public static string ToFileName(ObjectBase obj)
        {
            const string c_UnderScore = "_";
            Type t = obj.GetType();

            return ToNames(t) + c_UnderScore + ReplaceWhitespace(obj.Name, string.Empty).ToLower();

            static string ToNames(Type t)
            {
                string targetName = string.Empty;
                if (t.BaseType != null && !t.BaseType.Equals(TypeHelper.TypeOf<ObjectBase>.Type))
                {
                    targetName += ToNames(t.BaseType);
                    targetName += c_UnderScore;
                }
                string typeName;
                if (t.Name.Length > 3)
                {
                    typeName = t.Name.Substring(0, 2);
                    typeName += t.Name.Substring(t.Name.Length - 2, 2);

                    typeName = typeName.ToLower();
                }
                else typeName = t.Name.ToLower();

                targetName += typeName;
                return targetName;
            }
            static string ReplaceWhitespace(string input, string replacement)
            {
                return s_Whitespace.Replace(input, replacement);
            }
        }
    }
}
