using System.Collections.Generic;

namespace Syadeu.Database
{
    public interface ISQLiteReadOnlyTable
    {
        /// <summary>
        /// 열 정보를 순번(<paramref name="index"/>)으로 빠르게 불러옵니다.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        IReadOnlyList<KeyValuePair<string, object>> this[int index] { get; }
        /// <summary>
        /// 행 정보(<see cref="SQLiteColumn"/>)를 이름(<paramref name="name"/>)으로 빠르게 불러옵니다.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        SQLiteColumn this[string name] { get; }

        /// <summary>
        /// 이 테이블의 이름
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 이 테이블의 행
        /// </summary>
        IReadOnlyList<SQLiteColumn> Columns { get; }
        IReadOnlyDictionary<object, int> PrimaryKeyPairs { get; }
        /// <summary>
        /// 열 갯수
        /// </summary>
        int Count { get; }

        bool IsValid();

        /// <summary>
        /// 같은 컬럼을 가진 두 테이블 데이터를 메인 키(<paramref name="primaryKey"/>)값으로 비교합니다<br/>
        /// 단 하나라도 행의 값이 다르거나, 비교 대상(<paramref name="other"/>)이 
        /// 해당 키(<paramref name="primaryKey"/>)를 갖고있지 않으면 False를 반환합니다.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="primaryKey"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        bool CompairLine<TKey>(TKey primaryKey, in ISQLiteReadOnlyTable other);

        /// <summary>
        /// 해당 메인키(<paramref name="key"/>)가 존재하는지 반환합니다
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        bool HasKey<TKey>(TKey primaryKey);
        bool HasColumnValue(string column, object value);
        /// <summary>
        /// 이 테이블에서 해당 이름(<paramref name="name"/>)을 가진 행이 있는지 반환합니다<br/><br/>
        /// 비슷한 메소드<br/>
        /// <seealso cref="TryGetColumn(string, out SQLiteColumn)"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool HasColumn(string name);

        T ReadLine<T>(in int i) where T : struct;

        /// <summary>
        /// 이 테이블에서 해당 이름(<paramref name="name"/>)을 가진 행을 반환합니다<br/>
        /// 행 체크만 하는거면 <see cref="HasColumn(string)"/>을 사용하세요
        /// </summary>
        /// <param name="name"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        bool TryGetColumn(string name, out SQLiteColumn column);
        /// <summary>
        /// 순서대로 열을 불러옵니다<br/>
        /// <paramref name="line"/>의 키값은 컬럼의 이름이고, 밸류값은 <paramref name="index"/>로 불러온 데이터를 담습니다.<br/><br/>
        /// 비슷한 메소드<br/>
        /// <seealso cref="TryReadLine{T}(in int, out T)"/>: 시스템 비용이 크지만, 데이터 가공이 용이합니다<br/>
        /// <seealso cref="TryReadLineWithPrimary{T}(object, out T)"/>: 시스템 비용이 크지만, 메인 키값으로 불러오며 데이터 가공이 용이합니다
        /// </summary>
        /// <param name="index"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        bool TryReadLine(in int index, out IReadOnlyList<KeyValuePair<string, object>> line);
        /// <summary>
        /// 순서대로 열을 불러옵니다<br/>
        /// SQLiteTable Attribute가 선언된 커스텀 구조체에 담아서 반환합니다.<br/>
        /// Reflection 사용으로 시스템 비용이 큽니다.<br/>
        /// for문으로 사용할거면 <see cref="TryReadLine(int, out IReadOnlyList{KeyValuePair{string, object}}))"/> 을 사용하세요<br/><br/>
        /// 비슷한 메소드<br/>
        /// <seealso cref="TryReadLineWithPrimary{T}(object, out T)"/>: 동작방식 같음, 다만 메인 키값으로 접근<br/>
        /// <seealso cref="TryReadLine(int, out IReadOnlyList{KeyValuePair{string, object}})"/>: Reflection 미사용으로 접근이 매우 빠름
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        bool TryReadLine<T>(in int index, out T table) where T : struct;
        /// <summary>
        /// 해당 키의 열을 읽어옵니다<br/>
        /// SQLiteTable Attribute가 선언된 커스텀 구조체에 담아서 반환합니다.<br/>
        /// Reflection 사용으로 시스템 비용이 큽니다.<br/><br/>
        /// 비슷한 메소드<br/>
        /// <seealso cref="TryReadLine{T}(in int, out T)"/>: 동작방식 같음, 다만 순서대로 접근<br/>
        /// <seealso cref="TryReadLine(in int, out IReadOnlyList{KeyValuePair{string, object}})"/>: Reflection 미사용으로 접근이 매우 빠름
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKey"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        bool TryReadLineWithPrimary<T>(object primaryKey, out T table) where T : struct;
    }
}