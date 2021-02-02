using Syadeu.Extentions.EditorUtils;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using System.Data.SQLite;

using UnityEngine;

namespace Syadeu.Database
{
    /// <summary>
    /// <para><see cref="SQLiteTable"/>, <see cref="SQLiteColumn"/> 데이터베이스의 최상위 구조체</para>
    /// </summary>
    /// <remarks>
    /// 
    /// 데이터베이스를 불러올려면 <see cref="Initialize(string, string, string)"/>으로 데이터베이스 구조체를 생성해야합니다.<br/>
    /// <see cref="BackgroundJob"/>을 반환하는 모든 작업은 호출된 순서에 의해 순차적으로 수행되며, 
    /// 이 작업이 수행되는 동안 <see cref="SearchTable(string, string, object)"/>과 같이
    /// 데이터베이스에 직접 연결하여 불러오는 메소드들은 피해야됩니다. 이를 안전하게 사용하려면
    /// <see cref="bool"/> <see cref="IsConnectionOpened"/>으로 먼저 체크 후 사용하세요.<br/><br/>
    /// 
    /// <see cref="LoadInMemory"/>가 True일 경우, 모든 테이블들은
    /// <see cref="SQLiteTable"/>의 형태로 <see cref="Tables"/>에 저장되며,
    /// 이를 그대로 <see cref="TryGetTable(string, out SQLiteTable)"/>으로 불러서 사용하거나,
    /// <see cref="SQLiteTableAttribute"/>를 상속받은 구조체를 사용하여 
    /// <see cref="TryGetTableValue{T}(string, int, out T)"/>, 혹은 <see cref="SQLiteTable.TryReadLine{T}(int, out T)"/>
    /// 으로 가공된 데이터로 받아서 사용할 수 있습니다.<br/><br/>
    /// 
    /// 사용자 정의된 <see cref="SQLiteTableAttribute"/> 구조체로 가공된 데이터를 받아오면
    /// 쉽고 간편하게 데이터 재가공 및 읽기가 가능하다는 장점이 있지만, 시스템 비용이 크다는 단점이 존재합니다.
    /// 매우 많은 작업(예를 들어 for, foreach와 같은 <see cref="IEnumerator"/> 작업들)은
    /// 데이터를 가공하여 사용하는 대신, <see cref="SQLiteTable.TryReadLine(int, out IReadOnlyList{KeyValuePair{string, object}})"/>,
    /// <see cref="SQLiteTable.CompairLine{TKey}(TKey, SQLiteTable)"/>과 같이
    /// 미가공 데이터를 사용하거나 반환하는 비용이 작은 메소드로 대체될 수 있습니다. 
    /// 
    /// </remarks>
    public struct SQLiteDatabase
    {
        #region Initialize

        /// <summary>
        /// 싱글 쿼리용
        /// </summary>
        private static ConcurrentQueue<(string, SQLiteParameter[])> Queries { get; }
        private static int ExcuteWorker { get; }
        private ConcurrentQueue<bool> VacuumQueries { get; }
        private Dictionary<string, string> m_SafeWritingTables { get; }

        /// <summary>
        /// 실제 데이터 전체 주소
        /// </summary>
        private string DataPath { get; }
        /// <summary>
        /// 데이터 주소
        /// </summary>
        private string FilePath { get; }
        /// <summary>
        /// 데이터 파일 이름 (확장자 미포함)
        /// </summary>
        private string FileName { get; }

        private SQLiteConnection Connection { get; set; }
        public bool IsConnectionOpened { get; private set; }

        public bool Initialized { get; private set; }
        public bool LoadInMemory { get; private set; }
        public SQLiteTable VersionTable { get; private set; }
        public SQLiteTable[] Tables { get; private set; }

        /// <summary>
        /// 쿼리문을 수행하기 직전에 호출되는 델리게이트입니다. 현재 수행하려는 쿼리문을 넣어서 호출합니다
        /// </summary>
        /// <remarks>
        /// 연결된 메소드<br/>
        /// <seealso cref="Excute"/>
        /// </remarks>
        public Action<string> OnExcute { get; }

        static SQLiteDatabase()
        {
            Queries = new ConcurrentQueue<(string, SQLiteParameter[])>();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ExcuteWorker = -1;
            }
            else
#endif
            {
                ExcuteWorker = CoreSystem.CreateNewBackgroundJobWorker(true);
            }
        }
        private SQLiteDatabase(string path, string name, bool loadInMemory)
        {
            VacuumQueries = new ConcurrentQueue<bool>();
            m_SafeWritingTables = new Dictionary<string, string>();

            FilePath = path;
            FileName = name;

            DataPath = $"URI=file:{Path.Combine(path, $"{name}.db")}";

            VersionTable = default;
            //Tables = new List<SQLiteTable>();
            Tables = new SQLiteTable[0];

            OnExcute = null;

            Connection = null;
            IsConnectionOpened = false;
            Initialized = false;
            LoadInMemory = loadInMemory;
        }
        /// <summary>
        /// 해당 주소(<paramref name="path"/>)와 이름(<paramref name="name"/>)으로 
        /// SQLite 통신을 열어 모든 테이블들을 <paramref name="loadInMemory"/>가 true일 경우 
        /// <see cref="Tables"/>에 저장한 후 반환합니다.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name">확장자 붙이지마세요</param>
        /// <param name="loadInMemory">기본값 true, 초기화때 해당 db의 정보를 전부 메모리에 저장해놀지 결정</param>
        /// <returns></returns>
        public static SQLiteDatabase Initialize(string path, string name, bool loadInMemory)
            => Initialize(path, null, name, loadInMemory);
        /// <inheritdoc cref="Initialize(string, string, bool)"/>
        /// <remarks>
        /// <paramref name="path"/>에 해당 db파일이 없고, <paramref name="originPath"/>가 null 이 아닐경우
        /// <paramref name="originPath"/>에서 <paramref name="name"/>의 이름을 가진 파일을 복사해 붙여넣고
        /// 로드합니다
        /// </remarks>
        public static SQLiteDatabase Initialize(string path, string originPath, string name, bool loadInMemory = true)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            if (!File.Exists($"{Path.Combine(path, name)}.db"))
            {
                "세이브 파일이 없음".ToLog();
                if (File.Exists(Path.Combine(path, $"{name}.db.bak")))
                {
                    "백업 세이브 데이터가 존재함".ToLog();
                    File.Move(Path.Combine(path, $"{name}.db.bak"), Path.Combine(path, $"{name}.db"));
                }
                else
                {
                    if (!string.IsNullOrEmpty(originPath))
                    {
                        if (Application.platform == RuntimePlatform.Android)
                        {
                            //UnityEngine.Networking.UnityWebRequest web = 
                            //    UnityEngine.Networking.UnityWebRequest.Get("jar:file://" + Application.dataPath + "!/assets/saveInfo.db");
                            //while (!web.SendWebRequest().isDone)
                            //{
                            //    CoreSystem.ThreadAwaiter(10);
                            //}
                            //File.WriteAllBytes(Path.Combine(path, $"{name}.db"), web.downloadHandler.data);
                        }
                        else
                        {
                            Assert(!File.Exists(Path.Combine(originPath, $"{name}.db")), "복사될 오리지널 파일이 없음");

                            string copyPath = Path.Combine(originPath, $"{name}.db");
                            File.Copy(copyPath, Path.Combine(path, $"{name}.db"));
                        }
                    }

                    "새로 생성함".ToLog();
                }
            }
            SQLiteDatabase database = new SQLiteDatabase(path, name, loadInMemory);

            if (loadInMemory)
            {
                try
                {
                    database.OpenConnection();
                    database.InternalLoadTables();
                    database.CloseConnection();
                }
                catch (SQLiteUnreadableException unreadEx)
                {
                    database.CloseConnection();

                    $"세이브 데이터를 불러오는 도중 문제가 발생함".ToLog();

                    "불쌍한 녀석, 파일 자체가 깨져버림. 방법없음".ToLog();
                    if (File.Exists(Path.Combine(path, $"{name}_err.db")))
                    {
                        File.Delete(Path.Combine(path, $"{name}_err.db"));
                    }
                    File.Move(Path.Combine(path, $"{name}.db"), Path.Combine(path, $"{name}_err.db"));

                    if (File.Exists(Path.Combine(path, $"{name}.db.bak")))
                    {
                        "백업 세이브 데이터가 존재함".ToLog();
                        File.Move(Path.Combine(path, $"{name}.db.bak"), Path.Combine(path, $"{name}.db"));
                    }
                    else
                    {
                        "불쌍하게도 백업도 없네".ToLog();
                        if (!string.IsNullOrEmpty(originPath))
                        {
                            if (Application.platform == RuntimePlatform.Android)
                            {
                                //UnityEngine.Networking.UnityWebRequest web = 
                                //    UnityEngine.Networking.UnityWebRequest.Get("jar:file://" + Application.dataPath + "!/assets/saveInfo.db");
                                //while (!web.SendWebRequest().isDone)
                                //{
                                //    CoreSystem.ThreadAwaiter(10);
                                //}
                                //File.WriteAllBytes(Path.Combine(path, $"{name}.db"), web.downloadHandler.data);
                            }
                            else
                            {
                                Assert(!File.Exists(Path.Combine(path, $"{name}.db")), "복사될 오리지널 파일이 없음");

                                string copyPath = Path.Combine(originPath, $"{name}.db");
                                File.Copy(copyPath, Path.Combine(path, $"{name}.db"));
                            }
                        }
                        "새로 생성함".ToLog();
                    }

                    throw;

                    //if (unreadEx.IsMasterTable)
                    //{
                    //    "불쌍한 녀석, 파일 자체가 깨져버림. 방법없음".ToLog();
                    //    if (File.Exists(Path.Combine(path, $"{name}_err.db")))
                    //    {
                    //        File.Delete(Path.Combine(path, $"{name}_err.db"));
                    //    }
                    //    File.Move(Path.Combine(path, $"{name}.db"), Path.Combine(path, $"{name}_err.db"));

                    //    if (File.Exists(Path.Combine(path, $"{name}.db.bak")))
                    //    {
                    //        "백업 세이브 데이터가 존재함".ToLog();
                    //        File.Move(Path.Combine(path, $"{name}.db.bak"), Path.Combine(path, $"{name}.db"));
                    //    }
                    //    else
                    //    {
                    //        "불쌍하게도 백업도 없네".ToLog();
                    //        string copyPath = Path.Combine(Application.streamingAssetsPath, $"{name}.db");
                    //        File.Copy(copyPath, Path.Combine(path, $"{name}.db"));
                    //    }
                    //}
                    //else
                    //{
                    //    if (File.Exists(Path.Combine(path, $"{name}_err.db")))
                    //    {
                    //        File.Delete(Path.Combine(path, $"{name}_err.db"));
                    //    }
                    //    File.Move(Path.Combine(path, $"{name}.db"), Path.Combine(path, $"{name}_err.db"));

                    //    if (File.Exists(Path.Combine(path, $"{name}.db.bak")))
                    //    {
                    //        "백업 세이브 데이터가 존재함".ToLog();
                    //        File.Move(Path.Combine(path, $"{name}.db.bak"), Path.Combine(path, $"{name}.db"));
                    //    }
                    //    else
                    //    {
                    //        "불쌍하게도 백업도 없네".ToLog();
                    //        string copyPath = Path.Combine(Application.streamingAssetsPath, $"{name}.db");
                    //        File.Copy(copyPath, Path.Combine(path, $"{name}.db"));
                    //    }
                    //}
                    // 안전모드로 재로드 수행
                    //try
                    //{
                    //    database.OpenConnection();
                    //    database.InternalLoadTables(true);
                    //    database.CloseConnection();
                    //}
                    ////catch (SQLiteUnreadableException anotherEx)
                    ////{

                    ////}
                    //catch (Exception)
                    //{
                    //    database.CloseConnection();

                    //    Assert(true, "예상치 못한 예외사항 발생");
                    //    throw;
                    //}
                }
            }

