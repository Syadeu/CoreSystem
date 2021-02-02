using System.Collections.Generic;

namespace Syadeu.Database
{
    public interface ISQLiteReadOnlyTable
    {
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
    }
}