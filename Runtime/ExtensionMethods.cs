﻿using Syadeu.Database;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Syadeu.Extensions.Logs
{
    public static class EditorUtilExtensions
    {

    }
}

namespace Syadeu
{
    public static class ExtensionMethods
    {
        public static void ToLogError(this string log, bool overrideLog = false)
        {
#if UNITY_EDITOR
            Debug.LogError(log);
            //#elif DEVELOPMENT_BUILD
            //            ConsoleWindow.Log(log, ConsoleFlag.Error);
#endif
        }
        public static void ToLog(this string log, bool overrideLog = false)
        {
#if UNITY_EDITOR
            Debug.Log(log);
            //#elif DEVELOPMENT_BUILD
            //            ConsoleWindow.Log(log);
#endif
        }

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
        public static byte[] CompressToBytes(this Texture2D source, bool highQuality = false)
        {
            source.Compress(highQuality);

            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            return readableText.EncodeToPNG();
        }

        public static byte[] ToBytesWithStream<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        public static byte[] ToBytes<T>(this T str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
            }
            catch (OutOfMemoryException)
            {
                Debug.LogWarning(
                    $"CoreSystem Warnning: {typeof(T).Name}을 Marshaling (To Bytes) 하는 도중 메모리가 부족하여, " +
                    $"MemoryStream 으로 전환하여 작업시도함");
                return str.ToBytesWithStream();
            }
            
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static T ToObjectWithStream<T>(this byte[] arr)
        {
            using (var memStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                memStream.Write(arr, 0, arr.Length);
                memStream.Seek(0, SeekOrigin.Begin);

                return (T)formatter.Deserialize(memStream);
            }
        }
        public static T ToObject<T>(this byte[] arr)
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
            }
            catch (OutOfMemoryException)
            {
                Debug.LogWarning(
                    $"CoreSystem Warnning: {typeof(T).Name}을 Marshaling (To Object) 하는 도중 메모리가 부족하여, " +
                    $"MemoryStream 으로 전환하여 작업시도함");
                return arr.ToObjectWithStream<T>();
            }

            Marshal.Copy(arr, 0, ptr, size);
            T str = Marshal.PtrToStructure<T>(ptr);
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