            $"SQLite Database: New database({name}) initialized".ToLog();
            database.Initialized = true;
            return database;
        }

        #endregion

        #region 내부 통신용 함수

        private void OpenConnection()
        {
            Assert(string.IsNullOrEmpty(DataPath), "데이터 경로가 없는데 커넥션을 열려함");

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            IsConnectionOpened = true;
            Connection = new SQLiteConnection(DataPath);
            Connection.Open();
        }
        private void CloseConnection()
        {
            if (Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
                Connection = null;
            }
            IsConnectionOpened = false;
        }

        /// <summary>
        /// 임시파일 플래그가 있는 테이블("*_temp")들을 정리하고 머지가능하면 머지합니다.
        /// </summary>
        /// <exception cref="SQLiteUnreadableException"></exception>
        private void AddCheckTempTablesQuery()
        {
            List<SQLiteTable> tempTables = new List<SQLiteTable>();
            for (int i = 0; i < Tables.Length; i++)
            {
                if (Tables[i].Name.Contains("_temp"))
                {
                    tempTables.Add(Tables[i]);
                }
            }

            if (tempTables.Count > 0)
            {
                $"SQLite Database: Temp table data found {tempTables.Count}".ToLog();
                for (int i = 0; i < tempTables.Count; i++)
                {
                    string originalName = tempTables[i].Name.Replace("_temp", "");
                    SQLiteTable recovered = tempTables[i].Rename(originalName);
                    if (!HasTable(originalName))
                    {
                        $"SQLite Database: No original data({originalName}) found. Recovering temp data".ToLog();

                        AddQuery($"ALTER TABLE {tempTables[i].Name} RENAME TO {originalName}");
                    }
                    else
                    {
                        $"SQLite Database: Original data({originalName}) found. Updating data".ToLog();

                        using (var iter = GetUpdateTableQuery(recovered, 500))
                        {
                            while (iter.MoveNext())
                            {
                                AddQuery(iter.Current.Item1, iter.Current.Item2);
                            }
                        }

                        AddQuery($"DROP TABLE {tempTables[i].Name}");
                    }

                    if (LoadInMemory) AddReloadAllTablesQuery();
                }
            }
        }
        /// <summary>
        /// 마이그레이션 및 무결성 검사를 즉시 수행합니다.
        /// </summary>
        /// <param name="currentVersion"><see cref="Application.version"/>을 넣으세요</param>
        /// <param name="migrationPath"></param>
        /// <exception cref="SQLiteExcuteExcpetion"></exception>
        private void InternalCheckIntegrity(string currentVersion)
        {
            if (GetVersion() == currentVersion) return;

            for (int i = 0; i < Tables.Length; i++)
            {
                if (SQLiteMigrationTool.TryLoadMigrationRule(Tables[i].Name, out string data))
                {
                    SQLiteMigrationTool.ExcuteRule(ref this, Tables[i].Name, data);
                }
            }

            Excute().Await();
        }
        private void AddVersionInfo(string version)
        {
            SQLiteTable versionInfo = ToSQLiteTable("versionInfo",
                new SQLiteVersionInfoTableData[]
                {
                    new SQLiteVersionInfoTableData
                    {
                        Version = version
                    }
                });
            using (var iter = GetReplaceTableQuery(versionInfo, "versionInfo"))
            {
                while (iter.MoveNext())
                {
                    AddQuery(iter.Current.Item1, iter.Current.Item2);
                }
            }
            if (LoadInMemory) AddReloadTableQuery("versionInfo");

            VacuumQueries.Enqueue(true);
        }

        /// <summary><para>
        /// 데이터 파일 내부의 모든 데이터 테이블을 다시 읽어서 <see cref="Tables"/>에 로드합니다.
        /// </para></summary>
        /// <exception cref="SQLiteUnreadableException"></exception>
        private void InternalLoadTables(bool safeMode = false)
        {
            Assert(Connection == null, "커넥션이 왜 없지?");

            //Tables.Clear();
            List<string> tableNames = new List<string>();
            List<SQLiteTable> tables = new List<SQLiteTable>();

            using (SQLiteCommand cmd = Connection.CreateCommand())
            {
                cmd.CommandText = @"SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                try
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            tableNames.Add(rdr.GetString(0));
                        }
                        rdr.Close();
                    }
                }
                catch (Exception)
                {
                    // 마스터 파일이 깨진거면 복구 불가능
                    throw new SQLiteUnreadableException("sqlite_master", true);
                }

                List<string> corruptTables = new List<string>();

                for (int i = 0; i < tableNames.Count; i++)
                {
                    if (!LoadInMemory)
                    {
                        if (!tableNames[i].Equals("versionInfo")) continue;
                    }

                    cmd.CommandText = GetReadTableQuery(tableNames[i]);

                    List<SQLiteColumn> sqColumns = new List<SQLiteColumn>();

                    try
                    {
                        using (SQLiteDataReader rdr = cmd.ExecuteReader())
                        {
                            int columnCount = rdr.FieldCount;
                            string[] columns = new string[columnCount];
                            List<Type> columnTypes = new List<Type>();

                            for (int a = 0; a < columnCount; a++)
                            {
                                columns[a] = rdr.GetName(a);
                                Type t = rdr.GetFieldType(a);
                                if (t == typeof(double)) t = typeof(float);
                                else if (t == typeof(SQLiteBlob)) t = typeof(byte[]);
                                columnTypes.Add(t);

                                SQLiteColumn column =
                                        new SQLiteColumn(columnTypes[a], columns[a]);

                                sqColumns.Add(column);
                            }

                            while (rdr.Read())
                            {
                                for (int a = 0; a < sqColumns.Count; a++)
                                {
                                    if (sqColumns[a].Type == typeof(byte[]))
                                    {
                                        sqColumns[a].Values.Add(GetBytes(rdr, a));
                                    }
                                    else sqColumns[a].Values.Add(rdr.GetValue(a));
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (safeMode)
                        {
                            corruptTables.Add(tableNames[i]);
                            continue;
                        }
                        else throw new SQLiteUnreadableException(tableNames[i]);
                    }

                    SQLiteTable table = new SQLiteTable(tableNames[i], sqColumns);

                    if (table.Name == "versionInfo")
                    {
                        VersionTable = table;
                    }

                    tables.Add(table);
                }
            }

            Tables = tables.ToArray();

            $"SQLite Database: All Tables Fully Loaded".ToLog();
        }
        private static byte[] GetBytes(SQLiteDataReader rdr, int i)
        {
            const int CHUNK_SIZE = 2 * 1024;
            byte[] buffer = new byte[CHUNK_SIZE];
            long bytesRead;
            long fieldOffset = 0;
            using (MemoryStream stream = new MemoryStream())
            {
                while ((bytesRead = rdr.GetBytes(i, fieldOffset, buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, (int)bytesRead);
                    fieldOffset += bytesRead;
                }
                return stream.ToArray();
            }
        }
        /// <summary>
        /// 해당 테이블을 SQLite 통신하여 읽고 저장합니다.
        /// 이미 로드된 테이블이라면 다시 로드합니다.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="SQLiteUnreadableException"></exception>
        private void InternalLoadTable(string name)
        {
            Assert(Connection == null, "커넥션이 왜 없지?");

            if (HasTable(name, out int index))
            {
                InternalLoadInArrayTable(ref Tables[index]);
            }
            else
            {
                SQLiteTable newTable = InternalLoadInNewTable(name);

                if (newTable.Name == "versionInfo")
                {
                    VersionTable = newTable;
                }

                //for (int i = 0; i < Tables.Length; i++)
                //{
                //    if (Tables[i].Name == name)
                //    {
                //        Tables[i] = newTable;
                //        return;
                //    }
                //}
                var temp = Tables.ToList();
                temp.Add(newTable);
                Tables = temp.ToArray();
            }
        }
        private SQLiteTable InternalLoadInNewTable(string name)
        {
            List<SQLiteColumn> sqColumns = new List<SQLiteColumn>();
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = GetReadTableQuery(name);

                try
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        int columnCount = rdr.FieldCount;
                        List<string> columns = new List<string>();
                        List<Type> columnTypes = new List<Type>();

                        for (int a = 0; a < columnCount; a++)
                        {
                            columns.Add(rdr.GetName(a));
                            Type t = rdr.GetFieldType(a);
                            if (t == typeof(double)) t = typeof(float);
                            else if (t == typeof(SQLiteBlob)) t = typeof(byte[]);
                            columnTypes.Add(t);

                            SQLiteColumn column =
                                    new SQLiteColumn(columnTypes[a], columns[a]);

                            sqColumns.Add(column);
                        }

                        while (rdr.Read())
                        {
                            for (int a = 0; a < sqColumns.Count; a++)
                            {
                                if (sqColumns[a].Type == typeof(byte[]))
                                {
                                    sqColumns[a].Values.Add(GetBytes(rdr, a));
                                }
                                else sqColumns[a].Values.Add(rdr.GetValue(a));
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    throw new SQLiteUnreadableException(name);
                }
            }
            return new SQLiteTable(name, sqColumns);
        }
        private void InternalLoadInArrayTable(ref SQLiteTable table)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = GetReadTableQuery(table.Name);

                try
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        int columnCount = rdr.FieldCount;
                        List<string> columns = new List<string>();
                        List<Type> columnTypes = new List<Type>();

                        int row = 0;
                        while (rdr.Read())
                        {
                            for (int a = 0; a < columnCount; a++)
                            {
                                if (table.Columns[a].Type == typeof(byte[]))
                                {
                                    table.Columns[a].Values[row] = GetBytes(rdr, a);
                                }
                                else table.Columns[a].Values[row] = rdr.GetValue(a);
                            }
                            row++;
                        }
                    }
                }
                catch (Exception)
                {
                    throw new SQLiteUnreadableException(table.Name);
                }
            }
        }

        /// <inheritdoc cref="InternalLoadTable(string)"/>
        private void AddReloadTableQuery(string name)
        {
            Assert(!LoadInMemory, "메모리 로드 플래그가 활성되지않은 데이터베이스");
            AddQuery($"reload:{name}");
        }
        /// <inheritdoc cref="InternalLoadTables"/>
        private void AddReloadAllTablesQuery()
        {
            Assert(!LoadInMemory, "메모리 로드 플래그가 활성되지않은 데이터베이스");
            AddQuery("reload:master");
        }

        /// <summary>
        /// 테이블만 만드는 쿼리
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string GetCreateTableQuery(SQLiteTable data)
        {
            Assert(string.IsNullOrEmpty(data.Name), "만드려는 테이블의 이름이 빈란임");
            Assert(data.Columns.Count == 0, "만드려는 테이블의 컬럼이 0개임");

            string query = $"CREATE TABLE {data.Name} ({ConvertColumnInfoToString(data.Columns[0], true)}";
            for (int i = 1; i < data.Columns.Count; i++)
            {
                query += $", {ConvertColumnInfoToString(data.Columns[i])}";
            }
            query += $", PRIMARY KEY({data.Columns[0].Name}))";

            return query;
        }
        private string GetCreateTableQuery(string tableName, IList<KeyValuePair<Type, string>> columns)
        {
            string query = $"CREATE TABLE {tableName} ({ConvertColumnInfoToString(columns[0], true)}";
            for (int i = 1; i < columns.Count; i++)
            {
                query += $", {ConvertColumnInfoToString(columns[i])}";
            }
            query += $", PRIMARY KEY({columns[0].Value}))";

            return query;
        }
        private string GetCreateTableQuery(string tableName, IList<SQLiteColumn> columns)
        {
            string query = $"CREATE TABLE {tableName} ({ConvertColumnInfoToString(columns[0], true)}";
            for (int i = 1; i < columns.Count; i++)
            {
                query += $", {ConvertColumnInfoToString(columns[i])}";
            }
            query += $", PRIMARY KEY({columns[0].Name}))";

            return query;
        }
        private string GetReadTableQuery(string tableName, params string[] columns)
        {
            if (columns == null || columns.Length == 0)
            {
                return $"SELECT * FROM {tableName}";
            }

            string colNames = null;
            for (int i = 0; i < columns.Length; i++)
            {
                if (!string.IsNullOrEmpty(colNames)) colNames += ",";
                colNames += columns[i];
            }
            return $"SELECT {colNames} FROM {tableName}";
        }
        private string GetAddColumnQuery(string tableName, KeyValuePair<Type, string> column)
        {
            return $"ALTER TABLE {tableName} ADD COLUMN {ConvertColumnInfoToString(column)}";
        }

        private string QueryHelper(ref List<SQLiteParameter> parameters, int i, Type t, object value)
        {
            if (t == typeof(byte[]))
            {
                string temp = $"@item{i}";
                SQLiteParameter parameter = new SQLiteParameter(temp, System.Data.DbType.Binary)
                {
                    Value = value as byte[]
                };
                parameters.Add(parameter);
                return temp;
            }

            //parameters.Add(null);
            return ConvertToString(t, value);
        }
        private IEnumerator<(string, SQLiteParameter[])> GetInsertDataQuery(SQLiteTable table, int queryBlock = 1000)
        {
            Assert(table.IsValid, false, "정상적인 데이터 테이블이 아닌게 들어옴");
            if (table.Count == 0) yield break;

            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            string query = $"INSERT INTO {ConvertTableInfoToString(table, true)} ";

            string unions = " VALUES ";
            for (int i = 0; i < table.Count; i++)
            {
                string properties = null;
                for (int a = 0; a < table.Columns.Count; a++)
                {
                    if (!string.IsNullOrEmpty(properties)) properties += ", ";

                    Type valueType = table.Columns[a].Type;
                    properties += QueryHelper(ref parameters, i, valueType, table.Columns[a].Values[i]);
                    //if (valueType == typeof(byte[]))
                    //{
                    //    string temp = $"@item{i}";
                    //    parameters.Add(new SQLiteParameter
                    //    {
                    //        DbType = System.Data.DbType.Binary,
                    //        ParameterName = temp,
                    //        Value = table.Columns[a].Values[i]
                    //    });
                    //    properties += temp;
                    //}
                    //else
                    //{
                    //    properties += ConvertToString(valueType, table.Columns[a].Values[i]);
                    //    parameters.Add(null);
                    //}
                }
                unions += $"({properties}),";

                if (i != 0 && i % queryBlock == 0)
                {
                    yield return ($"{query}{unions.Substring(0, unions.Length - 1)}", parameters.ToArray());
                    parameters.Clear();
                    unions = " VALUES ";
                }
            }
            if (!string.IsNullOrEmpty(unions))
            {
                yield return ($"{query}{unions.Substring(0, unions.Length - 1)}", parameters.ToArray());
            }
        }
        private IEnumerator<(string, SQLiteParameter[])> GetReplaceTableQuery(SQLiteTable table, string into, int queryBlock = 1000)
        {
            Assert(table.IsValid, false, "정상적인 데이터 테이블이 아닌게 들어옴");

            if (TryGetTable(into, out SQLiteTable current))
            {
                string drop = $"DROP TABLE {into}";
                yield return (drop, null);
            }
            else current = table;

            yield return (GetCreateTableQuery(current), null);

            //if (table.Count == 0) yield break;

            if (table.Count != 0)
            {
                using (var iter = GetInsertDataQuery(table, queryBlock))
                {
                    while (iter.MoveNext())
                    {
                        yield return iter.Current;
                    }
                }
            }
        }
        private IEnumerator<(string, SQLiteParameter[])> GetUpdateTableQuery(SQLiteTable table, int queryBlock = 100)
        {
            Assert(table.IsValid, false, "정상적인 데이터 테이블이 아닌게 들어옴");
            if (table.Count == 0) yield break;

            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            string query = $"INSERT OR REPLACE INTO {ConvertTableInfoToString(table, true)} ";

            string unions = " VALUES ";
            for (int i = 0; i < table.Count; i++)
            {
                string properties = null;
                for (int a = 0; a < table.Columns.Count; a++)
                {
                    if (!string.IsNullOrEmpty(properties)) properties += ", ";

                    Type valueType = table.Columns[a].Type;
                    properties += QueryHelper(ref parameters, i, valueType, table.Columns[a].Values[i]);
                    //properties += ConvertToString(valueType, table.Columns[a].Values[i]);
                }
                unions += $"({properties}),";

                if (i != 0 && i % queryBlock == 0)
                {
                    yield return ($"{query}{unions.Substring(0, unions.Length - 1)}", parameters.ToArray());
                    parameters.Clear();
                    unions = " VALUES ";
                }
            }
            if (!string.IsNullOrEmpty(unions))
            {
                yield return ($"{query}{unions.Substring(0, unions.Length - 1)}", parameters.ToArray());
            }
        }
        private IEnumerator<string> GetRemoveRowsQuery<TValue>(string name, string keyName, IList<TValue> values, int queryBlock = 100)
        {
            Assert(values == null || values.Count == 0, "열 삭제 쿼리작성 메소드에 빈 리스트가 들어옴");

            string query = $"DELETE FROM {name} WHERE {keyName}";
            //Type t = typeof(TValue);
            Type t = values[0].GetType();
            Assert(t == typeof(object), "이게 오브젝트 타입으로 받으면 안될텐데");

            string sum = null;
            for (int i = 0; i < values.Count; i++)
            {
                if (!string.IsNullOrEmpty(sum)) sum += ",";

                string value = ConvertToString(t, values[i]);
                sum += $"{value}";

                if (i != 0 && i % queryBlock == 0)
                {
                    yield return $"{query} IN ({sum})";
                    sum = null;
                }
            }

            if (!string.IsNullOrEmpty(sum))
            {
                yield return $"{query} IN ({sum})";
            }
        }
        //private IEnumerator<string> GetRemoveColumnQuery(string tableName, params string[] columns)
        //{
        //    Assert(columns == null | columns.Length == 0, "제거하려는 컬럼이 0개임");

        //    yield return $"ALTER TABLE {tableName} RENAME TO {tableName}_temp";


        //}

        private void AddReplaceTableQuery(SQLiteTable table, string into, int queryBlock = 1000)
        {
            using (var iter = GetReplaceTableQuery(table, into, queryBlock))
            {
                while (iter.MoveNext())
                {
                    AddQuery(iter.Current.Item1, iter.Current.Item2);
                }
            }
        }
        /// <summary>
        /// 제거는 안함 넣거나 교체만함<br/> 
        /// 완전교체는 <see cref="AddReplaceTableQuery(SQLiteTable, SQLiteTable, int)"/>을 사용
        /// </summary>
        /// <param name="table"></param>
        private void AddUpdateTableQuery(SQLiteTable table, int queryBlock = 100)
        {
            if (table.Count == 0) return;

            using (var iter = GetUpdateTableQuery(table, queryBlock))
            {
                while (iter.MoveNext())
                {
                    AddQuery(iter.Current.Item1, iter.Current.Item2);
                }
            }
        }
        private void AddInsertDataQuery(SQLiteTable table, int queryBlock = 1000)
        {
            using (var iter = GetInsertDataQuery(table, queryBlock))
            {
                while (iter.MoveNext())
                {
                    AddQuery(iter.Current.Item1, iter.Current.Item2);
                }
            }
        }

        private void AddRemoveRowQuery(string name, string keyName, object value)
        {
            string query = $"DELETE FROM {name} WHERE {keyName} ";

            query += $"= {ConvertToString(value.GetType(), value)}";
            AddQuery(query);
        }
        private void AddRemoveRowsQuery<TValue>(string name, string keyName, IList<TValue> values, int queryBlock = 100)
        {
            using (var iter = GetRemoveRowsQuery(name, keyName, values, queryBlock))
            {
                while (iter.MoveNext())
                {
                    AddQuery(iter.Current);
                }
            }
        }

        /// <summary>
        /// 등록된 모든 쿼리를 수행합니다
        /// </summary>
        /// <exception cref="SQLiteExcuteExcpetion"></exception>
        private void InternalExcute()
        {
            if (Queries.Count == 0)
            {
                return;
            }

            OpenConnection();

            using (var transaction = Connection.BeginTransaction())
            {
                int count = Queries.Count;

                using (var cmd = Connection.CreateCommand())
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (!Queries.TryDequeue(out var query)) continue;

                        if (query.Item1.StartsWith("reload:"))
                        {
                            query.Item1 = query.Item1.Replace("reload:", "");

                            if (query.Item1 == "master")
                            {
                                InternalLoadTables();
                            }
                            else InternalLoadTable(query.Item1);
                        }
                        else
                        {
                            OnExcute?.Invoke(query.Item1);
                            cmd.Parameters.Clear();
                            try
                            {
                                $"SQLite 통신 중: {query.Item1}".ToLog();
                                cmd.CommandText = query.Item1;
                                if (query.Item2 != null &&
                                    query.Item2.Length > 0)
                                {
                                    cmd.Parameters.AddRange(query.Item2);
                                }
                                cmd.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                Assert(true, $"통신 중 문제 발생: {ex.Message}: {ex.StackTrace}\n{query}");
#else
                                // TODO : 로그 붙이기
                                throw new SQLiteExcuteExcpetion(query, ex);
#endif
                                CoreSystem.BackgroundThread.Abort();
                                break;

                            }
                        }

                        //
                    } // for end
                }

                transaction.Commit();
            }

            if (VacuumQueries.Count > 0)
            {
                using (var cmd = Connection.CreateCommand())
                {
                    if (VacuumQueries.TryDequeue(out _))
                    {
                        try
                        {
                            cmd.CommandText = "VACUUM";
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Assert(true, $"VACUUM 중 문제 발생: {ex.Message}: {ex.StackTrace}");
#else
                            // TODO : 로그 붙이기
                            throw new SQLiteExcuteExcpetion("VACUUM", ex);
#endif
                            CoreSystem.BackgroundThread.Abort();
                            throw;
                        }
                    }
                }
            }

            CloseConnection();
        }

        #endregion

        #region 통신 안하는 안전한 메소드

        /// <summary>
        /// <para>메모리에 로드된 정보에서 불러오는 메소드</para>
        /// 버전 정보를 받아옵니다. 정보가 없으면 null을 반환합니다
        /// </summary>
        /// <returns></returns>
        public string GetVersion()
        {
            if (!IsValid() || VersionTable.Columns == null
                || VersionTable.Columns.Count == 0 || VersionTable.Columns[0].Values.Count == 0)
            {
                return null;
            }

            return Convert.ToString(VersionTable.Columns[0].Values[0]);
        }
        /// <summary>
        /// <para>메모리에 로드된 정보에서 불러오는 메소드</para>
        /// 이 데이터 테이블이 정상인지 반환합니다
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(DataPath) || !Initialized) return false;
            return true;
        }

        /// <summary>
        /// <para>메모리에 로드된 정보에서 불러오는 메소드</para>
        /// 해당 이름(<paramref name="name"/>)의 테이블이 있는지 반환합니다
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasTable(string name) => HasTable(name, out _);
        /// <inheritdoc cref="HasTable(string)"/>/>
        public bool HasTable(string name, out int index)
        {
            for (int i = 0; i < Tables.Length; i++)
            {
                if (Tables[i].Name == name)
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }
        public SQLiteTable GetTable(string name)
        {
            for (int i = 0; i < Tables.Length; i++)
            {
                if (Tables[i].Name == name)
                {
                    return Tables[i];
                }
            }

            return default;
        }
        /// <summary>
        /// <para>메모리에 로드된 정보에서 불러오는 메소드</para>
        /// 해당 테이블(<paramref name="name"/>)을 가져올 수 있으면 가져옵니다
        /// </summary>
        /// <param name="name"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public bool TryGetTable(string name, out SQLiteTable table)
        {
            for (int i = 0; i < Tables.Length; i++)
            {
                if (Tables[i].Name == name)
                {
                    table = Tables[i];
                    return true;
                }
            }

            table = default;
            return false;
        }
        /// <summary>
        /// <para>메모리에 로드된 정보에서 불러오는 메소드</para>
        /// 해당 테이블(<paramref name="tableName"/>)에서 순번(<paramref name="index"/>)값이 일치하는 열 데이터를 
        /// <see cref="SQLiteTableAttribute"/>가 선언된 구조체에 데이터를 담아서 반환합니다<br/><br/>
        /// 
        /// 비슷한 메소드<br/>
        /// <seealso cref="TryGetTableValueWithPrimary{T}(string, object, out T)"/>:
        /// 입력한 메인 키값과 일치하는 열 데이터를 <see cref="SQLiteTableAttribute"/>가 선언된 구조체에 담아서 반환합니다
        /// </summary>
        /// <typeparam name="T"><see cref="SQLiteTableAttribute"/>가 선언된 구조체 타입</typeparam>
        /// <param name="tableName">테이블의 이름</param>
        /// <param name="index">테이블내의 인덱스 번호</param>
        /// <param name="table"><see cref="SQLiteTableAttribute"/>가 선언된 구조체</param>
        /// <returns></returns>
        public bool TryGetTableValue<T>(string tableName, int index, out T table) where T : struct
        {
            table = default;
            if (!TryGetTable(tableName, out var sqTable))
            {
                //$"SQLite Exception: 이름 ({tableName})을 가진 테이블이 존재하지 않습니다".ToLog();
                return false;
            }

            return sqTable.TryReadLine(index, out table);
        }
        /// <summary>
        /// <para>메모리에 로드된 정보에서 불러오는 메소드</para>
        /// 해당 테이블(<paramref name="tableName"/>)에서
        /// 메인 키(<paramref name="primaryKey"/>)값과 일치하는 열 데이터를 <see cref="SQLiteTableAttribute"/>가 선언된 구조체로 반환합니다<br/><br/>
        /// 
        /// 비슷한 메소드<br/>
        /// <see cref="TryGetTableValue{T}(string, int, out T)"/>: 순번값이 일치하는 열 데이터를 <see cref="SQLiteTableAttribute"/>가 선언된 구조체로 반환합니다
        /// </summary>
        /// <typeparam name="T"><see cref="SQLiteTableAttribute"/>가 선언된 구조체 타입</typeparam>
        /// <param name="tableName"></param>
        /// <param name="primaryKey"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public bool TryGetTableValueWithPrimary<T>(string tableName, object primaryKey, out T table)
            where T : struct
        {
            table = default;
            if (!TryGetTable(tableName, out var sqTable))
            {
                //$"SQLite Exception: 이름 ({tableName})을 가진 테이블이 존재하지 않습니다".ToLog();
                return false;
            }

            return sqTable.TryReadLineWithPrimary(primaryKey, out table);
        }
        /// <summary>
        /// <para>메모리에 로드된 정보에서 불러오는 메소드</para>
        /// 해당 테이블의 정보들을 <see cref="ObDictionary{TKey, TValue}"/> 형태로 변환하여 반환합니다.<br/>
        /// <typeparamref name="TValue"/>는 <see cref="SQLiteTableAttribute"/>가 선언된 구조체 타입이어야 됩니다.
        /// </summary>
        /// <remarks>
        /// 비슷한 메소드<br/>
        /// <seealso cref="TryLoadTableToDictionary{TKey, TValue}(string, IDictionary{TKey, TValue})"/>: 
        /// 일반 딕셔너리에 로드시킵니다
        /// </remarks>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="tableName">읽어올 테이블 이름</param>
        /// <param name="dic">딕셔너리는 먼저 생성되야됩니다</param>
        /// <param name="optional">값을 다 읽고 다음 값을 읽기 직전에 실행할 추가 델리게이트</param>
        /// <returns></returns>
        public bool TryLoadTableToDictionary<TKey, TValue>(string tableName, ObDictionary<TKey, TValue> dic, Action<TKey, TValue> optional = null)
            where TValue : struct, IEquatable<TValue>
        {
            if (TryGetTable($"{tableName}", out var table))
            {
                var primaryKeyInfo = SQLiteDatabaseUtils.GetPrimaryKeyInfo<TValue>();

                for (int i = 0; i < table.Count; i++)
                {
                    if (table.TryReadLine(i, out TValue data))
                    {
                        TKey key;
                        switch (primaryKeyInfo.MemberType)
                        {
                            case MemberTypes.Field:
                                key = SQLiteDatabaseUtils.ConvertSQL<TKey>((primaryKeyInfo as FieldInfo).GetValue(data));
                                break;
                            case MemberTypes.Property:
                                key = SQLiteDatabaseUtils.ConvertSQL<TKey>((primaryKeyInfo as PropertyInfo).GetValue(data));
                                break;
                            default:
                                throw new Exception();
                        }

                        if (dic.ContainsKey(key))
                        {
                            if (!dic[key].Equals(data))
                            {
                                dic[key] = data;
                            }
                        }
                        else
                        {
                            dic.Add(key, data, true);
                        }

                        optional?.Invoke(key, data);
                    }
                }
            }
            else
            {
                //$"SQLite Exception: {tableName} 로드 중 에러, 테이블을 찾을 수 없음".ToLog();
                return false;
            }

            $"{tableName} Loaded: {dic.Count}".ToLog();
            dic.ClearModified();
            return true;
        }
        /// <inheritdoc cref="LoadTableToDictionary{TKey, TValue}(SQLiteTable, IDictionary{TKey, TValue})"/>
        /// <remarks>
        /// 비슷한 메소드<br/>
        /// <seealso cref="TryLoadTableToDictionary{TKey, TValue}(string, ObDictionary{TKey, TValue}, Action{TKey, TValue})"/>
        /// </remarks>
        public bool TryLoadTableToDictionary<TKey, TValue>(string tableName, IDictionary<TKey, TValue> dic)
            where TValue : struct, IEquatable<TValue>
        {
            if (!TryGetTable(tableName, out SQLiteTable table)) return false;

            LoadTableToDictionary(table, dic);
            return true;
        }

        #endregion

        #region 데이터 버전, 검사 및 백업, 마이그레이션,
        /// <summary>
        /// 데이터 백업을 즉시 실행합니다
        /// </summary>
        /// <remarks>
        /// 비슷한 메소드<br/>
        /// <seealso cref="BackupAsync"/>: 데이터 백업을 백그라운드에서 수행합니다
        /// </remarks>
        public void Backup()
        {
            "세이브 데이터 백업 중".ToLog();
            if (File.Exists(Path.Combine(FilePath, $"{FileName}.db.bak")))
            {
                File.Move(Path.Combine(FilePath, $"{FileName}.db.bak"), Path.Combine(FilePath, $"{FileName}.db.old"));
            }
            File.Copy(Path.Combine(FilePath, $"{FileName}.db"), Path.Combine(FilePath, $"{FileName}.db.bak"));
            if (File.Exists(Path.Combine(FilePath, $"{FileName}.db.old")))
            {
                File.Delete(Path.Combine(FilePath, $"{FileName}.db.old"));
            }
            "세이브 데이터 백업 완료".ToLog();
        }
        /// <summary>
        /// 데이터 백업을 백그라운드에서 수행합니다
        /// </summary>
        /// <remarks>
        /// 비슷한 메소드<br/>
        /// <seealso cref="Backup"/>: 데이터 백업을 즉시 실행합니다.
        /// </remarks>
        /// <returns></returns>
        public BackgroundJob BackupAsync()
        {
            CoreSystem.AddBackgroundJob(ExcuteWorker, Backup, out var job);
            return job;
        }
        /// <summary>
        /// 버전 정보를 업데이트합니다 <see cref="Application.version"/>을 넣으세요
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public BackgroundJob UpdateVersionInfo(string version)
        {
            AddVersionInfo(version);
            return Excute();
        }
        /// <inheritdoc cref="InternalCheckIntegrity(string)"/>
        /// <exception cref="SQLiteAssertException"></exception>
        public void CheckIntegrity(string currentVersion)
        {
            Assert(IsValid, false, "정상적으로 불러온 데이터 객체가 아닙니다");

#if UNITY_EDITOR
            //if (!Application.isPlaying)
            //{
            //    InternalCheckIntegrity();
            //    return null;
            //}
            //else
#endif
            {
                //return CoreSystem.AddBackgroundJob(InternalCheckIntegrity);
                InternalCheckIntegrity(currentVersion);
            }
        }

        #endregion

        /// <summary>
        /// !! 위험한 메소드 !!<br/>
        /// 먼저 데이터 읽고 쓰는중인지(<see cref="SQLiteDatabase.IsConnectionOpened"/>) 체크하세요 !!
        /// </summary>
        /// <remarks>
        /// SQLite 통신하여 조건(컬럼(<paramref name="where"/>) == 값(<paramref name="targetValue"/>))에 맞는
        /// 행들을 즉시 검색하여 <see cref="SQLiteTable"/> 테이블로 반환합니다.
        /// </remarks>
        /// <param name="tableName">검색할 테이블</param>
        /// <param name="where">검색할 테이블내 컬럼(행) 이름</param>
        /// <param name="targetValue">검색할 값</param>
        /// <returns></returns>
        public SQLiteTable SearchTable(string tableName, string where, object targetValue)
        {
            OpenConnection();

            List<SQLiteColumn> sqColumns = new List<SQLiteColumn>();

            using (var cmd = Connection.CreateCommand())
            {
                //var tableBuilder = new SQLiteBuilder.TableBuilder(tableName);
                //var valueBuilder = tableBuilder.GetValueBuilder();

                //string sum = valueBuilder.BuildReader();
                string sum = GetReadTableQuery(tableName);
                sum += $" WHERE {where}={targetValue}";

                //cmd.CommandText = valueBuilder.BuildReader();
                cmd.CommandText = sum;

                using (var rdr = cmd.ExecuteReader())
                {
                    int columnCount = rdr.FieldCount;
                    List<string> columns = new List<string>();
                    List<Type> columnTypes = new List<Type>();

                    for (int a = 0; a < columnCount; a++)
                    {
                        columns.Add(rdr.GetName(a));
                        Type t = rdr.GetFieldType(a);
                        if (t == typeof(double)) t = typeof(float);
                        columnTypes.Add(t);

                        //tableBuilder.AddValue(t, columns[a]);

                        SQLiteColumn column =
                                new SQLiteColumn(columnTypes[a], columns[a]);

                        sqColumns.Add(column);
                    }

                    while (rdr.Read())
                    {
                        for (int a = 0; a < sqColumns.Count; a++)
                        {
                            sqColumns[a].Values.Add(rdr.GetValue(a));
                        }
                    }
                }
            }
            CloseConnection();

            SQLiteTable table = new SQLiteTable(tableName, sqColumns);
            return table;
        }
        /// <summary>
        /// !! 위험한 메소드 !!<br/>
        /// 먼저 데이터 읽고 쓰는중인지(<see cref="SQLiteDatabase.IsConnectionOpened"/>) 체크하세요 !!
        /// </summary>
        /// <remarks>
        /// <paramref name="tableName"/> 테이블을 즉시 읽고 메모리에 로드한뒤 반환합니다.
        /// </remarks>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public SQLiteTable LoadTable(string tableName)
        {
            OpenConnection();
            using (var transaction = Connection.BeginTransaction())
            {
                InternalLoadTable(tableName);
                transaction.Commit();
            }
            CloseConnection();

            return GetTable(tableName);
        }

        /// <summary>
        /// 이 Region 에 담긴 메소드들은 <see cref="LoadInMemory"/> 가 True일때 정상 작동하는 메소드들입니다.
        /// </summary>
        #region 비동기 데이터베이스 가공 및 읽기 작업

        /// <inheritdoc cref="AddCheckTempTablesQuery"/>
        public BackgroundJob CheckTempTables()
        {
            AddCheckTempTablesQuery();
            return Excute();
        }

        /// <summary>
        /// 해당 테이블(<paramref name="tableName"/>)을 안전하게 수정할 수 있도록 도와주는 메소드입니다.
        /// 이 메소드로 받은 <paramref name="divertedName"/>으로 <see cref="TryGetTable(string, out SQLiteTable)"/>
        /// 을 사용해 테이블 정보를 받아서 수정하거나, <see cref="SQLiteDatabaseUtils.GetDefaultTable{T}"/>
        /// 을 사용하여 기본 테이블 정보를 받아와서 수정할 수 있습니다.
        /// </summary>
        /// 
        /// <remarks>
        /// 연결된 메소드<br/>
        /// <seealso cref="CloseSafeWriting(string)"/>: 이 메소드로 열은 테이블(<paramref name="divertedName"/>)을 
        /// 안전하게 닫는 메소드입니다.
        /// </remarks>
        /// <param name="tableName">안전하게 작업할 테이블 이름</param>
        /// <param name="withEmpty">빈 테이블로 생성할지 완전 카피하여 생성할지</param>
        /// <param name="divertedName">작업해도 안전한 테이블로 이전된 새로운 테이블 이름</param>
        /// <returns></returns>
        public BackgroundJob OpenSafeWriting(string tableName, bool withEmpty, out string divertedName)
        {
            Assert(!HasTable(tableName), $"이름 ({tableName})을 가진 테이블이 존재하지 않습니다");

            TryGetTable(tableName, out SQLiteTable table);
            divertedName = $"{tableName}_temp";

            // 새로운 테이블을 작성합니다.
            AddQuery(GetCreateTableQuery(divertedName, table.Columns));

            if (!withEmpty)
            {
                string query = $"INSERT INTO {ConvertTableInfoToString(divertedName, table.Columns, true)} " +
                    $"SELECT {ConvertTableInfoToString(divertedName, table.Columns, false)} FROM {tableName}";
                AddQuery(query);
            }

            AddReloadTableQuery(divertedName);
            m_SafeWritingTables.Add(divertedName, tableName);
            return Excute();
        }
        /// <summary>
        /// <see cref="OpenSafeWriting(string, bool, out string)"/>으로 열어서 받은
        /// 테이블(<paramref name="divertedName"/>)을 안전하게 닫습니다.
        /// </summary>
        /// <param name="divertedName"></param>
        /// <returns></returns>
        public BackgroundJob CloseSafeWriting(string divertedName)
        {
            if (!m_SafeWritingTables.ContainsKey(divertedName))
            {
                Assert(!HasTable(divertedName), $"이름 ({divertedName})을 가진 테이블이 존재하지 않습니다");
            }

            string originalName = divertedName.Replace("_temp", "");

            AddQuery($"DROP TABLE {originalName}");
            AddQuery($"ALTER TABLE {divertedName} RENAME TO {originalName}");

            AddReloadAllTablesQuery();
            m_SafeWritingTables.Remove(divertedName);
            return Excute();
        }

        /// <summary>
        /// 메인키(<paramref name="primaryKey"/>)로 선언된 컬럼을 기준으로 일치하는 열을 삭제합니다.<br/>
        /// 열 자체가 삭제됩니다. 데이터만 지우는게 아닙니다.<br/><br/>
        /// 비슷한 메소드<br/>
        /// <seealso cref="DeleteRow(string, KeyValuePair{string, object})"/>: 입력된 컬럼 이름을 기준으로 일치하는 열을 삭제합니다
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKeys"></param>
        /// <returns></returns>
        public BackgroundJob DeleteRow(string tableName, object primaryKey)
        {
            if (!m_SafeWritingTables.ContainsKey(tableName))
            {
                Assert(!HasTable(tableName), $"이름 ({tableName})을 가진 테이블이 존재하지 않습니다");
            }

            TryGetTable(tableName, out SQLiteTable table);

            AddRemoveRowQuery(tableName, table.Columns[0].Name, primaryKey);
            AddReloadTableQuery(tableName);

            return Excute();
        }
        /// <summary>
        /// <paramref name="keyValue"/>의 키값으로 일치하는 이름의 컬럼을 찾아서 
        /// 밸류값이 일치하는 열들을 삭제합니다.<br/>
        /// 열 자체가 삭제됩니다. 데이터만 지우는게 아닙니다.<br/><br/>
        /// 비슷한 메소드<br/>
        /// <seealso cref="DeleteRow(string, object)"/>: 메인키 값을 기준으로 입력된 값과 일치하는 열을 삭제합니다
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public BackgroundJob DeleteRow(string tableName, KeyValuePair<string, object> keyValue)
        {
            if (!m_SafeWritingTables.ContainsKey(tableName))
            {
                Assert(!HasTable(tableName), $"이름 ({tableName})을 가진 테이블이 존재하지 않습니다");
            }

            AddRemoveRowQuery(tableName, keyValue.Key, keyValue.Value);
            AddReloadTableQuery(tableName);

            return Excute();
        }
        /// <summary>
        /// 메인 키로 선언된 컬럼을 기준으로 입력된 리스트(<paramref name="primaryKeys"/>)의 열들을 삭제합니다.<br/>
        /// 열 자체가 삭제됩니다. 데이터만 지우는게 아닙니다.<br/><br/>
        /// 비슷한 메소드<br/>
        /// <seealso cref="DeleteRows{T}(string, string, IList{T})"/>: 행 이름을 기준으로
        /// <see cref="SQLiteTableAttribute"/>가 선언된 구조체로 구성된 리스트와 일치하는 열을 삭제합니다.
        /// </summary>
        /// <param name="tableName">테이블 이름</param>
        /// <param name="primaryKeys">지울 메인 키값 리스트</param>
        /// <param name="queryBlock">한번에 지울 갯수</param>
        /// <returns></returns>
        public BackgroundJob DeleteRows<TKey>(string tableName, IList<TKey> primaryKeys, int queryBlock = 100)
        {
            if (!m_SafeWritingTables.ContainsKey(tableName))
            {
                Assert(!HasTable(tableName), $"이름 ({tableName})을 가진 테이블이 존재하지 않습니다");
            }

            TryGetTable(tableName, out SQLiteTable table);

            if (primaryKeys.Count == 1) return DeleteRow(tableName, primaryKeys[0]);

            AddRemoveRowsQuery(tableName, table.Columns[0].Name, primaryKeys, queryBlock);
            AddReloadTableQuery(tableName);

            return Excute();
        }
        /// <summary>
        /// 행 이름(<paramref name="columnName"/>)을 기준으로 
        /// 리스트(<paramref name="keyValues"/>)와 일치하는 열을 삭제합니다.<br/>
        /// 열 자체가 삭제됩니다. 데이터만 지우는게 아닙니다.
        /// </summary>
        /// <remarks>
        /// 비슷한 메소드<br/>
        /// <seealso cref="DeleteRows{TKey}(string, IList{TKey}, int)"/>: 메인 키로 선언된 컬럼을 기준으로 
        /// 입력된 리스트(<paramref name="primaryKeys"/>)의 열들을 삭제합니다.
        /// </remarks>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="keyValues"></param>
        /// <returns></returns>
        public BackgroundJob DeleteRows<TValue>(string tableName, string columnName, IList<TValue> keyValues, int queryBlock = 100)
        {
            if (!m_SafeWritingTables.ContainsKey(tableName))
            {
                Assert(!HasTable(tableName), $"이름 ({tableName})을 가진 테이블이 존재하지 않습니다");
            }

            if (keyValues.Count == 1) return DeleteRow(tableName, new KeyValuePair<string, object>(columnName, keyValues[0]));

            AddRemoveRowsQuery(tableName, columnName, keyValues, queryBlock);
            AddReloadTableQuery(tableName);

            return Excute();
        }

        /// <summary>
        /// 해당 테이블 데이터 열들을 데이터의 이름에 넣습니다.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="queryBlock"></param>
        /// <returns></returns>
        public BackgroundJob AddRows(SQLiteTable data, int queryBlock = 1000)
        {
            Assert(data.Count == 0, $"{data.Name}에 넣을 데이터가 하나도 없음");

            AddInsertDataQuery(data, queryBlock);
            return Excute();
        }

        /// <inheritdoc cref="InternalLoadTables"/>
        /// <remarks>
        /// 비슷한 메소드<br/>
        /// <seealso cref="ReloadTable(string)"/>: 단일 테이블을 다시 불러옵니다
        /// </remarks>
        /// <returns></returns>
        public BackgroundJob ReloadAllTables()
        {
            Assert(IsValid, false, "정상 로드되지않은 데이터베이스에서 ReloadAllTables");

            AddReloadAllTablesQuery();
            return Excute();
        }
        /// <inheritdoc cref="InternalLoadTable(string)"/>
        /// <remarks>
        /// 비슷한 메소드<br/>
        /// <seealso cref="ReloadAllTables"/>: 모든 테이블 정보를 다시 불러옵니다.
        /// </remarks>
        /// <param name="name"></param>
        /// <returns></returns>
        public BackgroundJob ReloadTable(string name)
        {
            Assert(!HasTable(name), $"이름 ({name})을 가진 테이블이 존재하지 않습니다");

            AddReloadTableQuery(name);
            return Excute();
        }
        /// <summary>
        /// 새로운 데이터 테이블을 생성하고 저장합니다
        /// </summary>
        /// 
        /// <remarks>
        /// 비슷한 메소드<br/>
        /// <seealso cref="CreateTable{T}(string)"/>: <see cref="SQLiteTableAttribute"/>가 선언된 타입의 구조체를
        /// 기반으로 새로운 테이블을 생성합니다.
        /// </remarks>
        /// 
        /// <param name="table"></param>
        /// <returns></returns>
        public BackgroundJob CreateTable(SQLiteTable table, int queryBlock = 100)
        {
            Assert(HasTable(table.Name), $"이름 ({table.Name})을 가진 테이블이 이미 존재합니다");

            // 새로운 테이블을 작성합니다.
            AddQuery(GetCreateTableQuery(table));

            // 새로운 데이터를 넣습니다.
            AddUpdateTableQuery(table, queryBlock);

            // 데이터를 메모리에 로드시킵니다.
            AddReloadTableQuery(table.Name);

            return Excute();
        }
        /// <summary>
        /// 해당 이름(<paramref name="name"/>)을 가지고 해당 컬럼(<paramref name="columns"/>)들을 가진 
        /// 새로운 테이블을 생성하고 저장합니다.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public BackgroundJob CreateTable(string name, IList<KeyValuePair<Type, string>> columns)
        {
            Assert(HasTable(name), $"이름 ({name})을 가진 테이블이 이미 존재합니다");
            Assert(columns.Count == 0, "테이블을 생성하려면 반드시 하나 이상의 컬럼 정보를 가지고 있어야됩니다");

            //SQLiteBuilder.TableBuilder tableBuilder = new SQLiteBuilder.TableBuilder(name);
            //for (int i = 0; i < columns.Count; i++)
            //{
            //    tableBuilder.AddValue(columns[i].Key, columns[i].Value);
            //}

            //AddQuery(tableBuilder.BuildCreateTable());
            AddQuery(GetCreateTableQuery(name, columns));
            AddReloadTableQuery(name);

            return Excute();
        }
        /// <summary>
        /// <see cref="SQLiteTableAttribute"/>가 선언된 타입의 구조체를 기반으로 새로운 테이블을 생성합니다.
        /// </summary>
        /// 
        /// <remarks>
        /// 비슷한 메소드<br/>
        /// <seealso cref="CreateTable(SQLiteTable)"/>: 입력된 테이블 정보를 바탕으로 새로운 테이블을 만듭니다
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public BackgroundJob CreateTable<T>(string tableName) where T : struct
        {
            Assert(HasTable(tableName), $"이름 ({tableName})을 가진 테이블이 이미 존재합니다");
            Assert(typeof(T).GetCustomAttribute<SQLiteTableAttribute>() == null, $"타입 ({typeof(T).Name})은 SQLiteTable 구조체가 아닙니다");

            SQLiteTable table = ToSQLiteTable(tableName, new List<T>()
            {
                SQLiteDatabaseUtils.GetDefaultTable<T>()
            });
            AddQuery(GetCreateTableQuery(table));

            AddReloadTableQuery(tableName);
            return Excute();
        }
        /// <summary>
        /// <para>
        /// !! 포함된 정보가 많을수록 오래걸립니다 <br/>
        /// 테이블을 새로운 정보로 완전히 교체합니다.
        /// </para>
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public BackgroundJob ReplaceTable(SQLiteTable table, string into, int queryBlock = 100)
        {
            // 새로운 정보를 넣습니다
            AddReplaceTableQuery(table, into, queryBlock);

            return Excute();
        }
        /// <summary>
        /// <para>!! 제거는 하지않습니다.
        /// 제거는 <see cref="DeleteRow(string, object)"/> 
        /// 혹은,<br/> <see cref="DeleteColumn(string, string, string[])"/> 을 사용하세요 !!</para>
        ///
        /// <see cref="SQLiteTableAttribute"/>가 선언된 구조체를 기반으로,
        /// 전체 테이블 교체가 아닌, 입력한 테이블의 컬럼이 있거나 없을 경우에 추가하거나 교체합니다.
        /// 전체 테이블 교체를 원하면 <seealso cref="ReplaceTable(SQLiteTable)"/> 을 사용하세요<br/><br/>
        /// 비슷한 메소드<br/>
        /// <seealso cref="UpdateTable(SQLiteTable)"/>: 동일한 기능을 수행합니다
        /// </summary>
        /// <typeparam name="T"><see cref="SQLiteTableAttribute"/>가 선언된 객체 타입</typeparam>
        /// <param name="tableName">목표 테이블의 이름</param>
        /// <param name="target"><see cref="SQLiteTableAttribute"/>이 선언된 객체</param>
        public BackgroundJob UpdateTable<T>(string tableName, T target) where T : struct
        {
            if (!m_SafeWritingTables.ContainsKey(tableName))
            {
                Assert(!HasTable(tableName), $"이름 ({tableName})을 가진 테이블이 존재하지 않습니다");
            }
            Assert(typeof(T).GetCustomAttribute<SQLiteTableAttribute>() == null, $"타입 ({typeof(T).Name})은 SQLiteTable 구조체가 아닙니다");

            SQLiteTable table = ToSQLiteTable(tableName, new T[] { target });
            return UpdateTable(table);
        }
        /// <summary>
        /// <para>!! 제거는 하지않습니다.
        /// 제거는 <see cref="DeleteRow(string, object)"/> 
        /// 혹은,<br/> <see cref="DeleteColumn(string, string, string[])"/> 을 사용하세요 !!</para>
        ///
        /// 전체 테이블 교체가 아닌, 입력한 테이블의 컬럼이 있거나 없을 경우에 추가하거나 교체합니다.<br/>
        /// 전체 테이블 교체를 원하면 <seealso cref="ReplaceTable(SQLiteTable)"/> 을 사용하세요<br/><br/>
        /// 비슷한 메소드<br/>
        /// <seealso cref="UpdateTable{T}(string, T)"/>: 동일한 기능을 수행합니다
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public BackgroundJob UpdateTable(SQLiteTable table, int queryBlock = 100)
        {
            if (!m_SafeWritingTables.ContainsKey(table.Name))
            {
                Assert(!HasTable(table.Name), $"이름 ({table.Name})을 가진 테이블이 존재하지 않습니다");
            }
            Assert(table.Count == 0, $"테이블 ({table.Name})에서 업데이트될 갯수가 0개 이므로 업데이트 할 수 없습니다.");

            AddUpdateTableQuery(table, queryBlock);
            AddReloadTableQuery(table.Name);

            return Excute();
        }
        /// <summary>
        /// 해당 테이블(<paramref name="tableName"/>)을 
        /// 새로운 이름(<paramref name="newTableName"/>)으로 그대로 복사하여 생성합니다.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="newTableName"></param>
        /// <returns></returns>
        public BackgroundJob CopyTable(string tableName, string newTableName)
        {
            TryGetTable(tableName, out SQLiteTable table);

            Assert(table.IsValid, false, $"이름 ({tableName})을 가진 테이블이 존재하지 않습니다");
            Assert(table.IsValid, false, $"이름 ({newTableName})을 가진 테이블이 이미 존재합니다. 복사 에러");

            SQLiteTable copied = table.Rename(newTableName);
            AddQuery(GetCreateTableQuery(copied));
            string query = $"INSERT INTO {ConvertTableInfoToString(copied, true)} " +
                $"SELECT {ConvertTableInfoToString(copied, false)} FROM {tableName}";
            AddQuery(query);

            AddReloadTableQuery(newTableName);

            return Excute();
        }
        /// <summary>
        /// <para>!! 테이블 정보를 전부 삭제하는 위험한 작업입니다 !!<br/>
        /// !! <see cref="ReloadAllTables"/> 을 이 작업 다음에 수행해야됩니다 !!</para>
        /// 해당 테이블(<paramref name="tableName"/>)을 삭제합니다
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public BackgroundJob DropTable(string tableName)
        {
            Assert(!HasTable(tableName), $"이름 ({tableName})을 가진 테이블이 존재하지 않습니다");

            string query = $@"DROP TABLE {tableName}";
            AddQuery(query);
            return Excute();
        }
        /// <inheritdoc cref="DropTable(string)"/>
        public bool TryDropTable(string tableName, out BackgroundJob job)
        {
            if (HasTable(tableName))
            {
                string query = $@"DROP TABLE {tableName}";
                AddQuery(query);
            }
            else
            {
                job = Excute();
                return false;
            }

            job = Excute();
            return true;
        }
        /// <summary>
        /// 해당 테이블(<paramref name="tableName"/>)의 이름을 원하는 이름(<paramref name="targetName"/>)으로 변경합니다
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="targetName"></param>
        /// <returns></returns>
        public BackgroundJob RenameTable(string tableName, string targetName)
        {
            Assert(!HasTable(tableName), $"이름 ({tableName})을 가진 테이블이 존재하지 않습니다");

            AddQuery($"ALTER TABLE {tableName} RENAME TO {targetName}");
            AddReloadTableQuery(targetName);
            return Excute();
        }

        /// <summary>
        /// 해당 테이블(<paramref name="tableName"/>)에 컬럼(<paramref name="name"/>)을 추가합니다
        /// </summary>
        /// <param name="tableName">추가할 컬럼의 테이블 이름</param>
        /// <param name="t">추가할 컬럼의 타입</param>
        /// <param name="name">추가할 컬럼의 이름</param>
        public BackgroundJob AddColumn(string tableName, Type t, string name)
        {
            Assert(!HasTable(tableName), $"이름 ({tableName})을 가진 테이블이 존재하지 않습니다");

            //SQLiteBuilder.TableBuilder tableBuilder = new SQLiteBuilder.TableBuilder(tableName);
            //AddQuery(tableBuilder.BuildAddColumn(t, name));
            AddQuery(GetAddColumnQuery(tableName, new KeyValuePair<Type, string>(t, name)));

            AddReloadTableQuery(tableName);
            return Excute();
        }
        /// <summary>
        /// 해당 컬럼(<paramref name="name"/>)을 지웁니다
        /// </summary>
        /// <param name="tableName">지울 컬럼의 테이블 이름</param>
        /// <param name="name">지울 컬럼의 이름</param>
        /// <param name="vs">추가로 지울 컬럼들의 이름들</param>
        public BackgroundJob DeleteColumn(string tableName, string name, params string[] vs)
        {
            TryGetTable(tableName, out var table);
            Assert(table.IsValid, false, $"이름 ({tableName})을 가진 테이블이 존재하지 않습니다");

            SQLiteBuilder.TableBuilder tableBuilder = new SQLiteBuilder.TableBuilder(tableName);
            for (int i = 0; i < table.Columns.Count; i++)
            {
                tableBuilder.AddValue(table.Columns[i].Type, table.Columns[i].Name);
            }

            var iter = tableBuilder.BuildRemoveColumn(name, vs);
            while (iter.MoveNext())
            {
                if (string.IsNullOrEmpty(iter.Current)) continue;

                AddQuery(iter.Current);
            }

            AddReloadTableQuery(tableName);
            return Excute();
        }

        #endregion

        /// <summary>
        /// <para>!! 등록된 쿼리는 <see cref="Excute"/> 되야됩니다 !!</para>
        /// <see cref="SQLiteDatabase"/> 통신을 위한 쿼리문을 추가합니다
        /// </summary>
        /// <param name="query"></param>
        public void AddQuery(string query, SQLiteParameter[] parameters = null)
        {
            //var job = new SQLiteQueryJob(query);
            Queries.Enqueue((query, parameters));
            //return job;
        }

        /// <inheritdoc cref="InternalExcute"/>
        /// <remarks>
        /// <para>!! 에디터에서 실행은 <see cref="ExcuteEditor"/> 을 사용하세요 !!</para>
        /// 연관 메소드<br/>
        /// <seealso cref="AddQuery(string)"/>: 쿼리문을 추가합니다.
        /// </remarks>
        public BackgroundJob Excute()
        {
            CoreSystem.AddBackgroundJob(ExcuteWorker, InternalExcute, out var job);
            return job;
        }

#if UNITY_EDITOR
        /// <summary>
        /// <para>!! 런타임에서는 <see cref="Excute"/> 를 사용하세요 !!</para>
        /// 에디터 실행 전용 메소드입니다.
        /// </summary>
        public void ExcuteEditor()
        {
            InternalExcute();
        }
#endif

        #region Utils

        /// <summary>
        /// 디버그용 메소드
        /// </summary>
        /// <param name="isTrue"></param>
        /// <param name="log"></param>
        public static void Assert(bool isTrue, string log)
        {
            if (isTrue)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                throw new SQLiteAssertException(log);
#else
                // TODO : 로그 붙이기
                throw new SQLiteAssertException(log);
#endif
            }
        }
        public static void Assert(Func<bool> func, bool isTrue, string log)
        {
            if (func.Invoke() == isTrue)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                throw new SQLiteAssertException(func.Method, isTrue, log);
#else
                // TODO : 로그 붙이기
                throw new SQLiteAssertException(func.Method, isTrue, log);
#endif
            }
        }
        /// <inheritdoc cref="SQLiteDatabaseUtils.GetDefaultTable{T}"/>
        public static T GetDefaultTable<T>() where T : struct
        {
            return SQLiteDatabaseUtils.GetDefaultTable<T>();
        }

        /// <summary>
        /// DB에 넣을떄용
        /// </summary>
        /// <param name="objType"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string ConvertToString(Type objType, object obj)
        {
            Assert(objType == null, "ConvertToString에서 오브젝트 타입이 왜 널이지");

            string sum = null;
            if (objType.IsArray)
            {
                string temp = SQLiteDatabaseUtils.ParseArrayToSQL(obj as IList);
                sum += $@"'{temp}'";
            }
            else if (objType.GenericTypeArguments != null && objType.GenericTypeArguments.Length > 0)
            {
                string temp = SQLiteDatabaseUtils.ParseArrayToSQL(obj as IList);
                sum += $@"'{temp}'";
            }
            else if (objType == typeof(string) || objType == typeof(Vector3))
            {
                object temp = SQLiteDatabaseUtils.ConvertSQL<string>(obj);
                Assert(temp == null, $"ConvertToString에서 {objType}, 1 널");
                sum += $@"'{temp}'";
            }
            else if (objType.IsEnum || objType == typeof(bool))
            {
                object temp = SQLiteDatabaseUtils.ConvertSQL(typeof(int), obj);
                Assert(temp == null, $"ConvertToString에서 {objType}, 2 널");
                sum += temp.ToString();
            }
            else
            {
                object temp = SQLiteDatabaseUtils.ConvertSQL(objType, obj);
                Assert(temp == null, $"ConvertToString에서 {objType}, 3 널");
                sum += temp.ToString();
            }
            return sum;
        }
        private static string ConvertColumnInfoToString(SQLiteColumn column, bool isUnique = false)
        {
            string properties = $"{column.Name}";
            string t;
            if (column.Type == typeof(byte[]) || column.Type == typeof(SQLiteBlob))
            {
                t = "BLOB";
            }
            else if (column.Type == typeof(string) || column.Type == typeof(Vector3) ||
                column.Type.IsArray || column.Type.GenericTypeArguments.Length > 0)
            {
                t = "TEXT";
            }
            else if (column.Type == typeof(double) || column.Type == typeof(float) ||
                column.Type == typeof(decimal))
            {
                t = "REAL";
            }
            else
            {
                t = "INTEGER";
            }
            properties += $" {t}";

            if (isUnique) properties += $" NOT NULL UNIQUE";

            return properties;
        }
        private static string ConvertColumnInfoToString(KeyValuePair<Type, string> column, bool isUnique = false)
        {
            string properties = $"{column.Value}";
            string t;
            if (column.Key == typeof(byte[]) || column.Key == typeof(SQLiteBlob))
            {
                t = "BLOB";
            }
            else if (column.Key == typeof(string) || column.Key == typeof(Vector3) ||
                column.Key.IsArray || column.Key.GenericTypeArguments.Length > 0)
            {
                t = "TEXT";
            }
            else if (column.Key == typeof(double) || column.Key == typeof(float) ||
                column.Key == typeof(decimal))
            {
                t = "REAL";
            }
            else
            {
                t = "INTEGER";
            }
            properties += $" {t}";

            if (isUnique) properties += $" NOT NULL UNIQUE";

            return properties;
        }
        private static string ConvertTableInfoToString(SQLiteTable data, bool withName, string namePrefix = "")
        {
            string info = null;

            if (withName) info = $"{data.Name} (";
            info += $"{namePrefix}{data.Columns[0].Name}";

            for (int i = 1; i < data.Columns.Count; i++)
            {
                info += $", {namePrefix}{data.Columns[i].Name}";
            }
            return withName ? $"{info})" : info;
        }
        private static string ConvertTableInfoToString(string tableName, IList<SQLiteColumn> columns, bool withName)
        {
            string info = null;

            if (withName) info = $"{tableName} (";
            info += $"{columns[0].Name}";

            for (int i = 1; i < columns.Count; i++)
            {
                info += $", {columns[i].Name}";
            }
            return withName ? $"{info})" : info;
        }

        /// <summary>
        /// 해당 테이블을 <see cref="SQLiteTableAttribute"/>가
        /// 선언된 구조체로 이루어진 <paramref name="dic"/> 딕셔너리에 로드시킵니다.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"><see cref="SQLiteTableAttribute"/>가 선언된 구조체</typeparam>
        /// <param name="table"></param>
        /// <param name="dic">딕셔너리는 인스턴스가 이미 생성되어있어야합니다</param>
        public static void LoadTableToDictionary<TKey, TValue>(SQLiteTable table, IDictionary<TKey, TValue> dic)
            where TValue : struct, IEquatable<TValue>
        {
            var primaryKeyInfo = SQLiteDatabaseUtils.GetPrimaryKeyInfo<TValue>();

            for (int i = 0; i < table.Count; i++)
            {
                if (table.TryReadLine(i, out TValue data))
                {
                    TKey key;
                    switch (primaryKeyInfo.MemberType)
                    {
                        case MemberTypes.Field:
                            key = SQLiteDatabaseUtils.ConvertSQL<TKey>((primaryKeyInfo as FieldInfo).GetValue(data));
                            break;
                        case MemberTypes.Property:
                            key = SQLiteDatabaseUtils.ConvertSQL<TKey>((primaryKeyInfo as PropertyInfo).GetValue(data));
                            break;
                        default:
                            throw new Exception();
                    }

                    if (dic.ContainsKey(key))
                    {
                        if (!dic[key].Equals(data))
                        {
                            dic[key] = data;
                        }
                    }
                    else
                    {
                        dic.Add(key, data);
                    }
                }
            }
        }
        /// <summary>
        /// <para>!! <see cref="SQLiteTableAttribute"/>가 선언된 구조체들로 구성된 리스트를
        /// <paramref name="tableDatas"/>에 넣어야됩니다. 
        /// <see cref="SQLiteTable"/>를 넣지마세요 !!</para>
        /// 
        /// 입력된 데이터 리스트(<paramref name="tableDatas"/>)를 가지고, 입력된 테이블 이름(<paramref name="tableName"/>)을 가진 
        /// <see cref="SQLiteTable"/> 테이블로 변환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableName">테이블 이름</param>
        /// <param name="tableDatas">테이블 아이템들</param>
        /// <returns></returns>
        public static SQLiteTable ToSQLiteTable<T>(string tableName, IList<T> tableDatas)
            where T : struct
        {
            Assert(typeof(T).GetCustomAttribute<SQLiteTableAttribute>() == null, $"객체 타입 ({typeof(T).Name})은 SQLite 데이터 객체가 아닙니다");

            List<MemberInfo> members = null;
            SQLiteColumn[] columns = null;

            for (int a = 0; a < tableDatas.Count; a++)
            {
                if (members == null)
                {
                    members = SQLiteDatabaseUtils.GetMembers(tableDatas[a].GetType());
                    columns = new SQLiteColumn[members.Count];
                    for (int i = 0; i < members.Count; i++)
                    {
                        Type memberType = null;
                        switch (members[i].MemberType)
                        {
                            case MemberTypes.Field:
                                memberType = (members[i] as FieldInfo).FieldType;
                                break;
                            case MemberTypes.Property:
                                memberType = (members[i] as PropertyInfo).PropertyType;
                                break;
                        }
                        if (memberType == null) continue;

                        if (memberType == typeof(Vector3))
                        {
                            memberType = typeof(string);
                        }

                        columns[i] = new SQLiteColumn(memberType, members[i].Name);
                    }
                }

                for (int i = 0; i < columns.Length; i++)
                {
                    switch (members[i].MemberType)
                    {
                        case MemberTypes.Field:
                            FieldInfo field = members[i] as FieldInfo;

                            columns[i].Values.Add(field.GetValue(tableDatas[a]));

                            break;
                        case MemberTypes.Property:
                            PropertyInfo property = members[i] as PropertyInfo;

                            columns[i].Values.Add(property.GetValue(tableDatas[a]));
                            break;
                    }
                }
            }

            SQLiteTable table = new SQLiteTable(tableName, (columns != null ? columns.ToList() : new List<SQLiteColumn>()));
            return table;
        }
        public static SQLiteTable ToSQLiteTable(string tableName, IList<KeyValuePair<Type, string>> columns)
        {
            SQLiteColumn[] newColumns = new SQLiteColumn[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                newColumns[i] = new SQLiteColumn(columns[i].Key, columns[i].Value);
            }

            return new SQLiteTable(tableName, newColumns);
        }

        public void UsedOnlyForAOTCodeGeneration()
        {
            DeleteRows<int>(null, null);
            DeleteRows<string>(null, null);
            DeleteRows<Vector3>(null, null);
            DeleteRows<ThreadSafe.Vector3>(null, null);

            DeleteRows<int>(null, null, null);
            DeleteRows<float>(null, null, null);
            DeleteRows<double>(null, null, null);
            DeleteRows<long>(null, null, null);
            DeleteRows<ulong>(null, null, null);
            DeleteRows<bool>(null, null, null);
            DeleteRows<string>(null, null, null);
            DeleteRows<object>(null, null, null);
            DeleteRows<Vector3>(null, null, null);
            DeleteRows<ThreadSafe.Vector3>(null, null, null);

            AddRemoveRowsQuery<int>(null, null, null);
            AddRemoveRowsQuery<string>(null, null, null);
            AddRemoveRowsQuery<Vector3>(null, null, null);
            AddRemoveRowsQuery<ThreadSafe.Vector3>(null, null, null);

            throw new InvalidOperationException();
        }

        #endregion
    }
}