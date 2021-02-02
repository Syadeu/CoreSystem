using Syadeu.Extentions.EditorUtils;

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Database
{
    /// <summary>
    /// SQLiteDatabase 마이그레이션을 위한 유틸 데이터객체
    /// </summary>
    public sealed class SQLiteMigrationTool
    {
        public static bool TryLoadMigrationRule(string tableName, out string data)
        {
            string name = SQLiteDatabaseUtils.TrimNumbers(tableName);
            return SQLiteMigrationData.Instance.MigrationData.TryGetValue(name, out data);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 전용
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static bool HasMigrationRule(string tableName)
        {
            tableName = SQLiteDatabaseUtils.TrimNumbers(tableName);

            for (int i = 0; i < SQLiteMigrationData.Instance.m_Migrations.Count; i++)
            {
                if (SQLiteMigrationData.Instance.m_Migrations[i].name == tableName)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 에디터 전용
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string LoadMigrationRule(string tableName)
        {
            tableName = SQLiteDatabaseUtils.TrimNumbers(tableName);

            for (int i = 0; i < SQLiteMigrationData.Instance.m_Migrations.Count; i++)
            {
                if (SQLiteMigrationData.Instance.m_Migrations[i].name == tableName)
                {
                    return SQLiteMigrationData.Instance.m_Migrations[i].text;
                }
            }
            return string.Empty;
        }
        /// <summary>
        /// 에디터 전용
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="contents"></param>
        /// <param name="dataPath"></param>
        public static void SaveMigrationRule(string tableName, string contents, string dataPath)
        {
            tableName = SQLiteDatabaseUtils.TrimNumbers(tableName);

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            File.WriteAllText(Path.Combine(dataPath, $"{tableName}.txt"), contents);
        }
#endif

        #region Migration Logic

        private static List<string> removeColumnList = new List<string>();

        private static bool ExcuteLogic(ref string pointer, ref SQLiteDatabase database, string tableName, string line)
        {
            //$"if in {line}".ToLog();
            pointer = "method";

            //line = line.Substring(3, line.Length - 4);
            line = line.Replace("if", "").Trim();
            string[] split = line.Split(' ');
            split[0] = split[0].Replace("(", "");
            split[2] = split[2].Replace(")", "");

            bool needLogic = false;
            if (split[1] == "==")
            {
                needLogic = true;
            }

            //$"logic {split[1]} in".ToLog();

            database.TryGetTable(tableName, out var table);
            bool has = table.TryGetColumn(split[0], out var column);

            if (split[2] == "null")
            {
                if (needLogic != has)
                {
                    //$"logic success {split[0]}: {needLogic} == {has}".ToLog();
                    return true;
                }
            }
            else
            {
                "??".ToLog();
            }

            //$"logic faild {split[2]}".ToLog();
            pointer = "pass";
            return false;
        }
        private static Type ToType(string value)
        {
            if (value.StartsWith("typeof"))
            {
                value = value.Replace("typeof(", "");
                value = value.Substring(0, value.Length - 1);
            }

            if (value == "UnityEngine.Vector3")
            {
                return typeof(Vector3);
            }
            if (value.StartsWith("List<"))
            {
                string typeName = value.Replace("List<", "");
                typeName = typeName.Substring(0, typeName.Length - 1);

                Type t = typeof(List<>).MakeGenericType(Type.GetType(typeName));
                return t;
            }
            else return Type.GetType(value);
        }
        private static void ExcuteMethod(ref SQLiteDatabase database, string tableName, string line)
        {
            //$"method in {line}".ToLog();
            line = line.Trim().Replace("\t", "");

            if (line.StartsWith("CreateColumn"))
            {
                line = line.Replace("CreateColumn(", "");
                line = line.Substring(0, line.Length - 1);

                string[] split = line.Split(',');
                Type t = ToType(split[0]);
                if (t == null)
                {
                    $"타입 parameter에서 ({split[0]})과 같은 타입을 찾을 수 없음".ToLog();
                    return;
                }

                database.AddColumn(tableName, t, split[1]).Await();
                //var job = database.Excute();
                //while (!job.IsDone)
                //{
                //    //"waitting 3".ToLog();
                //    CoreSystem.ThreadAwaiter(10);
                //}
                //Debug.Log(database.GetAddColumnQuery(tableName, t, split[1]));
            }
            else if (line.StartsWith("GetColumn"))
            {
                if (!database.TryGetTable(tableName, out SQLiteTable table))
                {
                    $"테이블({tableName})을 찾을 수 없음".ToLog();
                    return;
                }

                string[] split = line.Split('=');
                split[0] = split[0].Replace("GetColumn(", "");
                string[] paramContents = split[0].Split(',');

                Type columnType = ToType(paramContents[0].Trim());
                string columnName = paramContents[1].Split(')')[0].Trim();
                string valueName = paramContents[1].Split(')')[1].Replace(".", "").Trim();
                string targetColumnName = split[1].Trim();

                // GetColumn(columnType, columnName).valueName = targetColumnName

                if (columnType == null)
                {
                    $"입력된 타입이 올바르지 않음 ({paramContents[0].Trim()})".ToLog();
                    return;
                }
                //if (!table.TryGetColumn(targetColumnName, out SQLiteColumn targetColumn))
                //{
                //    "타겟 컬럼을 찾을 수 없음".ToLog();
                //    return;
                //}

                List<SQLiteColumn> newColumns = new List<SQLiteColumn>();
                SQLiteColumn targetColumn = default;
                //int targetColumnIndex = -1;
                int inputColumnIndex = -1;

                for (int i = 0; i < table.Columns.Count; i++)
                {
                    if (table.Columns[i].Name == targetColumnName)
                    {
                        targetColumn = table.Columns[i];
                        //targetColumnIndex = i;
                    }

                    if (table.Columns[i].Name == columnName)
                    {
                        inputColumnIndex = i;
                    }

                    newColumns.Add(new SQLiteColumn(table.Columns[i].Type, table.Columns[i].Name));
                }
                SQLiteTable newTableData = new SQLiteTable(table.Name, newColumns);

                if (!table.TryGetColumn(columnName, out SQLiteColumn column))
                {
                    $"컬럼({columnName})을 찾을 수 없음".ToLog();
                    return;
                }

                for (int i = 0; i < table.Count; i++)
                {
                    object value;
                    if (columnType == typeof(Vector3))
                    {
                        //SQLiteDatabaseUtils.TryParse(column.Values[i].ToString(), out Vector3 vector3);
                        Vector3 vector3 = SQLiteDatabaseUtils.ConvertSQL<Vector3>(column.Values[i]);
                        //$"기존값 : {column.Values[i]} :: {vector3}".ToLog();

                        if (valueName == "x")
                        {
                            vector3.x = float.Parse(targetColumn.Values[i].ToString());
                        }
                        else if (valueName == "y")
                        {
                            vector3.y = float.Parse(targetColumn.Values[i].ToString());
                        }
                        else if (valueName == "z")
                        {
                            vector3.z = float.Parse(targetColumn.Values[i].ToString());
                        }

                        //SQLiteDatabaseUtils.TryParse(vector3.ToString(), out vector3);
                        value = $"{vector3.x}/{vector3.y}/{vector3.z}";
                        //value = SQLiteDatabaseUtils.ParseVector3ToSQL(vector3.ToString());
                        //$"parsed to {value}".ToLog();
                    }
                    else if (columnType.GenericTypeArguments.Length > 0)
                    {
                        //$"{valueName}".ToLog();
                        valueName = valueName.Replace("[", "");
                        valueName = valueName.Replace("]", "");

                        int index = int.Parse(valueName);
                        //var list = SQLiteDatabaseUtils.ToList(columnType.GenericTypeArguments[0], column.Values[i].ToString());
                        var list = SQLiteDatabaseUtils.ParseArrayFromSQL(columnType.GenericTypeArguments[0], column.Values[i], false);

                        while (list.Count <= index)
                        {
                            list.Add(SQLiteDatabaseUtils.GetDefault(columnType.GenericTypeArguments[0]));
                        }

                        list[index] = SQLiteDatabaseUtils.ConvertSQL(columnType.GenericTypeArguments[0], targetColumn.Values[i]);
                        //value = SQLiteDatabaseUtils.ParseArray((System.Collections.ICollection)list);
                        value = SQLiteDatabaseUtils.ParseArrayToSQL(list);
                    }
                    else
                    {
                        value = targetColumn.Values[i];
                    }

                    for (int a = 0; a < newTableData.Columns.Count; a++)
                    {
                        if (a == inputColumnIndex)
                        {
                            newTableData.Columns[a].Values.Add(value);
                            //$"{newTableData.Columns[a].Name} 에 추가 {value}".ToLog();
                        }
                        else
                        {
                            newTableData.Columns[a].Values.Add(table.Columns[a].Values[i]);
                        }
                    }
                }
                //$"{newTableData.Name} updating".ToLog();
                database.UpdateTable(newTableData).Await();
            }
            else if (line.StartsWith("DeleteColumn"))
            {
                line = line.Replace("DeleteColumn(", "");
                line = line.Substring(0, line.Length - 1);

                //$"{line}".ToLog();

                if (!database.TryGetTable(tableName, out var table) ||
                    !table.TryGetColumn(line, out var column))
                {
                    $"존재하지 않는 컬럼을 지우려함 ({line})".ToLog();
                    return;
                }

                if (!removeColumnList.Contains(line))
                {
                    removeColumnList.Add(line);
                }
                //database.RemoveColumn(tableName, line);
            }
            else if (line.StartsWith("Log"))
            {
                line = line.Replace("Log(", "");
                line = line.Substring(0, line.Length - 1);

                $"{line}".ToLog();
            }
            else if (line.StartsWith("TestError"))
            {
                database.AddQuery("testerror");
                database.Excute();
            }
        }
        private static void ExcuteCodeBlock(ref string pointer, string line)
        {
            //if (line == "{")
            //{
            //    pointer = "method";
            //}
            if (line == "}")
            {
                pointer = null;
            }
        }

        #endregion

        public static void ExcuteRule(ref SQLiteDatabase database, string tableName, string rule)
        {
            string pointer = "method";

            using (StringReader rdr = new StringReader(rule))
            {
                string line = rdr.ReadLine();
                while (line != null)
                {
                    line = line.Trim();
                    ExcuteCodeBlock(ref pointer, line);

                    if (line.StartsWith(">>"))
                    {
                        line = line.Replace(">>", "");
                        if (line.StartsWith("version="))
                        {
                            string version = line.Replace("version=", "");
                            string dataVersion = database.GetVersion();
                            if (string.IsNullOrEmpty(dataVersion))
                            {
                                dataVersion = "null";
                            }

                            if (version != dataVersion)
                            {
                                pointer = "pass";
                            }
                            else pointer = null;
                        }
                        else if (line == "end")
                        {
                            pointer = null;
                        }
                    }
                    else if (!line.StartsWith("//") && pointer != "pass")
                    {
                        if (line.StartsWith("if"))
                        {
                            ExcuteLogic(ref pointer, ref database, tableName, line);
                        }
                        else
                        {
                            ExcuteMethod(ref database, tableName, line);
                        }
                    }

                    line = rdr.ReadLine();
                }
            }

            if (removeColumnList.Count > 0)
            {
                //string firstName = removeColumnList[0];
                //removeColumnList.RemoveAt(0);

                database.DeleteColumn(tableName, removeColumnList.ToArray()).Await();
                //while (!job.IsDone)
                //{
                //    //"waitting 1".ToLog();
                //    CoreSystem.ThreadAwaiter(10);
                //}

                removeColumnList.Clear();
            }
        }
        //public static void ExcuteRule(SQLiteDatabase database, string tableName, string dataPath)
        //{
        //    ExcuteRule(database, tableName, LoadMigrationRule(tableName, dataPath));
        //}
    }
}