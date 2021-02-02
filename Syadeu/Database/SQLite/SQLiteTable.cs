using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Syadeu.Database
{
    /// <summary>
    /// <para><see cref="SQLiteDatabase"/>에서 받아온 데이터 테이블 정보를 담습니다</para>
    /// 정보는 열(<seealso cref="SQLiteColumn"/>)을 기준으로, 정렬되어 담겨있습니다.
    /// 모든 <see cref="SQLiteTable"/>의 0번째 열(<c>Columns[0]</c>)은 메인 키값입니다.</summary>
    /// 
    /// <remarks>
    /// <see cref="SQLiteTable"/>[<see cref="int"/>]로 빠르게 테이블의 열을 탐색할 수 있습니다.<br/>
    /// foreach 문으로 빠르게 테이블의 열을 탐색할 수 있습니다.
    /// </remarks>
    public struct SQLiteTable : IEnumerable<IReadOnlyList<KeyValuePair<string, object>>>, ISQLiteReadOnlyTable
    {
        public IReadOnlyList<KeyValuePair<string, object>> this[int index]
        {
            get
            {
                SQLiteDatabase.Assert(index >= Count, $"인덱스가 {Name} 테이블의 열 갯수({Count})를 초과합니다.");

                List<KeyValuePair<string, object>> temp = new List<KeyValuePair<string, object>>();
                for (int i = 0; i < Columns.Count; i++)
                {
                    KeyValuePair<string, object> pair =
                        new KeyValuePair<string, object>(Columns[i].Name, Columns[i].Values[index]);
                    temp.Add(pair);
                }
                return temp;
            }
        }
        public SQLiteColumn this[string name]
        {
            get
            {
                for (int i = 0; i < Columns.Count; i++)
                {
                    if (Columns[i].Name == name)
                    {
                        return Columns[i];
                    }
                }

                SQLiteDatabase.Assert(true, $"{Name} 테이블에서 {name}의 컬럼은 존재하지 않습니다");
                throw new ArgumentOutOfRangeException($"{Name} 테이블에서 {name}의 컬럼은 존재하지 않습니다");
            }
        }

        public string Name { get; }
        public List<SQLiteColumn> Columns { get; }
        public Dictionary<object, int> PrimaryKeyPairs { get; }
        public int Count
        {
            get
            {
                if (Columns == null || Columns.Count == 0) return 0;
                return Columns[0].Values.Count;
            }
        }

        IReadOnlyList<SQLiteColumn> ISQLiteReadOnlyTable.Columns => Columns;
        IReadOnlyDictionary<object, int> ISQLiteReadOnlyTable.PrimaryKeyPairs => PrimaryKeyPairs;

        internal SQLiteTable(string name, in IList<SQLiteColumn> columns)
        {
            Name = name;
            Columns = new List<SQLiteColumn>(columns);

            PrimaryKeyPairs = new Dictionary<object, int>();
            for (int i = 0; i < Count; i++)
            {
                SQLiteDatabase.Assert(PrimaryKeyPairs.ContainsKey(Columns[0].Values[i]),
                    $"{Name}테이블의 PK({Columns[0].Name})에 같은 값이 추가됨: {Columns[0].Values[i]}");

                //if (Columns[0].Type == typeof(int))
                //{

                //}
                PrimaryKeyPairs.Add(Columns[0].Values[i], i);
            }
        }

        /// <summary>
        /// 실제 데이터에는 적용안되고 복사본을 반환합니다<br/>
        /// 실데이터 적용을 원하면 <see cref="SQLiteDatabase.RenameTable(string, string)"/>을 사용하세요
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        public SQLiteTable Rename(string newName)
        {
            List<SQLiteColumn> columns = new List<SQLiteColumn>();
            for (int i = 0; i < Columns.Count; i++)
            {
                columns.Add(Columns[i]);
            }

            return new SQLiteTable(newName, columns);
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(Name) || Columns == null) return false;
            return true;
        }
        /// <summary>
        /// 이 테이블의 컬럼 정보를 받아옵니다
        /// </summary>
        /// <returns></returns>
        public IList<KeyValuePair<Type, string>> GetColumnsInfo()
        {
            KeyValuePair<Type, string>[] columns = new KeyValuePair<Type, string>[Columns.Count];
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i] = new KeyValuePair<Type, string>(Columns[i].Type, Columns[i].Name);
            }
            return columns;
        }

        public bool HasKey<TKey>(TKey primaryKey)
        {
            SQLiteDatabase.Assert(IsValid, false, "정상적으로 로드된 테이블 데이터가 아닙니다");
            object compairKey = SQLiteDatabaseUtils.ConvertSQL(Columns[0].Type, primaryKey);
            return PrimaryKeyPairs.ContainsKey(compairKey);
        }
        public bool HasColumnValue(string column, object value)
        {
            if (!TryGetColumn(column, out SQLiteColumn col))
            {
                SQLiteDatabase.Assert(true, $"{Name}에서 해당 컬럼({column})이 존재하지않는데 찾으려함");
            }

            for (int i = 0; i < col.Values.Count; i++)
            {
                if (col.Values[i].Equals(value)) return true;
            }
            return false;
        }
        
        public bool CompairLine<TKey>(TKey primaryKey, in ISQLiteReadOnlyTable other)
        {
            SQLiteDatabase.Assert(IsValid, false, "정상적으로 로드된 테이블 데이터가 아닙니다");

            SQLiteDatabase.Assert(other.Columns[0].Type != Columns[0].Type, $"비교하려는 대상의 메인 키 값의 타입({Columns[0].Type}, 대상: {other.Columns[0].Type})이 다릅니다");
            SQLiteDatabase.Assert(!PrimaryKeyPairs.ContainsKey(primaryKey), $"{Name} 테이블의 메인키({primaryKey})를 읽어올 수 없습니다");

            object compairKey;
            try
            {
                compairKey = SQLiteDatabaseUtils.ConvertSQL(Columns[0].Type, primaryKey);
            }
            catch (Exception)
            {
                SQLiteDatabase.Assert(typeof(TKey) != Columns[0].Type, $"키 값의 타입이 다릅니다 {typeof(TKey).Name} : {Columns[0].Type}");
                throw;
            }

            int thisIndex = PrimaryKeyPairs[compairKey];

            // 비교 대상에 동일한 키값이 없을때
            if (!other.PrimaryKeyPairs.TryGetValue(compairKey, out int otherIndex))
            {
                return false;
            }

            for (int i = 0; i < Columns.Count; i++)
            {
                SQLiteDatabase.Assert(!Columns[i].Name.Equals(other.Columns[i].Name), "비교하려는 대상이 동일한 테이블이 아닙니다");

                if (!Columns[i].Values[thisIndex].Equals(other.Columns[i].Values[otherIndex]))
                {
                    return false;
                }
            }
            return true;
        }

        public bool HasColumn(string name)
        {
            if (!IsValid()) return false;

            for (int i = 0; i < Columns.Count; i++)
            {
                if (Columns[i].Name == name)
                {
                    return true;
                }
            }
            return false;
        }
        public bool TryGetColumn(string name, out SQLiteColumn column)
        {
            column = default;
            if (!IsValid()) return false;

            for (int i = 0; i < Columns.Count; i++)
            {
                if (Columns[i].Name == name)
                {
                    column = Columns[i];
                    return true;
                }
            }
            return false;
        }

        public bool TryReadLine(int index, out IReadOnlyList<KeyValuePair<string, object>> line)
        {
            if (!IsValid() || Columns.Count == 0 || index >= Columns[0].Values.Count)
            {
                //$"SQLite Exception: {Name} 테이블의 인덱스({index})를 읽어올 수 없습니다\n이 요청은 무시됩니다".ToLog();
                line = null;
                return false;
            }

            line = this[index];
            return true;
        }
        public bool TryReadLineWithPrimary<T>(object primaryKey, out T table) where T : struct
        {
            table = default;
            if (!IsValid() || Count == 0)
            {
                //$"SQLite Exception: {Name} 테이블의 메인키({primaryKey})를 읽어올 수 없습니다\n이 요청은 무시됩니다".ToLog();
                return false;
            }
            var t = primaryKey.GetType();
            object searchValue;
            try
            {
                searchValue = SQLiteDatabaseUtils.ConvertSQL(Columns[0].Type, primaryKey);
            }
            catch (Exception)
            {
                SQLiteDatabase.Assert(t != Columns[0].Type,
                    $"SQLite Exception: 입력된 키({primaryKey})는 {Name} 테이블의 지정된 타입({Columns[0].Type})으로 변환불가");
                throw;
            }

            if (PrimaryKeyPairs.TryGetValue(searchValue, out int index) &&
                TryReadLine(index, out T foundedTable))
            {
                table = foundedTable;
                return true;
            }
            else
            {
                //$"SQLite Exception: {Name} 테이블의 메인키({primaryKey})를 찾을 수 없습니다\n이 요청은 무시됩니다".ToLog();
                return false;
            }
        }
        public bool TryReadLine<T>(int index, out T table) where T : struct
        {
            table = default;
            if (!IsValid() || Columns.Count == 0 || index >= Columns[0].Values.Count)
            {
                //$"SQLite Exception: {Name} 테이블의 인덱스({index})를 읽어올 수 없습니다\n이 요청은 무시됩니다".ToLog();
                return false;
            }

            object boxed = table;

            for (int i = 0; i < Columns.Count; i++)
            {
                MemberInfo field = SQLiteDatabaseUtils.GetMemberInfo<T>(Columns[i].Name, out var att);
                if (field == null || index >= Columns[i].Values.Count) continue;

                object value = null;
                switch (field.MemberType)
                {
                    case MemberTypes.Field:
                        value = SQLiteDatabaseUtils.ConvertSQL((field as FieldInfo).FieldType,
                    Columns[i].Values[index]);
                        //if (value == null || string.IsNullOrEmpty(value.ToString()))
                        if (value == null || string.IsNullOrEmpty(value.ToString()))
                        {
                            //value = field.GetCustomAttribute<SQLiteDatabaseAttribute>().DefaultValue;
                            value = SQLiteDatabaseUtils.GetDataDefaultValue((field as FieldInfo).FieldType, att);
                        }

                        (field as FieldInfo).SetValue(boxed, value);
                        break;
                    case MemberTypes.Property:
                        value = SQLiteDatabaseUtils.ConvertSQL((field as PropertyInfo).PropertyType,
                    Columns[i].Values[index]);
                        //if (value == null || string.IsNullOrEmpty(value.ToString()))
                        if (value == null || string.IsNullOrEmpty(value.ToString()))
                        {
                            //value = field.GetCustomAttribute<SQLiteDatabaseAttribute>().DefaultValue;
                            value = SQLiteDatabaseUtils.GetDataDefaultValue((field as PropertyInfo).PropertyType, att);
                        }

                        (field as PropertyInfo).SetValue(boxed, value);
                        break;
                    default:
                        continue;
                }
            }

            table = (T)boxed;
            return true;
        }

        public IEnumerator<IReadOnlyList<KeyValuePair<string, object>>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}