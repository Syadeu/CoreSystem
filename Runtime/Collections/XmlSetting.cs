﻿// Copyright 2022 Seung Ha Kim
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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace Syadeu.Collections
{
    public sealed class XmlSettings
    {
        private static readonly string s_GlobalConfigPath = Path.Combine(CoreSystemFolder.CoreSystemDataPath, "config.xml");
        private static readonly Dictionary<Type, FieldInfo[]> s_CachedSettingFields = new Dictionary<Type, FieldInfo[]>();

        private static XDocument LoadDocumentFromDisk()
        {
            XDocument doc;
            if (File.Exists(s_GlobalConfigPath))
            {
                using (var rdr = new StreamReader(s_GlobalConfigPath))
                {
                    string xml = rdr.ReadToEnd();
                    doc = XDocument.Parse(xml);
                }
            }
            else
            {
                doc = new XDocument(
                    new XElement("Root")
                    );
            }
            return doc;
        }
        private static XDocument LoadDocumentFromPref(string key)
        {
            string xmlString = PlayerPrefs.GetString(key);
            XDocument doc;
            if (!string.IsNullOrEmpty(xmlString)) doc = XDocument.Parse(xmlString);
            else
            {
                doc = new XDocument(
                    new XElement(key)
                    );
            }
            return doc;
        }

        private static void SaveDocument(XDocument doc, bool saveToDisk)
        {
            if (!saveToDisk)
            {
                PlayerPrefs.SetString(doc.Root.Name.ToString(), doc.Document.ToString());
                return;
            }

            string xml = doc.Document.ToString();
            Debug.Log(xml);

            using (var wr = new StreamWriter(s_GlobalConfigPath, false))
            {
                wr.Write(xml);
            }
        }
        private static void SaveDocument(XElement doc, bool saveToDisk)
        {
            if (!saveToDisk)
            {
                PlayerPrefs.SetString(doc.Name.ToString(), doc.Document.ToString());
                return;
            }

            string xml = doc.Document.ToString();
            Debug.Log(xml);

            using (var wr = new StreamWriter(s_GlobalConfigPath, false))
            {
                wr.Write(xml);
            }
        }
        
        private static XElement GetElement(object obj, XmlSettingsAttribute settings = null)
        {
            Type t = obj.GetType();
            if (settings == null)
            {
                settings = t.GetCustomAttribute<XmlSettingsAttribute>();
            }

            string key;
            if (settings.Name.IsNullOrEmpty()) key = TypeHelper.ToString(t);
            else key = settings.Name;

            XDocument doc = settings.SaveToDisk ? LoadDocumentFromDisk() : LoadDocumentFromPref(key);
            XElement objRoot;
            if (settings.SaveToDisk)
            {
                objRoot = doc.Root.Element(key);
                if (objRoot == null)
                {
                    objRoot = new XElement(key);
                    doc.Root.Add(objRoot);
                }
            }
            else
            {
                objRoot = doc.Element(key);
                if (objRoot == null)
                {
                    objRoot = new XElement(key);
                    doc.Add(objRoot);
                }
            }

            return objRoot;
        }
        private static FieldInfo[] GetSettingFields(Type t)
        {
            if (s_CachedSettingFields.TryGetValue(t, out FieldInfo[] fields)) return fields;

            fields = t
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(t => t.GetCustomAttribute<XmlFieldAttribute>() != null)
                .ToArray();
            s_CachedSettingFields.Add(t, fields);

            return fields;
        }

        public static void SaveSettings(object obj)
        {
            Type t = obj.GetType();
            XmlSettingsAttribute att = t.GetCustomAttribute<XmlSettingsAttribute>();
            if (att == null) return;

            XElement objRoot = GetElement(obj, att);

            IEnumerable<FieldInfo> fieldsIter = GetSettingFields(t);
            foreach (FieldInfo fieldInfo in fieldsIter)
            {
                if (!ValidateFieldType(fieldInfo))
                {
                    Debug.Log($"not valid {fieldInfo.Name}");
                    continue;
                }

                XmlFieldAttribute fieldAtt = fieldInfo.GetCustomAttribute<XmlFieldAttribute>();

                string elementName = fieldAtt.Name.IsNullOrEmpty() ? fieldInfo.Name : fieldAtt.Name;
                XElement element = objRoot.Element(elementName);
                if (element == null)
                {
                    objRoot.Add(
                        new XElement(
                            elementName,
                            fieldInfo.GetValue(obj).ToString()
                            )
                        );
                    continue;
                }

                element.Value = fieldInfo.GetValue(obj).ToString();
            }

            Debug.Log($"save doc for {obj.GetType().Name}");
            SaveDocument(objRoot, att.SaveToDisk);
        }
        public static void LoadSettings(object obj)
        {
            Type t = obj.GetType();
            XmlSettingsAttribute att = t.GetCustomAttribute<XmlSettingsAttribute>();
            if (att == null) return;

            XElement objRoot = GetElement(obj, att);

            IEnumerable<FieldInfo> fieldsIter = GetSettingFields(t);
            foreach (FieldInfo fieldInfo in fieldsIter)
            {
                if (!ValidateFieldType(fieldInfo))
                {
                    Debug.Log($"not valid {fieldInfo.Name}");
                    continue;
                }

                XmlFieldAttribute fieldAtt = fieldInfo.GetCustomAttribute<XmlFieldAttribute>();

                string elementName = fieldAtt.Name.IsNullOrEmpty() ? fieldInfo.Name : fieldAtt.Name;
                XElement element = objRoot.Element(elementName);
                if (element == null)
                {
                    objRoot.Add(
                        new XElement(
                            elementName,
                            //TypeHelper.GetDefaultValue(fieldInfo.FieldType).ToString()
                            fieldInfo.GetValue(obj).ToString()
                            )
                        );
                    Debug.Log($"not exist {elementName} adding");
                    continue;
                }
                object value = Convert.ChangeType(element.Value, fieldInfo.FieldType);
                Debug.Log($"{fieldInfo.Name}={value} :: loaded");
                fieldInfo.SetValue(obj, value);
            }

            //Debug.Log(doc.ToString());
            SaveDocument(objRoot, att.SaveToDisk);
        }
        public static T LoadValueFromDisk<T>(string key, string name, T defaultValue = default(T))
        {
            XDocument doc = LoadDocumentFromDisk();
            XElement element = doc.Element(key);
            bool isModified = false;

            if (element == null)
            {
                element = new XElement(key);
                doc.Add(element);
                isModified = true;
            }
            XElement value = element.Element(name);
            if (value == null)
            {
                value = new XElement(name, defaultValue.ToString());
                element.Add(value);
                isModified = true;

                Debug.Log($"value not found for {key}:{name}. using default {defaultValue}");
            }

            if (isModified)
            {
                SaveDocument(doc, true);
            }

            T result = (T)Convert.ChangeType(value.Value, TypeHelper.TypeOf<T>.Type);
            Debug.Log($"loaded result {result}");
            return result;
        }
        public static T LoadValueFromPref<T>(string key, string name, T defaultValue = default(T))
        {
            XDocument doc = LoadDocumentFromPref(key);
            XElement element = doc.Element(key);
            bool isModified = false;

            if (element == null)
            {
                element = new XElement(key);
                doc.Add(element);
                isModified = true;
            }
            XElement value = element.Element(name);
            if (value == null)
            {
                value = new XElement(name, defaultValue.ToString());
                element.Add(value);
                isModified = true;

                Debug.Log($"value not found for {key}:{name}. using default {defaultValue}");
            }

            if (isModified)
            {
                SaveDocument(doc, false);
            }

            T result = (T)Convert.ChangeType(value.Value, TypeHelper.TypeOf<T>.Type);
            Debug.Log($"loaded result {result}");
            return result;
        }

        // https://www.delftstack.com/howto/csharp/serialize-object-to-xml-in-csharp/#:~:text=The%20XmlSerializer%20class%20converts%20class,an%20XML%20file%20or%20string.
        private static bool ValidateFieldType(FieldInfo fieldInfo)
        {
            Type t = fieldInfo.FieldType;
            if (TypeHelper.TypeOf<bool>.Type.Equals(t) ||
                TypeHelper.TypeOf<float>.Type.Equals(t) ||
                TypeHelper.TypeOf<int>.Type.Equals(t) ||
                TypeHelper.TypeOf<double>.Type.Equals(t) ||
                TypeHelper.TypeOf<string>.Type.Equals(t)
                )
            {
                return true;
            }
            return false;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class XmlSettingsAttribute : Attribute
    {
        public string Name;
        public bool SaveToDisk;
    }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class XmlFieldAttribute : Attribute 
    {
        public string Name;
    }
}
