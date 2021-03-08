using Syadeu.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Syadeu.Extentions.EditorUtils
{
    public static class EditorUtilExtensions
    {
        public static void ToLogError(this string log, bool overrideLog = false)
        {
#if UNITY_EDITOR
            Debug.LogError(log);
#endif
        }
        public static void ToLog(this string log, bool overrideLog = false)
        {
#if UNITY_EDITOR
            Debug.Log(log);
#endif
        }
    }
}

namespace Syadeu
{
    public static class ExtensionMethods
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
        public static void SaveTextureAsPNG(this Texture2D _texture, string _fullPath)
        {
            byte[] _bytes = _texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(_fullPath, _bytes);
            //Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
        }

        public static byte[] ToByteWithStream<T>(this T str) where T : struct
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();

            formatter.Serialize(stream, str);
            return stream.ToArray();
        }
        public static byte[] ToByte<T>(this T str) where T : struct
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static T FromByteWithStream<T>(this byte[] arr) where T : struct
        {
            var stream = new MemoryStream(arr);
            var formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }
        public static T FromByte<T>(this byte[] arr) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            T str = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);

            return str;
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
        public static SQLiteTable ToSQLiteTable<T>(this IList<T> tableDatas, in string tableName) where T : struct
        {
            SQLiteTableAttribute tableAtt = typeof(T).GetCustomAttribute<SQLiteTableAttribute>();
            SQLiteDatabase.Assert(tableAtt == null, $"객체 타입 ({typeof(T).Name})은 SQLite 데이터 객체가 아닙니다");

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

            SQLiteTable table = new SQLiteTable(
                tableName, 
                (columns != null ? columns.ToList() : new List<SQLiteColumn>()),
                tableAtt.SaveAsByte);
            return table;
        }
    }
}