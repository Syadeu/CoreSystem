using System;
using System.Collections.Generic;

namespace Syadeu.Collections
{
    /// <summary>
    /// <para><see cref="SQLiteTable"/>의 열 정보입니다</para>
    /// <c>Values</c>의 타입은 db에 저장된 실제 데이터 형식이며,
    /// <see cref="int"/>, 혹은 <see cref="float"/>와 같은 
    /// <see cref="IConvertible"/>타입들을 제외한 나머지 타입,
    /// 예를 들어 <see cref="UnityEngine.Vector3"/>, <see cref="List{T}"/>와 같은 타입들은 
    /// <see cref="string"/> 형태로 담겨있습니다. 이 컬럼의 데이터 형식은 <c>Type</c>에서 받아올 수 있습니다.<br/>
    /// <br/>
    /// 담긴 데이터를 원하는 타입으로 가공하려면 
    /// <seealso cref="SQLiteDatabaseUtils.ConvertSQL{T}(in object)"/>으로 데이터를 가공하거나,
    /// <seealso cref="SQLiteTableAttribute"/>가 선언된 구조체를 이용하여 
    /// <seealso cref="SQLiteDatabase.TryGetTableValueWithPrimary{T}(string, object, out T)"/>, 혹은
    /// <seealso cref="SQLiteTable.TryReadLineWithPrimary{T}(object, out T)"/> 을 사용하세요.
    /// </summary>
    public struct SQLiteColumn : IValidation
    {
        /// <summary>
        /// 실제 db에 담긴 데이터 형식
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// 이름
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 값들
        /// </summary>
        public List<object> Values { get; }

        public SQLiteColumn(Type t, string name)
        {
            Type = t;
            Name = name;

            Values = new List<object>();
        }

        public T GetValue<T>(int i) => (T)Values[i];

        public bool IsValid()
        {
            if (Type == null ||
                Values == null) return false;

            return true;
        }
    }
}