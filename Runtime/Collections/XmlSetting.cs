// Copyright 2022 Seung Ha Kim
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
        public static void LoadSettings(object obj)
        {
            Type t = obj.GetType();
            XmlSettingsAttribute att = t.GetCustomAttribute<XmlSettingsAttribute>();
            if (att == null) return;

            //CoreSystem.Logger.Log(Channel.Core, $"Setting loading for {t.Name}");

            string key;
            if (att.Name.IsNullOrEmpty()) key = TypeHelper.ToString(t);
            else key = att.Name;

            string xmlString = PlayerPrefs.GetString(key);
            //if (xmlString.IsNullOrEmpty())
            //{
            //    Debug.Log("not found return");
            //    return;
            //}

            XDocument doc;
            if (!string.IsNullOrEmpty(xmlString)) doc = XDocument.Parse(xmlString);
            else
            {
                doc = new XDocument(
                    new XElement("Root")
                    );
            }

            IEnumerable<FieldInfo> fieldsIter = t
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(t => t.GetCustomAttribute<XmlFieldAttribute>() != null);
            foreach (FieldInfo fieldInfo in fieldsIter)
            {
                if (!ValidateFieldType(fieldInfo))
                {
                    Debug.Log($"not valid {fieldInfo.Name}");
                    continue;
                }

                XmlFieldAttribute fieldAtt = fieldInfo.GetCustomAttribute<XmlFieldAttribute>();

                string elementName = fieldAtt.Name.IsNullOrEmpty() ? fieldInfo.Name : fieldAtt.Name;
                XElement element = doc.Root.Element(elementName);
                if (element == null)
                {
                    doc.Root.Add(
                        new XElement(elementName, TypeHelper.GetDefaultValue(fieldInfo.FieldType).ToString()));
                    Debug.Log($"not exist {elementName} adding");
                    continue;
                }
                object value = Convert.ChangeType(element.Value, fieldInfo.FieldType);
                Debug.Log($"{fieldInfo.Name}={value} :: loaded");
                fieldInfo.SetValue(obj, value);
            }

            Debug.Log(doc.ToString());
            PlayerPrefs.SetString(key, doc.ToString());
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
        public string Name { get; set; }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class XmlFieldAttribute : Attribute 
    {
        public string Name { get; set; }
    }
}
