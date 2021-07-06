using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Unsafe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Syadeu.Unsafe
{
    public static class ExtensionMethods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static IntPtr AddressOf<T>(T t) where T : unmanaged
        {
            TypedReference reference = __makeref(t);
            return *(IntPtr*)(&reference);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static IntPtr AddressOfRef<T>(ref T t) where T : unmanaged
        {
            TypedReference reference = __makeref(t);
            TypedReference* pRef = &reference;
            return (IntPtr)pRef; //(&pRef)
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static T ReadGenericFromPtr<T>(IntPtr source, int sizeOfT) where T : unmanaged
        {
            byte* bytePtr = (byte*)source;

            T result = default(T);
            TypedReference resultRef = __makeref(result);
            byte* resultPtr = (byte*)*((IntPtr*)&resultRef);

            for (int i = 0; i < sizeOfT; ++i)
            {
                resultPtr[i] = bytePtr[i];
            }

            return result;
        }

        public static byte[] ToBytes<T>(ref T str) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            byte[] arr = new byte[size];

            IntPtr ptr = AddressOfRef(ref str);

            Marshal.Copy(ptr, arr, 0, size);
            return arr;
        }
        unsafe public static T ToObject<T>(byte[] arr) where T : unmanaged
        {
            fixed (byte* ptr = &arr[0])
            {
                return (T)Marshal.PtrToStructure((IntPtr)ptr, TypeHelper.TypeOf<T>.Type);
            }
        }
    }
}

namespace Syadeu
{
    public static class ExtensionMethods
    {
        private const string c_Indent = "    ";

        public static void ToLogError(this string log, UnityEngine.Object target = null)
        {
#if UNITY_EDITOR
            if (target == null) Debug.LogError(log);
            else Debug.LogError(log, target);
#endif
        }
        public static void ToLog(this string log, UnityEngine.Object target = null)
        {
#if UNITY_EDITOR
            if (target == null) Debug.Log(log);
            else Debug.Log(log, target);
#endif
        }
        public static void ToLogConsole(this string log, ResultFlag flag = ResultFlag.Normal)
        {
            ConsoleWindow.Log(log, flag);
        }
        public static void ToLogConsole(this string log, int indent, ResultFlag flag = ResultFlag.Normal)
        {
            string txt = "";
            for (int i = 0; i < indent; i++)
            {
                txt += c_Indent;
            }
            txt += log;
            ConsoleWindow.Log(txt, flag);
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

        public static T FindFor<T>(this IList<T> list, Func<T, bool> predictate) where T : class
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (predictate.Invoke(list[i])) return list[i];
            }

            return null;
        }
        public static T RemoveFor<T, TA>(this T list, TA value) where T : IList<TA>/* where TA : IEquatable<TA>*/
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(value))
                {
                    list.RemoveAt(i);
                    break;
                }
            }
            return list;
        }

        private static readonly Dictionary<UnityEngine.Object, CoreRoutine> Lerps 
            = new Dictionary<UnityEngine.Object, CoreRoutine>();
        public static CoreRoutine Lerp(this CanvasGroup canvasGroup, float target, float time)
        {
            if (Lerps.ContainsKey(canvasGroup))
            {
                CoreSystem.RemoveUnityUpdate(Lerps[canvasGroup]);
                Lerps.Remove(canvasGroup);
            }

            CoreRoutine routine = CoreSystem.StartUnityUpdate(
                canvasGroup,
                FloatLerp(() => canvasGroup.alpha, (other) => canvasGroup.alpha = other, target, time)
                );

            Lerps.Add(canvasGroup, routine);
            return routine;
        }
        private static IEnumerator FloatLerp(Func<float> getter, Action<float> setter, float target, float time)
        {
            while (getter.Invoke() != target)
            {
                setter.Invoke(Mathf.Lerp(getter.Invoke(), target, time));

                if (getter.Invoke() < target)
                {
                    if (getter.Invoke() >= target - .01f)
                    {
                        setter.Invoke(target);
                        break;
                    }
                }
                else
                {
                    if (getter.Invoke() <= target + .01f)
                    {
                        setter.Invoke(target);
                        break;
                    }
                }

                yield return null;
            }
        }
    }
}