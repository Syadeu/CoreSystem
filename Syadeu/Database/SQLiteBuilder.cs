using Syadeu.Extentions.EditorUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Syadeu.Database
{
    public sealed class SQLiteBuilder
    {
        public enum IFLogic
        {
            NONE,

            EXISTS,
            NOT_EXISTS,
        }

        #region Builders

        /// <summary>
        /// 테이블의 쿼리문을 작성하는 빌더 클래스입니다
        /// </summary>
        public class TableBuilder
        {
            private string m_Property = null;

            /// <summary>
            /// 이 테이블의 이름
            /// </summary>
            public string m_Name = null;
            /// <summary>
            /// 이 테이블이 현재 가지고있는 컬럼들
            /// </summary>
            public readonly List<(Type, string)> m_Values = new List<(Type, string)>();

            public TableBuilder(string name)
            {
                m_Name = name;
            }
            /// <summary>
            /// 컬럼을 추가합니다
            /// </summary>
            /// <param name="t"></param>
            /// <param name="valueName"></param>
            /// <returns></returns>
            public TableBuilder AddValue(Type t, string valueName)
            {
                m_Values.Add((t, valueName));
                return this;
            }

            /// <summary>
            /// 테이블 생성 쿼리문을 반환합니다
            /// </summary>
            /// <returns></returns>
            public string BuildCreateTable(IFLogic logic = IFLogic.NONE)
            {
                string property = $@"{m_Name}(";
                for (int i = 0; i < m_Values.Count; i++)
                {
                    string t = ConvertType(m_Values[i].Item1);
                    string sum = $@"{m_Values[i].Item2} {t}";

                    if (i == 0)
                    {
                        sum += @" PRIMARY KEY";
                    }
                    if (i + 1 < m_Values.Count)
                    {
                        sum += @", ";
                    }
                    property += sum;
                }
                property += @")";

                string query = @"CREATE TABLE";
                if (logic != IFLogic.NONE)
                {
                    query += $@" {ConvertLogic(logic)}";
                }
                query += $@" {property}";

                return query;
            }
            /// <summary>
            /// 입력된 컬럼으로 구성된 테이블 정보 쿼리를 반환합니다 없으면 전부 반환
            /// </summary>
            /// <param name="values"></param>
            /// <returns></returns>
            public string GetProperties(params string[] values)
            {
                if (values == null || values.Length == 0)
                {
                    if (string.IsNullOrEmpty(m_Property))
                    {
                        m_Property = $@"{m_Name}({GetAllValues()})";
                        //for (int i = 0; i < m_Values.Count; i++)
                        //{
                        //    m_Property += $@"{m_Values[i].Item2}";
                        //    if (i + 1 < m_Values.Count)
                        //    {
                        //        m_Property += @", ";
                        //    }
                        //}
                        //m_Property += @")";
                    }

                    return m_Property;
                }
                else
                {
                    string sum = $@"{m_Name}(";
                    for (int i = 0; i < values.Length; i++)
                    {
                        bool error = true;
                        for (int a = 0; a < m_Values.Count; a++)
                        {
                            if (values[i] == m_Values[i].Item2)
                            {
                                error = false;
                                break;
                            }
                        }

                        if (error)
                        {
                            $"SQLite Exception :: {values[i]}와 일치하는 값이 테이블({m_Name})에 없어서 패스됨".ToLog();
                            continue;
                        }

                        sum += $@"{values[i]}";
                        if (i + 1 < values.Length)
                        {
                            sum += @", ";
                        }
                    }
                    sum += @")";

                    return sum;
                }
            }
            private string GetAllValues()
            {
                string sum = null;
                for (int i = 0; i < m_Values.Count; i++)
                {
                    sum += $@"{m_Values[i].Item2}";
                    if (i + 1 < m_Values.Count)
                    {
                        sum += @", ";
                    }
                }
                return sum;
            }

            /// <summary>
            /// 이 테이블을 기반으로하는 컬럼 빌더 클래스를 생성 반환합니다
            /// </summary>
            /// <returns></returns>
            public ValueBuilder GetValueBuilder()
            {
                return new ValueBuilder(this);
            }

            #region After Build

            /// <summary>
            /// 해당 컬럼이 존재하는지 체크합니다
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public bool HasColumn(string name)
            {
                for (int i = 0; i < m_Values.Count; i++)
                {
                    if (m_Values[i].Item2 == name) return true;
                }

                return false;
            }

            public string BuildReader()
            {
                string sum = $@"SELECT * FROM {m_Name}";
                return sum;
            }

            /// <summary>
            /// 테이블을 생성한뒤 추가로 컬럼을 추가하려할때 필요한 쿼리문을 작성합니다
            /// </summary>
            /// <returns></returns>
            public string BuildAddColumn(Type t, string name, IFLogic logic = IFLogic.NONE)
            {
                //if (HasColumn(name))
                //{
                //    "SQLite Exception :: 같은 이름을 가진 컬럼이 이미 존재합니다".ToLog();
                //    return null;
                //}

                string sum = $@"ALTER TABLE {m_Name} ADD COLUMN";
                if (logic != IFLogic.NONE)
                {
                    sum += $@" {ConvertLogic(logic)}";
                }

                sum += $@"{name} {ConvertType(t)}";
                AddValue(t, name);
                return sum;
            }
            /// <summary>
            /// 테이블을 생성한뒤 테이블의 이름을 바꾸려할때 필요한 쿼리문을 작성합니다.
            /// </summary>
            /// <param name="to"></param>
            /// <returns></returns>
            public string BuildRenameTable(string to)
            {
                string sum = $@"ALTER TABLE {m_Name} RENAME TO {to}";
                m_Name = to;
                return sum;
            }
            /// <summary>
            /// 테이블을 생성한뒤 테이블내 컬럼 이름을 바꾸려할때 필요한 쿼리문을 작성합니다.
            /// </summary>
            /// <param name="original"></param>
            /// <param name="to"></param>
            /// <returns></returns>
            public string BuildRenameColumn(string original, string to)
            {
                for (int i = 0; i < m_Values.Count; i++)
                {
                    if (m_Values[i].Item2 == original)
                    {
                        m_Values[i] = (m_Values[i].Item1, to);
                        return $@"ALTER TABLE {m_Name} RENAME COLUMN {original} TO {to}";
                    }
                }

                "SQLite Exception :: 해당 이름을 가진 컬럼은 존재하지않음".ToLog();
                return null;
            }

            /// <summary>
            /// 해당 컬럼 제거 시퀀스를 받아옵니다
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public IEnumerator<string> BuildRemoveColumn(string name, params string[] vs)
            {
                //if (!HasColumn(name))
                //{
                //    $"SQLite Exception :: 해당 이름({name})을 가진 컬럼은 존재하지않음".ToLog();
                //    yield break;
                //}
                //foreach (var item in vs)
                //{
                //    if (!HasColumn(item))
                //    {
                //        $"SQLite Exception :: 해당 이름({item})을 가진 컬럼은 존재하지않음".ToLog();
                //        yield break;
                //    }
                //}

                string query = $@"ALTER TABLE {m_Name} RENAME TO {m_Name}_old";
                yield return query;

                for (int i = 0; i < m_Values.Count; i++)
                {
                    if (m_Values[i].Item2 == name)
                    {
                        m_Values.RemoveAt(i);
                        i--;
                        continue;
                    }
                    if (vs != null && vs.Contains(m_Values[i].Item2))
                    {
                        m_Values.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                query = BuildCreateTable();
                yield return query;

                m_Property = null;
                query = $@"INSERT INTO {GetProperties()} SELECT {GetAllValues()} FROM {m_Name}_old";
                yield return query;

                query = $@"DROP TABLE {m_Name}_old";
                yield return query;
            }

            #endregion
        }
        /// <summary>
        /// 컬럼의 쿼리문을 작성하는 빌더 클래스입니다
        /// </summary>
        public class ValueBuilder
        {
            public enum InsertFlag
            {
                /// <summary>
                /// 넣기
                /// </summary>
                INSERT = 1 << 0,

                /// <summary>
                /// Row 교체 옵션, 무조건 해당 Row의 모든 데이터가 입력되야됨
                /// </summary>
                REPLACE = 1 << 1,
                /// <summary>
                /// Column 업데이트 옵션, 무조건 해당 컬럼이 존재해야됨
                /// </summary>
                UPDATE = 1 << 2,

                Insert_Replace = INSERT | REPLACE,
                Insert_Update = INSERT | UPDATE
            }

            /// <summary>
            /// 현재 연결된 테이블빌더
            /// </summary>
            private TableBuilder m_Table = null;
            private string m_Query = null;

            /// <summary>
            /// 작성하려는 타겟 컬럼 값들
            /// </summary>
            public readonly Dictionary<string, object> Values = new Dictionary<string, object>();

            public ValueBuilder(TableBuilder table)
            {
                m_Table = table;
            }

            private string ConvertFlag(InsertFlag insertFlag)
            {
                string sum = null;
                if (insertFlag.HasFlag(InsertFlag.INSERT))
                {
                    sum = "INSERT";
                }

                if (insertFlag.HasFlag(InsertFlag.REPLACE))
                {
                    if (!string.IsNullOrEmpty(sum))
                    {
                        sum += " OR ";
                    }
                    sum += "REPLACE";
                }
                else if (insertFlag.HasFlag(InsertFlag.UPDATE))
                {
                    if (!string.IsNullOrEmpty(sum))
                    {
                        sum += " OR ";
                    }
                    sum += "UPDATE";
                }

                return sum;
            }

            /// <summary>
            /// 컬럼, 데이터 페어를 추가합니다.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="where"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public ValueBuilder AddValue<T>(string where, T value) where T : struct, IConvertible
            {
                if (Values.ContainsKey(where))
                {
                    "SQLite Exception :: 추가하려는 키값이 중복되어 덮어씌워짐".ToLog();
                    Values[where] = value;
                }
                else Values.Add(where, value);
                return this;
            }
            /// <summary>
            /// 컬럼, 데이터 페어를 추가합니다.
            /// </summary>
            /// <param name="where"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public ValueBuilder AddValue(string where, string value)
            {
                if (Values.ContainsKey(where))
                {
                    "SQLite Exception :: 추가하려는 키값이 중복되어 덮어씌워짐".ToLog();
                    Values[where] = value;
                }
                else Values.Add(where, value);
                return this;
            }

            /// <summary>
            /// Insert 쿼리문을 작성합니다
            /// </summary>
            /// <returns></returns>
            public string BuildInsert(InsertFlag insertFlag = InsertFlag.INSERT | InsertFlag.REPLACE)
            {
                string[] valueKeys = Values.Keys.ToArray();
                m_Query = $@"{ConvertFlag(insertFlag)} INTO {m_Table.GetProperties(valueKeys)} VALUES(";

                for (int i = 0; i < valueKeys.Length; i++)
                {
                    if (Values[valueKeys[i]].GetType() == typeof(string))
                    {
                        m_Query += $@"'{Values[valueKeys[i]]}'";
                    }
                    else
                    {
                        m_Query += Values[valueKeys[i]];
                    }

                    if (i + 1 < valueKeys.Length)
                    {
                        m_Query += @",";
                    }
                }

                m_Query += @")";
                return m_Query;
            }
            /// <summary>
            /// Read 쿼리문을 작성합니다.<br/>
            /// vs는 읽어올 컬럼 이름들의 리스트입니다.
            /// </summary>
            /// <param name="vs"></param>
            /// <returns></returns>
            public string BuildReader(params string[] vs)
            {
                string sum = null;
                if (vs == null || vs.Length == 0)
                {
                    sum = $@"SELECT * FROM {m_Table.m_Name}";
                }
                else
                {
                    sum = @"SELECT ";
                    for (int i = 0; i < vs.Length; i++)
                    {
                        if (m_Table.HasColumn(vs[i]))
                        {
                            sum += vs[i];
                            if (i + 1 < vs.Length)
                            {
                                sum += @",";
                            }
                        }
                        else
                        {
                            $"SQLite Exception :: 해당 이름({vs[i]})을 가진 컬럼이 없어 Select 문이 패스됨".ToLog();
                        }
                    }

                    sum += $@" FROM {m_Table.m_Name}";
                }

                return sum;
            }
            /// <summary>
            /// Read 쿼리문을 Where 문을 포함해 작성합니다.<br/><br/>
            /// where에 컬럼 이름을 넣고, value에 찾고자하는 값을 넣으면,<br/>
            /// vs에서 지정한 컬럼을 바탕으로 읽어온 데이터 리스트안에서 sort되어 반환하는 쿼리문
            /// </summary>
            /// <param name="where">컬럼 이름</param>
            /// <param name="value">찾고자하는 컬럼내의 목표 값</param>
            /// <param name="vs"></param>
            /// <returns></returns>
            public string BuildReader(string where, object value, params string[] vs)
            {
                string sum = BuildReader(vs);

                if (m_Table.HasColumn(where))
                {
                    sum += $@" WHERE {where}={value}";
                }
                else
                {
                    $"SQLite Exception :: 해당 이름({where})을 가진 컬럼이 없어 Where 문이 패스됨".ToLog();
                }

                return sum;
            }
        }

        #endregion

        private static string ConvertType(Type t)
        {
            if (t == typeof(int) || t == typeof(float) ||
                t == typeof(long) || t == typeof(ulong) || t == typeof(double))
            {
                return "INTEGER";
            }
            else
            {
                return "TEXT";
            }
        }
        private static string ConvertLogic(IFLogic logic)
        {
            switch (logic)
            {
                case IFLogic.EXISTS:
                    return "IF EXISTS";
                case IFLogic.NOT_EXISTS:
                    return "IF NOT EXISTS";
                default:
                    return null;
            }
        }

        #region Quick Builder

        /// <summary>
        /// SQLite 커넥션용 URI 문 작성 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string BuildPath(string path, string name)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return $"URI=file:{path}{Path.DirectorySeparatorChar}{name}.db";
        }

        /// <summary>
        /// 테이블 퀵빌더
        /// </summary>
        /// <param name="name"></param>
        /// <param name="vs"></param>
        /// <returns></returns>
        public static TableBuilder BuildTable(string name, params (Type t, string name)[] vs)
        {
            TableBuilder builder = new TableBuilder(name);
            for (int i = 0; i < (vs != null ? vs.Length : 0); i++)
            {
                builder.AddValue(vs[i].t, vs[i].name);
            }
            return builder;
        }
        /// <summary>
        /// 데이터 컬럼 퀵빌더
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="vs"></param>
        /// <returns></returns>
        public static ValueBuilder BuildValue<T>(TableBuilder table, params (string name, T value)[] vs) where T : struct, IConvertible
        {
            ValueBuilder builder = new ValueBuilder(table);
            for (int i = 0; i < (vs != null ? vs.Length : 0); i++)
            {
                builder.AddValue(vs[i].name, vs[i].value);
            }
            return builder;
        }
        /// <summary>
        /// 데이터 컬럼 퀵빌더
        /// </summary>
        /// <param name="table"></param>
        /// <param name="vs"></param>
        /// <returns></returns>
        public static ValueBuilder BuildValue(TableBuilder table, params (string name, string value)[] vs)
        {
            ValueBuilder builder = new ValueBuilder(table);
            for (int i = 0; i < (vs != null ? vs.Length : 0); i++)
            {
                builder.AddValue(vs[i].name, vs[i].value);
            }
            return builder;
        }

        #endregion

        //public void Example()
        //{
        //    string path = BuildPath("", "");
        //    SQLiteDataReader rdr;
        //    IFLogic iFLogic;

        //    // Option 1
        //    // 존재할때만 쿼리문 실행하기
        //    iFLogic = IFLogic.EXISTS;
        //    // Option 2
        //    // 존재하지않을때만 쿼리문 실행하기
        //    iFLogic = IFLogic.NOT_EXISTS;

        //    using (SQLiteConnection connection = new SQLiteConnection(path))
        //    {
        //        connection.Open();

        //        using (SQLiteCommand cmd = connection.CreateCommand())
        //        {
        //            // ---------- 테이블
        //            // Option 1
        //            // 퀵빌더로 그냥 빌드반환해서 쓰기
        //            TableBuilder table = BuildTable("testTable", (typeof(int), "index"), (typeof(string), "testVal"));
        //            //Option 2
        //            // 직접 만들어서 컬럼 추가해서 쓰기
        //            table = new TableBuilder("testTable")
        //                .AddValue(typeof(int), "index")
        //                .AddValue(typeof(string), "beforeVal");

        //            // 테이블 생성하기
        //            cmd.CommandText = table.BuildCreateTable(iFLogic);
        //            cmd.ExecuteNonQuery();

        //            // 테이블 이름 바꾸기
        //            cmd.CommandText = table.BuildRenameTable("changedTable");
        //            cmd.ExecuteNonQuery();

        //            // 컬럼 이름 바꾸기
        //            cmd.CommandText = table.BuildRenameColumn("beforeVal", "testVal");
        //            cmd.ExecuteNonQuery();

        //            // 컬럼 추가하기
        //            cmd.CommandText = table.BuildAddColumn(typeof(int), "newCol", IFLogic.NOT_EXISTS);
        //            cmd.ExecuteNonQuery();

        //            // 컬럼 제거하기
        //            var iter = table.BuildRemoveColumn("newCol");
        //            do
        //            {
        //                if (string.IsNullOrEmpty(iter.Current)) continue;

        //                cmd.CommandText = iter.Current;
        //                cmd.ExecuteNonQuery();

        //            } while (iter.MoveNext());

        //            // ---------- 컬럼
        //            // Option 1
        //            // 퀵 빌더 쓰기
        //            // 단점 => 컬럼의 값중 스트링이 단 하나라도 포함되면 입력값이 전부 스트링이어야함.
        //            ValueBuilder value = BuildValue(table, ("index", 0), ("testVal", 123));
        //            // Option 2
        //            // 테이블에서 생성반환한 컬럼 빌더 쓰기
        //            value = table.GetValueBuilder();
        //            // Option 3
        //            // 직접 생성하여 테이블 연결해 쓰기
        //            value = new ValueBuilder(table);

        //            // 쿼리를 쓰고자 하는 컬럼과 값 지정하기
        //            value.AddValue("index", 0)
        //                .AddValue("testVal", "this is a test");

        //            // 데이터 Insert 옵션을 선택가능
        //            // Option 1
        //            // 무조건 넣기
        //            ValueBuilder.InsertFlag insertFlag = ValueBuilder.InsertFlag.INSERT;
        //            // Option 2
        //            // 조합하기
        //            insertFlag = ValueBuilder.InsertFlag.INSERT | ValueBuilder.InsertFlag.REPLACE;
        //            insertFlag = ValueBuilder.InsertFlag.Insert_Replace;
        //            // 혹은
        //            insertFlag = ValueBuilder.InsertFlag.INSERT | ValueBuilder.InsertFlag.UPDATE;
        //            insertFlag = ValueBuilder.InsertFlag.Insert_Update;
        //            // 적용할 수 없는 옵션
        //            // 이 경우에는 둘중 하나만 선택되어 적용됨
        //            insertFlag = ValueBuilder.InsertFlag.REPLACE | ValueBuilder.InsertFlag.UPDATE;

        //            cmd.CommandText = value.BuildInsert(insertFlag);
        //            cmd.ExecuteNonQuery();

        //            #region 데이터 읽기 예시

        //            // 데이터를 전부 불러와서 읽기
        //            cmd.CommandText = value.BuildReader();
        //            rdr = cmd.ExecuteReader();

        //            $"{rdr.GetName(0),-3} {rdr.GetName(1),8}".ToLog();
        //            while (rdr.Read())
        //            {
        //                $"{rdr.GetInt32(0) - 3} {rdr.GetString(1),8}".ToLog();
        //            }
        //            // --------------------------------------------------------------

        //            // 데이터를 전부 불러와 sort 하여 일치하는 값만 읽기
        //            cmd.CommandText = value.BuildReader("index", 5);
        //            rdr = cmd.ExecuteReader();

        //            $"{rdr.GetName(0), -3} {rdr.GetName(1), 8}".ToLog();
        //            // => {index} {testVal}
        //            while (rdr.Read())
        //            {
        //                $"{rdr.GetInt32(0) - 3} {rdr.GetString(1), 8}".ToLog();
        //            }
        //            // --------------------------------------------------------------

        //            // 데이터 테이블에서 필요한 컬럼만 불러와 읽기
        //            string[] vs = new string[]
        //            {
        //                "index"
        //            };
        //            cmd.CommandText = value.BuildReader(vs);
        //            rdr = cmd.ExecuteReader();

        //            $"{rdr.GetName(0),-3}".ToLog();
        //            // => {index}
        //            while (rdr.Read())
        //            {
        //                $"{rdr.GetInt32(0) - 3}".ToLog();
        //            }
        //            // --------------------------------------------------------------

        //            #endregion
        //        }
        //    }
        //}
    }
}
