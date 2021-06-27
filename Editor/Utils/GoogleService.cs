using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Syadeu;
using UnityEngine;

namespace SyadeuEditor
{
    public class GoogleService
    {
        private static GoogleService c_Instance;

        private UserCredential m_UserCredential;
        private FileDataStore m_FileDataStore;

        private SheetsService m_SheetsService;
        private Spreadsheet m_Spreadsheet;

        private string m_ClientID = "809230237075-0f5u4paaddpcj9k1jjqcbllerpiracaa.apps.googleusercontent.com";
        private string m_ClientSecret = "2AkmoffdHmC_5R4680PhUIEj";

        public string m_SheetID = "1mEIxoUieG3f83yQbWr5Lz3pEENLFb4GUouv9BpZQ7t8";

        internal static GoogleService Instance
        {
            get
            {
                if (c_Instance == null)
                {
                    c_Instance = new GoogleService();
                }
                return c_Instance;
            }
        }

        private UserCredential UserCredential
        {
            get
            {
                if (m_UserCredential == null)
                {
                    m_UserCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
                    {
                        ClientId = m_ClientID,
                        ClientSecret = m_ClientSecret
                    },
                    new string[] { SheetsService.Scope.Spreadsheets },
                    "user",
                    CancellationToken.None, FileDataStore).Result;
                }
                return m_UserCredential;
            }
        }
        private FileDataStore FileDataStore
        {
            get
            {
                if (m_FileDataStore == null)
                {
                    m_FileDataStore = new FileDataStore($"{Application.temporaryCachePath}/Google/Credential", true);
                }
                return m_FileDataStore;
            }
        }
        private SheetsService SheetsService
        {
            get
            {
                if (m_SheetsService == null)
                {
                    m_SheetsService = new SheetsService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = UserCredential,
                        ApplicationName = "Syadeu"
                    });
                }
                return m_SheetsService;
            }
        }

        public static void SetSheedID(string id) => Instance.m_SheetID = id;
        public static Sheet DownloadSheet(string name)
        {
            var request = Instance.SheetsService.Spreadsheets.Get(Instance.m_SheetID);

            List<string> ranges = new List<string>() { name };
            request.Ranges = ranges;
            request.IncludeGridData = true;

            return request.Execute().Sheets[0];
        }


    }
    public static class GoogleServiceEditor
    {
        public static T[] ToObject<T>(this Sheet sheet)
        {
            if (sheet.Data[0].RowData.Count < 2) return null;
            T[] list = new T[sheet.Data[0].RowData.Count - 1];

            Type t = typeof(T);
            MemberInfo[] allMembers = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            GridData data = sheet.Data[0];

            List<MemberInfo> targetMembers = new List<MemberInfo>();
            for (int i = 0; i < data.RowData[0]?.Values?.Count; i++)
            {
                if (string.IsNullOrEmpty(data.RowData[0].Values[i].FormattedValue)) break;

                string propertyName = data.RowData[0].Values[i].FormattedValue;
                MemberInfo property = allMembers.Where((other) => other.Name.Equals(propertyName)).First();
                targetMembers.Add(property);
            }

            for (int a = 0; a < list.Length; a++)
            {
                T temp = Activator.CreateInstance<T>();
                TypedReference reference = __makeref(temp);

                for (int i = 0; i < targetMembers.Count; i++)
                {
                    if (targetMembers[i] == null) continue;

                    object value;
                    string strValue = data.RowData[a + 1].Values[i].FormattedValue;
                    if (int.TryParse(strValue, out int intVal))
                    {
                        value = intVal;
                    }
                    else if (float.TryParse(strValue, out float floatVal))
                    {
                        value = floatVal;
                    }
                    else if (bool.TryParse(strValue, out bool boolVal))
                    {
                        value = boolVal;
                    }
                    else value = strValue;

                    //$"{targetMembers[i].Name}: {strValue}".ToLog();

                    if (targetMembers[i] is FieldInfo field)
                    {
                        field.SetValueDirect(reference, value);
                    }
                    else if (targetMembers[i] is PropertyInfo property)
                    {
                        object temptemp = temp;
                        property.GetSetMethod().Invoke(temptemp, new object[] { value });
                        temp = (T)temptemp;
                    }
                }

                list[a] = temp;
            }

            return list;
        }
    }
}
