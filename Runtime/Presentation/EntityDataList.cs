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
using UnityEngine;

namespace Syadeu.Database
{
    [PreferBinarySerialization] [CustomStaticSetting("Syadeu")]
    public sealed class EntityDataList : StaticSettingEntity<EntityDataList>
    {
        const string jsonPostfix = "*.json";
        const string json = ".json";
        const string c_JsonFilePath = "{0}/{1}" + json;

        private static readonly Regex s_Whitespace = new Regex(@"\s+");
        private static bool s_IsLoaded = false;

        public Dictionary<Hash, ObjectBase> m_Objects;
        private Dictionary<ulong, Hash> m_EntityNameHash;

        public static bool IsLoaded => s_IsLoaded;

        private void OnEnable()
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

            m_Objects = new Dictionary<Hash, ObjectBase>();
            m_EntityNameHash = new Dictionary<ulong, Hash>();

            Load<EntityDataBase>(CoreSystemFolder.EntityPath);
            Load<AttributeBase>(CoreSystemFolder.AttributePath);
            Load<ActionBase>(CoreSystemFolder.ActionPath);
            Load<DataObjectBase>(CoreSystemFolder.DataPath);

            s_IsLoaded = true;

            void Load<T>(string path) where T : ObjectBase
            {
                string[] dataPaths = Directory.GetFiles(path, jsonPostfix, SearchOption.AllDirectories);

                Type[] dataTypes = TypeHelper.GetTypes((other) => TypeHelper.TypeOf<T>.Type.IsAssignableFrom(other));
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
                            CoreSystem.Logger.LogWarning(Channel.Entity, $"Data({t?.Name}) at {dataPaths[i]} is invalid. This data has been ignored");
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        CoreSystem.Logger.LogWarning(Channel.Entity, $"Data({t?.Name}) at {dataPaths[i]} is invalid. This data has been ignored");
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
        }
        public void SaveData<T>(T obj) where T : ObjectBase
        {
            DirectoryCheck();

            Type objType = obj.GetType();
            //Type objType = TypeHelper.TypeOf<T>.Type;
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
        public T[] GetData<T>() where T : ObjectBase
        {
            if (m_Objects == null) return Array.Empty<T>();
            return m_Objects
                    .Where((other) =>
                    {
                        return TypeHelper.TypeOf<T>.Type.IsAssignableFrom(other.Value.GetType());
                    })
                    .Select((other) => (T)other.Value)
                    .ToArray();
        }

        public ObjectBase GetObject(Hash hash)
        {
            if (hash.Equals(Hash.Empty)) return null;
            if (m_Objects.TryGetValue(hash, out var value)) return value;
            return null;
        }
        public ObjectBase GetObject(string name) => GetObject(m_EntityNameHash[Hash.NewHash(name)]);

        private static string ToFileName(ObjectBase obj)
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
