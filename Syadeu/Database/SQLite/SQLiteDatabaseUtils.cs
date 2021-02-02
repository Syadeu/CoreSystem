using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Reflection;

using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Database
{
    [Preserve]
    /// <summary>
    /// SQLiteDatabase 사용 편의성을 위한 유틸 클래스
    /// </summary>
    public static class SQLiteDatabaseUtils
    {
        private static char[] numbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        public static bool HasNumbers(string value)
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                if (value.Contains(numbers[i].ToString()))
                {
                    return true;
                }
            }
            return false;
        }
        public static string TrimNumbers(string value)
        {
            string trimed = value.Trim(numbers);
            return trimed;
        }
        public static MemberInfo GetMemberInfo<T>(string name, out SQLiteDatabaseAttribute att)
        {
            MemberInfo member = typeof(T).GetField(name);
            if (member == null)
            {
                member = typeof(T).GetProperty(name);
            }

            att = member?.GetCustomAttribute<SQLiteDatabaseAttribute>();

            if (att == null) return null;
            return member;
        }
        public static MemberInfo GetPrimaryKeyInfo<T>()
        {
            Type t = typeof(T);

            PropertyInfo[] properties = t.GetProperties();
            FieldInfo[] fields = t.GetFields();

            foreach (var item in properties)
            {
                var att = item.GetCustomAttribute<SQLiteDatabaseAttribute>();
                if (att != null && att.IsPrimaryKey)
                {
                    return item;
                }
            }
            foreach (var item in fields)
            {
                var att = item.GetCustomAttribute<SQLiteDatabaseAttribute>();
                if (att != null && att.IsPrimaryKey)
                {
                    return item;
                }
            }
            return null;
        }

        public static List<MemberInfo> GetMembers(Type t)
        {
            if (t == null) return null;

            PropertyInfo[] properties = t.GetProperties();
            FieldInfo[] fields = t.GetFields();

            List<MemberInfo> temp = new List<MemberInfo>();

            foreach (var item in properties)
            {
                if (item.GetCustomAttribute<SQLiteDatabaseAttribute>() != null)
                {
                    temp.Add(item);
                }
            }
            foreach (var item in fields)
            {
                if (item.GetCustomAttribute<SQLiteDatabaseAttribute>() != null)
                {
                    temp.Add(item);
                }
            }
            return temp;
        }
        private static List<MemberInfo> SortMembers(MemberInfo[] membersInfo)
        {
            List<MemberInfo> members = new List<MemberInfo>();
            for (int i = 0; i < membersInfo.Length; i++)
            {
                var memberAtt = membersInfo[i].GetCustomAttribute<SQLiteDatabaseAttribute>();

                if (memberAtt != null)
                {
                    if (memberAtt.IsPrimaryKey)
                    {
                        members.Insert(0, membersInfo[i]);
                    }
                    else
                    {
                        if (memberAtt.Order > 0)
                        {
                            members.Insert(memberAtt.Order, membersInfo[i]);
                        }
                        else members.Add(membersInfo[i]);
                    }
                }
            }
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] == null)
                {
                    members.RemoveAt(i);
                    i--;
                    continue;
                }
            }

            return members;
        }

        /// <summary>
        /// <see cref="Vector3"/>를 <see cref="Vector3.ToString"/>한 값을 
        /// <paramref name="vector3"/>에 넣으면 <see cref="SQLiteColumn"/> 데이터 형식의 스트링 형태로 반환합니다.
        /// </summary>
        /// <param name="vector3"></param>
        /// <returns></returns>
        public static string ParseVector3ToSQL(string vector3)
        {
            string trim = vector3.Trim();
            trim = trim.Replace("(", "");
            trim = trim.Replace(")", "");
            string[] split = trim.Split(',');

            double[] xyz = new double[3];
            for (int i = 0; i < split.Length; i++)
            {
                xyz[i] = double.Parse(split[i].Trim());
            }
            return $"{xyz[0]}/{xyz[1]}/{xyz[2]}";
        }
        public static Vector3 ParseVector3FromSQL(object obj)
        {
            string trim = Convert.ToString(obj).Trim();
            string[] split;

            if (trim.StartsWith("("))
            {
                trim = trim.Replace("(", "");
                trim = trim.Replace(")", "");
                split = trim.Split(',');
            }
            else
            {
                split = trim.Split('/');
            }

            return new Vector3(
                    float.Parse(split[0]),
                    float.Parse(split[1]),
                    float.Parse(split[2]));
        }

        public static IList ParseArrayFromSQL(Type t, object array, bool isArray)
        {
            if (array.GetType() == typeof(DBNull)) array = string.Empty;
            SQLiteDatabase.Assert(array.GetType() != typeof(string), $"어레이를 변환할라면 스트링이어야됨 현재: {array.GetType()}");

            string[] split = Convert.ToString(array).Trim().Split('&');

            IList list;
            if (isArray)
            {
                list = Array.CreateInstance(t, split.Length);

                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = ConvertSQL(t, split[i]);
                }
            }
            else
            {
                Type listType = typeof(List<>).MakeGenericType(t);
                list = (IList)Activator.CreateInstance(listType);

                for (int i = 0; i < split.Length; i++)
                {
                    list.Add(ConvertSQL(t, split[i]));
                }
            }

            return list;
        }
        public static List<T> ParseArrayFromSQL<T>(object array)
        {
            if (array.GetType() == typeof(DBNull)) array = string.Empty;
            SQLiteDatabase.Assert(array.GetType() != typeof(string), $"어레이를 변환할라면 스트링이어야됨 현재: {array.GetType()}");

            string[] split = Convert.ToString(array).Trim().Split('&');
            List<T> list = new List<T>();

            for (int i = 0; i < split.Length; i++)
            {
                list.Add(ConvertSQL<T>(split[i]));
            }
            return list;
        }
        public static string ParseArrayToSQL(IList array)
        {
            if (array == null || array.Count == 0) return string.Empty;

            string sum = null;
            Type t = array[0].GetType();
            for (int i = 0; i < array.Count; i++)
            {
                if (!string.IsNullOrEmpty(sum)) sum += "&";

                if (t == typeof(Vector3))
                {
                    sum += ParseVector3ToSQL(array[i].ToString());
                }
                else sum += array[i].ToString();
            }
            return sum;
        }

        /// <summary>
        /// 양방향 컨버터, 스트링에 ' ' 을 안붙이므로 이걸로 SQLite에 바로넣을시 주의
        /// </summary>
        /// <param name="t"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object ConvertSQL(Type t, object obj)
        {
            if (obj == null)
            {
                return GetDefault(t);
            }

            if (t != typeof(byte[]))
            {
                SQLiteDatabase.Assert(obj.GetType().IsArray || obj.GetType().GenericTypeArguments.Length > 0,
                "어레이는 지원안함 ParseArray시리즈를 사용");
            }

            string tx = Convert.ToString(obj);
            if (string.IsNullOrEmpty(tx)) return GetDefault(t);

            object rtrn;

            if (t == typeof(Vector3))
            {
                rtrn = ParseVector3FromSQL(tx);
            }
            #region Basic Types
            else if (t == typeof(int))
            {
                Type objType = obj.GetType();
                if (objType == typeof(bool)) rtrn = bool.Parse(tx) ? 1 : 0;
                else if (objType.IsEnum)
                {
                    rtrn = (int)obj;
                }
                else
                {
                    try
                    {
                        rtrn = int.Parse(tx);
                    }
                    catch (Exception)
                    {
                        throw new InvalidOperationException($"{tx} 은 int 로 변환될수없음");
                    }
                }
            }
            else if (t == typeof(float))
            {
                rtrn = float.Parse(tx);
            }
            else if (t == typeof(double))
            {
                rtrn = double.Parse(tx);
            }
            else if (t == typeof(long))
            {
                rtrn = long.Parse(tx);
            }
            else if (t == typeof(ulong))
            {
                rtrn = ulong.Parse(tx);
            }
            else if (t == typeof(bool))
            {
                rtrn = int.Parse(tx) == 1;
            }
            else if (t == typeof(decimal))
            {
                rtrn = decimal.Parse(tx);
            }
            #endregion
            else if (t == typeof(byte[]))
            {
                if (obj.GetType() != typeof(byte[]))
                {
                    throw new InvalidOperationException("byte[] 타입이 아님");
                }
                rtrn = obj.ToString();
            }
            else if (t == typeof(string))
            {
                Type objType = obj.GetType();
                if (objType == typeof(Vector3))
                {
                    rtrn = ParseVector3ToSQL(tx);
                }
                else rtrn = tx;
            }
            else if (t.IsEnum)
            {
                int index;
                try
                {
                    index = int.Parse(tx);
                }
                catch (Exception)
                {
                    throw new InvalidOperationException($"{tx} 는 enum으로 변환될수없음");
                }

                string[] names = System.Enum.GetNames(t);
                rtrn = System.Enum.Parse(t, names[index]);
            }
            else if (t.IsArray)
            {
                if (obj.GetType() == typeof(string))
                {
                    rtrn = ParseArrayFromSQL(t.GetElementType(), tx, true);
                }
                else
                {
                    SQLiteDatabase.Assert(true, $"리스트로 변환하려면 스트링형태여야됨 {t.Name} : {obj.GetType().Name}");
                    throw new InvalidOperationException();
                }
            }
            else if (t.GenericTypeArguments.Length > 0)
            {
                if (obj.GetType() == typeof(string))
                {
                    rtrn = ParseArrayFromSQL(t.GenericTypeArguments[0], tx, false);
                }
                else
                {
                    SQLiteDatabase.Assert(true, $"리스트로 변환하려면 스트링형태여야됨 {t.Name} : {obj.GetType().Name}");
                    throw new InvalidOperationException();
                }
            }
            else
            {
                SQLiteDatabase.Assert(true, $"컨버트 지정되지 않은 타입 {t.Name}");
                rtrn = null;
            }
            return rtrn;
        }
        public static T ConvertSQL<T>(object obj)
            => (T)ConvertSQL(typeof(T), obj);

        /// <summary>
        /// 기본 테이블 구조체를 생성하여 반환합니다.<br/>
        /// <typeparamref name="T"/> 는 <see cref="SQLiteTableAttribute"/>가 선언된 구조체여야됩니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetDefaultTable<T>() where T : struct
        {
            T table = default;
            object boxed = table;

            Type t = typeof(T);

            SQLiteTableAttribute tableAtt = t.GetCustomAttribute<SQLiteTableAttribute>();
            SQLiteDatabase.Assert(tableAtt == null, $"객체 타입 ({typeof(T).Name})은 SQLite 데이터 객체가 아닙니다");

            var tempMembers = t.GetMembers();
            List<MemberInfo> dataMembers = SortMembers(tempMembers);

            for (int i = 0; i < dataMembers.Count; i++)
            {
                if (dataMembers[i] == null) continue;

                var att = dataMembers[i].GetCustomAttribute<SQLiteDatabaseAttribute>();
                object value = null;
                if (att.DefaultValue != null)
                {
                    switch (dataMembers[i].MemberType)
                    {
                        case MemberTypes.Field:
                            FieldInfo field = dataMembers[i] as FieldInfo;

                            value = GetDataDefaultValue(field.FieldType, att);

                            field.SetValue(boxed, value);

                            break;
                        case MemberTypes.Property:
                            PropertyInfo property = dataMembers[i] as PropertyInfo;

                            value = GetDataDefaultValue(property.PropertyType, att);

                            property.SetValue(boxed, value);

                            break;
                    }
                }
            }
            table = (T)boxed;
            return table;
        }
        public static object GetDataDefaultValue(Type t, SQLiteDatabaseAttribute att)
        {
            object value;

            if (att.DefaultValue == null)
            {
                if (t.IsArray)
                {
                    value = Array.CreateInstance(t.GetElementType(), 0);
                }
                else if (t.GenericTypeArguments.Length > 0)
                {
                    var arr = Array.CreateInstance(t.GenericTypeArguments[0], 0);
                    value = Activator.CreateInstance(t, arr);
                }
                else if (t.IsEnum)
                {
                    value = 0;
                }
                else if (t == typeof(string))
                {
                    value = string.Empty;
                }
                else
                {
                    //Func<object> f = InternalGetDefault<object>;
                    //value = f.Method.GetGenericMethodDefinition().MakeGenericMethod(t).Invoke(null, null);
                    value = GetDefault(t);
                }

            }
            else
            {
                Type defaultType = att.DefaultValue.GetType();

                if (t.IsArray && defaultType == typeof(int))
                {
                    value = Array.CreateInstance(t.GetElementType(), (int)att.DefaultValue);
                }
                else if (t.GenericTypeArguments.Length > 0 && defaultType == typeof(int))
                {
                    var arr = Array.CreateInstance(t.GenericTypeArguments[0], (int)att.DefaultValue);
                    value = Activator.CreateInstance(t, arr);
                }
                else if (t.IsEnum && defaultType == typeof(int))
                {
                    int index = int.Parse(att.DefaultValue.ToString());
                    string[] names = System.Enum.GetNames(t);
                    value = System.Enum.Parse(t, names[index]);
                }
                else value = ConvertSQL(t, att.DefaultValue);
            }

            return value;
        }

        [Preserve]
        private static T InternalGetDefault<T>()
        {
            return default(T);
        }
        [Preserve]
        public static object GetDefault(Type t)
        {
            if (t == typeof(string)) return string.Empty;

            Func<object> f = InternalGetDefault<object>;
            return f.Method.GetGenericMethodDefinition().MakeGenericMethod(t).Invoke(null, null);
        }

        public static void UsedOnlyForAOTCodeGeneration()
        {
            ConvertSQL<int>(null);
            ConvertSQL<float>(null);
            ConvertSQL<double>(null);
            ConvertSQL<bool>(null);
            ConvertSQL<long>(null);
            ConvertSQL<ulong>(null);
            ConvertSQL<object>(null);
            ConvertSQL<Vector3>(null);

            ParseArrayFromSQL<int>(null);
            ParseArrayFromSQL<float>(null);
            ParseArrayFromSQL<double>(null);
            ParseArrayFromSQL<bool>(null);
            ParseArrayFromSQL<long>(null);
            ParseArrayFromSQL<ulong>(null);
            ParseArrayFromSQL<Vector3>(null);
            ParseArrayFromSQL<object>(null);

            InternalGetDefault<int>();
            InternalGetDefault<float>();
            InternalGetDefault<double>();
            InternalGetDefault<bool>();
            InternalGetDefault<long>();
            InternalGetDefault<ulong>();
            InternalGetDefault<Vector3>();
            InternalGetDefault<object>();

            throw new InvalidOperationException();
        }
    }
}