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

        public Dictionary<Hash, ObjectBase> m_Objects;
        private Dictionary<ulong, Hash> m_EntityNameHash;

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

            #region Load Entities

            string[] entityPaths = Directory.GetFiles(CoreSystemFolder.EntityPath, jsonPostfix, SearchOption.AllDirectories);

            Type[] entityTypes = TypeHelper.GetTypes(
                (other) => TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(other));
            for (int i = 0; i < entityPaths.Length; i++)
            {
                string lastFold = Path.GetFileName(Path.GetDirectoryName(entityPaths[i]));
                Type t = entityTypes.FindFor((other) => other.Name.Equals(lastFold));

                object obj = JsonConvert.DeserializeObject(File.ReadAllText(entityPaths[i]), t);
                if (!(obj is ObjectBase))
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, $"Entity({t?.Name}) at {entityPaths[i]} is invalid. This entity has been ignored");
                    continue;
                }

                ObjectBase temp = (ObjectBase)obj;

                if (m_Objects.ContainsKey(temp.Hash))
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, $"Entity({t?.Name}) at {entityPaths[i]} is already registered. This entity has been ignored and removed.");
                    File.Delete(entityPaths[i]);
                    continue;
                }
                m_Objects.Add(temp.Hash, temp);
                m_EntityNameHash.Add(Hash.NewHash(temp.Name), temp.Hash);
            }

            #endregion

            #region Load Attributes

            string[] attPaths = Directory.GetFiles(CoreSystemFolder.AttributePath, jsonPostfix, SearchOption.AllDirectories);

            Type[] attTypes = TypeHelper.GetTypes((other) => TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(other));
            for (int i = 0; i < attPaths.Length; i++)
            {
                string lastFold = Path.GetFileName(Path.GetDirectoryName(attPaths[i]));
                Type t = attTypes.FindFor((other) => other.Name.Equals(lastFold));

                object obj;
                try
                {
                    obj = JsonConvert.DeserializeObject(File.ReadAllText(attPaths[i]), t);
                    if (!(obj is AttributeBase))
                    {
                        CoreSystem.Logger.LogWarning(Channel.Entity, $"Attribute({t?.Name}) at {attPaths[i]} is invalid. This attribute has been ignored");
                        continue;
                    }
                }
                catch (Exception)
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, $"Attribute({t?.Name}) at {attPaths[i]} is invalid. This attribute has been ignored");
                    continue;
                }
                var temp = (AttributeBase)obj;

                if (m_Objects.ContainsKey(temp.Hash))
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, $"Attribute({t?.Name}) at {attPaths[i]} is already registered. This attribute has been ignored and removed.");
                    File.Delete(attPaths[i]);
                    continue;
                }

                m_Objects.Add(temp.Hash, temp);
            }

            #endregion

            #region Load Actions

            string[] actionPaths = Directory.GetFiles(CoreSystemFolder.ActionPath, jsonPostfix, SearchOption.AllDirectories);

            Type[] actionTypes = TypeHelper.GetTypes((other) => TypeHelper.TypeOf<ActionBase>.Type.IsAssignableFrom(other));
            for (int i = 0; i < actionPaths.Length; i++)
            {
                string lastFold = Path.GetFileName(Path.GetDirectoryName(actionPaths[i]));
                Type t = actionTypes.FindFor((other) => other.Name.Equals(lastFold));

                object obj;
                try
                {
                    obj = JsonConvert.DeserializeObject(File.ReadAllText(actionPaths[i]), t);
                    if (!(obj is ActionBase))
                    {
                        CoreSystem.Logger.LogWarning(Channel.Entity, $"Action({t?.Name}) at {actionPaths[i]} is invalid. This action has been ignored");
                        continue;
                    }
                }
                catch (Exception)
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, $"Action({t?.Name}) at {actionPaths[i]} is invalid. This action has been ignored");
                    continue;
                }
                var temp = (ActionBase)obj;

                if (m_Objects.ContainsKey(temp.Hash))
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, $"Action({t?.Name}) at {actionPaths[i]} is already registered. This action has been ignored and removed.");
                    File.Delete(actionPaths[i]);
                    continue;
                }

                m_Objects.Add(temp.Hash, temp);
            }

            #endregion

            Load<DataObjectBase>(CoreSystemFolder.DataPath);

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

            #region Save Entities

            ObjectBase[] m_Entites = GetEntities();
            if (m_Entites != null)
            {
                string[] entityPaths = Directory.GetFiles(CoreSystemFolder.EntityPath, jsonPostfix, SearchOption.AllDirectories);
                for (int i = 0; i < entityPaths.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(entityPaths[i]);
                    if (!m_Entites.Where((other) => other.Name.Equals(fileName)).Any())
                    {
                        File.Delete(entityPaths[i]);
                    }
                }
                for (int i = 0; i < m_Entites.Length; i++)
                {
                    string entityPath = Path.Combine(CoreSystemFolder.EntityPath, m_Entites[i].GetType().Name);
                    if (!Directory.Exists(entityPath)) Directory.CreateDirectory(entityPath);

                    if (m_Entites[i].Hash.Equals(Hash.Empty)) m_Entites[i].Hash = Hash.NewHash();

                    File.WriteAllText(string.Format(c_JsonFilePath, entityPath, ToFileName(m_Entites[i])),
                JsonConvert.SerializeObject(m_Entites[i], Formatting.Indented));
                }
            }
            else "nothing to save entit".ToLog();

            #endregion

            #region Save Attributes

            AttributeBase[] m_Attributes = GetAttributes();
            if (m_Attributes != null)
            {
                string[] atts = Directory.GetFiles(CoreSystemFolder.AttributePath, jsonPostfix, SearchOption.AllDirectories);
                for (int i = 0; i < atts.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(atts[i]);
                    if (!m_Attributes.Where((other) => other.Name.Equals(fileName)).Any())
                    {
                        File.Delete(atts[i]);
                    }
                }

                for (int i = 0; i < m_Attributes.Length; i++)
                {
                    string attPath = Path.Combine(CoreSystemFolder.AttributePath, m_Attributes[i].GetType().Name);
                    if (!Directory.Exists(attPath)) Directory.CreateDirectory(attPath);

                    if (m_Attributes[i].Hash.Equals(Hash.Empty)) m_Attributes[i].Hash = Hash.NewHash();

                    File.WriteAllText(string.Format(c_JsonFilePath, attPath, ToFileName(m_Attributes[i])),
                JsonConvert.SerializeObject(m_Attributes[i], Formatting.Indented));
                }
            }
            else "nothing to save att".ToLog();

            #endregion

            #region Save Actions

            ActionBase[] actions = GetActions();
            if (actions != null)
            {
                string[] atts = Directory.GetFiles(CoreSystemFolder.ActionPath, jsonPostfix, SearchOption.AllDirectories);
                for (int i = 0; i < atts.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(atts[i]);
                    if (!actions.Where((other) => other.Name.Equals(fileName)).Any())
                    {
                        File.Delete(atts[i]);
                    }
                }

                for (int i = 0; i < actions.Length; i++)
                {
                    string attPath = Path.Combine(CoreSystemFolder.ActionPath, actions[i].GetType().Name);
                    if (!Directory.Exists(attPath)) Directory.CreateDirectory(attPath);

                    if (actions[i].Hash.Equals(Hash.Empty)) actions[i].Hash = Hash.NewHash();

                    File.WriteAllText(string.Format(c_JsonFilePath, attPath, ToFileName(actions[i])),
                JsonConvert.SerializeObject(actions[i], Formatting.Indented));
                }
            }
            #endregion

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
        public EntityDataBase[] GetEntities()
        {
            if (m_Objects == null) return Array.Empty<EntityDataBase>();
            return m_Objects
                    .Where((other) =>
                    {
                        return TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(other.Value.GetType());
                    })
                    .Select((other) => (EntityDataBase)other.Value)
                    .ToArray();
        }
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
        [Obsolete]
        public ActionBase[] GetActions()
        {
            if (m_Objects == null) return Array.Empty<ActionBase>();
            return m_Objects
                    .Where((other) =>
                    {
                        return TypeHelper.TypeOf<ActionBase>.Type.IsAssignableFrom(other.Value.GetType());
                    })
                    .Select((other) => (ActionBase)other.Value)
                    .ToArray();
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
